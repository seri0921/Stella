using System.Collections;
using Effekseer;
using UnityEngine;

/// <summary>
/// 掃除されたゴミをゴミ箱の上へ転送します。
/// </summary>
public class TrashWarpManager : MonoBehaviour
{
    public static TrashWarpManager Instance { get; private set; }

    [Header("ワープエフェクト設定")]
    [SerializeField] private float warpEffectStartDelay = 0.65f;
    [SerializeField] private EffekseerEffectAsset departureEffect;
    [SerializeField, Min(0f)] private float departureEffectScale = 0.25f;
    [SerializeField, Min(0f)] private float departureEffectDuration = 0.25f;
    [SerializeField] private EffekseerEffectAsset arrivalEffect;
    [SerializeField, Min(0f)] private float arrivalEffectScale = 0.25f;

    [Header("ワープ先設定")]
    [SerializeField] private Transform[] warpDestinationPoints = new Transform[2];

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

    /// <summary>
    /// ゴミのワープ処理を開始します。
    /// </summary>
    public bool TryWarpTrash(TrashObject trash, float cleanStartedAt)
    {
        if (warpDestinationPoints == null || warpDestinationPoints.Length != 2 ||
            warpDestinationPoints[0] == null || warpDestinationPoints[1] == null)
        {
            Debug.LogError("2つのワープ先ポイントを設定してください。TrashWarpManagerを確認してください。");
            return false;
        }

        Transform nearestDestination = GetNearestDestinationPoint(trash.transform.position);
        StartCoroutine(WarpTrashRoutine(trash, nearestDestination, cleanStartedAt));
        return true;
    }

    private Transform GetNearestDestinationPoint(Vector3 trashPosition)
    {
        Transform nearestDestination = warpDestinationPoints[0];
        float nearestDistance = (trashPosition - nearestDestination.position).sqrMagnitude;

        for (int i = 1; i < warpDestinationPoints.Length; i++)
        {
            float distance = (trashPosition - warpDestinationPoints[i].position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDestination = warpDestinationPoints[i];
                nearestDistance = distance;
            }
        }

        return nearestDestination;
    }

    private IEnumerator WarpTrashRoutine(TrashObject trash, Transform destination, float cleanStartedAt)
    {
        float remainingDelay = cleanStartedAt + warpEffectStartDelay - Time.time;
        if (remainingDelay > 0f)
        {
            yield return new WaitForSeconds(remainingDelay);
        }

        if (trash == null || destination == null) yield break;

        Vector3 departurePosition = trash.transform.position;
        EffekseerHandle? departure = departureEffect != null
            ? PlayWarpEffect(departureEffect, departurePosition, departureEffectScale)
            : (EffekseerHandle?)null;

        try
        {
            if (departureEffectDuration > 0f)
            {
                yield return new WaitForSeconds(departureEffectDuration);
            }
        }
        finally
        {
            departure?.Stop();
        }

        if (trash == null || destination == null) yield break;

        Vector3 arrivalPosition = destination.position;
        trash.transform.SetPositionAndRotation(arrivalPosition, destination.rotation);
        if (arrivalEffect != null)
        {
            PlayWarpEffect(arrivalEffect, arrivalPosition, arrivalEffectScale);
        }

        trash.BeginBinDrop();
    }

    private static EffekseerHandle PlayWarpEffect(EffekseerEffectAsset effect, Vector3 position, float scale)
    {
        EffekseerHandle handle = EffekseerSystem.PlayEffect(effect, position);
        if (handle.exists)
        {
            handle.SetScale(Vector3.one * scale);
        }

        return handle;
    }
}
