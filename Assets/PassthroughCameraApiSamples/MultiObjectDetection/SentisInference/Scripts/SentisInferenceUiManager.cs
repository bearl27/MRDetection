// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Meta.XR.Samples;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    /// <summary>
    /// Sentis推論結果のUI表示管理クラス
    /// AIモデルによる検出結果をバウンディングボックスとして3D空間に描画し、
    /// 現実世界の座標系との変換も行う
    /// </summary>
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class SentisInferenceUiManager : MonoBehaviour
    {
        [Header("Placement configureation")]
        // 環境レイキャスト機能（現実世界への座標変換用）
        [SerializeField] private EnvironmentRayCastSampleManager m_environmentRaycast;
        // WebCamテクスチャ管理（カメラパラメータ取得用）
        [SerializeField] private WebCamTextureManager m_webCamTextureManager;
        // 使用するカメラの目（左右どちらのカメラを使用するか）
        private PassthroughCameraEye CameraEye => m_webCamTextureManager.Eye;

        [Header("UI display references")]
        // 検出されたオブジェクトのUI表示を管理するキャンバス
        [SerializeField] private SentisObjectDetectedUiManager m_detectionCanvas;
        // カメラ映像を表示するRawImageコンポーネント
        [SerializeField] private RawImage m_displayImage;
        // バウンディングボックス用のスプライト画像
        [SerializeField] private Sprite m_boxTexture;
        // バウンディングボックスの色
        [SerializeField] private Color m_boxColor;
        // ラベルテキスト用のフォント
        [SerializeField] private Font m_font;
        // ラベルテキストの色
        [SerializeField] private Color m_fontColor;
        // ラベルテキストのフォントサイズ
        [SerializeField] private int m_fontSize = 80;
        [Space(10)]
        // オブジェクト検出時に発生するイベント（検出数を通知）
        public UnityEvent<int> OnObjectsDetected;

        // 描画されたバウンディングボックスの情報リスト
        public List<BoundingBox> BoxDrawn = new();

        // オブジェクトクラス名のラベル配列
        private string[] m_labels;
        // バウンディングボックスUIのオブジェクトプール
        private List<GameObject> m_boxPool = new();
        // バウンディングボックス表示位置の基準Transform
        private Transform m_displayLocation;

        /// <summary>
        /// バウンディングボックスの情報を格納する構造体
        /// 2D座標、3D座標、ラベル情報などを含む
        /// </summary>
        public struct BoundingBox
        {
            public float CenterX;      // 中心X座標（ピクセル）
            public float CenterY;      // 中心Y座標（ピクセル）
            public float Width;        // 幅（ピクセル）
            public float Height;       // 高さ（ピクセル）
            public string Label;       // 表示用ラベル文字列
            public Vector3? WorldPos;  // 3D世界座標（null可能）
            public string ClassName;   // オブジェクトクラス名
        }

        #region Unity Functions
        /// <summary>
        /// 初期化処理：表示位置の基準となるTransformを設定
        /// </summary>
        private void Start()
        {
            m_displayLocation = m_displayImage.transform;
        }
        #endregion

        #region Detection Functions
        /// <summary>
        /// オブジェクト検出エラー時の処理
        /// 現在のバウンディングボックスをクリアし、検出数を0にリセット
        /// </summary>
        public void OnObjectDetectionError()
        {
            // 現在のバウンディングボックスをクリア
            ClearAnnotations();

            // 検出オブジェクト数を0に設定
            OnObjectsDetected?.Invoke(0);
        }
        #endregion

        #region BoundingBoxes functions
        /// <summary>
        /// オブジェクトクラス名のラベルデータを設定
        /// AIモデルが出力するクラスIDを人間が読める名前に変換するため
        /// </summary>
        /// <param name="labelsAsset">クラス名リストを含むテキストアセット</param>
        public void SetLabels(TextAsset labelsAsset)
        {
            // ニューラルネットワークのラベルを解析
            m_labels = labelsAsset.text.Split('\n');
        }

        /// <summary>
        /// 検出用キャプチャ画像を設定
        /// UI表示用の画像を更新し、キャンバスの位置情報をキャプチャ
        /// </summary>
        /// <param name="image">表示する画像テクスチャ</param>
        public void SetDetectionCapture(Texture image)
        {
            m_displayImage.texture = image;
            m_detectionCanvas.CapturePosition();
        }

        /// <summary>
        /// AIモデルの推論結果からUIバウンディングボックスを描画
        /// 2D検出結果を3D空間座標に変換し、バウンディングボックスとして表示
        /// </summary>
        /// <param name="output">推論結果の座標データテンソル</param>
        /// <param name="labelIDs">推論結果のラベルIDテンソル</param>
        /// <param name="imageWidth">入力画像の幅</param>
        /// <param name="imageHeight">入力画像の高さ</param>
        public void DrawUIBoxes(Tensor<float> output, Tensor<int> labelIDs, float imageWidth, float imageHeight)
        {
            // キャンバス位置を更新
            m_detectionCanvas.UpdatePosition();

            // 現在のバウンディングボックスをクリア
            ClearAnnotations();

            // 表示サイズとモデル入力サイズの比率を計算
            var displayWidth = m_displayImage.rectTransform.rect.width;
            var displayHeight = m_displayImage.rectTransform.rect.height;

            var scaleX = displayWidth / imageWidth;
            var scaleY = displayHeight / imageHeight;

            var halfWidth = displayWidth / 2;
            var halfHeight = displayHeight / 2;

            // 検出されたボックス数を取得
            var boxesFound = output.shape[0];
            if (boxesFound <= 0)
            {
                OnObjectsDetected?.Invoke(0);
                return;
            }
            // 最大200個までのボックスを処理（パフォーマンス考慮）
            var maxBoxes = Mathf.Min(boxesFound, 200);

            OnObjectsDetected?.Invoke(maxBoxes);

            // カメラの内部パラメータを取得
            var intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(CameraEye);
            var camRes = intrinsics.Resolution;

            // バウンディングボックスを描画
            for (var n = 0; n < maxBoxes; n++)
            {
                // バウンディングボックスの中心座標を取得
                var centerX = output[n, 0] * scaleX - halfWidth;
                var centerY = output[n, 1] * scaleY - halfHeight;
                var perX = (centerX + halfWidth) / displayWidth;
                var perY = (centerY + halfHeight) / displayHeight;

                // オブジェクトクラス名を取得
                var classname = m_labels[labelIDs[n]].Replace(" ", "_");

                // Depth Raycastを使用して3Dマーカーのワールド位置を取得
                var centerPixel = new Vector2Int(Mathf.RoundToInt(perX * camRes.x), Mathf.RoundToInt((1.0f - perY) * camRes.y));
                var ray = PassthroughCameraUtils.ScreenPointToRayInWorld(CameraEye, centerPixel);
                var worldPos = m_environmentRaycast.PlaceGameObjectByScreenPos(ray);

                // 新しいバウンディングボックスを作成
                var box = new BoundingBox
                {
                    CenterX = centerX,
                    CenterY = centerY,
                    ClassName = classname,
                    Width = output[n, 2] * scaleX,
                    Height = output[n, 3] * scaleY,
                    Label = $"Id: {n} Class: {classname} Center (px): {(int)centerX},{(int)centerY} Center (%): {perX:0.00},{perY:0.00}",
                    WorldPos = worldPos,
                };

                // ボックスリストに追加
                BoxDrawn.Add(box);

                // 2Dボックスを描画
                DrawBox(box, n);
            }
        }

        /// <summary>
        /// すべてのバウンディングボックス表示をクリア
        /// オブジェクトプールを使用してメモリ効率を向上
        /// </summary>
        private void ClearAnnotations()
        {
            foreach (var box in m_boxPool)
            {
                box?.SetActive(false);
            }
            BoxDrawn.Clear();
        }

        /// <summary>
        /// 個別のバウンディングボックスを描画
        /// オブジェクトプールから再利用するか新規作成してボックスを表示
        /// </summary>
        /// <param name="box">描画するバウンディングボックス情報</param>
        /// <param name="id">ボックスのID（プール管理用）</param>
        private void DrawBox(BoundingBox box, int id)
        {
            // バウンディングボックスのグラフィックを作成またはプールから取得
            GameObject panel;
            if (id < m_boxPool.Count)
            {
                panel = m_boxPool[id];
                if (panel == null)
                {
                    panel = CreateNewBox(m_boxColor);
                }
                else
                {
                    panel.SetActive(true);
                }
            }
            else
            {
                panel = CreateNewBox(m_boxColor);
            }

            // ボックスの位置を設定
            panel.transform.localPosition = new Vector3(box.CenterX, -box.CenterY, box.WorldPos.HasValue ? box.WorldPos.Value.z : 0.0f);
            // ボックスの回転を設定（カメラの方向を向く）
            panel.transform.rotation = Quaternion.LookRotation(panel.transform.position - m_detectionCanvas.GetCapturedCameraPosition());
            // ボックスのサイズを設定
            var rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(box.Width, box.Height);
            // ラベルテキストを設定
            var label = panel.GetComponentInChildren<Text>();
            label.text = box.Label;
            label.fontSize = 12;
        }

        /// <summary>
        /// 新しいバウンディングボックスUIオブジェクトを作成
        /// Image コンポーネントとText ラベルを含む完全なUIを構築
        /// </summary>
        /// <param name="color">ボックスの色</param>
        /// <returns>作成されたバウンディングボックスGameObject</returns>
        private GameObject CreateNewBox(Color color)
        {
            // ボックスを作成してImageを設定
            var panel = new GameObject("ObjectBox");
            _ = panel.AddComponent<CanvasRenderer>();
            var img = panel.AddComponent<Image>();
            img.color = color;
            img.sprite = m_boxTexture;
            img.type = Image.Type.Sliced;
            img.fillCenter = false;
            panel.transform.SetParent(m_displayLocation, false);

            // ラベルを作成
            var text = new GameObject("ObjectLabel");
            _ = text.AddComponent<CanvasRenderer>();
            text.transform.SetParent(panel.transform, false);
            var txt = text.AddComponent<Text>();
            txt.font = m_font;
            txt.color = m_fontColor;
            txt.fontSize = m_fontSize;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;

            // ラベルのレイアウト設定
            var rt2 = text.GetComponent<RectTransform>();
            rt2.offsetMin = new Vector2(20, rt2.offsetMin.y);
            rt2.offsetMax = new Vector2(0, rt2.offsetMax.y);
            rt2.offsetMin = new Vector2(rt2.offsetMin.x, 0);
            rt2.offsetMax = new Vector2(rt2.offsetMax.x, 30);
            rt2.anchorMin = new Vector2(0, 0);
            rt2.anchorMax = new Vector2(1, 1);

            // プールに追加
            m_boxPool.Add(panel);
            return panel;
        }
        #endregion
    }
}
