// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Assertions;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    /// <summary>
    /// 検出されたオブジェクトのUI表示キャンバス管理クラス
    /// カメラ映像に合わせてUIキャンバスの位置・サイズを動的に調整し、
    /// 現実世界のカメラ視野角に基づいた正確な表示を実現
    /// </summary>
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class SentisObjectDetectedUiManager : MonoBehaviour
    {
        // WebCamテクスチャ管理コンポーネント（カメラパラメータ取得用）
        [SerializeField] private WebCamTextureManager m_webCamTextureManager;
        // 使用するカメラの目（左右どちらのカメラを使用するか）
        private PassthroughCameraEye CameraEye => m_webCamTextureManager.Eye;
        // 要求するカメラ解像度
        private Vector2Int CameraResolution => m_webCamTextureManager.RequestedResolution;
        // 検出結果表示用のUIキャンバス
        [SerializeField] private GameObject m_detectionCanvas;
        // カメラからキャンバスまでの距離（メートル単位）
        [SerializeField] private float m_canvasDistance = 1f;

        // キャプチャ時のカメラポーズ情報
        private Pose m_captureCameraPose;
        // キャプチャ時のカメラ位置
        private Vector3 m_capturePosition;
        // キャプチャ時のカメラ回転
        private Quaternion m_captureRotation;

        /// <summary>
        /// 初期化処理：カメラ権限の確認とキャンバスサイズの調整
        /// カメラの視野角に基づいてUIキャンバスのスケールを計算し、
        /// 現実のカメラ映像と正確に一致するよう調整する
        /// </summary>
        private IEnumerator Start()
        {
            // WebCamTextureManager の必須チェック
            if (m_webCamTextureManager == null)
            {
                Debug.LogError($"PCA: {nameof(m_webCamTextureManager)} field is required "
                            + $"for the component {nameof(SentisObjectDetectedUiManager)} to operate properly");
                enabled = false;
                yield break;
            }

            // シーンではマネージャーを無効にし、必要な権限が付与された時のみ有効にする
            Assert.IsFalse(m_webCamTextureManager.enabled);
            while (PassthroughCameraPermissions.HasCameraPermission != true)
            {
                yield return null;
            }

            // 要求する解像度を設定してマネージャーを有効化
            m_webCamTextureManager.RequestedResolution = PassthroughCameraUtils.GetCameraIntrinsics(CameraEye).Resolution;
            m_webCamTextureManager.enabled = true;

            // カメラの視野角に基づいてキャンバスサイズを調整
            var cameraCanvasRectTransform = m_detectionCanvas.GetComponentInChildren<RectTransform>();

            // カメラの水平視野角を計算（左端と右端のレイを使用）
            var leftSidePointInCamera = PassthroughCameraUtils.ScreenPointToRayInCamera(CameraEye, new Vector2Int(0, CameraResolution.y / 2));
            var rightSidePointInCamera = PassthroughCameraUtils.ScreenPointToRayInCamera(CameraEye, new Vector2Int(CameraResolution.x, CameraResolution.y / 2));
            var horizontalFoVDegrees = Vector3.Angle(leftSidePointInCamera.direction, rightSidePointInCamera.direction);
            var horizontalFoVRadians = horizontalFoVDegrees / 180 * Math.PI;

            // 指定距離でのキャンバス幅を計算（三角関数を使用）
            var newCanvasWidthInMeters = 2 * m_canvasDistance * Math.Tan(horizontalFoVRadians / 2);
            var localScale = (float)(newCanvasWidthInMeters / cameraCanvasRectTransform.sizeDelta.x);
            cameraCanvasRectTransform.localScale = new Vector3(localScale, localScale, localScale);
        }

        /// <summary>
        /// キャンバス位置の更新
        /// キャプチャ時の位置と回転でキャンバスを配置
        /// </summary>
        public void UpdatePosition()
        {
            // キャンバスをカメラの前に配置
            m_detectionCanvas.transform.position = m_capturePosition;
            m_detectionCanvas.transform.rotation = m_captureRotation;
        }

        /// <summary>
        /// 現在のカメラ位置をキャプチャ
        /// 推論時のカメラポーズを記録し、キャンバスをカメラ前方に配置
        /// </summary>
        public void CapturePosition()
        {
            // カメラポーズをキャプチャし、キャンバスをカメラの前に配置
            m_captureCameraPose = PassthroughCameraUtils.GetCameraPoseInWorld(CameraEye);
            m_capturePosition = m_captureCameraPose.position + m_captureCameraPose.rotation * Vector3.forward * m_canvasDistance;
            m_captureRotation = Quaternion.Euler(0, m_captureCameraPose.rotation.eulerAngles.y, 0);
        }

        /// <summary>
        /// キャプチャされたカメラ位置を取得
        /// バウンディングボックスの向きを計算する際に使用
        /// </summary>
        /// <returns>キャプチャ時のカメラ位置</returns>
        public Vector3 GetCapturedCameraPosition()
        {
            return m_captureCameraPose.position;
        }

    }
}
