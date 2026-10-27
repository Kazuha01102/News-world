# ソファ・寝具の輪郭修正（2026-10-27）

ALCOM管理の `NightHome`、制作シーン `Assets/NightHome/NightHome_Blockout.unity` が対象。Blender MCP（Blender 5.2.3）でメッシュを生成・保存し、Unity MCP（Unity 2022.3.22f1）で読み込み、比較撮影、参照・通路検査、保存を行った。

## 原因と修正

以前は共通の超楕円体生成で、座面・背面・枕・マットレスを均等に丸めていた。ソファの肘掛けも幅150mmに対して65mmのベベルがあり、芯のある構造より膨張した塊に見えていた。

- 座面：四角いフォーム芯、ほぼ平らな上面、1〜3mmの浅い沈み。側面は平らにし、角だけ丸めた。
- 背面：角を残す矩形、下部の絞りと圧縮、控えめなたるみ。既存の傾斜を保持。
- 肘掛け：内部フレームに沿う側面と薄い上部パッドを別構造にした。
- 縫い目：太い丸チューブから幅の狭い平らな帯へ変更。側面の縫い目付近をわずかに締めた。
- 置きクッション：四角い角、薄い縫製縁、中央の詰め物を別の形として表現。支持面に触れる部分だけ圧縮。
- マットレス：上面・側面は平面、小さな角丸。枕は矩形の布グリッドで中央の厚みと薄い縁を作った。
- 掛け布団：マットレス上の平坦部、端の垂れ、足元の角から生じる大きな折れ、頭側の折り返しを分けた。全面のランダムなしわは使用していない。

座面1個と枕1個の旧・新モデルを先に比較し、面のつなぎ目による陰影を修正してから全体へ展開。クロスシミュレーションは使用せず、編集可能な静的メッシュと生成スクリプトを保存した。リビングと個室の全ソファ、ベッド、床クッションを更新。

## 比較画像

固定の撮影ステージで、カメラ、ライト、環境光、露出条件を揃えている。実際の室内照明は変更していない。

| 対象 | 変更前 | 形状のみ変更（素材は旧設定） | 最終（光沢・ノーマルも調整） |
| --- | --- | --- | --- |
| ソファ斜め | [前](Before_Sofa_Angle.png) | [形状](Geometry_Sofa_Angle.png) | [後](After_Sofa_Angle.png) |
| ソファ正面 | [前](Before_Sofa_Front.png) | [形状](Geometry_Sofa_Front.png) | [後](After_Sofa_Front.png) |
| ソファ近景 | [前](Before_Sofa_Near.png) | [形状](Geometry_Sofa_Near.png) | [後](After_Sofa_Near.png) |
| ベッド斜め | [前](Before_Bed_Angle.png) | [形状](Geometry_Bed_Angle.png) | [後](After_Bed_Angle.png) |
| ベッド正面 | [前](Before_Bed_Front.png) | [形状](Geometry_Bed_Front.png) | [後](After_Bed_Front.png) |
| 枕近景 | [前](Before_Bed_Near.png) | [形状](Geometry_Bed_Near.png) | [後](After_Bed_Near.png) |

試作は `Prototype_SeatBefore/SeatAfter/PillowBefore/PillowAfter_{Front,Angle,Near}.png` の12枚。生成時の旧形状もスクリプト内に明示しており、架空の過去履歴は追加していない。

## 素材・保存先

布は既存の自作織り目テクスチャを継続使用。メートル基準のUVと4回/mのタイル設定を維持し、織り目は約2mm。GrayFabricのSmoothnessは0.04、Normal強度0.10、BedLinenは0.03 / 0.08。新規購入・外部素材はなし。

- 共通形状：`SourceAssets/Upholstery/cloth_shapes.py`
- 試作：同フォルダの `prototype_cloth.py` / `ClothPrototype.blend`、`Assets/NightHome/UpholsteryStudy/Models/`
- 本番：`SourceAssets/LivingFurniture/` と `SourceAssets/WholeFinish/` のスクリプト、編集用 `.blend`、統計JSON
- FBX：`Assets/NightHome/LivingFinish/Models/` と `WholeFinish/Models/` の既存6ファイルを同じGUIDで更新
- 素材：既存GrayFabric / SeamGray / BedLinenを更新
- Prefab：既存Prefabのメッシュ参照を確認・再保存。GUIDと参照が同じなのでPrefab・シーンのYAML差分は不要だった。シーンも保存済み。

各メッシュはモディファイアを評価して書き出し、カメラ・ライトを含まない。縫い目の細い帯だけは意図した開放面。細い帯を含めて法線を再計算し、フォーム芯にはWeighted Normalを使用。生成元を残したままFBX化している。最大ファイルは約1.22MBの `.blend` で、Git LFSは不要。

## 確認結果

| モデル | 三角形数 | マテリアル枠 |
| --- | ---: | ---: |
| Sofa320 / Sofa300 | 各12,868 | 各3 |
| QuietSofa300 | 12,868 | 4 |
| QuietSofa240 | 10,520 | 4 |
| BedroomBed（フレーム込み） | 9,302 | 4 |
| FloorCushion | 1,136 | 1 |

Unityで正面・斜め・近景の輪郭、縫製縁、接触、陰影を目視確認し、試作時の面の折れを修正した。素材UVの尺度は保持。追加ライト、ベイク、影の変更は行っていない。

- C#エラー0。Udon全プログラムをコンパイル。既存のPipelineManager旧APIに関する警告1件あり。
- SDK Builderの `NoGuiErrorsOrIssues() = true`。
- 配置・有効状態・Collider・Udonオブジェクト参照の1,199行を変更前と比較して完全一致。Udonは114個、Missing Script / Missing active meshとも0。
- スポーン、定員16、個室音声境界、ミラー初期OFF、Useスイッチへの到達を確認。半径0.22mのカプセルを既存3通路に沿って検査し、障害サンプル0。
- Collider・着座マーカーの変更なし。ソファ幅・奥行きと家具配置は保持。布の輪郭修正によりソファ上端は約2.8mm低くなった。
- 詳細は [Validation.txt](Validation.txt)、[全体の通路検査](../WholeFinish/Validation.txt)、前後のProtectedStateファイルを参照。

今回の状態ではBuild & Testを再実行していない。VRChat実機での外観・音声・VR操作・QvPen描画・複数人同期・16人負荷は未確認。後日、既存の [部屋巡回チェックリスト](../WholeFinish/WALKTHROUGH.md) に加え、ソファと枕の近景、ベッド脇の移動、布の細かい模様のちらつきを確認する。ニュースURL自動取得、既存SDK例外の恒久対処は引き続き保留。VRChatへのアップロードは行っていない。
