-- レイディバグ検証用のまとめスクリプト（BizHawk 2.11 / BSNESv115+ 用）
-- keep-bright.lua（暗転中も画面を見えるようにする）と
-- freeze-detect.lua（フリーズと長い待ちを検出する）を1本にしたもの。
-- さらに、いま読んでいるメモが何枚目か（スタックの段数）を画面に表示する。
--   1行目：MEMO 実際に命令を読んでいるメモが何枚目か と、その読み取り位置（バンク:番地）
--     命令の読み出し口（$00:A0A5）で最後に使われたメモを表示する。枚数の数字（$129B）ではない。
--     開始直後・ステート読み込み直後など、まだ命令を読んでいない間だけ、枚数の数字で代用する。
--   2行目：12枚目のメモの中身（時計と重なっていて毎フレーム変わる）
--   3行目：IDLE 最後にスクリプトの命令を読んでからのフレーム数（開始直後・ステート読み込み直後は ?）
--   STACK：CPUのスタックの位置（S）。メモ（スクリプトのしおり）とは別物で、処理の入れ子が深いほど小さくなる。
--     普段のイベント終了時は $1EE8 付近、バグ発火時は $1D96。STACK_WARN 未満で黄、STACK_DANGER 未満で赤。
--     $1A00 台まで下がると、ゲームが作業用に使う領域と重なり、ボタンを押しただけで暴走することがある。
--   12枚目以上を読んでいる間は赤、それ以外は白で表示する。
--   COUNT：枚数の数字（$129B）が、実際に読んでいるメモとちがうときだけ出る（橙）。
--     枚数の数字だけが1枚目に戻され、積まれたメモが上から順に読まれ続けることがある。
--   画面の下のほうに 1〜11枚目の読み取り位置と終了位置を並べる（SHOW_MEMO_LIST）。
--     「枚目 バンク:読み取り位置 >終了位置」の形。終了位置は、読み取り位置がここにぴったり重なったら
--     スクリプトが終わる、という目印（メモの4〜5バイト目）。
--     読んでいないメモの読み取り位置か終了位置が変わったら（命令の作業用の値による上書き）、
--     そのメモを OVERWRITE_SHOW フレームのあいだ赤で表示し、記録にも1行書く。
--   入力を受け付けているときは、その下に表示する：
--     INPUT: CHOICE  選択肢の入力待ち（水色）。A・B・←・→で結果が変わる
--     INPUT: PRESS   ボタンを押すのを待っている（会話送り・何か押せば進む待ちなど。命令 46 のように時間で勝手に進む待ちもある）
--     INPUT: HOLD    押しっぱなしのボタンを見ている（フィールドの歩行など。SHOW_HOLD = true のときだけ）
--   エンディングに入ったら、入口（40 6F か 40 70）、命令 40 から入ったか命令を読まずに飛んできたか、
--   その直前に読んだ命令の位置を記録に書き、画面に ENDING と出す。
-- さらに、メモの動きをファイルに記録する（USE_LOG）。
--   毎フレームは書かず、次のときだけ1行書く：
--     ・読んでいるメモが変わった／枚数の数字（$129B）が変わった
--     ・読んでいるメモの読み取り位置が変わった
--     ・12枚目の値が「時計で1フレームに1ずつ減る」以外の変わり方をした
--     ・ステートを読み込んだ／フリーズを検出した／LOG_TICK フレームたった
--   巻き戻し中は書かず、進み始めたところで「巻き戻し」と1行書く。
--   列はタブ区切りで、フレーム、読んでいるメモ（何枚目）、読み取り位置、12枚目の値、きっかけ。
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
local STACK_WARN = 0x1C00        -- CPUのスタック（S）がこれより下がったら STACK を黄色にする
local STACK_DANGER = 0x1B00      -- これより下がったら赤にし、記録に1行書く（$1A00 台で作業領域と重なった例あり）
local SHOW_MEMO_LIST = true      -- 1〜11枚目の読み取り位置を並べて表示し、上書きを見張る
local OVERWRITE_SHOW = 300       -- 上書きされたメモを赤で表示しておくフレーム数
local SHOW_HOLD = false          -- 押しっぱなしのボタンを見ているとき（フィールドの歩行、デモなど）も INPUT: HOLD と出す

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
-- その途中で 40 4B を読むと、メモは全部捨てられ、呼び出しの入れ子は1段だけ戻る。
-- そのあとは場面しだい：直後に操作不能・フリーズ・暴走になる例もあれば、
-- 40 4B を何度か通って入れ子が浅くなり、港のスクリプトを読み続ける例もある。
-- フリーズを避けて長く進んだ試行は、どれもここを通っていた（docs/lady-bug/freezes.md）。
-- 正常な場面ではスタックの位置は $1EE8 付近。入れ子1段ごとに下がるので、$1EA0 未満なら入れ子とみなす。
local nested_end_frame = nil
local nested_end_s = 0
local function on_end_event()
  local sp = reg("S")
  if sp == nil or sp >= 0x1EA0 then return end
  nested_end_frame = emu.framecount()
  nested_end_s = sp
  console.log(string.format("freeze-detect: フレーム %d で、呼び出しの入れ子が残ったままイベント終了（40 4B）を読みました（スタック $%04X）。", nested_end_frame, sp))
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

