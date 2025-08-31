// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    /// <summary>
    /// UIテキストの点滅アニメーション制御クラス
    /// 指定されたテキストラベルを一定間隔で点滅させる視覚効果を提供
    /// </summary>
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class DetectionUiBlinkText : MonoBehaviour
    {
        [Header("UI要素")]
        // 点滅させるテキストラベルの参照
        [SerializeField] private Text m_labelInfo;

        [Header("アニメーション設定")]
        // 点滅の間隔（秒）- 値が小さいほど高速で点滅
        [SerializeField] private float m_blinkSpeed = 0.2f;

        // 現在の点滅タイマー（内部制御用）
        private float m_blinkTime = 0.0f;
        // テキストの色情報を保持（透明度の制御に使用）
        private Color m_color;

        /// <summary>
        /// 初期化処理：テキストラベルの初期色を保存
        /// 点滅アニメーション開始前の元の色情報を記録
        /// </summary>
        private void Start()
        {
            m_color = m_labelInfo.color;
        }

        /// <summary>
        /// 毎フレーム後に実行される更新処理（LateUpdate）
        /// 点滅タイマーを更新し、指定間隔でテキストの透明度を切り替え
        /// </summary>
        private void LateUpdate()
        {
            // 点滅タイマーを時間経過で増加
            m_blinkTime += Time.deltaTime;

            // 指定された点滅間隔に達した場合
            if (m_blinkTime >= m_blinkSpeed)
            {
                // 透明度を0（透明）と1（不透明）で切り替え
                // 現在透明でない場合は透明に、透明の場合は不透明に変更
                m_color.a = m_color.a > 0f ? 0f : 1f;

                // 変更した色をテキストラベルに適用
                m_labelInfo.color = m_color;

                // タイマーをリセットして次の点滅サイクルを開始
                m_blinkTime = 0;
            }
        }
    }
}
