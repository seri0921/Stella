using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoTransitionManager : MonoBehaviour
{
    [Header("Video Playerの設定")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoRawImage; // 動画描画用のUI（RawImageを使用する場合）

    [Header("フェーズごとの動画クリップ")]
    [SerializeField] private VideoClip transition1Clip; // 前半 ➔ 後半の動画
    [SerializeField] private VideoClip transition2Clip; // 後半 ➔ タイトルの動画

    private void Start()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        // 初期状態で非表示
        SetVideoUIVisibility(false);

        // 動画終了イベントの登録
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
        }
        else
        {
            Debug.LogError("VideoPlayer コンポーネントが見つかりません。");
        }

        // GameManagerのフェーズ遷移イベントを購読
        if (IngameGameManager.Instance != null)
        {
            IngameGameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }

        if (IngameGameManager.Instance != null)
        {
            IngameGameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
        }
    }

    /// <summary>
    /// GameManagerのフェーズ変更に応じて動画再生を制御します。
    /// </summary>
    private void HandlePhaseChanged(IngameGameManager.GamePhase newPhase)
    {
        if (videoPlayer == null) return;

        if (newPhase == IngameGameManager.GamePhase.VideoTransition1)
        {
            PlayTransitionVideo(transition1Clip);
        }
        else if (newPhase == IngameGameManager.GamePhase.VideoTransition2)
        {
            PlayTransitionVideo(transition2Clip);
        }
        else
        {
            // 動画再生以外のフェーズでは停止して非表示
            if (videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }
            SetVideoUIVisibility(false);
        }
    }

    /// <summary>
    /// 指定された動画クリップの再生を開始します。
    /// </summary>
    private void PlayTransitionVideo(VideoClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("動画クリップが設定されていないため、即座に終了処理を行います。");
            // クリップがない場合は即座にGameManagerに完了を通知して次に進めます
            Invoke(nameof(NotifyGameManagerVideoComplete), 0.5f);
            return;
        }

        videoPlayer.clip = clip;
        SetVideoUIVisibility(true);
        videoPlayer.Play();
        Debug.Log($"動画再生を開始しました: {clip.name}");
    }

    /// <summary>
    /// 動画の再生が完了した時のコールバック。
    /// </summary>
    private void OnVideoFinished(VideoPlayer source)
    {
        Debug.Log("動画の再生が終了しました。");
        SetVideoUIVisibility(false);
        NotifyGameManagerVideoComplete();
    }

    /// <summary>
    /// GameManagerに動画完了を通知します。
    /// </summary>
    private void NotifyGameManagerVideoComplete()
    {
        if (IngameGameManager.Instance != null)
        {
            IngameGameManager.Instance.OnVideoComplete();
        }
    }

    /// <summary>
    /// 動画UI（RawImage等）の表示・非表示を切り替えます。
    /// </summary>
    private void SetVideoUIVisibility(bool visible)
    {
        if (videoRawImage != null)
        {
            videoRawImage.gameObject.SetActive(visible);
        }
    }
}
