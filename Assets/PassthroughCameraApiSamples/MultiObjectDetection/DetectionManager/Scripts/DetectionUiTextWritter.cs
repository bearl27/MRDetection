// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    /// <summary>
    /// タイプライター効果を実現するUIテキスト表示クラス
    /// 文字を一文字ずつ順番に表示し、レトロなコンピュータ風の演出を提供
    /// </summary>
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class DetectionUiTextWritter : MonoBehaviour
    {
        [Header("UI要素")]
        // タイプライター効果を適用するテキストラベル
        [SerializeField] private Text m_labelInfo;

        [Header("アニメーション設定")]
        // 1文字表示する間隔（秒）- 値が小さいほど高速で文字が表示
        [SerializeField] private float m_writtingSpeed = 0.00015f;
        // コロン（:）文字の後の追加待機時間（情報項目の区切り強調用）
        [SerializeField] private float m_writtingInfoPause = 0.005f;

        [Header("効果音")]
        // タイプライター効果の効果音（キーボードタイピング音など）
        [SerializeField] private AudioSource m_writtingSound;

        // タイプライター効果の開始/終了時に発生するイベント
        public UnityEvent OnStartWritting;
        public UnityEvent OnFinishWritting;

        // 内部制御用の変数
        private float m_writtingTime = 0;         // 文字表示タイマー
        private bool m_isWritting = false;        // 現在書き込み中かどうかのフラグ
        private string m_currentInfo = "";        // 表示対象の完全なテキスト
        private int m_currentInfoIndex = 0;       // 現在表示中の文字インデックス

        /// <summary>
        /// 開始時の初期化：タイプライター効果の設定を実行
        /// </summary>
        private void Start()
        {
            SetWrittingConfig();
        }

        /// <summary>
        /// オブジェクトが有効になった時の処理
        /// タイプライター効果を再開始
        /// </summary>
        private void OnEnable()
        {
            SetWrittingConfig();
        }

        /// <summary>
        /// オブジェクトが無効になった時の処理
        /// タイプライター効果を停止し、完全なテキストを即座に表示
        /// </summary>
        private void OnDisable()
        {
            m_isWritting = false;
            m_writtingTime = 0;
            m_currentInfoIndex = 0;
            m_labelInfo.text = m_currentInfo;
        }

        /// <summary>
        /// 毎フレーム後に実行される更新処理
        /// タイプライター効果のタイミング制御と文字表示を管理
        /// </summary>
        private void LateUpdate()
        {
            // タイプライター効果が実行中の場合
            if (m_isWritting)
            {
                // 文字表示タイマーが0以下になった場合（次の文字を表示するタイミング）
                if (m_writtingTime <= 0)
                {
                    // 次回の文字表示タイミングを設定
                    m_writtingTime = m_writtingSpeed;

                    // タイピング効果音を再生
                    m_writtingSound?.Play();

                    // 現在のインデックス位置の文字を取得
                    var nextChar = m_currentInfo.Substring(m_currentInfoIndex, 1);
                    // テキストラベルに文字を追加
                    m_labelInfo.text += nextChar;

                    // コロン文字の場合は追加の待機時間を設定（情報項目の区切り強調）
                    if (nextChar == ":")
                    {
                        m_writtingTime += m_writtingInfoPause;
                    }

                    // 次の文字へインデックスを進める
                    m_currentInfoIndex++;

                    // すべての文字を表示完了した場合
                    if (m_currentInfoIndex >= m_currentInfo.Length)
                    {
                        m_isWritting = false;
                        OnFinishWritting?.Invoke();
                    }
                }
                else
                {
                    // タイマーを減算
                    m_writtingTime -= Time.deltaTime;
                }
            }
        }

        /// <summary>
        /// タイプライター効果の初期設定
        /// 表示対象テキストを保存し、効果を開始
        /// </summary>
        private void SetWrittingConfig()
        {
            // 既に書き込み中でない場合のみ実行
            if (!m_isWritting)
            {
                m_isWritting = true;
                // 現在のテキストラベルの内容を保存
                m_currentInfo = m_labelInfo.text;
                // テキストラベルを空にしてタイプライター効果を開始
                m_labelInfo.text = "";
                OnStartWritting?.Invoke();
            }
        }
    }
}
