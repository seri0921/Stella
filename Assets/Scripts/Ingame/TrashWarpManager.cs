using System.Collections;
using UnityEngine;

/// <summary>
/// 掃除されたゴミをワープゲート経由でゴミ箱の上へ移動させます。
/// </summary>
public class TrashWarpManager : MonoBehaviour
{
    public static TrashWarpManager Instance { get; private set; }

    [Header("ワープゲート設定")]
    [SerializeField] private GameObject warpGatePrefab;
    [SerializeField] private Transform warpGateOrientation;
    [SerializeField] private float warpGateAppearDuration = 0.25f;
    [SerializeField] private float warpDelay = 0.4f;
    [SerializeField] private float warpGateVisibleDuration = 1f;

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
    public bool TryWarpTrash(TrashObject trash)
    {
        if (warpDestinationPoints == null || warpDestinationPoints.Length != 2 ||
            warpDestinationPoints[0] == null || warpDestinationPoints[1] == null)
        {
            Debug.LogError("2つのワープ先ポイントを設定してください。TrashWarpManagerを確認してください。");
            return false;
        }

        if (warpGatePrefab == null)
        {
            Debug.LogError("ワープゲートPrefabが設定されていません。TrashWarpManagerを確認してください。");
            return false;
        }

        Transform nearestDestination = GetNearestDestinationPoint(trash.transform.position);
        StartCoroutine(WarpTrashRoutine(trash, nearestDestination));
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

    private IEnumerator WarpTrashRoutine(TrashObject trash, Transform destination)
    {
        Quaternion gateRotation = warpGateOrientation != null
            ? warpGateOrientation.rotation
            : Quaternion.identity;

        GameObject gate = Instantiate(
            warpGatePrefab,
            trash.transform.position,
            gateRotation
        );

        Vector3 targetScale = gate.transform.localScale;
        gate.transform.localScale = Vector3.zero;
        Destroy(gate, Mathf.Max(0f, warpGateVisibleDuration));

        float elapsedTime = 0f;
        while (elapsedTime < warpGateAppearDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / warpGateAppearDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            if (gate != null)
            {
                gate.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, easedT);
            }

            yield return null;
        }

        if (gate != null)
        {
            gate.transform.localScale = targetScale;
        }

        if (warpDelay > 0f)
        {
            yield return new WaitForSeconds(warpDelay);
        }

        if (trash != null)
        {
            trash.WarpToBin(
                destination.position,
                destination.rotation
            );
        }
    }
}
