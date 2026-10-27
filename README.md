# NightHome / News-world

PC版VRChat向け、最大16人を想定した「夜の自宅」ホームワールドの制作途中スナップショットです。完成版ではありません。

制作対象はALCOMで管理する **NightHome** です。以前の `NightHomeWorld` は初期作業用の保管コピーで、今後の変更先にはしません。制作記録は `Documentation` にまとめます。

## 現在の内容

- リビング、ベッド付き寝室、4〜6人目安の個室、廊下と入口。
- ソファ、ローテーブル、ラグ、本棚、マグ、観葉植物などの簡易家具。
- 木目床、暖色照明、窓外の住宅街の夜景。
- 既存のAVProニューステレビとURL手動入力、音量・消音・再生・停止・再接続操作。
- QvPen 3.3.15の公式Prefab参照と返却台・描画スペース。本体はリポジトリに同梱しません。
- JST（日本時間 UTC+09:00）の入退室ログ。直近32件をインスタンス内で同期する初期実装。
- 入退室で異なる短い音、本人だけの音量・ON/OFF設定。
- 個室の内外で声を分離し、室内ではテレビ・入退室音を消すワールド側の音声制御。
- Useした本人だけが切り替えるローカルミラー。初期OFF。

個室の音声制御は完全なプライバシーを保証しません。個人ミュート設定は変更しません。

## 確認できたこと

- Unity / Worlds SDKでのコンパイル、参照、レイヤー・Collision Matrix、Builder検証を確認。
- **ローカルBuild & Test成功。PC版VRChatのホーム内でニュース映像を確認済み。**
- Unity ClientSimでは音量・消音、再生／停止状態、再接続要求、個室入退室時のテレビ・チャイムの消音／復帰、ミラーOFF→ON→OFFを確認。

Unity内での確認を実機・複数人の確認としては扱いません。

## 未確認・未完成

- **音声の実聴、実機での各テレビ操作、VR操作、QvPen描画・復帰、複数人同期、16人負荷は未確認。**
- 途中参加者へのログ・描線復元、所有権移譲、個室の双方向音声分離、他者のミラー表示への無影響は実機複数人試験が必要。
- **ニュースURLの外部自動取得は未完成。** 現在は登録済みの固定URLと手動入力を利用。予備URLは同局の固定入口で、別局の予備チャンネルとして検証したものではありません。
- **SDK情報表示処理のNullReferenceExceptionの恒久対処は未完了。** ビルド中のUIリソース取得失敗を記録しています。ローカル書き出し・起動は成功しています。SDKソースは変更していません。
- 家具・内装の作り込み、照明ベイク、VRと16人での負荷調整は今後の作業。

## 使用環境とシーン

| 項目 | バージョン / 場所 |
|---|---|
| Unity | 2022.3.22f1 |
| VRChat SDK Base / Worlds | 3.10.5 / 3.10.5 |
| QvPen | 3.3.15 |
| 対象 | Standalone Windows 64-bit / PC版VRChat |
| 制作シーン | `Assets/NightHome/NightHome_Blockout.unity` |
| ニュース比較用 | `Assets/NightHome/NewsPlaybackTest.unity` |
| 最新の制作記録 | [Documentation/CurrentStatus.md](Documentation/CurrentStatus.md) |
| 平面図 | [Documentation/FloorPlan.svg](Documentation/FloorPlan.svg) |

## クローン後の復元

1. このリポジトリを任意のフォルダー（`NightHome`推奨）へクローンします。
2. ALCOMの「既存のプロジェクトを追加」でそのフォルダーを選択します。
3. QvPen公式の[VPM導入ページ](https://vpm.ureishi.net/install)からリポジトリを登録します。登録URLは `https://vpm.ureishi.net/repos.json`。
4. ALCOMのパッケージ管理で、`Packages/vpm-manifest.json` に記録した **Base / Worlds 3.10.5、QvPen 3.3.15** を復元します。自動的に最新版へ更新せず、指定版を選びます。アバター用SDKは入れません。
5. Unity **2022.3.22f1** で開き、Unityパッケージの取得、C# / Udonコンパイル、SDK生成データの再作成が終わるまで待ちます。MCPなどのUPM依存関係は`Packages/manifest.json`と`packages-lock.json`を参照します。
6. `Assets/NightHome/NightHome_Blockout.unity` を開き、欠落スクリプトや参照、SDK Builderの検証を確認します。必要に応じて `VRChat SDK > Udon Sharp > Refresh All UdonSharp Assets` を実行し、生成済みUdonデータを再作成します。
7. `VRChat SDK > Show Control Panel` で自分のアカウントへログインし、Builderの **Build & Test** を使用します。今回VRChatへのアップロードは行っていません。

第三者パッケージと生成データを再取得するため、クローン直後のままオフラインで開ける完全同梱プロジェクトではありません。クローン先での復元ビルドはこのコミット時点では未確認です。

## 保存するもの・除外するもの

`Assets/NightHome` とその`.meta`、独自スクリプト・シーン・素材、`Packages`の管理ファイル、`ProjectSettings`、`Documentation`を保存します。

UnityのLibrary・Temp・Logs・UserSettings、ローカルバックアップ、ビルド成果物、生成済みUdonプログラム、SDK生成ヘルパー、第三者パッケージ本体、開発用DLL、認証情報は除外します。実機スクリーンショットや生ログ・音声デバイス観測値はローカル記録として保持し、この公開リポジトリには含めません。`Documentation/CurrentStatus.md`内のそれらのファイル名はローカル記録への案内です。

導入アセットの条件と復元元は [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) を参照してください。このREADMEは第三者アセットの再配布許諾を付与するものではありません。

## 履歴について

ALCOMの制作フォルダーにはGit履歴がありませんでした。初回コミットは保存時点の現状をまとめたものです。過去の途中状態を表す架空のコミットは作成していません。
