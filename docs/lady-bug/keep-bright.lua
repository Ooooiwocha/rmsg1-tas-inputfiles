-- 暗転中も画面を見えるようにする（BizHawk 2.11 / BSNESv115+ 用）
--
-- しくみ：
--   ゲームが画面の明るさレジスタ $2100 に書いた値を見張り、
--   「表示はオンのまま明るさだけを下げている」ときに限って、$0F（最大の明るさ）で上書きする。
--   ゲームが「表示オフ」（値 $80 以上）にしている間は手を出さない。
--   表示オフの間はゲームが画像データを読み込んでいて、そこで無理に表示をオンにすると
--   読み込みが失敗して画面が壊れたままになるため。
--   $2100 は書き込み専用でゲーム側からは読めないので、入力の同期には影響しない。
--
-- 使い方：ROM（またはムービー）を読み込んでから、Tools → Lua Console でこのファイルを開く。
--         止めるときはスクリプトを停止する。

local BRIGHT = 0x0F
local BUS = "System Bus"

-- スクリプトの開始直後とステートの読み込み直後は、ゲームが最後に書いた値が分からない。
-- true ：分からない間も明るくする（暗転中のステートから始めるとき用）
-- false：ゲームが次に $2100 へ書くまで何もしない（安全側）
local BRIGHTEN_WHEN_UNKNOWN = true

local found = false
local names = {}
for _, name in pairs(memory.getmemorydomainlist()) do
  names[#names + 1] = name
  if name == BUS then found = true end
end
if not found then
  console.log("keep-bright: 「System Bus」が見つからないため、何も書き込まずに終了します。")
  console.log("keep-bright: いまの領域一覧 = " .. table.concat(names, ", "))
  console.log("keep-bright: SNESコアを BSNESv115+ にして、ROMを読み込んでから実行してください。")
  return
end

local last = nil      -- ゲームが最後に $2100 へ書いた値（分からないときは nil）
local mine = false    -- 自分の書き込み中は見張りを無視する

local function watch(addr, val, flags)
  if not mine and val ~= nil then last = val end
end

-- $2100 はバンク $00〜$3F と $80〜$BF のどこからでも同じレジスタに届く。
-- ゲームはバンク $00 以外からも書くので、全部のバンクを見張る
-- （バンク $00 だけを見張ると「表示オフ」を見逃して画面が壊れる）。
for bank = 0, 0xFF do
  if bank % 0x80 < 0x40 then
    event.on_bus_write(watch, bank * 0x10000 + 0x2100, "keep-bright-watch-" .. bank, BUS)
  end
end

event.onloadstate(function() last = nil end, "keep-bright-load")

console.log("keep-bright: 動作中")
while true do
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
  emu.frameadvance()
end
