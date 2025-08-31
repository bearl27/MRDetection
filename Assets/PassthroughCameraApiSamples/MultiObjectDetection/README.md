メインの GameObject 構成

1. [BuildingBlock] Camera Rig (ID: 1629220518)
   役割: VR カメラリグの中心的な GameObject
   コンポーネント:
   OVRCameraRig: Oculus VR カメラシステムを管理
   OVRManager: VR 機能の総合管理（トラッキング、描画設定等）
   OVRHeadsetEmulator: エディタでの VR ヘッドセット操作をシミュレート
   子オブジェクト: TrackingSpace
2. TrackingSpace (ID: 632844191)
   役割: VR トラッキング空間の基準点
   子オブジェクト:
   LeftEyeAnchor (ID: 897444003): 左目カメラ（無効化状態）
   CenterEyeAnchor (ID: 1744462752): 中央カメラ（メインカメラ、AudioListener 付き）
   RightEyeAnchor (ID: 1959261769): 右目カメラ（無効化状態）
   TrackerAnchor (ID: 733445207): 追加トラッカー用
   LeftHandAnchor / RightHandAnchor: 手の位置追跡用
3. [BuildingBlock] Passthrough (ID: 875434744)
   役割: Meta Quest のパススルー機能を管理
   コンポーネント:
   OVRPassthroughLayer: パススルー映像の表示制御
   カラーマッピング、透明度、エッジレンダリング設定
4. DetectionManagerPrefab (プレハブインスタンス)
   役割: オブジェクト検出システムの中央管理
   機能: 検出結果の統合、UI 管理、環境レイキャスト連携
5. SentisInferenceManagerPrefab (プレハブインスタンス)
   役割: Unity Sentis を使用した AI 推論エンジン
   機能: 機械学習モデルによるオブジェクト検出・識別
6. EnvironmentRaycastPrefab (プレハブインスタンス)
   役割: 環境への 3D レイキャスト機能
   機能: 現実空間での位置特定、オブジェクト配置支援
7. WebCamTextureManagerPrefab (プレハブインスタンス)
   役割: ウェブカメラ映像の管理
   設定: 解像度 800x600、右目カメラ使用
8. DetectionUiMenuPrefab (プレハブインスタンス)
   役割: 検出結果表示用 UI
   配置: CenterEyeAnchor の子要素として 0.65m 前方に配置
   サイズ: 600x600 の CanvasUI
9. EventSystem (ID: 1487398119)
   役割: UI 入力イベントシステム
   コンポーネント: EventSystem、StandaloneInputModule
10. Metadata (ID: 1884619604)
    役割: シーンメタデータ管理
    VR ハンドトラッキング関連オブジェクト
    左手系統:
    LeftHandAnchor: 左手の基準位置
    LeftControllerAnchor: 左コントローラー位置
    LeftControllerInHandAnchor: 手に持ったコントローラー位置
    LeftHandOnControllerAnchor: コントローラー上の手の位置
    LeftHandAnchorDetached: 分離状態の左手位置
    右手系統:
    RightHandAnchor: 右手の基準位置
    RightControllerAnchor: 右コントローラー位置
    RightControllerInHandAnchor: 手に持ったコントローラー位置
    RightHandOnControllerAnchor: コントローラー上の手の位置
    RightHandAnchorDetached: 分離状態の右手位置
    システムの相互連携
    このシーンは、VR パススルー環境でリアルタイムオブジェクト検出を行うシステムとして設計されています：

WebCamTextureManager がカメラ映像を取得
SentisInferenceManager が AI 推論でオブジェクトを検出
DetectionManager が検出結果を統合・管理
EnvironmentRaycast が 3D 空間での位置を特定
DetectionUiMenu が結果をユーザーに表示
Passthrough が現実世界を背景として表示
この構成により、Mixed Reality 環境でのインテリジェントなオブジェクト認識・可視化システムが実現されています。