-- メモの終了位置（4〜5バイト目。下位、上位）
local function memo_end(index)
  local base = 0x129C + index * 8
  return string.format("%02X%02X", memory.read_u8(base + 4, "WRAM"), memory.read_u8(base + 3, "WRAM"))
end

-- 最後にスクリプトの命令を読んでから何フレームたったか（命令の読み出し口 $00:A0A5 を見張る）
local last_cmd_frame = nil
-- 直前に読んだ命令の位置を覚えておく（エンディングの入口を記録するため）。
-- 読み出し口 $00:A0A5 に来たとき、X がいま読んでいるメモの位置（枚数×8）を指している。
local RECENT_MAX = 12
local recent = {}
local read_slots = {}        -- このフレームに命令を読んだメモ（0 が1枚目）
local last_slot = nil        -- 最後に命令を読んだメモ（0 が1枚目）。まだ読んでいなければ nil
local mismatch_slot = nil    -- 最後に読んだメモが枚数とちがっていたら、そのメモ（0 が1枚目）
local mismatch_frame = -1000
-- 1〜11枚目の読み取り位置の見張り。読んでいないのに変わったら上書きとみなす。
local memo_prev = {}         -- 前のフレームの読み取り位置（文字列）
local memo_prev_end = {}     -- 前のフレームの終了位置（文字列）
local memo_hit = {}          -- 上書きを見つけたフレーム
local memo_end_logged = {}   -- 終了位置の上書きを最後に書き出したフレーム
local overwrite_notes = {}   -- 記録に書く上書きの内容
local function reset_memo_watch()
  memo_prev = {}
  memo_prev_end = {}
  memo_end_logged = {}
  memo_hit = {}
  overwrite_notes = {}
  read_slots = {}
  mismatch_slot = nil
  last_slot = nil
