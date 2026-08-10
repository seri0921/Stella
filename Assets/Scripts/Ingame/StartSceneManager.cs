using System.Collections;
using Microsoft.Azure.Kinect.BodyTracking;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartSceneManager : MonoBehaviour
{
    [Header("遷移先")]
    [SerializeField] private string gameSceneName = "Ingame";

    [Header("タッチ対象")]
    [SerializeField] private Collider startTarget;
    [SerializeField] private float touchRadius = 0.18f;

    [Header("Kinect")]
    [SerializeField] private TrackerHandler trackerHandler;

    [Header("マウス確認")]
    [SerializeField] private bool enableMouseDebug = true;
    [SerializeField] private Camera mainCamera;

    [Header("暗転")]
    [SerializeField] private float fadeDuration = 0.8f;

    private CanvasGroup fadeCanvasGroup;
    private bool isTransitioning;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (trackerHandler == null)
        {
            trackerHandler = FindObjectOfType<TrackerHandler>();
        }

        fadeCanvasGroup = CreateFadeCanvas();
    }

    private void Update()
    {
        if (isTransitioning || startTarget == null) return;

        if (ProcessKinectTouch() || ProcessMouseClick())
        {
            StartGame();
        }
    }

    // 外部から直接呼びたい場合にも使える開始処理です。
    public void StartGame()
    {
        if (isTransitioning) return;

        StartCoroutine(FadeAndLoadScene());
    }

    private bool ProcessKinectTouch()
    {
        if (trackerHandler == null) return false;

        return IsHandTouchingTarget(JointId.HandRight) || IsHandTouchingTarget(JointId.HandLeft);
    }

    private bool IsHandTouchingTarget(JointId handJoint)
    {
        Vector3 handPosition = trackerHandler.GetJointWorldPosition(handJoint);
        if (handPosition.sqrMagnitude < 0.001f) return false;

        Vector3 closestPoint = startTarget.ClosestPoint(handPosition);
        return Vector3.Distance(handPosition, closestPoint) <= touchRadius;
    }

    private bool ProcessMouseClick()
    {
        if (!enableMouseDebug || mainCamera == null || !Input.GetMouseButtonDown(0)) return false;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return false;

        return hit.collider == startTarget || hit.collider.transform.IsChildOf(startTarget.transform);
    }

    private IEnumerator FadeAndLoadScene()
    {
        isTransitioning = true;
        yield return FadeToBlack();

        Debug.Log($"ゲームを開始します。シーン {gameSceneName} をロード中...");
        SceneManager.LoadScene(gameSceneName);
    }

    private IEnumerator FadeToBlack()
    {
        if (fadeCanvasGroup == null) yield break;

        fadeCanvasGroup.blocksRaycasts = true;

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }

    private CanvasGroup CreateFadeCanvas()
    {
        GameObject canvasObject = new GameObject("FadeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        CanvasGroup canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        GameObject imageObject = new GameObject("FadeImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = Color.black;

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        return canvasGroup;
    }
}
