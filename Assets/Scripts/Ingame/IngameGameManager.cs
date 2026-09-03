using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IngameGameManager : MonoBehaviour
{
    public static IngameGameManager Instance { get; private set; }

    public enum GamePhase
    {
        Setup,             // 準備
        EatingSnucks,      // お菓子を食べる（90秒）
        VideoTransition1,  // 映像再生
        CleaningTrash,     // ゴミ掃除（90秒）
        VideoTransition2,  // 映像再生
        GameEnd            // 終了
    }

    [Header("フェーズ管理")]
    [SerializeField] private GamePhase currentPhase = GamePhase.Setup;
    public GamePhase CurrentPhase => currentPhase;

    [Header("掃除フェーズ連動オブジェクト")]
    [SerializeField] private GameObject[] cleaningPhaseObjects;

    [Header("制限時間設定 (秒)")]
    [SerializeField] private float eatingDuration = 90f;
    [SerializeField] private float cleaningDuration = 90f;

    [Header("デバッグ機能")]
    [SerializeField] private bool useShortTimeForTest = false;
    [SerializeField] private float testDuration = 5f; // テスト時の時間

    // タイマー変数
    private float timer;
    public float CurrentTimer => timer;

    // シーン内のアクティブなゴミの数
    private int activeTrashCount = 0;
    public int ActiveTrashCount => activeTrashCount;

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
        StartGame();
    }

    private void Update()
    {
        UpdateTimer();
    }

    /// <summary>
    /// ゲームの開始処理
    /// </summary>
    public void StartGame()
    {
        ChangePhase(GamePhase.Setup);
        
        // セットアップが完了したら、お菓子フェーズへ遷移
        ChangePhase(GamePhase.EatingSnucks);
    }

    /// <summary>
    /// 各フェーズのタイマー処理
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
    /// タイマーが0になった時の処理
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
    /// ゲームフェーズを変更し、各フェーズの初期化処理
    /// </summary>
    public void ChangePhase(GamePhase newPhase)
    {
        currentPhase = newPhase;
        Debug.Log($"【Phase Changed】現在のフェーズ: {currentPhase}");

        UpdateCleaningPhaseObjects(newPhase);

        // 各フェーズの開始処理
        switch (currentPhase)
        {
            case GamePhase.Setup:
                // 初期化処理
                break;

            case GamePhase.EatingSnucks:
                // お菓子フェーズ開始
                timer = useShortTimeForTest ? testDuration : eatingDuration;
                break;

            case GamePhase.VideoTransition1:
                // 動画遷移1開始
                break;

            case GamePhase.CleaningTrash:
                // ゴミ掃除フェーズ開始
                timer = useShortTimeForTest ? testDuration : cleaningDuration;
                break;

            case GamePhase.VideoTransition2:
                // 動画遷移2開始
                break;

            case GamePhase.GameEnd:
                // タイトルシーンへ戻る
                Transition_Endgame();
                break;
        }

        OnPhaseChanged?.Invoke(currentPhase);
    }

    /// <summary>
    /// 掃除フェーズ中だけ対象オブジェクトを有効にします。
    /// </summary>
    private void UpdateCleaningPhaseObjects(GamePhase phase)
    {
        bool shouldBeActive = phase == GamePhase.CleaningTrash;

        if (cleaningPhaseObjects == null) return;

        foreach (GameObject targetObject in cleaningPhaseObjects)
        {
            if (targetObject != null)
            {
                targetObject.SetActive(shouldBeActive);
            }
        }
    }

    /// <summary>
    /// 動画再生が完了したときに外部（VideoTransitionManager等）から呼ばれるメソッド。
    /// </summary>
    public void OnVideoComplete()
    {
        if (currentPhase == GamePhase.VideoTransition1)
        {
            // 動画1が終わったら、ゴミ掃除へ
            ChangePhase(GamePhase.CleaningTrash);
        }
        else if (currentPhase == GamePhase.VideoTransition2)
        {
            // 動画2が終わったらタイトルへ
            ChangePhase(GamePhase.GameEnd);
        }
    }

    /// <summary>
    /// タイトルシーンへ戻る処理
    /// </summary>
    private void Transition_Endgame()
    {
        SceneManager.LoadScene("Endgame");
    }

    /// <summary>
    /// シーン内にゴミが生成された時に登録する処理
    /// </summary>
    public void RegisterTrash()
    {
        activeTrashCount++;
        Debug.Log($"【Trash Registered】現在のゴミの数: {activeTrashCount}");
    }

    /// <summary>
    /// ゴミが消去された時に登録解除し、全てのゴミが消えたら次の動画フェーズへ遷移します。
    /// </summary>
    public void UnregisterTrash()
    {
        activeTrashCount--;
        Debug.Log($"【Trash Unregistered】現在のゴミの数: {activeTrashCount}");

        if (activeTrashCount < 0)
        {
            activeTrashCount = 0;
        }

        // ゴミ掃除フェーズ中にすべてのゴミが消えた場合、即座に次の動画遷移へ
        if (currentPhase == GamePhase.CleaningTrash && activeTrashCount == 0)
        {
            Debug.Log("【Clear】すべてのゴミが掃除されました！動画再生フェーズへ移行します。");
            ChangePhase(GamePhase.VideoTransition2);
        }
    }
}
