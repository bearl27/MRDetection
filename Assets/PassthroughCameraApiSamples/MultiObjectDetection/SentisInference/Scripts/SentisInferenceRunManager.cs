// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using Meta.XR.Samples;
using Unity.Sentis;
using UnityEngine;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    /// <summary>
    /// Sentis AIモデルを使用したオブジェクト検出推論エンジンの管理クラス
    /// Unity Sentisを活用してリアルタイムでWebCam映像からオブジェクトを検出・分類する
    /// </summary>
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class SentisInferenceRunManager : MonoBehaviour
    {
        [Header("Sentis Model config")]
        // AIモデルへの入力画像サイズ（通常は正方形で640x640ピクセル）
        [SerializeField] private Vector2Int m_inputSize = new(640, 640);
        // 推論実行バックエンド（CPU/GPU選択、パフォーマンスに影響）
        [SerializeField] private BackendType m_backend = BackendType.CPU;
        // 学習済みAIモデルのアセット（YOLO等のオブジェクト検出モデル）
        [SerializeField] private ModelAsset m_sentisModel;
        // フレーム毎に処理するニューラルネットワークレイヤー数（メインスレッドブロック防止）
        [SerializeField] private int m_layersPerFrame = 25;
        // オブジェクトクラス名のリストを含むテキストファイル
        [SerializeField] private TextAsset m_labelsAsset;
        // モデルの読み込み完了状態（他のコンポーネントが参照）
        public bool IsModelLoaded { get; private set; } = false;

        [Header("UI display references")]
        // 推論結果の UI 表示を管理するコンポーネント
        [SerializeField] private SentisInferenceUiManager m_uiInference;

        [Header("[Editor Only] Convert to Sentis")]
        // エディタ専用：ONNXモデルからSentis形式への変換用
        public ModelAsset OnnxModel;
        // IoU（交差比）閾値：重複した検出ボックスの統合基準
        [SerializeField, Range(0, 1)] private float m_iouThreshold = 0.6f;
        // 信頼度スコア閾値：この値以下の検出結果は除外
        [SerializeField, Range(0, 1)] private float m_scoreThreshold = 0.23f;
        [Space(40)]

        // Sentis推論エンジンのワーカーインスタンス
        private Worker m_engine;
        // 非同期推論処理のスケジューラー
        private IEnumerator m_schedule;
        // 推論処理の開始状態フラグ
        private bool m_started = false;
        // モデルへの入力テンソル（画像データ）
        private Tensor<float> m_input;
        // ロードされたAIモデル
        private Model m_model;
        // 推論結果のダウンロード処理ステート
        private int m_download_state = 0;
        // 推論結果の座標データテンソル
        private Tensor<float> m_output;
        // 推論結果のラベルIDデータテンソル
        private Tensor<int> m_labelIDs;
        // 座標データの非同期取得用テンソル
        private Tensor<float> m_pullOutput;
        // ラベルIDの非同期取得用テンソル
        private Tensor<int> m_pullLabelIDs;
        // 非同期データ取得の待機状態フラグ
        private bool m_isWaiting = false;

        #region Unity Functions
        /// <summary>
        /// 初期化処理：UIの準備完了を待ってからモデルを読み込み
        /// Sentisモデルの読み込みはメインスレッドをブロックするため、UI準備後に実行
        /// </summary>
        private IEnumerator Start()
        {
            // UIの準備が完了するまで待機（Sentisモデル読み込みでメインスレッドがブロックされるのを防ぐ）
            yield return new WaitForSeconds(0.05f);

            // UIマネージャーにラベル情報を設定
            m_uiInference.SetLabels(m_labelsAsset);
            // AIモデルを読み込み
            LoadModel();
        }

        /// <summary>
        /// 毎フレーム実行される更新処理
        /// 推論処理の進行状況を管理し、レイヤー単位で処理を分割してメインスレッドのブロックを防ぐ
        /// </summary>
        private void Update()
        {
            InferenceUpdate();
        }

        /// <summary>
        /// オブジェクト破棄時のクリーンアップ処理
        /// メモリリークを防ぐためリソースを適切に解放
        /// </summary>
        private void OnDestroy()
        {
            // 進行中のコルーチンを停止
            if (m_schedule != null)
            {
                StopCoroutine(m_schedule);
            }
            // Sentisテンソルとエンジンを破棄してメモリを解放
            m_input?.Dispose();
            m_engine?.Dispose();
        }
        #endregion

        #region Public Functions
        /// <summary>
        /// 指定されたテクスチャを使用して推論を開始
        /// WebCamテクスチャを受け取り、AIモデル用のテンソルに変換して推論をスケジュール
        /// </summary>
        /// <param name="targetTexture">推論対象のテクスチャ（WebCam映像など）</param>
        public void RunInference(Texture targetTexture)
        {
            // 推論が実行中でない場合のみ入力を準備
            if (!m_started)
            {
                // 前回の入力テンソルをクリーンアップ
                m_input?.Dispose();
                // カメラからのテクスチャが有効かチェック
                if (!targetTexture)
                {
                    return;
                }
                // 検出用キャプチャデータを更新
                m_uiInference.SetDetectionCapture(targetTexture);
                // テクスチャをSentisテンソルに変換し、推論をスケジュール
                m_input = TextureConverter.ToTensor(targetTexture, m_inputSize.x, m_inputSize.y, 3);
                m_schedule = m_engine.ScheduleIterable(m_input);
                m_download_state = 0;
                m_started = true;
            }
        }

        /// <summary>
        /// 推論処理の実行状態を確認
        /// </summary>
        /// <returns>推論が実行中の場合はtrue</returns>
        public bool IsRunning()
        {
            return m_started;
        }
        #endregion

        #region Inference Functions
        /// <summary>
        /// AIモデルの読み込みと初期化
        /// モデルをメモリに読み込み、推論エンジンを作成して準備状態にする
        /// </summary>
        private void LoadModel()
        {
            // Sentisモデルアセットからモデルを読み込み
            var model = ModelLoader.Load(m_sentisModel);
            Debug.Log($"Sentis model loaded correctly with iouThreshold: {m_iouThreshold} and scoreThreshold: {m_scoreThreshold}");

            // 指定されたバックエンドで推論エンジンを作成
            m_engine = new Worker(model, m_backend);

            // 空の入力でダミー推論を実行してモデルをメモリにロード（メインスレッドの一時停止を防ぐ）
            var input = TextureConverter.ToTensor(new Texture2D(m_inputSize.x, m_inputSize.y), m_inputSize.x, m_inputSize.y, 3);
            m_engine.Schedule(input);
            IsModelLoaded = true;
        }

        /// <summary>
        /// 推論処理の更新管理
        /// レイヤー単位で推論を実行し、メインスレッドのブロックを防ぐ
        /// </summary>
        private void InferenceUpdate()
        {
            // メインスレッドをブロックしないよう、レイヤー単位で推論を実行
            if (m_started)
            {
                try
                {
                    if (m_download_state == 0)
                    {
                        var it = 0;
                        // 指定されたレイヤー数まで処理を進める
                        while (m_schedule.MoveNext())
                        {
                            if (++it % m_layersPerFrame == 0)
                                return;
                        }
                        m_download_state = 1;
                    }
                    else
                    {
                        // 全レイヤーの処理が完了したら結果を取得
                        GetInferencesResults();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Sentis error: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 推論結果の座標データを非同期で取得
        /// </summary>
        private void PollRequestOuput()
        {
            // モデル出力0（座標データ）をSentisプルリクエストで取得
            m_pullOutput = m_engine.PeekOutput(0) as Tensor<float>;
            if (m_pullOutput.dataOnBackend != null)
            {
                m_pullOutput.ReadbackRequest();
                m_isWaiting = true;
            }
            else
            {
                Debug.LogError("Sentis: No data output m_output");
                m_download_state = 4;
            }
        }

        /// <summary>
        /// 推論結果のラベルIDデータを非同期で取得
        /// </summary>
        private void PollRequestLabelIDs()
        {
            // モデル出力1（ラベルIDデータ）をSentisプルリクエストで取得
            m_pullLabelIDs = m_engine.PeekOutput(1) as Tensor<int>;
            if (m_pullLabelIDs.dataOnBackend != null)
            {
                m_pullLabelIDs.ReadbackRequest();
                m_isWaiting = true;
            }
            else
            {
                Debug.LogError("Sentis: No data output m_labelIDs");
                m_download_state = 4;
            }
        }

        /// <summary>
        /// 推論結果の取得と処理
        /// 座標データとラベルデータを順次取得し、UIに描画するためのデータを準備
        /// </summary>
        private void GetInferencesResults()
        {
            // メインスレッドをブロックしないよう、異なるフレームで各出力を取得
            switch (m_download_state)
            {
                case 1:
                    if (!m_isWaiting)
                    {
                        // 座標データの取得リクエストを開始
                        PollRequestOuput();
                    }
                    else
                    {
                        // 座標データの取得完了をチェック
                        if (m_pullOutput.IsReadbackRequestDone())
                        {
                            m_output = m_pullOutput.ReadbackAndClone();
                            m_isWaiting = false;

                            if (m_output.shape[0] > 0)
                            {
                                Debug.Log("Sentis: m_output ready");
                                m_download_state = 2;
                            }
                            else
                            {
                                Debug.LogError("Sentis: m_output empty");
                                m_download_state = 4;
                            }
                        }
                    }
                    break;
                case 2:
                    if (!m_isWaiting)
                    {
                        // ラベルIDデータの取得リクエストを開始
                        PollRequestLabelIDs();
                    }
                    else
                    {
                        // ラベルIDデータの取得完了をチェック
                        if (m_pullLabelIDs.IsReadbackRequestDone())
                        {
                            m_labelIDs = m_pullLabelIDs.ReadbackAndClone();
                            m_isWaiting = false;

                            if (m_labelIDs.shape[0] > 0)
                            {
                                Debug.Log("Sentis: m_labelIDs ready");
                                m_download_state = 3;
                            }
                            else
                            {
                                Debug.LogError("Sentis: m_labelIDs empty");
                                m_download_state = 4;
                            }
                        }
                    }
                    break;
                case 3:
                    // 取得したデータでUIバウンディングボックスを描画
                    m_uiInference.DrawUIBoxes(m_output, m_labelIDs, m_inputSize.x, m_inputSize.y);
                    m_download_state = 5;
                    break;
                case 4:
                    // エラー発生時の処理
                    m_uiInference.OnObjectDetectionError();
                    m_download_state = 5;
                    break;
                case 5:
                    // 推論処理の完了とリソースクリーンアップ
                    m_download_state++;
                    m_started = false;
                    m_output?.Dispose();
                    m_labelIDs?.Dispose();
                    break;
            }
        }
        #endregion
    }
}
