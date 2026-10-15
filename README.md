# ロマンシング サ・ガ TAS 入力ファイル

BizHawk用の入力ファイル（`.bk2`）と、その短縮履歴です。

## まず再生するなら：7,325F版

**[最新・最良の7,325F版をダウンロード](https://raw.githubusercontent.com/Ooooiwocha/rmsg1-tas-inputfiles/main/latest/RS1_BizHawk_reset49_connected_7325F_verified.bk2)**

- ファイル：`RS1_BizHawk_reset49_connected_7325F_verified.bk2`
- 前の7,337F版から **12F短縮**。収録中の7,780F版から **455F短縮**。
- F0から独立に2回、38,000Fまで再生し、クレジット表示を経て既知のタイトル画像が表示されるところまで確認済み。
- エンディング入口の実行到達はCPUフレーム7,324（入力の7,325F目を実行中）。旧7,337F版のCPUフレーム7,336から **12F早く到達**。
- 現時点で確認できている最良の入力です。絶対最短を証明したものではありません。

### 最新版の検証内容

- 2回の独立再生で、56地点のCPU状態・全WRAMと、5地点のRGB画像・WRAMダンプが一致。
- 26,000Fと38,000Fのタイトル画像には黒いマスク領域があり、以前の検証済み版と同じ画像であることを確認。
- 一致確認は記録した比較地点についての結果です。全フレームの状態一致を検証したものではありません。

### 再生環境

最新BK2のヘッダーに記録されている環境：

- BizHawk **2.11.1**
- コア：**BSNESv115+**
- ゲーム：**Romancing SaGa (Japan)**（SNES）

ゲーム本体のROMは含まれていません。再生には対応するROMを別途用意してください。

## フォルダの案内

- [latest/](latest/)：最新・最良の検証済み入力を1件だけ置きます。
- [archive/](archive/)：過去の入力16件を、比較・記録用に残しています。

新記録ができたら、新版を `latest/` に置き、前の最新版を `archive/` に移します。このREADMEの最新版リンク・フレーム数・検証情報・一覧も同時に更新します。

## 入力ファイル一覧・短縮履歴

フレーム数が少ない順です。全17ファイルを掲載しています。
F数は入力ログの長さ（操作終了の目安）で、エンディング映像末尾までの時間ではありません。
「最良との差」は7,325Fとの差分です。説明はファイル名の識別用ラベルを日本語にしたものです。

| フレーム数 | 最良との差 | 識別用の説明 | ダウンロード |
| ---: | ---: | --- | --- |
| 7,325F | — | リセット49F＋移動・待機の接続調整（現時点の最良） | [BK2](latest/RS1_BizHawk_reset49_connected_7325F_verified.bk2) |
| 7,337F | +12F | 町内移動の左分岐調整 | [BK2](archive/RS1_BizHawk_town_split_left_7337F_verified.bk2) |
| 7,371F | +46F | 早期退室＋待機短縮 | [BK2](archive/RS1_BizHawk_early_room_exit_waittrim_7371F_verified.bk2) |
| 7,381F | +56F | 早期退室＋船 | [BK2](archive/RS1_BizHawk_early_room_exit_boat_7381F_verified.bk2) |
| 7,388F | +63F | 早期退出＋待機短縮 | [BK2](archive/RS1_BizHawk_early_exit_waittrim_7388F_verified.bk2) |
| 7,392F | +67F | 会話の早期退出＋船 | [BK2](archive/RS1_BizHawk_early_dialogue_exit_boat_7392F_verified.bk2) |
| 7,439F | +114F | 最初の出港経路・船移動の調整 | [BK2](archive/RS1_BizHawk_first_departure_edge_mirror_boat_7439F_verified.bk2) |
| 7,456F | +131F | NPC位相＋船 | [BK2](archive/RS1_BizHawk_npc_phase_boat_7456F_verified.bk2) |
| 7,458F | +133F | NPC位相＋船 | [BK2](archive/RS1_BizHawk_npc_phase_boat_7458F_verified.bk2) |
| 7,473F | +148F | 北側上陸 | [BK2](archive/RS1_BizHawk_north_landing_7473F.bk2) |
| 7,490F | +165F | エンディング到達経路の変更 | [BK2](archive/RS1_BizHawk_alternate_ending_7490F.bk2) |
| 7,502F | +177F | 東移動32／船操作42 | [BK2](archive/RS1_BizHawk_east32_boatrow42_7502F.bk2) |
| 7,536F | +211F | 最初のマップA | [BK2](archive/RS1_BizHawk_first_map_A_7536F.bk2) |
| 7,537F | +212F | 会話待機サイクル | [BK2](archive/RS1_BizHawk_meeting_wait_cycle_7537F.bk2) |
| 7,554F | +229F | オープニングマップ | [BK2](archive/RS1_BizHawk_opening_map_7554F.bk2) |
| 7,599F | +274F | 最初の会話スクロール | [BK2](archive/RS1_BizHawk_first_meeting_scroll_7599F.bk2) |
| 7,780F | +455F | 会話調整 | [BK2](archive/RS1_BizHawk_meeting_7780F.bk2) |

## ファイル名の読み方

例：`RS1_BizHawk_reset49_connected_7325F_verified.bk2`

- `RS1`：ロマンシング サ・ガ
- `BizHawk`：使用エミュレーター
- `reset49_connected` など：試行や調整内容を区別する識別子
- `7325F`：入力ログのフレーム数
- `verified`：検証済みとして保存したファイルの識別子。個々の検証条件がすべて同じという意味ではありません。
- `.bk2`：BizHawkのムービー形式

`verified` がない旧版も保存しています。名前だけで再生可否を判断せず、通常は上の最新・最良版を選んでください。

## 最新版のファイル確認

- サイズ：2,085 bytes
- SHA-256：`ef0203f331f005967c043d9c76e052fc094457869ca3b11be462d5681a8270fa`

過去16件のBK2は内容を変更せずに保存しています。前の最新版7,337Fは `archive/` に移しました。以前のroot直下や旧最新版へのリンクをお使いの場合は、このページのリンクに更新してください。

