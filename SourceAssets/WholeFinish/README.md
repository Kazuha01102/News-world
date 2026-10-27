# 編集用データ

`WholeHomeFurniture.blend` の `Source_` コレクションは部品・モディファイアを残した編集用、`Export_` は変換を適用した書き出し用です。通常はベッドのSourceだけを表示します。別の家具を編集する場合はOutlinerで対応するSourceの表示を切り替えてください。

`make_whole_home.py` は既存 `../LivingFurniture/make_furniture.py` のベベル・クッション・メートルUV関数を再利用し、22種類の家具を生成します。開いているBlender内のデータを作り直すため、別の作品を開いた状態では実行しないでください。以前の家具・植物のblendファイルは上書きしません。

出力先は `Assets/NightHome/WholeFinish/Models`。家具メッシュだけを選択し、FBX Scale All、軸変換・スケールを適用して出力します。カメラ・ライト・参照オブジェクトは出力しません。Unity取り込み後は回転0・スケール1で実寸を確認しています。

布・木目・金属等はリビングの共有マテリアルを再利用しています。新規素材と床画像の作成、Prefab・Collider・配置は `Assets/NightHome/Editor/WholeHomeFinishBuilder.cs` に記録しました。スクリプトは既存シーンを二重に組まないよう適用済みチェックがあります。

作者側の形状集計は `mesh_stats.json`、Unity取り込み後の集計と確認結果は `Documentation/WholeFinish/Validation.txt`。新規モデル・模様は自作で、外部モデル・画像の購入や生成サービスの利用はありません。
