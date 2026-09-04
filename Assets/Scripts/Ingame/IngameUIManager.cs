using System;
using TMPro;
using UnityEngine;

public class IngameUIManager : MonoBehaviour
{
    [Header("UI要素の参照")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI phaseText;
    [SerializeField] private CanvasGroup gamePlayCanvasGroup; // ゲームプレイUI（タイマーなど）をまとめて非表示にする用

    private void Start()
    {
        // IngameGameManagerのイベントを購読
        if (IngameGameManager.Instance != null)
        {
            IngameGameManager.Instance.OnTimerUpdated += UpdateTimerUI;
            IngameGameManager.Instance.OnPhaseChanged += OnPhaseChanged;
            
            // 初期状態の表示を更新
            OnPhaseChanged(IngameGameManager.Instance.CurrentPhase);
        }
        else
        {
            Debug.LogError("IngameGameManager Instance が見つかりません。");
        }
    }

    private void OnDestroy()
    {
        // イベントの解除
        if (IngameGameManager.Instance != null)
        {
            IngameGameManager.Instance.OnTimerUpdated -= UpdateTimerUI;
            IngameGameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
        }
    }

    /// <summary>
    /// 残り時間タイマーのUIを更新します。
    /// </summary>
    /// <param name="remainingTime">残り時間（秒）</param>
    private void UpdateTimerUI(float remainingTime)
    {
        if (timerText == null) return;

        // 分:秒 のフォーマットに変換
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        timerText.text = string.Format("{0:0}:{1:00}", minutes, seconds);
    }

    /// <summary>
    /// フェーズが変更された際のUI表示制御。
    /// </summary>
    private void OnPhaseChanged(IngameGameManager.GamePhase newPhase)
    {
        if (phaseText == null) return;

        switch (newPhase)
        {
            case IngameGameManager.GamePhase.Setup:
                phaseText.text = "準備中...";
                SetGameplayUIVisibility(false);
                break;

            case IngameGameManager.GamePhase.EatingSnucks:
                phaseText.text = "おかしをたくさんたべよう！";
                SetGameplayUIVisibility(true);
                break;

            case IngameGameManager.GamePhase.VideoTransition1:
                phaseText.text = "";
                SetGameplayUIVisibility(false);
                break;

            case IngameGameManager.GamePhase.CleaningTrash:
                phaseText.text = "ゴミをきれいにそうじしよう！";
                SetGameplayUIVisibility(true);
                break;

            case IngameGameManager.GamePhase.VideoTransition2:
                phaseText.text = "";
                SetGameplayUIVisibility(false);
                break;

            case IngameGameManager.GamePhase.GameEnd:
                phaseText.text = "おしまい！";
                SetGameplayUIVisibility(false);
                break;
        }
    }

    /// <summary>
    /// ゲームプレイ用UIの表示・非表示を切り替えます。
    /// </summary>
    private void SetGameplayUIVisibility(bool visible)
    {
        if (gamePlayCanvasGroup != null)
        {
            gamePlayCanvasGroup.alpha = visible ? 1f : 0f;
            gamePlayCanvasGroup.blocksRaycasts = visible;
            gamePlayCanvasGroup.interactable = visible;
        }
    }
}
