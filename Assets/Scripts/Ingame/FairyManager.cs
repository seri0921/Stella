using UnityEngine;

public class FairyManager : MonoBehaviour
{
    [Header("妖精の設定")]
    [Tooltip("左右に動く速さ")]
    public float move_speed = 2.5f;
    [Tooltip("左右に動く距離")]
    public float move_distance = 5.0f;
    [Tooltip("下に弧を描く")]
    public float arc_depth = 1.0f;

    private Vector3 startPos; // 始めの位置を記憶
    private bool FairyMove; 　// 妖精が動いているかどうか

    void Start()
    {
        // ゲーム開始時の定位置を記憶
        startPos = transform.position;

        // IngameGameManagerのフェーズ変更イベントを連携
        if (IngameGameManager.Instance != null) {
            IngameGameManager.Instance.OnPhaseChanged += OnPhaseChanged;

            // ゲーム開始直後のフェーズ状態を取得して初期化
            OnPhaseChanged(IngameGameManager.Instance.CurrentPhase);
        }
    }

    void Update()
    {
        // 左右に動かす
        if (FairyMove)
        {
            // -1.0～1.0の間の波を作る
            float wave = Mathf.Sin(Time.time * move_speed);
            // 波 * 動く距離
            float xOffset = wave * move_distance;
            // 波 ** 2 - 1（U字の弧を作る）※wave = -1.0～1.0の時はy = 0、wave = 0の時はy = -arc_depth
            float yOffset = (wave * wave - 1f) * arc_depth;

            transform.position = startPos + new Vector3(xOffset, yOffset, 0);
        }
    }

    private void Destroy()
    {
        if (IngameGameManager.Instance != null) {
            IngameGameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
        }
    }

    private void OnPhaseChanged(IngameGameManager.GamePhase newPhase)
    {
        // 食べる・綺麗にするフェーズのとき、妖精が動く
        if (newPhase == IngameGameManager.GamePhase.EatingSnucks || newPhase == IngameGameManager.GamePhase.CleaningTrash)
        {
            FairyMove = true;
        }
        else
        {
            FairyMove = false;
            transform.position = startPos;
        }
    }
}