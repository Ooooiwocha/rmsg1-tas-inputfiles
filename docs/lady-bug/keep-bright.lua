-- 暗転中も画面を見えるようにする（BizHawk 2.11 / BSNESv115+ 用）
-- 毎フレームの直前に、画面の明るさレジスタ $2100 へ $0F（最大の明るさ・表示オン）を書き込む。
-- $2100 は書き込み専用でゲーム側からは読めないため、入力の同期には影響しない。
-- 使い方：ROM（またはムービー）を読み込んでから、Tools → Lua Console でこのファイルを開く。
--         止めるときはスクリプトを停止する。

local BRIGHT = 0x0F
local BUS = "System Bus"

-- 「System Bus」がない状態では何も書き込まずに終了する
-- （ない場合、BizHawk は別の領域に書き込んでしまい、ゲームのRAMを壊すため）
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

console.log("keep-bright: 動作中（" .. BUS .. " の $2100 に書き込みます）")
while true do
  memory.write_u8(0x2100, BRIGHT, BUS)
  emu.frameadvance()
end
