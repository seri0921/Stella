using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoTransitionManager : MonoBehaviour
{
    [Header("Video Playerの設定")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoRawImage;

    [Header("フェーズごとの動画クリップ")]
    [SerializeField] private VideoClip transition1Clip;
    [SerializeField] private VideoClip transition2Clip;

    [Header("切り替えの設定")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.25f;
    [SerializeField, Min(1f)] private float preparationTimeout = 15f;

    private IngameGameManager gameManager;
    private Coroutine playbackRoutine;
    private bool firstFrameReady;
    private bool playbackFinished;
    private bool videoFailed;
    private VideoClip preparingClip;

    private void Awake()
    {
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.Stop();
        }
        SetVideoVisibility(false);
    }

    private void Start()
    {
        if (videoPlayer != null)
        {
            videoPlayer.frameReady += OnFrameReady;
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.errorReceived += OnVideoError;
        }
        else
        {
            Debug.LogError("VideoPlayer コンポーネントが見つかりません。", this);
        }

        gameManager = IngameGameManager.Instance;
        if (gameManager != null)
        {
            gameManager.OnPhaseChanged += HandlePhaseChanged;
            // Startの実行順によって初期フェーズの通知を取り逃しても準備する。
            HandlePhaseChanged(gameManager.CurrentPhase);
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.frameReady -= OnFrameReady;
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.sendFrameReadyEvents = false;
        }
        if (gameManager != null) gameManager.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void HandlePhaseChanged(IngameGameManager.GamePhase phase)
    {
        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }
        SetVideoVisibility(false);
        if (videoPlayer != null) videoPlayer.sendFrameReadyEvents = false;

        switch (phase)
        {
            case IngameGameManager.GamePhase.Navigation:
            case IngameGameManager.GamePhase.EatingSnucks:
                PrepareVideo(transition1Clip);
                break;
            case IngameGameManager.GamePhase.CleaningTrash:
                PrepareVideo(transition2Clip);
                break;
            case IngameGameManager.GamePhase.VideoTransition1:
                playbackRoutine = StartCoroutine(PlayTransitionVideo(transition1Clip));
                break;
            case IngameGameManager.GamePhase.VideoTransition2:
                playbackRoutine = StartCoroutine(PlayTransitionVideo(transition2Clip));
                break;
            default:
                if (videoPlayer != null) videoPlayer.Stop();
                preparingClip = null;
                break;
        }
    }

    private void PrepareVideo(VideoClip clip)
    {
        if (videoPlayer == null || clip == null) return;
        // 同じ動画の準備をやり直すと、事前準備の効果が失われる。
        if (preparingClip == clip && !videoFailed) return;
        videoPlayer.Stop();
        videoPlayer.clip = clip;
        preparingClip = clip;
        videoFailed = false;
        videoPlayer.Prepare();
    }

    private IEnumerator PlayTransitionVideo(VideoClip clip)
    {
        // Coroutineの参照が保存されてから完了通知を行う。
        yield return null;
        firstFrameReady = false;
        playbackFinished = false;
        if (videoPlayer == null || clip == null)
        {
            Debug.LogWarning("動画を再生できないため、次のフェーズへ進みます。", this);
            yield return new WaitForSecondsRealtime(0.5f);
            CompleteTransition();
            yield break;
        }

        PrepareVideo(clip);
        float deadline = Time.realtimeSinceStartup + preparationTimeout;
        while (!videoPlayer.isPrepared && !videoFailed && Time.realtimeSinceStartup < deadline)
            yield return null;

        if (videoFailed || !videoPlayer.isPrepared)
        {
            SkipFailedVideo();
            yield break;
        }

        videoPlayer.sendFrameReadyEvents = true;
        videoPlayer.Play();
        deadline = Time.realtimeSinceStartup + preparationTimeout;
        while (!firstFrameReady && !videoFailed && !playbackFinished && Time.realtimeSinceStartup < deadline)
            yield return null;

        if (videoFailed || !firstFrameReady)
        {
            SkipFailedVideo();
            yield break;
        }

        // frameReadyは描画前の通知なので、RenderTextureへの反映を待つ。
        yield return new WaitForEndOfFrame();
        SetVideoAlpha(0f);
        SetVideoVisibility(true);
        float elapsed = 0f;
        while (elapsed < fadeDuration && !videoFailed)
        {
            elapsed += Time.unscaledDeltaTime;
            SetVideoAlpha(Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }
        SetVideoAlpha(1f);

        while (!playbackFinished && !videoFailed) yield return null;

        if (videoRawImage != null && videoRawImage.texture != null)
        {
            // 次の動画のPrepare/Stopで共有RenderTextureが変わる前に最終フレームを退避する。
            VideoTransitionOverlay.Show(videoRawImage, fadeDuration,
                gameManager.CurrentPhase == IngameGameManager.GamePhase.VideoTransition2);
        }
        CompleteTransition();
    }

    private void SkipFailedVideo()
    {
        Debug.LogWarning("動画の準備または最初のフレームの取得に失敗したため、次のフェーズへ進みます。", this);
        videoPlayer.Stop();
        preparingClip = null;
        CompleteTransition();
    }

    private void CompleteTransition()
    {
        playbackRoutine = null;
        SetVideoVisibility(false);
        if (videoPlayer != null) videoPlayer.sendFrameReadyEvents = false;
        if (gameManager != null) gameManager.OnVideoComplete();
    }

    private void OnFrameReady(VideoPlayer source, long frameIndex)
    {
        firstFrameReady = true;
        source.sendFrameReadyEvents = false;
    }

    private void OnVideoFinished(VideoPlayer source) => playbackFinished = true;

    private void OnVideoError(VideoPlayer source, string message)
    {
        videoFailed = true;
        Debug.LogError($"動画の再生エラー: {message}", this);
    }

    private void SetVideoVisibility(bool visible)
    {
        if (videoRawImage != null) videoRawImage.gameObject.SetActive(visible);
    }

    private void SetVideoAlpha(float alpha)
    {
        if (videoRawImage == null) return;
        Color color = videoRawImage.color;
        color.a = alpha;
        videoRawImage.color = color;
    }
}
