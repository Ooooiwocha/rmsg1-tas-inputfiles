-- レイディバグ検証用：抜けられないループと長い待ちを検出する（BizHawk 2.11 / BSNESv115+ 用）
--
-- 検出するもの：
--   1) 窓の行送りループ（フリーズ）
--      窓に文字を書く処理は「行番号が窓の高さ以上なら1行送って行番号を1減らす」を繰り返す。
--      高さが0の窓を相手にすると終わらない。この間は入力を読まないので、何を押しても進まない。
--      ループの入口（$00:A464 と $00:C75A）に来たとき、窓の高さ（$1606+Y）が0なら検出とする。
--   1b) 選択肢の窓の送りループ（フリーズ）
--      選択位置（$12CC）が見えるまで一覧を送る処理が、$12CC=$FF などのとき終わらない。
--   1c) フレーム待ちから戻れない（フリーズ）
--      割り込みが止められたまま「次のフレームまで待つ」処理に入ると戻れない。
--   2) 命令 14 の長い待ち
--      命令 14 n は n×2 フレーム待つ。n が 0 のときは約65,000フレーム（約18分）待つ。
--      LONG_WAIT フレーム以上の待ちが始まったら知らせる。
--
-- ゲームのメモリには何も書き込まないので、入力の同期には影響しない。
-- 使い方：ROM（またはムービー）を読み込んでから、Tools → Lua Console でこのファイルを開く。

local PAUSE_ON_FREEZE = true   -- フリーズを検出したらエミュレータを一時停止する
local LONG_WAIT = 600          -- この値以上のフレーム数を待つ命令 14 を知らせる

local BUS = "System Bus"
local found = false
for _, name in pairs(memory.getmemorydomainlist()) do
  if name == BUS then found = true end
end
if not found then
  console.log("freeze-detect: 「System Bus」が見つかりません。SNESコアを BSNESv115+ にして、ROMを読み込んでから実行してください。")
  return
end

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

-- フレーム待ちから戻れない型：ゲームは「次のフレームまで待つ」とき、割り込み処理が
-- 合図（$1206）を消すのを待つ。割り込みが止められたままこの待ちに入ると永久に戻れない。
-- このときゲームは入力を読まないフレーム（ラグフレーム）が続くので、その連続数で判定する
-- （7,279Fのムービーでは最長でも37フレーム）。
local LAG_FREEZE = 180
local lag_run = 0
local function check_lag()
  if emu.islagged() then lag_run = lag_run + 1 else lag_run = 0 end
  if lag_run >= LAG_FREEZE then
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

event.on_bus_exec(on_scroll_loop, 0x00A464, "freeze-detect-a464", BUS)
event.on_bus_exec(on_scroll_loop, 0x00C75A, "freeze-detect-c75a", BUS)
event.on_bus_exec(on_wait14, 0x00C6A4, "freeze-detect-c6a4", BUS)
event.on_bus_exec(on_list_loop, 0x00BD97, "freeze-detect-bd97", BUS)
event.onloadstate(function()
  freeze_frame = -1000
  lag_run = 0
  freeze_at = nil
  wait_until = -1
end, "freeze-detect-load")

console.log("freeze-detect: 動作中")
while true do
  check_lag()
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
  emu.frameadvance()
end
