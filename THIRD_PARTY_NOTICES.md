# Third-party assets and restoration

確認日：2026-10-27 JST。

| 対象 | 版・出典 | このリポジトリでの扱い |
|---|---|---|
| QvPen | 3.3.15、[作者公式配布](https://booth.pm/ja/items/1555789)、[公式リポジトリ](https://github.com/ureishi/QvPen) | パッケージ本体・生成済みバイナリ・配布ZIPは同梱しない。シーンのPrefab参照とVPM指定を保存 |
| VRChat SDK Base / Worlds | 3.10.5、[公式パッケージ](https://vrchat.github.io/packages/index.json)、[SDK利用規約](https://hello.vrchat.com/legal/sdk) | 本体とSDK生成ヘルパーを同梱せず、VPM管理ファイルから復元 |
| Unity UPM packages | `Packages/manifest.json` / `packages-lock.json` に記録 | 本体を同梱せずUnity Package Managerから取得 |
| MCP for Unity | `com.coplaydev.unity-mcp`、[開発元](https://github.com/CoplayDev/unity-mcp) | Git参照とロック情報のみ。開発時の操作用 |
| Zen Kaku Gothic New Regular | [Google Fonts](https://github.com/google/fonts/tree/main/ofl/zenkakugothicnew) | フォント本体・`.meta`とSIL Open Font Licenseを保存 |

## QvPen

公式配布ページは、個人利用でVRChatワールドに組み込むことを認める一方、配布ファイルを不特定の人が別サイトからダウンロードできる状態にしないよう求めています。本リポジトリではパッケージ本体を再配布しません。生成済みUdonプログラムも除外します。

ALCOMへ登録する作者公式VPMリポジトリ：`https://vpm.ureishi.net/repos.json`。`net.ureishi.qvpen` **3.3.15** を導入してください。元の配布物の作者・協力者・各依存素材の表記を保持し、それぞれの条件を確認してください。

## フォント

Copyright 2022 The Zen Kaku Gothic Project Authors。
フォントはSIL Open Font License 1.1に従います。全文は `Assets/NightHome/Fonts/OFL.txt` に保存しています。フォントのライセンスと著作権表記を削除しないでください。

## 独自に作成したもの

簡易建築・家具、木目テクスチャ、チャイム、ホーム用スクリプトと記録文書は今回の制作物です。プロジェクト全体に一律のオープンソースライセンスを設定してはいません。

## ニュース映像

ニュース映像・音声・期限付き配信URL・映像入りの実機スクリーンショットはリポジトリへ同梱しません。テレビは公開された公式配信URLを参照し、各クライアントが配信元から再生する構成です。配信元やプラットフォームの条件を別途確認してください。


## 今回のリビング仕上げ素材

全室仕上げの追加分（`SourceAssets/WholeFinish`、`Assets/NightHome/WholeFinish`）は自作の家具メッシュ・床画像・マテリアル・Prefabです。以前の自作リビングの生成関数と共有素材を再利用しています。外部モデル・写真・有料素材・生成サービスは使用していません。既存フォント・QvPen・SDKの条件は変更せず、そのまま保持しています。

観葉植物・壁・建具の追加分（`SourceAssets/PlantsWalls`、`Assets/NightHome/PlantsWalls`）も自作です。メッシュ、葉脈・土・壁紙の画像、頂点カラー対応の葉Shader、夜空Shader、小規模LightingTrialを含み、外部画像・モデルや有料生成サービスは使っていません。素材の出所と保存内容は `Documentation/PlantsWalls/README.md` に記録しています。

`SourceAssets/LivingFurniture` のblend・Python、`Assets/NightHome/LivingFinish` のFBX・布/木目PNG・マテリアル・Prefabは本制作で自作しました。外部モデル・画像の購入やダウンロード、生成サービスの課金はありません。Blender/Unityの標準機能を使用。既存QvPenやSDKなど第三者パッケージの利用条件は上記から変更していません。
