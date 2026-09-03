using UnityEngine;

/// <summary>
/// ゴミ箱のTrigger Colliderに入ったゴミを消去します。
/// </summary>
[RequireComponent(typeof(Collider))]
public class TrashBinReceiver : MonoBehaviour
{
    private void Awake()
    {
        Collider receiverCollider = GetComponent<Collider>();
        if (!receiverCollider.isTrigger)
        {
            Debug.LogWarning("TrashBinReceiverのColliderはIs Triggerを有効にしてください。", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TrashObject trash = other.GetComponentInParent<TrashObject>();
        if (trash != null)
        {
            trash.DisposeInTrashBin();
        }
    }
}
