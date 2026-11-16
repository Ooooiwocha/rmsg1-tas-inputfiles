-- レイディバグ検証用のまとめスクリプト（BizHawk 2.11 / BSNESv115+ 用）
-- keep-bright.lua（暗転中も画面を見えるようにする）と
-- freeze-detect.lua（フリーズと長い待ちを検出する）を1本にしたもの。
-- さらに、いま読んでいるメモが何枚目か（スタックの段数）を画面に表示する。
--   1行目：MEMO 何枚目 と、その読み取り位置（バンク:番地）
--   2行目：12枚目のメモの中身（時計と重なっていて毎フレーム変わる）
--   3行目：IDLE 最後にスクリプトの命令を読んでからのフレーム数（開始直後・ステート読み込み直後は ?）
--   12枚目以上を読んでいる間は赤、それ以外は白で表示する。
-- さらに、メモの動きをファイルに記録する（USE_LOG）。
--   毎フレームは書かず、次のときだけ1行書く：
--     ・メモの枚数が変わった
--     ・読んでいるメモの読み取り位置が変わった
--     ・12枚目の値が「時計で1フレームに1ずつ減る」以外の変わり方をした
--     ・ステートを読み込んだ／フリーズを検出した／LOG_TICK フレームたった
--   巻き戻し中は書かず、進み始めたところで「巻き戻し」と1行書く。
--   列はタブ区切りで、フレーム、何枚目、読み取り位置、12枚目の値、きっかけ。
-- ロマンシング サガ（日本版）以外のゲームでは何もせずに終了する。
-- それぞれのしくみは元の2つのファイルの冒頭の説明を参照。
--
-- 使い方：ROM（またはムービー）を読み込んでから、Tools → Lua Console でこのファイルを開く。
--         起動時の引数なら  EmuHawk.exe --lua="このファイル" "ROM"

local USE_KEEP_BRIGHT = true     -- 暗転中も画面を明るくする
local USE_FREEZE_DETECT = true   -- フリーズと長い待ちを検出する
local USE_MEMO_DISPLAY = true    -- いま読んでいるメモが何枚目かを表示する
local USE_LOG = true             -- メモの枚数と読み取り位置の変化をファイルに記録する
local LOG_PATH = "ladybug-log.txt"  -- 記録先。場所を決めたいときは "C:/.../ladybug-log.txt" のように書く
local LOG_TICK = 600             -- 変化がなくても、このフレーム数ごとに1行書く（0 で書かない）
local IDLE_WARN = 1800           -- 命令をこのフレーム数読んでいなければ知らせる（IDLE を黄色にし、記録に1行書く）

local BRIGHT = 0x0F
local BRIGHTEN_WHEN_UNKNOWN = true  -- 開始直後・ステート読み込み直後も明るくする
local PAUSE_ON_FREEZE = true        -- フリーズを検出したら一時停止する
local LONG_WAIT = 600               -- このフレーム数以上の待ち（命令 14）を知らせる
local PAUSE_ON_NESTED_END = false  -- 入れ子の途中で「イベント終了」（40 4B）を読んだら一時停止する

local BUS = "System Bus"
local ROM_SHA1 = "D00960176AA980DBD7BFF4062E52B7450B9986DD"

-- ゲームの判定（ハッシュが一致するか、名前に Romancing SaGa を含むか）
local hash = string.upper(tostring(gameinfo.getromhash() or ""))
local name = tostring(gameinfo.getromname() or "")
if not string.find(hash, ROM_SHA1, 1, true) and not string.find(name, "Romancing SaGa", 1, true) then
  console.log("ladybug-tools: ロマンシング サガ（日本版）ではないので何もしません。 名前=" .. name .. " ハッシュ=" .. hash)
  return
end

