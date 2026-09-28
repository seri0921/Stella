using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 最終フレームの表示を、シーンの破棄や次の動画の準備から独立させる。
public class VideoTransitionOverlay : MonoBehaviour
{
    private RenderTexture snapshot;
    private RawImage image;
    private float fadeDuration;
    private bool waitingForScene;

    public static void Show(RawImage source, float duration, bool waitForScene)
    {
        GameObject root = new GameObject("VideoTransitionOverlay", typeof(RectTransform), typeof(Canvas));
        DontDestroyOnLoad(root);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        VideoTransitionOverlay overlay = root.AddComponent<VideoTransitionOverlay>();
        overlay.fadeDuration = duration;
        overlay.waitingForScene = waitForScene;
        overlay.snapshot = new RenderTexture(source.texture.width, source.texture.height, 0, RenderTextureFormat.ARGB32);
        overlay.snapshot.Create();
        Graphics.Blit(source.texture, overlay.snapshot);

        GameObject imageObject = new GameObject("LastVideoFrame", typeof(RectTransform), typeof(RawImage));
        imageObject.transform.SetParent(root.transform, false);
        overlay.image = imageObject.GetComponent<RawImage>();
        overlay.image.texture = overlay.snapshot;
        overlay.image.color = source.color;
        overlay.image.uvRect = source.uvRect;
        overlay.image.raycastTarget = false;

        // 元のUIと同じ画面上の領域を使い、切り替え時のサイズの跳ねを防ぐ。
        Vector3[] corners = new Vector3[4];
        source.rectTransform.GetWorldCorners(corners);
        Canvas sourceCanvas = source.canvas;
        Camera camera = sourceCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : sourceCanvas.worldCamera;
        Vector2 lower = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 upper = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        RectTransform rect = overlay.image.rectTransform;
        rect.anchorMin = new Vector2(lower.x / Screen.width, lower.y / Screen.height);
        rect.anchorMax = new Vector2(upper.x / Screen.width, upper.y / Screen.height);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        if (waitForScene) SceneManager.sceneLoaded += overlay.OnSceneLoaded;
        overlay.StartCoroutine(overlay.FadeOut());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Endgame") waitingForScene = false;
    }

    private IEnumerator FadeOut()
    {
        while (waitingForScene) yield return null;
        // 次フェーズのUI更新と次シーンのStartを待ってから下の画面を見せる。
        yield return null;
        float elapsed = 0f;
        Color color = image.color;
        float initialAlpha = color.a;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            color.a = initialAlpha * (1f - Mathf.Clamp01(elapsed / fadeDuration));
            image.color = color;
            yield return null;
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (snapshot != null)
        {
            snapshot.Release();
            Destroy(snapshot);
        }
    }
}
