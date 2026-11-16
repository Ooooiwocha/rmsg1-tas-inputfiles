-- 暗転中も画面を見えるようにする（BizHawk 2.11 / BSNESv115+ 用）
-- 毎フレームの直前に、画面の明るさレジスタ $2100 へ $0F（最大の明るさ・表示オン）を書き込む。
-- $2100 は書き込み専用でゲーム側からは読めないため、入力の同期には影響しない。
-- 使い方：Tools → Lua Console でこのファイルを開く。止めるときはスクリプトを停止する。

local BRIGHT = 0x0F

while true do
  memory.write_u8(0x2100, BRIGHT, "System Bus")
  emu.frameadvance()
end
