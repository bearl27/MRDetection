// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    /// <summary>
    /// オブジェクト検出アプリケーションのUIメニュー管理クラス
    /// 初期メニュー、権限チェック、検出情報表示などの全体的なUI状態を制御
    /// </summary>
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class DetectionUiMenuManager : MonoBehaviour
    {
        [Header("UI buttons")]
        // メニュー操作用のアクションボタン（デフォルト：Aボタン）
        [SerializeField] private OVRInput.RawButton m_actionButton = OVRInput.RawButton.A;

        [Header("UI elements ref.")]
        // ローディング画面のパネル
        [SerializeField] private GameObject m_loadingPanel;
        // 初期メニューのパネル
        [SerializeField] private GameObject m_initialPanel;
        // 権限がない場合のエラーパネル
        [SerializeField] private GameObject m_noPermissionPanel;
        // 検出情報を表示するテキストラベル
        [SerializeField] private Text m_labelInfromation;
        // ボタン操作時の効果音
        [SerializeField] private AudioSource m_buttonSound;

        // 入力受付の有効/無効状態を制御
        public bool IsInputActive { get; set; } = false;

        // ポーズ状態変更時に発生するイベント
        public UnityEvent<bool> OnPause;

        // 初期メニュー表示状態のフラグ
        private bool m_initialMenu;

        // 検出統計情報
        private int m_objectsDetected = 0;    // 現在検出中のオブジェクト数
        private int m_objectsIdentified = 0;  // 識別済みオブジェクトの累計数

        // アプリケーションの一時停止状態
        public bool IsPaused { get; private set; } = true;

        #region Unity Functions
        /// <summary>
        /// 開始処理：UIパネルの初期化とSentisモデル・権限の確認
        /// アプリケーション開始時の各種チェックを順次実行
        /// </summary>
        private IEnumerator Start()
        {
            // 初期UI状態の設定：ローディング画面のみ表示
            m_initialPanel.SetActive(false);
            m_noPermissionPanel.SetActive(false);
            m_loadingPanel.SetActive(true);

            // Sentisモデルの読み込み完了まで待機
            var sentisInference = FindFirstObjectByType<SentisInferenceRunManager>();
            while (!sentisInference.IsModelLoaded)
            {
                yield return null;
            }
            m_loadingPanel.SetActive(false);

            // カメラ権限の確認が完了するまで待機
            while (!PassthroughCameraPermissions.HasCameraPermission.HasValue)
            {
                yield return null;
            }

            // カメラ権限がない場合はエラーメニューを表示
            if (PassthroughCameraPermissions.HasCameraPermission == false)
            {
                OnNoPermissionMenu();
            }
        }

        /// <summary>
        /// 毎フレーム実行される更新処理
        /// アクティブなメニュー状態に応じた入力処理を実行
        /// </summary>
        private void Update()
        {
            // 入力が無効な場合は処理を停止
            if (!IsInputActive)
                return;

            // 初期メニューがアクティブな場合の更新処理
            if (m_initialMenu)
            {
                InitialMenuUpdate();
            }
        }
        #endregion

        #region Ui state: No permissions Menu
        /// <summary>
        /// 権限なしメニューの表示
        /// カメラまたはシーン権限が不足している場合のエラー画面を表示
        /// </summary>
        private void OnNoPermissionMenu()
        {
            m_initialMenu = false;
            IsPaused = true;
            m_initialPanel.SetActive(false);
            m_noPermissionPanel.SetActive(true);
        }
        #endregion

        #region Ui state: Initial Menu
        /// <summary>
        /// 初期メニューの表示制御
        /// シーン権限の状態に応じて適切なメニューを表示
        /// </summary>
        /// <param name="hasScenePermission">シーン認識権限の有無</param>
        public void OnInitialMenu(bool hasScenePermission)
        {
            // シーン認識権限がある場合
            if (hasScenePermission)
            {
                m_initialMenu = true;
                IsPaused = true;
                m_initialPanel.SetActive(true);
                m_noPermissionPanel.SetActive(false);
            }
            else
            {
                // 権限がない場合はエラーメニューを表示
                OnNoPermissionMenu();
            }
        }

        /// <summary>
        /// 初期メニューでの入力処理
        /// アクションボタンまたはEnterキーでアプリケーションを開始
        /// </summary>
        private void InitialMenuUpdate()
        {
            if (OVRInput.GetUp(m_actionButton) || Input.GetKey(KeyCode.Return))
            {
                // ボタン効果音を再生
                m_buttonSound?.Play();
                // ポーズ状態を解除してアプリケーションを開始
                OnPauseMenu(false);
            }
        }

        /// <summary>
        /// ポーズメニューの制御
        /// アプリケーションの一時停止/再開を管理
        /// </summary>
        /// <param name="visible">ポーズメニューの表示状態</param>
        private void OnPauseMenu(bool visible)
        {
            m_initialMenu = false;
            IsPaused = visible;

            // すべてのメニューパネルを非表示に
            m_initialPanel.SetActive(false);
            m_noPermissionPanel.SetActive(false);

            // ポーズ状態変更イベントを発生
            OnPause?.Invoke(visible);
        }
        #endregion

        #region Ui state: detection information
        /// <summary>
        /// 検出情報ラベルの更新
        /// Sentisバージョン、AIモデル、検出統計情報を表示
        /// </summary>
        private void UpdateLabelInformation()
        {
            m_labelInfromation.text = $"Unity Sentis version: 2.1.1\nAI model: Yolo\nDetecting objects: {m_objectsDetected}\nObjects identified: {m_objectsIdentified}";
        }

        /// <summary>
        /// 現在検出中のオブジェクト数を更新
        /// リアルタイムでの検出数をUIに反映
        /// </summary>
        /// <param name="objects">検出されたオブジェクト数</param>
        public void OnObjectsDetected(int objects)
        {
            m_objectsDetected = objects;
            UpdateLabelInformation();
        }

        /// <summary>
        /// 識別済みオブジェクト数を更新
        /// 3Dマーカーとして配置されたオブジェクトの累計数を管理
        /// </summary>
        /// <param name="objects">新たに識別されたオブジェクト数（負数の場合はリセット）</param>
        public void OnObjectsIndentified(int objects)
        {
            if (objects < 0)
            {
                // カウンターをリセット（トラッキング空間再センタリング時など）
                m_objectsIdentified = 0;
            }
            else
            {
                // 新たに識別されたオブジェクト数を累計に加算
                m_objectsIdentified += objects;
            }
            UpdateLabelInformation();
        }
        #endregion
    }
}
