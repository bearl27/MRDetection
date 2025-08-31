// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    /// <summary>
    /// 検出されたオブジェクトに配置される3Dマーカーのアニメーション管理クラス
    /// マーカーの回転アニメーションとテキストラベルの表示を制御する
    /// </summary>
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class DetectionSpawnMarkerAnim : MonoBehaviour
    {
        [Header("アニメーション設定")]
        // マーカーの各軸での回転速度（度/秒）
        [SerializeField] private Vector3 m_anglesSpeed = new(20.0f, 40.0f, 60.0f);

        [Header("3Dモデル参照")]
        // 回転アニメーションを適用する3Dモデルのトランスフォーム
        [SerializeField] private Transform m_model;

        [Header("テキスト表示")]
        // オブジェクトクラス名を表示するテキストメッシュ
        [SerializeField] private TextMesh m_textModel;
        // テキスト表示用のトランスフォーム（カメラ向きに回転）
        [SerializeField] private Transform m_textEntity;

        // 現在の回転角度を保持（X, Y, Z軸）
        private Vector3 m_angles;
        // VRカメラリグの参照（テキストをカメラに向けるため）
        private OVRCameraRig m_camera;

        /// <summary>
        /// 毎フレーム実行される更新処理
        /// マーカーの回転アニメーションとテキストのビルボード効果を制御
        /// </summary>
        private void Update()
        {
            // 各軸の回転角度を時間経過に基づいて更新
            m_angles.x = AddAngle(m_angles.x, m_anglesSpeed.x * Time.deltaTime);
            m_angles.y = AddAngle(m_angles.y, m_anglesSpeed.y * Time.deltaTime);
            m_angles.z = AddAngle(m_angles.z, m_anglesSpeed.z * Time.deltaTime);

            // 計算された角度をマーカーモデルに適用
            m_model.rotation = Quaternion.Euler(m_angles);

            // VRカメラリグの参照が未取得の場合は検索
            if (!m_camera)
            {
                m_camera = FindFirstObjectByType<OVRCameraRig>();
            }
            else
            {
                // テキストラベルを常にカメラの方向に向ける（ビルボード効果）
                m_textEntity.gameObject.transform.LookAt(m_camera.centerEyeAnchor);
            }
        }

        /// <summary>
        /// 角度値を安全に加算し、0-360度の範囲内に正規化する
        /// </summary>
        /// <param name="value">現在の角度値</param>
        /// <param name="toAdd">追加する角度値</param>
        /// <returns>正規化された角度値（0-360度）</returns>
        private float AddAngle(float value, float toAdd)
        {
            value += toAdd;

            // 360度を超えた場合は0度にリセット
            if (value > 360.0f)
            {
                value -= 360.0f;
            }

            // 0度未満の場合は360度から減算
            if (value < 0.0f)
            {
                value = 360.0f - value;
            }

            return value;
        }

        /// <summary>
        /// YOLOモデルによって検出されたオブジェクトのクラス名を設定
        /// マーカーのテキストラベルに表示される
        /// </summary>
        /// <param name="name">設定するクラス名（例: "person", "car", "bottle"など）</param>
        public void SetYoloClassName(string name)
        {
            m_textModel.text = name;
        }

        /// <summary>
        /// 現在設定されているYOLOクラス名を取得
        /// 重複チェックなどで使用される
        /// </summary>
        /// <returns>現在のクラス名</returns>
        public string GetYoloClassName()
        {
            return m_textModel.text;
        }
    }
}