end
local function on_cmd_read()
  local now = emu.framecount()
  last_cmd_frame = now
  local x = reg("X")
  if x == nil then return end
  -- 実際に読んでいるメモ（X÷8）。枚数（$129B）とちがっていたら覚えておく
  local slot = math.floor((x % 0x100) / 8)
  read_slots[slot] = true
  last_slot = slot
  if slot ~= memory.read_u8(0x129B, "WRAM") then
    mismatch_slot = slot
    mismatch_frame = now
  else
    mismatch_slot = nil
  end
  local base = 0x129C + (x % 0x100)
  local lo = memory.read_u8(base, "WRAM")
  local hi = memory.read_u8(base + 1, "WRAM")
  local bank = memory.read_u8(base + 2, "WRAM")
  local addr = bank * 0x10000 + hi * 0x100 + lo
  local byte = memory.read_u8(addr, BUS)
  recent[#recent + 1] = string.format("%d:%02X:%02X%02X=%02X", now, bank, hi, lo, byte)
  if #recent > RECENT_MAX then table.remove(recent, 1) end
end
if USE_MEMO_DISPLAY then
  event.on_bus_exec(on_cmd_read, 0x00A0A5, "ladybug-cmd", BUS)
  event.onloadstate(function() last_cmd_frame = nil recent = {} reset_memo_watch() end, "ladybug-cmd-load")
end

-- 1〜11枚目の読み取り位置を前のフレームと比べる。毎フレーム1回呼ぶ。
local function watch_memos()
  local now = emu.framecount()
  local depth = memory.read_u8(0x129B, "WRAM")
  for i = 0, 10 do
    local cur = memo_ptr(i)
    local old = memo_prev[i]
    local cur_end = memo_end(i)
    local old_end = memo_prev_end[i]
    -- 読んでいるメモ・このフレームに読んだメモ・その1つ上（呼び出しで新しく作られる）は普通に変わる
    -- 見張るのは、いま読んでいるメモより下のメモだけ（上のメモは使われていないので、変わっても害がない）
    local idle = i < (last_slot or depth) and i ~= depth and i ~= depth + 1 and i ~= mismatch_slot
       and not read_slots[i] and not read_slots[i - 1]
    if idle and old ~= nil and cur ~= old then
      memo_hit[i] = now
      overwrite_notes[#overwrite_notes + 1] = string.format("%d枚目 %s→%s", i + 1, old, cur)
      console.log(string.format("ladybug-tools: フレーム %d で %d枚目のメモが上書きされました（%s → %s）。", now, i + 1, old, cur))
    end
    -- 待ちのカウンタと重なると毎フレーム変わるので、書き出しは同じメモにつき60フレームに1回まで
    if idle and old_end ~= nil and cur_end ~= old_end then memo_hit[i] = now end
    local quiet = memo_end_logged[i] ~= nil and now - memo_end_logged[i] >= 0 and now - memo_end_logged[i] < 60
    if idle and old_end ~= nil and cur_end ~= old_end and not quiet then
      memo_end_logged[i] = now
      overwrite_notes[#overwrite_notes + 1] = string.format("%d枚目 終了位置 %s→%s", i + 1, old_end, cur_end)
      console.log(string.format("ladybug-tools: フレーム %d で %d枚目のメモの終了位置が上書きされました（%s → %s）。", now, i + 1, old_end, cur_end))
    end
    memo_prev[i] = cur
    memo_prev_end[i] = cur_end
  end
  read_slots = {}
end

local function draw_memo_list()
  local now = emu.framecount()
  for i = 0, 10 do
    local color = "white"
    local d = memo_hit[i] and (now - memo_hit[i]) or nil
    if d ~= nil and d >= 0 and d <= OVERWRITE_SHOW then color = "red" end
    gui.text(4 + (i % 2) * 190, 172 + math.floor(i / 2) * 16, string.format("%2d %s >%s", i + 1, memo_ptr(i), memo_end(i)), color)
  end
end

local function draw_memo()
  local depth = memory.read_u8(0x129B, "WRAM")
  local cur = last_slot or depth   -- 実際に読んでいるメモ（分からない間は枚数の数字）
  local color = "white"
  if cur >= 11 then color = "red" end
  local where = "--:----"
  if cur < 16 then where = memo_ptr(cur) end
  gui.text(4, 60, string.format("MEMO %d  %s", cur + 1, where), color)
  if cur ~= depth then
    gui.text(200, 60, string.format("COUNT %d", depth + 1), "orange")
  end
  gui.text(4, 76, string.format("12th   %s >%s", memo_ptr(11), memo_end(11)), color)
  if last_cmd_frame == nil then
    gui.text(4, 92, "IDLE   ?", "white")
  else
    local idle = emu.framecount() - last_cmd_frame
    local idle_color = "white"
    if idle >= IDLE_WARN then idle_color = "yellow" end
    gui.text(4, 92, string.format("IDLE   %d", idle), idle_color)
  end
  local sp = reg("S")
  if sp ~= nil then
    sp = sp % 0x10000
    local sp_color = "white"
    if sp < STACK_DANGER then sp_color = "red" elseif sp < STACK_WARN then sp_color = "yellow" end
    gui.text(4, 156, string.format("STACK  S=$%04X", sp), sp_color)
  end
  if SHOW_MEMO_LIST then draw_memo_list() end
end

---------------------------------------------------------------- 選択肢待ちの表示
-- 入力待ちの間、ゲームは共通の「1フレーム待つ」処理で止まっている。
-- 選択肢の処理（$00:BADA から始まる。$00:B952 から呼ばれる）の中にいるときは、
-- スタックに戻り先 $B954 が積まれている。これがあれば「選択肢待ち」と表示する。
-- （ふだんは $BAF9 も積まれているが、カーソルの点滅などで数フレームだけ別の戻り先になるので見ない）
-- 選択肢待ちでは A（カーソルの行）・B（キャンセル $FF）・←（$FC）・→（$FD）で結果が変わる。
local USE_CHOICE_DISPLAY = true
local function in_choice_wait()
  local sp = reg("S")
  if sp == nil then return false end
  sp = sp % 0x10000
  if sp >= 0x1FA0 then return false end
  for i = 1, 0x60 do
    if memory.read_u8(sp + i, "WRAM") == 0x54 and memory.read_u8(sp + i + 1, "WRAM") == 0xB9 then
      return true
    end
  end
  return false
end
-- 入力を受け付けているかどうか。
-- コントローラーの状態は毎フレーム割り込みの中（$00:82AE〜$00:82E4）で RAM に写される：
--   $1200〜$1201 = いま押されているボタン、$1202〜$1203 = 新しく押されたボタン。
-- ゲーム側の処理がこれを読んだフレームは「入力を見ている」とみなす。
--   ・$1202〜$1203 を読む（会話送り・何か押せば進む待ち・選択肢など、押した瞬間を待つもの）
--     共通の読み出し口は $00:82F8 と $00:8305。選択肢の処理（$00:BAFC）は直接読む。
--   ・$1200〜$1201 を割り込みの外で読む（フィールドの歩行、文字送りの早送りなど、押しっぱなしを見るもの）
local input_new_frame = -1000
local input_held_frame = -1000
local function on_pad_new()
  input_new_frame = emu.framecount()
end
local function on_pad_held()
  local pc = reg("PC")
  if pc == nil then return end
  if pc >= 0x0082A0 and pc <= 0x0082F0 then return end  -- 割り込みの中で写しているだけ
  input_held_frame = emu.framecount()
end
if USE_CHOICE_DISPLAY then
  for _, a in ipairs({0x001202, 0x001203, 0x7E1202, 0x7E1203}) do
    event.on_bus_read(on_pad_new, a, string.format("ladybug-pad-%06X", a), BUS)
  end
  for _, a in ipairs({0x001200, 0x001201, 0x7E1200, 0x7E1201}) do
    event.on_bus_read(on_pad_held, a, string.format("ladybug-pad-%06X", a), BUS)
  end
end

-- 会話の文字を出している途中などは2フレームに1回しか読まないので、3フレーム以内に読んでいれば表示する
local function draw_choice()
  local now = emu.framecount()
  local dn = now - input_new_frame
  local dh = now - input_held_frame
  local new_on = dn >= 0 and dn <= 3    -- ステート読み込みや巻き戻しで前後した分は数えない
  local held_on = dh >= 0 and dh <= 3
  if new_on and in_choice_wait() then
    gui.text(4, 124, "INPUT: CHOICE (A / B / LEFT / RIGHT)", "cyan")
  elseif new_on then
    gui.text(4, 124, "INPUT: PRESS (wait for button)", "lime")
  elseif held_on and SHOW_HOLD then
    gui.text(4, 124, "INPUT: HOLD (held buttons)", "lime")
  end
end

local prev_frame = nil
local rewinding = false   -- このフレームが巻き戻しで来たものか（毎フレームの処理で更新）

---------------------------------------------------------------- 記録
local log_file = nil
local log_prev_depth = nil
local log_prev_cur = nil
local log_prev_ptr = nil
local log_prev_lo = nil
local log_prev_clock = nil
local log_last_frame = -1000000
local log_note = nil          -- 次の行に付けるきっかけ（ステート読み込みなど）
local log_seen_freeze = nil
local log_seen_nested = nil
local log_prev_danger = false   -- 前のフレームでスタックが STACK_DANGER 未満だったか
local log_prev_mismatch = nil   -- 前に記録した「枚数とちがうメモ」

if USE_LOG then
  local err
  log_file, err = io.open(LOG_PATH, "a")
  if log_file == nil then
    console.log("ladybug-tools: 記録ファイルを開けません（" .. tostring(err) .. "）。記録なしで続けます。")
  else
    log_file:write(string.format("# ladybug-tools 記録開始 フレーム %d\n", emu.framecount()))
    log_file:write("# フレーム\t読んでいるメモ\t読み取り位置\t12枚目\tきっかけ\n")
    log_file:flush()
    event.onloadstate(function() log_note = "ステート読み込み" end, "ladybug-log-load")
    event.onexit(function() if log_file ~= nil then log_file:close() log_file = nil end end, "ladybug-log-exit")
  end
end

local function log_step()
  if log_file == nil then return end
  local now = emu.framecount()
  local depth = memory.read_u8(0x129B, "WRAM")
  local cur = last_slot or depth
  local ptr = "--:----"
  if cur < 16 then ptr = memo_ptr(cur) end
  local lo = memory.read_u8(0x12F4, "WRAM")
  local clock = memory.read_u8(0x12F5, "WRAM") + memory.read_u8(0x12F6, "WRAM") * 256

  -- 巻き戻し中は書かない。巻き戻しが終わって進み始めたフレームで1行だけ書く
  if rewinding then
    log_note = "巻き戻し"
    log_prev_depth = depth
    log_prev_cur = cur
    log_prev_ptr = ptr
    log_prev_lo = lo
    log_prev_clock = clock
    log_seen_freeze = nil
    log_seen_nested = nil
    log_prev_danger = false
    log_prev_mismatch = nil
    overwrite_notes = {}
    return
  end

  local why = log_note
  log_note = nil
  if why == nil and nested_end_frame ~= nil and nested_end_frame ~= log_seen_nested then
    why = string.format("入れ子のままイベント終了 S=%04X", nested_end_s)
  end
  log_seen_nested = nested_end_frame
  local sp_now = reg("S")
  local danger = (sp_now ~= nil) and (sp_now % 0x10000 < STACK_DANGER)
  if why == nil and danger and not log_prev_danger and crash_run == 0 then
    why = string.format("スタックが深い S=%04X", sp_now % 0x10000)
  end
  log_prev_danger = danger
  if #overwrite_notes > 0 then
    if why == nil then why = "メモ上書き " .. table.concat(overwrite_notes, " ") end
    overwrite_notes = {}
  end
  if mismatch_slot ~= log_prev_mismatch then
    if why == nil and mismatch_slot ~= nil then
      why = string.format("枚数の数字（%d）とちがうメモを読んだ（%d枚目 %s）", depth + 1, mismatch_slot + 1, memo_ptr(mismatch_slot))
    end
    log_prev_mismatch = mismatch_slot
  end
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
    elseif cur ~= log_prev_cur then
      why = "読むメモ"
    elseif depth ~= log_prev_depth then
      why = string.format("枚数の数字 %d", depth + 1)
    else
      -- 12枚目は時計で1フレームに1ずつ減るのが普通。それ以外の変わり方だけを拾う
      local clock_ok = (lo == log_prev_lo) and (clock == (log_prev_clock - 1) % 0x10000 or clock == log_prev_clock)
      if not clock_ok then
        why = "12枚目"
      elseif cur ~= 11 and ptr ~= log_prev_ptr then
        why = "位置"
      elseif LOG_TICK > 0 and now - log_last_frame >= LOG_TICK then
        why = "定期"
      end
    end
  end

  if why ~= nil then
    log_file:write(string.format("%d\t%d\t%s\t%s\t%s\n", now, cur + 1, ptr, memo_ptr(11), why))
    log_file:flush()
    log_last_frame = now
  end
  log_prev_depth = depth
  log_prev_cur = cur
  log_prev_ptr = ptr
  log_prev_lo = lo
  log_prev_clock = clock
end

---------------------------------------------------------------- エンディングの入口
-- 40 6F（エピローグから）は $03:800C、40 70（風景とスタッフロールから）は $03:800F へ飛ぶ。
-- ここに来たら、入口と直前に読んだ命令の位置（フレーム:バンク:番地=読んだ値）を記録とコンソールに出す。
local ending_frame = nil
local ending_kind = ""
local function on_ending(kind)
  return function()
    local now = emu.framecount()
    if ending_frame ~= nil and now - ending_frame >= 0 and now - ending_frame < 60 then return end
    ending_frame = now
    ending_kind = kind
    -- 直前に読んだ命令が 40 でなければ、命令を読まずにここへ飛んできた（暴走など）
    local via = "命令を読まずに飛んできた（暴走など）"
    -- 副命令（6F・70）は読み出し口を通らないので、最後に読んだ命令が 40 かどうかで見る
    local n = #recent
    if n >= 1 and recent[n]:sub(-2) == "40" then via = "命令 40 から" end
    local msg = string.format("エンディング開始（%s の入口、%s） 直前に読んだ命令: %s", kind, via, table.concat(recent, " "))
    console.log("ladybug-tools: フレーム " .. now .. " " .. msg)
    if log_file ~= nil then
      log_file:write(string.format("%d\t-\t-\t-\t%s\n", now, msg))
      log_file:flush()
    end
  end
end
event.on_bus_exec(on_ending("40 6F"), 0x03800C, "ladybug-end-6f", BUS)
event.on_bus_exec(on_ending("40 70"), 0x03800F, "ladybug-end-70", BUS)
event.onloadstate(function() ending_frame = nil end, "ladybug-end-load")

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
    recent = {}
    reset_memo_watch()
    ending_frame = nil
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
  if USE_MEMO_DISPLAY then
    if SHOW_MEMO_LIST and not rewinding then watch_memos() end
    draw_memo()
  end
  if USE_CHOICE_DISPLAY then draw_choice() end
  if USE_FREEZE_DETECT then
    check_crash()
    check_lag()
    local now = emu.framecount()
    if now - freeze_frame <= 30 and freeze_at ~= nil then
      gui.text(4, 40, string.format("FREEZE: %s since frame %d", freeze_kind, freeze_at), "red")
    elseif now < wait_until then
      gui.text(4, 20, string.format("LONG WAIT: %d frames left (of %d)", wait_until - now, wait_len), "yellow")
    end
    if ending_frame ~= nil then
      gui.text(4, 140, string.format("ENDING (%s) at frame %d", ending_kind, ending_frame), "yellow")
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
