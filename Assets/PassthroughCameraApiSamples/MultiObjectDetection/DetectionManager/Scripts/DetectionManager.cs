// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    /// <summary>
    /// オブジェクト検出とマーカー配置を管理するメインクラス
    /// WebCamからの映像を解析し、検出されたオブジェクトに3Dマーカーを配置する
    /// </summary>
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class DetectionManager : MonoBehaviour
    {
        // WebCamテクスチャを管理するコンポーネント
        [SerializeField] private WebCamTextureManager m_webCamTextureManager;

        [Header("Controls configuration")]
        // マーカー配置用のアクションボタン（デフォルトはAボタン）
        [SerializeField] private OVRInput.RawButton m_actionButton = OVRInput.RawButton.A;

        [Header("Ui references")]
        // UIメニューを管理するコンポーネント
        [SerializeField] private DetectionUiMenuManager m_uiMenuManager;

        [Header("Placement configureation")]
        // 3Dマーカーとして生成するプレハブ
        [SerializeField] private GameObject m_spwanMarker;
        // 環境レイキャスト機能を管理するコンポーネント
        [SerializeField] private EnvironmentRayCastSampleManager m_environmentRaycast;
        // 同じオブジェクトのマーカー配置を防ぐための最小距離
        [SerializeField] private float m_spawnDistance = 0.25f;
        // マーカー配置時の効果音
        [SerializeField] private AudioSource m_placeSound;

        [Header("Sentis inference ref")]
        // Sentis推論エンジンのランナー
        [SerializeField] private SentisInferenceRunManager m_runInference;
        // Sentis推論結果のUI表示管理
        [SerializeField] private SentisInferenceUiManager m_uiInference;
        [Space(10)]
        // オブジェクト識別時に発生するイベント（検出されたオブジェクト数を通知）
        public UnityEvent<int> OnObjectsIdentified;

        // アプリケーションの一時停止状態
        private bool m_isPaused = true;
        // 生成された3Dマーカーのリスト
        private List<GameObject> m_spwanedEntities = new();
        // アプリケーションの開始状態
        private bool m_isStarted = false;
        // Sentisモデルの準備完了状態
        private bool m_isSentisReady = false;
        // ポーズメニューから戻った後のボタン入力遅延時間
        private float m_delayPauseBackTime = 0;

        #region Unity Functions
        /// <summary>
        /// 初期化処理：トラッキング空間の再センタリング時のコールバック登録
        /// </summary>
        private void Awake() => OVRManager.display.RecenteredPose += CleanMarkersCallBack;

        /// <summary>
        /// 開始処理：Sentisモデルの読み込み完了を待機
        /// </summary>
        private IEnumerator Start()
        {
            // Sentisモデルが読み込まれるまで待機
            var sentisInference = FindAnyObjectByType<SentisInferenceRunManager>();
            while (!sentisInference.IsModelLoaded)
            {
                yield return null;
            }
            m_isSentisReady = true;
        }

        /// <summary>
        /// 毎フレーム実行される更新処理
        /// オブジェクト検出とマーカー配置の制御を行う
        /// </summary>
        private void Update()
        {
            // WebCamTextureのCPU画像データの取得状態をチェック
            var hasWebCamTextureData = m_webCamTextureManager.WebCamTexture != null;

            if (!m_isStarted)
            {
                // 初期UIメニューの管理
                if (hasWebCamTextureData && m_isSentisReady)
                {
                    m_uiMenuManager.OnInitialMenu(m_environmentRaycast.HasScenePermission());
                    m_isStarted = true;
                }
            }
            else
            {
                // Aボタンを押して3Dマーカーを生成
                if (OVRInput.GetUp(m_actionButton) && m_delayPauseBackTime <= 0)
                {
                    SpwanCurrentDetectedObjects();
                }
                // ポーズメニューから戻った後のAボタンのクールダウン処理
                m_delayPauseBackTime -= Time.deltaTime;
                if (m_delayPauseBackTime <= 0)
                {
                    m_delayPauseBackTime = 0;
                }
            }

            // アプリが一時停止中または有効なWebCamTextureがない場合は推論を開始しない
            if (m_isPaused || !hasWebCamTextureData)
            {
                if (m_isPaused)
                {
                    // ポーズメニューから戻るためのAボタンの遅延時間を設定
                    m_delayPauseBackTime = 0.1f;
                }
                return;
            }

            // 現在の推論が終了したら新しい推論を実行
            if (!m_runInference.IsRunning())
            {
                m_runInference.RunInference(m_webCamTextureManager.WebCamTexture);
            }
        }
        #endregion

        #region Marker Functions
        /// <summary>
        /// トラッキング空間が再センタリングされた時に3Dマーカーをクリーンアップ
        /// ユーザーがヘッドセットの位置をリセットした場合に呼び出される
        /// </summary>
        private void CleanMarkersCallBack()
        {
            foreach (var e in m_spwanedEntities)
            {
                Destroy(e, 0.1f);
            }
            m_spwanedEntities.Clear();
            OnObjectsIdentified?.Invoke(-1);
        }

        /// <summary>
        /// 現在検出されているオブジェクトに対して3Dマーカーを生成
        /// 推論結果のボックス情報を基にワールド座標にマーカーを配置
        /// </summary>
        private void SpwanCurrentDetectedObjects()
        {
            var count = 0;
            // 検出されたすべてのボックスに対してマーカー配置を試行
            foreach (var box in m_uiInference.BoxDrawn)
            {
                if (PlaceMarkerUsingEnvironmentRaycast(box.WorldPos, box.ClassName))
                {
                    count++;
                }
            }
            if (count > 0)
            {
                // 新しいマーカーが配置された場合に効果音を再生
                m_placeSound.Play();
            }
            OnObjectsIdentified?.Invoke(count);
        }

        /// <summary>
        /// 環境レイキャストを使用してマーカーを配置
        /// 同じ位置に同じクラスのマーカーが既に存在する場合は配置しない
        /// </summary>
        /// <param name="position">配置する3D座標</param>
        /// <param name="className">検出されたオブジェクトのクラス名</param>
        /// <returns>新しいマーカーが配置された場合はtrue</returns>
        private bool PlaceMarkerUsingEnvironmentRaycast(Vector3? position, string className)
        {
            // 位置が有効かチェック
            if (!position.HasValue)
            {
                return false;
            }

            // 以前に同じオブジェクトを生成したかチェック
            var existMarker = false;
            foreach (var e in m_spwanedEntities)
            {
                var markerClass = e.GetComponent<DetectionSpawnMarkerAnim>();
                if (markerClass)
                {
                    var dist = Vector3.Distance(e.transform.position, position.Value);
                    // 最小距離内に同じクラスのマーカーが存在するかチェック
                    if (dist < m_spawnDistance && markerClass.GetYoloClassName() == className)
                    {
                        existMarker = true;
                        break;
                    }
                }
            }

            if (!existMarker)
            {
                // ビジュアルマーカーを生成
                var eMarker = Instantiate(m_spwanMarker);
                m_spwanedEntities.Add(eMarker);

                // 実世界の座標でマーカーの位置と回転を更新
                eMarker.transform.SetPositionAndRotation(position.Value, Quaternion.identity);
                eMarker.GetComponent<DetectionSpawnMarkerAnim>().SetYoloClassName(className);
            }

            return !existMarker;
        }
        #endregion

        #region Public Functions
        /// <summary>
        /// ポーズメニューがアクティブな時に検出ロジックを一時停止
        /// UIメニューが表示されている間は推論処理を停止する
        /// </summary>
        /// <param name="pause">一時停止状態（true: 停止, false: 再開）</param>
        public void OnPause(bool pause)
        {
            m_isPaused = pause;
        }
        #endregion
    }
}
