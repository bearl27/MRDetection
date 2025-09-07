# Sentis推論システム（SentisInference）についてのまとめ

## 概要
SentisInferenceは、Unity Sentis AIフレームワークを使用してリアルタイムオブジェクト検出を実現するAI推論エンジンです。YOLOv9モデルを活用してWebCam映像からオブジェクトを検出し、バウンディングボックスとして結果を表示します。Meta Quest デバイス上で高性能な機械学習推論を実行します。

## システム構成

### Scripts（スクリプト）
| ファイル名 | 役割 | 主要機能 |
|------------|------|----------|
| `SentisInferenceRunManager.cs` | AI推論エンジン | モデル読み込み、非同期推論実行、結果データ取得 |
| `SentisInferenceUiManager.cs` | 推論結果UI表示 | バウンディングボックス描画、3D座標変換、オブジェクトプール管理 |
| `SentisObjectDetectedUiManager.cs` | UIキャンバス管理 | カメラ視野角調整、キャンバス位置制御、権限管理 |

### Editor（エディタツール）
| ファイル名 | 用途 | 説明 |
|------------|------|------|
| `SentisModelEditorConverter.cs` | モデル変換ツール | ONNXモデルをSentis形式に変換するエディタ拡張 |

### Model（AIモデル）
| ファイル名 | 形式 | 説明 |
|------------|------|------|
| `yolov9onnx.onnx` | ONNX | 元となるYOLOv9オブジェクト検出モデル |
| `yolov9sentis.sentis` | Sentis | Unity Sentis用に最適化されたモデル |
| `SentisYoloClasses.txt` | テキスト | YOLO検出可能オブジェクトクラス名リスト（80クラス） |

### Prefabs（プレハブ）
| ファイル名 | 用途 | 説明 |
|------------|------|------|
| `SentisInferenceManagerPrefab.prefab` | 推論システム本体 | Sentis推論機能を統合したメインプレハブ |

## 主要パラメーター

### SentisInferenceRunManager
| パラメーター | 型 | デフォルト値 | 説明 |
|--------------|-----|-------------|------|
| `m_inputSize` | Vector2Int | (640, 640) | AIモデルへの入力画像サイズ（ピクセル） |
| `m_backend` | BackendType | CPU | 推論実行バックエンド（CPU/GPU選択） |
| `m_layersPerFrame` | int | 25 | フレーム毎に処理するニューラルネットワークレイヤー数 |
| `m_iouThreshold` | float | 0.6f | IoU閾値（重複ボックス統合基準） |
| `m_scoreThreshold` | float | 0.23f | 信頼度スコア閾値（検出結果フィルタリング） |

### SentisInferenceUiManager
| パラメーター | 型 | デフォルト値 | 説明 |
|--------------|-----|-------------|------|
| `m_boxColor` | Color | - | バウンディングボックスの色 |
| `m_fontColor` | Color | - | ラベルテキストの色 |
| `m_fontSize` | int | 80 | ラベルテキストのフォントサイズ |

### SentisObjectDetectedUiManager
| パラメーター | 型 | デフォルト値 | 説明 |
|--------------|-----|-------------|------|
| `m_canvasDistance` | float | 1f | カメラからUIキャンバスまでの距離（メートル） |

## YOLOv9 検出可能オブジェクトクラス

### 人・動物（Person & Animals）
```
person, bicycle, car, motorcycle, airplane, bus, train, truck, boat,
traffic light, fire hydrant, stop sign, parking meter, bench, bird,
cat, dog, horse, sheep, cow, elephant, bear, zebra, giraffe
```

### 家具・日用品（Furniture & Household）
```
backpack, umbrella, handbag, tie, suitcase, frisbee, skis, snowboard,
sports ball, kite, baseball bat, baseball glove, skateboard, surfboard,
tennis racket, bottle, wine glass, cup, fork, knife, spoon, bowl,
banana, apple, sandwich, orange, broccoli, carrot, hot dog, pizza,
donut, cake, chair, couch, potted plant, bed, dining table, toilet,
tv, laptop, mouse, remote, keyboard, cell phone, microwave, oven,
toaster, sink, refrigerator, book, clock, vase, scissors, teddy bear,
hair drier, toothbrush
```

## 技術仕様

### AI モデル仕様
- **アーキテクチャ**: YOLOv9（You Only Look Once version 9）
- **検出クラス数**: 80クラス（COCO Dataset準拠）
- **入力解像度**: 640×640ピクセル
- **出力形式**: バウンディングボックス座標 + クラス確率

### パフォーマンス最適化
- **レイヤー分割推論**: メインスレッドブロック防止
- **非同期データ取得**: GPU→CPU データ転送の最適化
- **オブジェクトプール**: UIエレメントの効率的再利用
- **フレーム制限**: 最大200ボックス/フレーム

### メモリ管理
- **テンソル自動解放**: 推論完了時の自動メモリクリーンアップ
- **エンジン破棄処理**: アプリ終了時のリソース解放
- **入力テンソル管理**: フレーム間での適切なメモリ管理

## 推論フロー

### 1. 初期化フェーズ
1. **モデル読み込み**: Sentisモデルアセットの読み込み
2. **エンジン作成**: 指定バックエンドでWorkerインスタンス生成
3. **ダミー推論**: モデルのメモリ初期化（初回ブロック防止）

### 2. 推論実行フェーズ
1. **テクスチャ変換**: WebCamTextureをSentisテンソルに変換
2. **推論スケジュール**: レイヤー単位での非同期推論実行
3. **進行管理**: フレーム毎のレイヤー処理数制御

### 3. 結果取得フェーズ
1. **座標データ取得**: 推論結果の座標情報を非同期で取得
2. **ラベルデータ取得**: クラスID情報を非同期で取得
3. **UI描画**: バウンディングボックスとラベルの表示

## UI表示システム

### バウンディングボックス機能
- **動的生成**: 検出結果に応じたボックス数の調整
- **色分け**: オブジェクトクラスに応じた色付け
- **リサイズ**: カメラ解像度に応じた適切なスケーリング
- **3D配置**: 現実世界座標系への正確な位置合わせ

### キャンバス管理
- **視野角計算**: カメラFoVに基づいた自動サイズ調整
- **位置追跡**: カメラポーズに連動したキャンバス配置
- **権限管理**: カメラアクセス権限の動的チェック

## 開発者向け情報

### モデル変換手順
1. **ONNX準備**: YOLOv9のONNXモデルを用意
2. **エディタ変換**: `SentisModelEditorConverter`で変換実行
3. **最適化**: Sentis形式での最適化適用
4. **検証**: 変換後モデルの動作確認

### カスタマイズポイント
- **検出クラス**: `SentisYoloClasses.txt`の編集
- **推論精度**: 閾値パラメーターの調整
- **パフォーマンス**: バックエンドとレイヤー数の最適化
- **UI外観**: ボックス色とフォントのカスタマイズ

### デバッグ機能
- **推論状態ログ**: 各フェーズの実行状況表示
- **エラーハンドリング**: 推論失敗時の適切な処理
- **パフォーマンス監視**: フレームレートと処理時間の測定