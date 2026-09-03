using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TrashObject : MonoBehaviour
{
    [Header("消去演出の設定")]
    [SerializeField] private float cleanAnimDuration = 0.4f; // 演出時間（少し長めに設定して吹き飛びを見せる）
    [SerializeField] private float flyForce = 8f; // 上に吹き飛ぶ力
    [SerializeField] private float horizontalSpread = 2f; // 左右に散らばる力
    [SerializeField] private float torqueForce = 15f; // 回転させる力

    private bool isCleaned = false;
    private bool isRegistered = false;
    private bool isDisposed = false;
    private Vector3 initialScale;

    private void Start()
    {
        // GameManagerへゴミの存在を登録
        if (IngameGameManager.Instance != null)
        {
            IngameGameManager.Instance.RegisterTrash();
            isRegistered = true;
        }
    }

    private void OnDestroy()
    {
        Unregister();
    }

    /// <summary>
    /// GameManagerからゴミの登録を解除します。
    /// </summary>
    private void Unregister()
    {
        if (isRegistered && IngameGameManager.Instance != null)
        {
            IngameGameManager.Instance.UnregisterTrash();
            isRegistered = false;
        }
    }

    /// <summary>
    /// ゴミを掃除（スワイプ）する処理。
    /// </summary>
    public void Clean()
    {
        if (isCleaned) return;
        isCleaned = true;

        StartCoroutine(CleanRoutine());
    }

    private IEnumerator CleanRoutine()
    {
        initialScale = transform.localScale;
        float elapsedTime = 0f;

        // 重複判定を防ぐためにコライダーを即座に無効化
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Rigidbody を使って上に吹き飛ばし、回転させる
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            // 上方向への力 ＋ 左右ランダムな方向への力
            Vector3 forceDirection = Vector3.up * flyForce + new Vector3(
                Random.Range(-horizontalSpread, horizontalSpread),
                0f,
                Random.Range(-horizontalSpread, horizontalSpread)
            );
            rb.AddForce(forceDirection, ForceMode.Impulse);

            // ランダムな軸でくるくる回転させる
            Vector3 randomTorque = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ).normalized * torqueForce;
            rb.AddTorque(randomTorque, ForceMode.Impulse);
        }

        // 吹き飛びながらだんだん小さくなる
        while (elapsedTime < cleanAnimDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / cleanAnimDuration;
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, t);
            yield return null;
        }

        transform.localScale = Vector3.zero;

        // ワープ管理が設定されている場合は、ゴミ箱の上へ移動させます。
        // 未設定の場合は従来どおり破棄します。
        if (TrashWarpManager.Instance != null && TrashWarpManager.Instance.TryWarpTrash(this))
        {
            yield break;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// ワープ後にゴミ箱の上へ配置し、落下できる状態へ戻します。
    /// </summary>
    public void WarpToBin(Vector3 position, Quaternion rotation)
    {
        if (!isCleaned || isDisposed) return;

        transform.SetPositionAndRotation(position, rotation);
        transform.localScale = initialScale;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    /// <summary>
    /// ゴミ箱に触れたときにゴミを消去し、GameManagerから登録解除します。
    /// </summary>
    public void DisposeInTrashBin()
    {
        if (!isCleaned || isDisposed) return;
        isDisposed = true;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        Unregister();
        Destroy(gameObject);
    }

    // デバッグ用：マウスでなぞる（ホバー）またはクリックで消去
    private void OnMouseEnter()
    {
        // マウスがドラッグ（長押し）された状態でホバーした時に消去する（スワイプのエミュレーション）
        if (Input.GetMouseButton(0))
        {
            TryCleanDebug();
        }
    }

    private void OnMouseDown()
    {
        // 単純クリックでも消去可能にしておく
        TryCleanDebug();
    }

    private void TryCleanDebug()
    {
        if (IngameGameManager.Instance != null && IngameGameManager.Instance.CurrentPhase == IngameGameManager.GamePhase.CleaningTrash)
        {
            Clean();
        }
    }
}
