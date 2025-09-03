// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace PassthroughCameraSamples.MultiObjectDetection
{
    /// <summary>
    /// 環境レイキャスト機能を管理するクラス
    /// Meta Quest デバイスの空間認識機能を使用して、現実世界の表面にオブジェクトを配置する
    /// </summary>
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class EnvironmentRayCastSampleManager : MonoBehaviour
    {
        // 空間認識機能を使用するためのAndroid権限識別子
        private const string SPATIALPERMISSION = "com.oculus.permission.USE_SCENE";

        // Meta Quest の環境レイキャスト機能を提供するマネージャー
        [SerializeField] private EnvironmentRaycastManager m_raycastManager;

        /// <summary>
        /// 初期化処理：環境レイキャスト機能のサポート状況を確認
        /// Meta Quest デバイスで空間認識機能が利用可能かどうかをチェック
        /// </summary>
        private void Start()
        {
            // 環境レイキャスト機能がサポートされているかどうかを確認
            if (!EnvironmentRaycastManager.IsSupported)
            {
                Debug.LogError("EnvironmentRaycastManager is not supported: please read the official documentation to get more details. (https://developers.meta.com/horizon/documentation/unity/unity-depthapi-overview/)");
            }
        }

        /// <summary>
        /// 空間認識機能の使用権限を確認
        /// Android環境では実際の権限をチェックし、その他の環境では常にtrueを返す
        /// </summary>
        /// <returns>権限が許可されている場合はtrue、そうでなければfalse</returns>
        public bool HasScenePermission()
        {
#if UNITY_ANDROID
            // Android環境では実際の権限状態をチェック
            return Permission.HasUserAuthorizedPermission(SPATIALPERMISSION);
#else
            // Android以外の環境（エディタなど）では常に許可されているとみなす
            return true;
#endif
        }

        /// <summary>
        /// スクリーン座標からレイキャストを実行してオブジェクト配置位置を決定
        /// 現実世界の表面（壁、床、机など）との交点を計算して配置可能な座標を返す
        /// </summary>
        /// <param name="ray">スクリーン座標から変換されたレイ情報</param>
        /// <returns>配置可能な3D座標（交点が見つからない場合はnull）</returns>
        public Vector3? PlaceGameObjectByScreenPos(Ray ray)
        {
            // 環境レイキャスト機能がサポートされているかチェック
            if (EnvironmentRaycastManager.IsSupported)
            {
                // レイキャストを実行して現実世界の表面との交点を検索
                if (m_raycastManager.Raycast(ray, out var hitInfo))
                {
                    // 交点が見つかった場合、その座標を返す
                    return hitInfo.point;
                }
                else
                {
                    // 交点が見つからなかった場合（空中を指している等）
                    Debug.Log("RaycastManager failed");
                    return null;
                }
            }
            else
            {
                // 環境レイキャスト機能がサポートされていない場合
                Debug.LogError("EnvironmentRaycastManager is not supported");
                return null;
            }
        }
    }
}
