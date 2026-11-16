-- レイディバグ検証用のまとめスクリプト（BizHawk 2.11 / BSNESv115+ 用）
-- keep-bright.lua（暗転中も画面を見えるようにする）と
-- freeze-detect.lua（フリーズと長い待ちを検出する）を1本にしたもの。
-- さらに、いま読んでいるメモが何枚目か（スタックの段数）を画面に表示する。
--   1行目：MEMO 何枚目 と、その読み取り位置（バンク:番地）
--   2行目：12枚目のメモの中身（時計と重なっていて毎フレーム変わる）
--   12枚目以上を読んでいる間は赤、それ以外は白で表示する。
-- ロマンシング サガ（日本版）以外のゲームでは何もせずに終了する。
-- それぞれのしくみは元の2つのファイルの冒頭の説明を参照。
--
-- 使い方：ROM（またはムービー）を読み込んでから、Tools → Lua Console でこのファイルを開く。
--         起動時の引数なら  EmuHawk.exe --lua="このファイル" "ROM"

local USE_KEEP_BRIGHT = true     -- 暗転中も画面を明るくする
local USE_FREEZE_DETECT = true   -- フリーズと長い待ちを検出する
local USE_MEMO_DISPLAY = true    -- いま読んでいるメモが何枚目かを表示する

local BRIGHT = 0x0F
local BRIGHTEN_WHEN_UNKNOWN = true  -- 開始直後・ステート読み込み直後も明るくする
local PAUSE_ON_FREEZE = true        -- フリーズを検出したら一時停止する
local LONG_WAIT = 600               -- このフレーム数以上の待ち（命令 14）を知らせる

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
-- $12CC が $FF などのとき終わらない。このループはフレームを待たずに回り続けるので、
-- 1フレームの間にループ本体（$00:BD97）を通った回数で判定する（正常なら多くても128回）。
local list_count = 0
local list_frame = -1
local function on_list_loop()
  local now = emu.framecount()
  if now ~= list_frame then
    list_frame = now
    list_count = 0
  end
  list_count = list_count + 1
  if list_count == 200 then
    if now - freeze_frame > 30 then
      freeze_at = now
      freeze_kind = "list loop"
      console.log(string.format("freeze-detect: フレーム %d で選択肢の窓の送りループ（フリーズ）に入りました。$12CC=$%02X", now, memory.read_u8(0x12CC, "WRAM")))
      if PAUSE_ON_FREEZE then pause_pending = 2 end
    end
    freeze_frame = now
  end
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
event.on_bus_exec(on_list_loop, 0x00BD97, "freeze-detect-bd97", BUS)
end
event.onloadstate(function()
  freeze_frame = -1000
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

local function draw_memo()
  local depth = memory.read_u8(0x129B, "WRAM")
  local color = "white"
  if depth >= 11 then color = "red" end
  local where = "--:----"
  if depth < 16 then where = memo_ptr(depth) end
  gui.text(4, 60, string.format("MEMO %d  %s", depth + 1, where), color)
  gui.text(4, 76, string.format("12th   %s", memo_ptr(11)), color)
end

---------------------------------------------------------------- 毎フレームの処理
console.log("ladybug-tools: 動作中（明るさ固定=" .. tostring(USE_KEEP_BRIGHT) .. "、フリーズ検出=" .. tostring(USE_FREEZE_DETECT) .. "、メモ表示=" .. tostring(USE_MEMO_DISPLAY) .. "）")
while true do
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
    local now = emu.framecount()
    if now - freeze_frame <= 30 and freeze_at ~= nil then
      gui.text(4, 40, string.format("FREEZE: %s since frame %d", freeze_kind, freeze_at), "red")
    elseif now < wait_until then
      gui.text(4, 20, string.format("LONG WAIT: %d frames left (of %d)", wait_until - now, wait_len), "yellow")
    end
    if pause_pending == 1 then
      pause_pending = 0
      client.pause()
    elseif pause_pending == 2 then
      pause_pending = 1
    end
  end
  emu.frameadvance()
end
