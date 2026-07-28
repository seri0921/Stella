using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IngameGameManager : MonoBehaviour
{
    public static IngameGameManager Instance { get; private set; }

    public enum GamePhase
    {
        Setup,             // ゲーム準備中
        EatingSnucks,      // 前半：お菓子を食べる（90秒）
        VideoTransition1,  // 前半終了後の動画再生
        CleaningTrash,     // 後半：ゴミ掃除（90秒）
        VideoTransition2,  // 後半終了後の動画再生
        GameEnd            // ゲーム終了（タイトルへ遷移）
    }

    [Header("フェーズ管理")]
    [SerializeField] private GamePhase currentPhase = GamePhase.Setup;
    public GamePhase CurrentPhase => currentPhase;

    [Header("制限時間設定 (秒)")]
    [SerializeField] private float eatingDuration = 90f;
    [SerializeField] private float cleaningDuration = 90f;

    [Header("デバッグ機能")]
    [SerializeField] private bool useShortTimeForTest = false;
    [SerializeField] private float testDuration = 5f; // テスト時の時間

    // タイマー変数
    private float timer;
    public float CurrentTimer => timer;

    // 進行状況のイベント
    public event Action<GamePhase> OnPhaseChanged;
    public event Action<float> OnTimerUpdated; // 残り時間の通知（UI更新用）

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // ゲームが始まったらセットアップを開始
        StartGame();
    }

    private void Update()
    {
        UpdateTimer();
    }

    /// <summary>
    /// ゲームの開始処理を行います。
    /// </summary>
    public void StartGame()
    {
        ChangePhase(GamePhase.Setup);
        
        // セットアップが完了したら前半（お菓子フェーズ）へ遷移
        // 実際にはカウントダウン演出などを挟んでも良いですが、まずは直接遷移します
        ChangePhase(GamePhase.EatingSnucks);
    }

    /// <summary>
    /// 各フェーズのタイマー処理を行います。
    /// </summary>
    private void UpdateTimer()
    {
        if (currentPhase == GamePhase.EatingSnucks || currentPhase == GamePhase.CleaningTrash)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = 0f;
                OnTimerUpdated?.Invoke(timer);
                OnTimeUp();
            }
            else
            {
                OnTimerUpdated?.Invoke(timer);
            }
        }
    }

    /// <summary>
    /// タイマーが0になった時の処理。
    /// </summary>
    private void OnTimeUp()
    {
        Debug.Log($"【TimeUp】フェーズ {currentPhase} が終了しました。");

        if (currentPhase == GamePhase.EatingSnucks)
        {
            // 前半終了 ➔ 動画遷移1へ
            ChangePhase(GamePhase.VideoTransition1);
        }
        else if (currentPhase == GamePhase.CleaningTrash)
        {
            // 後半終了 ➔ 動画遷移2へ
            ChangePhase(GamePhase.VideoTransition2);
        }
    }

    /// <summary>
    /// ゲームフェーズを変更し、各フェーズの初期化処理を行います。
    /// </summary>
    public void ChangePhase(GamePhase newPhase)
    {
        currentPhase = newPhase;
        Debug.Log($"【Phase Changed】現在のフェーズ: {currentPhase}");

        // 各フェーズの開始処理
        switch (currentPhase)
        {
            case GamePhase.Setup:
                // 初期化処理（必要に応じて）
                break;

            case GamePhase.EatingSnucks:
                // 前半：お菓子フェーズ開始
                timer = useShortTimeForTest ? testDuration : eatingDuration;
                // TODO: お菓子のスポーン処理をここに呼ぶ
                break;

            case GamePhase.VideoTransition1:
                // 動画遷移1開始（前半から後半への繋ぎ動画）
                // TODO: VideoTransitionManagerに動画再生を指示
                break;

            case GamePhase.CleaningTrash:
                // 後半：ゴミ掃除フェーズ開始
                timer = useShortTimeForTest ? testDuration : cleaningDuration;
                // 前半のゴミが残った状態で掃除開始
                break;

            case GamePhase.VideoTransition2:
                // 動画遷移2開始（後半からタイトルへの繋ぎ動画）
                // TODO: VideoTransitionManagerに動画再生を指示
                break;

            case GamePhase.GameEnd:
                // タイトルシーンへ戻る
                ReturnToTitle();
                break;
        }

        OnPhaseChanged?.Invoke(currentPhase);
    }

    /// <summary>
    /// 動画再生が完了したときに外部（VideoTransitionManager等）から呼ばれるメソッド。
    /// </summary>
    public void OnVideoComplete()
    {
        if (currentPhase == GamePhase.VideoTransition1)
        {
            // 動画1が終わったら後半（ゴミ掃除）へ
            ChangePhase(GamePhase.CleaningTrash);
        }
        else if (currentPhase == GamePhase.VideoTransition2)
        {
            // 動画2が終わったらタイトルへ
            ChangePhase(GamePhase.GameEnd);
        }
    }

    /// <summary>
    /// タイトルシーンへ戻る。
    /// </summary>
    private void ReturnToTitle()
    {
        Debug.Log("タイトルシーンへ戻ります。");
        // タイトルシーン名（Start.unityに対応するシーン名）を指定してロード
        SceneManager.LoadScene("Start");
    }
}