local found = false
local names = {}
for _, n in pairs(memory.getmemorydomainlist()) do
  names[#names + 1] = n
  if n == BUS then found = true end
end
if not found then
  console.log("ladybug-tools: 「System Bus」が見つからないため終了します。領域一覧 = " .. table.concat(names, ", "))
  console.log("ladybug-tools: SNESコアを BSNESv115+ にして、ROMを読み込んでから実行してください。")
  return
end

---------------------------------------------------------------- keep-bright
local last = nil      -- ゲームが最後に $2100 へ書いた値（分からないときは nil）
local mine = false    -- 自分の書き込み中は見張りを無視する

local function watch(addr, val, flags)
  if not mine and val ~= nil then last = val end
end

-- $2100 はバンク $00〜$3F と $80〜$BF のどこからでも同じレジスタに届く。
-- ゲームはバンク $00 以外からも書くので、全部のバンクを見張る
-- （バンク $00 だけを見張ると「表示オフ」を見逃して画面が壊れる）。
if USE_KEEP_BRIGHT then
for bank = 0, 0xFF do
  if bank % 0x80 < 0x40 then
    event.on_bus_write(watch, bank * 0x10000 + 0x2100, "keep-bright-watch-" .. bank, BUS)
  end
end
end

event.onloadstate(function() last = nil end, "keep-bright-load")


---------------------------------------------------------------- freeze-detect
local freeze_frame = -1000   -- 最後にフリーズのループを見たフレーム
local freeze_at = nil        -- フリーズが始まったフレーム
local freeze_kind = "scroll loop"
local pause_pending = 0     -- 一時停止は、赤字を画面に出した次のフレームで行う
local wait_until = -1        -- 長い待ちが終わる予定のフレーム
local wait_len = 0
local warned_reg = false

local function reg(name)
  local ok, v = pcall(emu.getregister, name)
  if ok and v ~= nil then return v end
  if not warned_reg then
    warned_reg = true
    console.log("freeze-detect: レジスタ " .. name .. " を読めません。検出できません。")
  end
  return nil
end

local function on_scroll_loop()
  local y = reg("Y")
  if y == nil then return end
  local height = memory.read_u8(0x1606 + (y % 0x10000), "WRAM")
  if height ~= 0 then return end
  local now = emu.framecount()
  if now - freeze_frame > 30 then
    freeze_at = now
    freeze_kind = "scroll loop"
    console.log(string.format("freeze-detect: フレーム %d で窓の行送りループ（フリーズ）に入りました。窓の位置 Y=$%02X", now, y))
    if PAUSE_ON_FREEZE then pause_pending = 2 end
  end
  freeze_frame = now
end

-- 選択肢の窓の送りループ：選択位置（$12CC）が見えるところまで一覧を2行ずつ送る処理。
-- $12CC が $FF などのとき終わらない。ループ本体（$00:BD97）に来たときの $12CC の値で判定する。
-- フレームを待たずに回る場合に備えて、1フレームに200回以上通ったときもフリーズとみなす。
local list_count = 0
local list_frame = -1
local function on_list_loop()
  local now = emu.framecount()
  if now ~= list_frame then
    list_frame = now
    list_count = 0
  end
  list_count = list_count + 1
  -- 終わらない条件：選択位置が $FF、または $FE で下端の行番号が偶数（2ずつ増えるので追い越せない）
  local target = memory.read_u8(0x12CC, "WRAM")
  local endless = (target == 0xFF)
  if target == 0xFE then
    local y = reg("Y")
    if y ~= nil and memory.read_u8(0x1618 + (y % 0x10000), "WRAM") % 2 == 0 then endless = true end
  end
  if endless or list_count == 200 then
    if now - freeze_frame > 30 then
      freeze_at = now
      freeze_kind = "list loop"
      console.log(string.format("freeze-detect: フレーム %d で選択肢の窓の送りループ（フリーズ）に入りました。$12CC=$%02X", now, target))
      if PAUSE_ON_FREEZE then pause_pending = 2 end
    end
    freeze_frame = now
  end
end

-- 暴走：プログラムがROMではなくRAMの中を実行し始めた状態。スタックの位置もでたらめになり、
-- メモリを壊しながら回り続ける。フレームの切れ目での実行位置が RAM（番地 $8000 未満、
-- またはバンク $7E/$7F）にある状態が CRASH_FRAMES フレーム続いたら暴走とみなす
-- （7,279Fのムービーでは1回もない）。
local CRASH_FRAMES = 5
local crash_run = 0
local function check_crash()
  local pc = reg("PC")
  if pc == nil then return end
  local bank = math.floor(pc / 0x10000) % 0x100
  local addr = pc % 0x10000
  if addr < 0x8000 or bank == 0x7E or bank == 0x7F then crash_run = crash_run + 1 else crash_run = 0 end
  if crash_run >= CRASH_FRAMES then
    local now = emu.framecount()
    if crash_run == CRASH_FRAMES then
      freeze_at = now - CRASH_FRAMES
      freeze_kind = "crash (running in RAM)"
      console.log(string.format("freeze-detect: フレーム %d ごろからプログラムが暴走しています（RAMの中を実行中。実行位置 $%06X）。", freeze_at, pc))
      if PAUSE_ON_FREEZE then pause_pending = 2 end
    end
    freeze_frame = now
  end
end

-- フレーム待ちから戻れない型：ゲームは「次のフレームまで待つ」とき、割り込み処理が
-- 合図（$1206）を消すのを待つ。割り込みが止められたままこの待ちに入ると永久に戻れない。
-- このときゲームは入力を読まないフレーム（ラグフレーム）が続くので、その連続数で判定する
-- （7,279Fのムービーでは最長でも37フレーム）。
local LAG_FREEZE = 180
local lag_run = 0
local function check_lag()
  if emu.islagged() then lag_run = lag_run + 1 else lag_run = 0 end
  if lag_run >= LAG_FREEZE and crash_run < CRASH_FRAMES then
    local now = emu.framecount()
    if lag_run == LAG_FREEZE then
      freeze_at = now - LAG_FREEZE
      freeze_kind = "frame wait (interrupt off)"
      console.log(string.format("freeze-detect: フレーム %d ごろから、フレーム待ちから戻れなくなっています（割り込みが止まったまま）。", freeze_at))
      if PAUSE_ON_FREEZE then pause_pending = 2 end
    end
    freeze_frame = now
  end
end

-- 入れ子の途中での「イベント終了」（副命令 40 4B、処理は $00:B861）。
-- 港の追い返しのたびに「イベント → フィールド側の命令 → イベント」の呼び出しが1段ずつ深くなる。
-- その途中で 40 4B を読むと、メモは全部捨てられるのに呼び出しの入れ子は残り、
-- 内側の処理に取り残される（操作不能のまま回り続ける、あとで暴走する、などを確認）。
-- 正常な場面ではスタックの位置は $1EE8 付近。入れ子1段ごとに下がるので、$1EA0 未満なら入れ子とみなす。
local nested_end_frame = nil
local nested_end_s = 0
local function on_end_event()
  local sp = reg("S")
  if sp == nil or sp >= 0x1EA0 then return end
  nested_end_frame = emu.framecount()
  nested_end_s = sp
  console.log(string.format("freeze-detect: フレーム %d で、呼び出しの入れ子が残ったままイベント終了（40 4B）を読みました（スタック $%04X）。行き止まりになる可能性が高い状態です。", nested_end_frame, sp))
  if PAUSE_ON_NESTED_END then pause_pending = 2 end
end

local function on_wait14()
  local x = reg("X")
  if x == nil then return end
  local frames = x
  if frames == 0 then frames = 65536 end
  if frames >= LONG_WAIT then
    local now = emu.framecount()
    wait_until = now + frames
    wait_len = frames
    console.log(string.format("freeze-detect: フレーム %d で命令 14 の長い待ち（%d フレーム）が始まりました。", now, frames))
  end
end

if USE_FREEZE_DETECT then
event.on_bus_exec(on_scroll_loop, 0x00A464, "freeze-detect-a464", BUS)
event.on_bus_exec(on_scroll_loop, 0x00C75A, "freeze-detect-c75a", BUS)
event.on_bus_exec(on_wait14, 0x00C6A4, "freeze-detect-c6a4", BUS)
event.on_bus_exec(on_end_event, 0x00B861, "freeze-detect-b861", BUS)
event.on_bus_exec(on_list_loop, 0x00BD97, "freeze-detect-bd97", BUS)
end
event.onloadstate(function()
  freeze_frame = -1000
  lag_run = 0
  crash_run = 0
  nested_end_frame = nil
  freeze_at = nil
  wait_until = -1
end, "freeze-detect-load")


---------------------------------------------------------------- メモの表示
-- 枚数は $129B（0 が1枚目）。メモは1枚8バイトで $129C から並び、
-- 先頭3バイトが読み取り位置（下位、上位、バンク）。
local function memo_ptr(index)
  local base = 0x129C + index * 8
  local lo = memory.read_u8(base, "WRAM")
  local hi = memory.read_u8(base + 1, "WRAM")
  local bank = memory.read_u8(base + 2, "WRAM")
  return string.format("%02X:%02X%02X", bank, hi, lo)
end

-- 最後にスクリプトの命令を読んでから何フレームたったか（命令の読み出し口 $00:A0A5 を見張る）
local last_cmd_frame = nil
if USE_MEMO_DISPLAY then
  event.on_bus_exec(function() last_cmd_frame = emu.framecount() end, 0x00A0A5, "ladybug-cmd", BUS)
  event.onloadstate(function() last_cmd_frame = nil end, "ladybug-cmd-load")
end

local function draw_memo()
  local depth = memory.read_u8(0x129B, "WRAM")
  local color = "white"
  if depth >= 11 then color = "red" end
  local where = "--:----"
  if depth < 16 then where = memo_ptr(depth) end
  gui.text(4, 60, string.format("MEMO %d  %s", depth + 1, where), color)
  gui.text(4, 76, string.format("12th   %s", memo_ptr(11)), color)
  if last_cmd_frame == nil then
    gui.text(4, 92, "IDLE   ?", "white")
  else
    local idle = emu.framecount() - last_cmd_frame
    local idle_color = "white"
    if idle >= IDLE_WARN then idle_color = "yellow" end
    gui.text(4, 92, string.format("IDLE   %d", idle), idle_color)
  end
end

local prev_frame = nil
local rewinding = false   -- このフレームが巻き戻しで来たものか（毎フレームの処理で更新）

---------------------------------------------------------------- 記録
local log_file = nil
local log_prev_depth = nil
local log_prev_ptr = nil
local log_prev_lo = nil
local log_prev_clock = nil
local log_last_frame = -1000000
local log_note = nil          -- 次の行に付けるきっかけ（ステート読み込みなど）
local log_seen_freeze = nil
local log_seen_nested = nil

if USE_LOG then
  local err
  log_file, err = io.open(LOG_PATH, "a")
  if log_file == nil then
    console.log("ladybug-tools: 記録ファイルを開けません（" .. tostring(err) .. "）。記録なしで続けます。")
  else
    log_file:write(string.format("# ladybug-tools 記録開始 フレーム %d\n", emu.framecount()))
    log_file:write("# フレーム\t何枚目\t読み取り位置\t12枚目\tきっかけ\n")
    log_file:flush()
    event.onloadstate(function() log_note = "ステート読み込み" end, "ladybug-log-load")
    event.onexit(function() if log_file ~= nil then log_file:close() log_file = nil end end, "ladybug-log-exit")
  end
end

local function log_step()
  if log_file == nil then return end
  local now = emu.framecount()
  local depth = memory.read_u8(0x129B, "WRAM")
  local ptr = "--:----"
  if depth < 16 then ptr = memo_ptr(depth) end
  local lo = memory.read_u8(0x12F4, "WRAM")
  local clock = memory.read_u8(0x12F5, "WRAM") + memory.read_u8(0x12F6, "WRAM") * 256

  -- 巻き戻し中は書かない。巻き戻しが終わって進み始めたフレームで1行だけ書く
  if rewinding then
    log_note = "巻き戻し"
    log_prev_depth = depth
    log_prev_ptr = ptr
    log_prev_lo = lo
    log_prev_clock = clock
    log_seen_freeze = nil
    log_seen_nested = nil
    return
  end

  local why = log_note
  log_note = nil
  if why == nil and nested_end_frame ~= nil and nested_end_frame ~= log_seen_nested then
    why = string.format("入れ子のままイベント終了 S=%04X", nested_end_s)
  end
  log_seen_nested = nested_end_frame
  if why == nil and last_cmd_frame ~= nil and now - last_cmd_frame == IDLE_WARN then
    why = string.format("命令を%dフレーム読んでいない", IDLE_WARN)
  end
  if why == nil and freeze_at ~= nil and freeze_at ~= log_seen_freeze then
    why = "フリーズ検出 " .. freeze_kind
  end
  if freeze_at ~= nil then log_seen_freeze = freeze_at end
  if why == nil then
    if log_prev_depth == nil then
      why = "開始"
    elseif depth ~= log_prev_depth then
      why = "枚数"
    else
      -- 12枚目は時計で1フレームに1ずつ減るのが普通。それ以外の変わり方だけを拾う
      local clock_ok = (lo == log_prev_lo) and (clock == (log_prev_clock - 1) % 0x10000 or clock == log_prev_clock)
      if not clock_ok then
        why = "12枚目"
      elseif depth ~= 11 and ptr ~= log_prev_ptr then
        why = "位置"
      elseif LOG_TICK > 0 and now - log_last_frame >= LOG_TICK then
        why = "定期"
      end
    end
  end

  if why ~= nil then
    log_file:write(string.format("%d\t%d\t%s\t%s\t%s\n", now, depth + 1, ptr, memo_ptr(11), why))
    log_file:flush()
    log_last_frame = now
  end
  log_prev_depth = depth
  log_prev_ptr = ptr
  log_prev_lo = lo
  log_prev_clock = clock
end

---------------------------------------------------------------- 毎フレームの処理
-- 巻き戻し（フレーム番号が前より小さくなった）への対応。
-- 巻き戻しではステート読み込みの通知が来ないので、ここで検出の状態を捨てる。
-- 捨てないと、巻き戻す前のフリーズの記憶が残り、巻き戻した先で同じフリーズに入っても検出されない。
local function check_rewind()
  local now = emu.framecount()
  rewinding = (prev_frame ~= nil and now < prev_frame)
  prev_frame = now
  if rewinding then
    freeze_frame = -1000
    freeze_at = nil
    wait_until = -1
    lag_run = 0
    crash_run = 0
    pause_pending = 0
    nested_end_frame = nil
    last_cmd_frame = nil
    last = nil
  end
end

console.log("ladybug-tools: 動作中（明るさ固定=" .. tostring(USE_KEEP_BRIGHT) .. "、フリーズ検出=" .. tostring(USE_FREEZE_DETECT) .. "、メモ表示=" .. tostring(USE_MEMO_DISPLAY) .. "、記録=" .. tostring(USE_LOG) .. "）")
while true do
  check_rewind()
  if USE_KEEP_BRIGHT then
    local dark
    if last == nil then
      dark = BRIGHTEN_WHEN_UNKNOWN
    else
      dark = (last < 0x80) and (last % 16 < BRIGHT)
    end
    if dark then
      mine = true
      memory.write_u8(0x2100, BRIGHT, BUS)
      mine = false
    end
  end
  if USE_MEMO_DISPLAY then draw_memo() end
  if USE_FREEZE_DETECT then
    check_crash()
    check_lag()
    local now = emu.framecount()
    if now - freeze_frame <= 30 and freeze_at ~= nil then
      gui.text(4, 40, string.format("FREEZE: %s since frame %d", freeze_kind, freeze_at), "red")
    elseif now < wait_until then
      gui.text(4, 20, string.format("LONG WAIT: %d frames left (of %d)", wait_until - now, wait_len), "yellow")
    end
    if nested_end_frame ~= nil then
      gui.text(4, 108, string.format("NESTED END (40 4B) at frame %d  S=%04X", nested_end_frame, nested_end_s), "yellow")
    end
    if pause_pending == 1 then
      pause_pending = 0
      client.pause()
    elseif pause_pending == 2 then
      pause_pending = 1
    end
  end
  if USE_LOG then log_step() end
  emu.frameadvance()
end
