using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class IngameUIManager : MonoBehaviour
{
    [Header("UI要素の参照")]
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] TextMeshProUGUI phaseText;
    [SerializeField] CanvasGroup gamePlayCanvasGroup; // ゲームプレイUI（タイマーなど）をまとめて非表示にする用

    [Header("セリフのUI")]
    [Tooltip("オブジェクト・セリフをまとめたパネル")]
    [SerializeField] GameObject NaviUI;
    [Tooltip("セリフの位置")]
    [SerializeField] Vector3 NaviUIPosition = new Vector3(5f, 2f, -7);

    [Header("妖精のオブジェクト")]
    [SerializeField] Transform NaviTransform;

    [Header("NavigationPhase")]
    [Tooltip("妖精のTransform")]
    [SerializeField] Vector3 NaviPosition = new Vector3(0f, 4f, -7f);
    [SerializeField] Vector3 NaviRotation = new Vector3(-90f, 0f, 0f);
    [SerializeField] Vector3 NaviScale = new Vector3(200f, 200f, 200f);

    [Header("NavigationPhase")]
    [Tooltip("妖精のTransform")]
    [SerializeField] Vector3 NaviPosition_Ingame = new Vector3(0f, 4f, -7f);
    [SerializeField] Vector3 NaviRotation_Ingame = new Vector3(-90f, 0f, 0f);
    [SerializeField] Vector3 NaviScale_Ingame = new Vector3(200f, 200f, 200f);

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

    // 残り時間タイマーのUIを更新します。
    /// <param name="remainingTime">残り時間（秒）</param>
    private void UpdateTimerUI(float remainingTime)
    {
        if (timerText == null) return;

        // 分:秒 のフォーマットに変換
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        timerText.text = string.Format("{0:0}:{1:00}", minutes, seconds);
    }

    // フェーズが変更された際のUI表示制御。
    private void OnPhaseChanged(IngameGameManager.GamePhase newPhase)
    {
        if (phaseText == null) return;

        bool isNavigation = (newPhase == IngameGameManager.GamePhase.Navigation);

        if (NaviUI != null)
        {
            NaviUI.SetActive(isNavigation);

            if (isNavigation) NaviUI.transform.localPosition = NaviUIPosition;
        }

        if (NaviTransform != null)
        {
            // 表示させるフェーズ
            bool Navi_visible = (newPhase == IngameGameManager.GamePhase.Navigation ||
                                   newPhase == IngameGameManager.GamePhase.EatingSnucks ||
                                   newPhase == IngameGameManager.GamePhase.CleaningTrash);

            NaviTransform.gameObject.SetActive(Navi_visible);

            // フェーズによって位置・大きさ・角度を切り替える
            if (newPhase == IngameGameManager.GamePhase.Navigation)
            {
                // セリフフェーズ中の設定
                NaviTransform.localPosition = NaviPosition;
                NaviTransform.localScale = NaviScale;
                NaviTransform.localRotation = Quaternion.Euler(NaviRotation);
            }
            else if (newPhase == IngameGameManager.GamePhase.EatingSnucks ||
                     newPhase == IngameGameManager.GamePhase.CleaningTrash)
            {
                // ゲームプレイ中の設定
                NaviTransform.localPosition = NaviPosition_Ingame;
                NaviTransform.localScale = NaviScale_Ingame;
                NaviTransform.localRotation = Quaternion.Euler(NaviRotation_Ingame);
            }
        }

        switch (newPhase)
        {
            case IngameGameManager.GamePhase.Setup:
                phaseText.text = "準備中...";
                SetGameplayUIVisibility(false);
                break;

            case IngameGameManager.GamePhase.EatingSnucks:
                phaseText.text = "お菓子をたくさん食べよう！";
                SetGameplayUIVisibility(true);
                break;

            case IngameGameManager.GamePhase.VideoTransition1:
                phaseText.text = "";
                SetGameplayUIVisibility(false);
                break;

            case IngameGameManager.GamePhase.CleaningTrash:
                phaseText.text = "ゴミを綺麗に掃除しよう！";
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

    // ゲームプレイ用UIの表示・非表示を切り替えます。
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
