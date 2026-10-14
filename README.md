# ロマンシング サ・ガ TAS 入力ファイル

BizHawk用の入力ファイル（`.bk2`）と、その短縮履歴です。

## まず再生するなら：7,371F版

**[最新・最良の7,371F版をダウンロード](https://raw.githubusercontent.com/Ooooiwocha/rmsg1-tas-inputfiles/main/latest/RS1_BizHawk_early_room_exit_waittrim_7371F_verified.bk2)**

- ファイル：`RS1_BizHawk_early_room_exit_waittrim_7371F_verified.bk2`
- 前の7,381F版から **10F短縮**。収録中の7,780F版から **409F短縮**。
- F0から独立に2回再生し、エンディング末尾まで確認済み。
- 現時点で確認できている最良の入力です。絶対最短を証明したものではありません。

### 再生環境

最新BK2のヘッダーに記録されている環境：

- BizHawk **2.11.1**
- コア：**BSNESv115+**
- ゲーム：**Romancing SaGa (Japan)**（SNES）

ゲーム本体のROMは含まれていません。再生には対応するROMを別途用意してください。

## フォルダの案内

- [latest/](latest/)：最新・最良の検証済み入力を1件だけ置きます。
- [archive/](archive/)：過去の入力14件を、比較・記録用に残しています。

新記録ができたら、新版を `latest/` に置き、前の最新版を `archive/` に移します。このREADMEの最新版リンク・フレーム数・検証情報・一覧も同時に更新します。

## 入力ファイル一覧・短縮履歴

フレーム数が少ない順です。全15ファイルを掲載しています。
F数は入力ログの長さ（操作終了の目安）で、エンディング映像末尾までの時間ではありません。
「最良との差」は7,371Fとの差分です。説明はファイル名の識別用ラベルを日本語にしたものです。

| フレーム数 | 最良との差 | 識別用の説明 | ダウンロード |
| ---: | ---: | --- | --- |
| 7,371F | — | 早期退室＋待機短縮（現時点の最良） | [BK2](latest/RS1_BizHawk_early_room_exit_waittrim_7371F_verified.bk2) |
| 7,381F | +10F | 早期退室＋船 | [BK2](archive/RS1_BizHawk_early_room_exit_boat_7381F_verified.bk2) |
| 7,388F | +17F | 早期退出＋待機短縮 | [BK2](archive/RS1_BizHawk_early_exit_waittrim_7388F_verified.bk2) |
| 7,392F | +21F | 会話の早期退出＋船 | [BK2](archive/RS1_BizHawk_early_dialogue_exit_boat_7392F_verified.bk2) |
| 7,439F | +68F | 最初の出港経路・船移動の調整 | [BK2](archive/RS1_BizHawk_first_departure_edge_mirror_boat_7439F_verified.bk2) |
| 7,456F | +85F | NPC位相＋船 | [BK2](archive/RS1_BizHawk_npc_phase_boat_7456F_verified.bk2) |
| 7,458F | +87F | NPC位相＋船 | [BK2](archive/RS1_BizHawk_npc_phase_boat_7458F_verified.bk2) |
| 7,473F | +102F | 北側上陸 | [BK2](archive/RS1_BizHawk_north_landing_7473F.bk2) |
| 7,490F | +119F | エンディング到達経路の変更 | [BK2](archive/RS1_BizHawk_alternate_ending_7490F.bk2) |
| 7,502F | +131F | 東移動32／船操作42 | [BK2](archive/RS1_BizHawk_east32_boatrow42_7502F.bk2) |
| 7,536F | +165F | 最初のマップA | [BK2](archive/RS1_BizHawk_first_map_A_7536F.bk2) |
| 7,537F | +166F | 会話待機サイクル | [BK2](archive/RS1_BizHawk_meeting_wait_cycle_7537F.bk2) |
| 7,554F | +183F | オープニングマップ | [BK2](archive/RS1_BizHawk_opening_map_7554F.bk2) |
| 7,599F | +228F | 最初の会話スクロール | [BK2](archive/RS1_BizHawk_first_meeting_scroll_7599F.bk2) |
| 7,780F | +409F | 会話調整 | [BK2](archive/RS1_BizHawk_meeting_7780F.bk2) |

## ファイル名の読み方

例：`RS1_BizHawk_early_room_exit_waittrim_7371F_verified.bk2`

- `RS1`：ロマンシング サ・ガ
- `BizHawk`：使用エミュレーター
- `early_room_exit_waittrim` など：試行や調整内容を区別する識別子
- `7371F`：入力ログのフレーム数
- `verified`：検証済みとして保存したファイルの識別子。個々の検証条件がすべて同じという意味ではありません。
- `.bk2`：BizHawkのムービー形式

`verified` がない旧版も保存しています。名前だけで再生可否を判断せず、通常は上の最新・最良版を選んでください。

## 最新版のファイル確認

- サイズ：2,107 bytes
- SHA-256：`908db87617db98fdfd444902816311a51c0a4f904bdb3b368d493ceefcdf0def`

入力ファイルの名前と内容は変更せず、フォルダだけ整理しました。以前のroot直下へのリンクをお使いの場合は、このページのリンクに更新してください。

