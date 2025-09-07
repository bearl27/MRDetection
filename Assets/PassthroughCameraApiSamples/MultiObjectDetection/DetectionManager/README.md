# 物体検出システム（DetectionManager）についてのまとめ

## 概要
DetectionManagerは、Meta Quest デバイス上でリアルタイムオブジェクト検出を実現するMixed Realityアプリケーションの中核システムです。Unity Sentis AIフレームワークを使用してWebCam映像からオブジェクトを検出し、現実世界の3D空間にマーカーを配置します。

## システム構成

### Scripts（スクリプト）
| ファイル名 | 役割 | 主要機能 |
|------------|------|----------|
| `DetectionManager.cs` | メイン制御クラス | オブジェクト検出とマーカー配置の全体制御、入力処理、推論管理 |
| `DetectionSpawnMarkerAnim.cs` | 3Dマーカーアニメーション | マーカーの回転アニメーション、ビルボード効果、ラベル表示 |
| `DetectionUiMenuManager.cs` | UIメニュー管理 | 初期メニュー、権限チェック、検出統計情報の表示制御 |
| `DetectionUiBlinkText.cs` | テキスト点滅効果 | UIテキストの点滅アニメーション（注意喚起用） |
| `DetectionUiTextWritter.cs` | タイプライター効果 | テキストの1文字ずつ表示アニメーション |

### Prefabs（プレハブ）
| ファイル名 | 用途 | 説明 |
|------------|------|------|
| `DetectionManagerPrefab.prefab` | システム本体 | 検出システム全体を統合したメインプレハブ |
| `DetectionSpawnMarker.prefab` | 3Dマーカー | 検出されたオブジェクトに配置される視覚的マーカー |
| `DetectionUiMenuPrefab.prefab` | UIメニュー | 初期設定とメニュー表示用のUIプレハブ |

### Materials（マテリアル）
| ファイル名 | 用途 | 説明 |
|------------|------|------|
| `DetectionSpawnMarkerMat.mat` | マーカー本体 | 3Dマーカーのメインマテリアル |
| `DetectionSpawnMarkerPointMat.mat` | マーカー中心点 | マーカーの中心を示すポイントマテリアル |

### Audio（音声）
| ファイル名 | 用途 | 説明 |
|------------|------|------|
| `Detection_Ui_Accept.mp3` | 操作確認音 | ボタン押下やマーカー配置時の効果音 |
| `Detection_Ui_Ambient.mp3` | 環境音 | タイプライター効果などの背景音 |

### Textures（テクスチャ）
| ファイル名 | 用途 | 説明 |
|------------|------|------|
| `DetectionSpawnMarkerTexture.png` | マーカー外観 | 3Dマーカーの視覚的デザイン |

## 主要パラメーター

### DetectionManager
| パラメーター | 型 | デフォルト値 | 説明 |
|--------------|-----|-------------|------|
| `m_actionButton` | OVRInput.RawButton | A | マーカー配置用のコントローラーボタン |
| `m_spawnDistance` | float | 0.25f | 同一オブジェクトの重複配置を防ぐ最小距離（メートル） |

### DetectionSpawnMarkerAnim
| パラメーター | 型 | デフォルト値 | 説明 |
|--------------|-----|-------------|------|
| `m_anglesSpeed` | Vector3 | (20, 40, 60) | X/Y/Z軸の回転速度（度/秒） |

### DetectionUiBlinkText
| パラメーター | 型 | デフォルト値 | 説明 |
|--------------|-----|-------------|------|
| `m_blinkSpeed` | float | 0.2f | 点滅の間隔（秒） |

### DetectionUiTextWritter
| パラメーター | 型 | デフォルト値 | 説明 |
|--------------|-----|-------------|------|
| `m_writtingSpeed` | float | 0.00015f | 1文字表示する間隔（秒） |
| `m_writtingInfoPause` | float | 0.005f | コロン文字後の追加待機時間 |
