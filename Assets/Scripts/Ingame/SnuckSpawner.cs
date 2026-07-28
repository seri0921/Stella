using System.Collections.Generic;
using UnityEngine;

public class SnuckSpawner : MonoBehaviour
{
    [Header("お菓子のプレハブ")]
    [SerializeField] private GameObject[] snuckPrefabs;

    [Header("スポーン設定")]
    [SerializeField] private int spawnCount = 30; // 配置するお菓子の数
    [SerializeField] private Vector3 spawnAreaCenter = Vector3.zero;
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(8f, 4f, 2f); // 配置する範囲（幅、高さ、奥行き）

    [Header("リスポン設定")]
    [SerializeField] private bool enableRespawn = true; // リスポン（再生成）を有効にするか
    [SerializeField] private float respawnInterval = 3f; // 補充チェックの間隔（秒）

    private List<GameObject> spawnedSnucks = new List<GameObject>();
    private float respawnTimer;

    private void Start()
    {
        if (IngameGameManager.Instance != null)
        {
            IngameGameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        }
        respawnTimer = 0f;
    }

    private void OnDestroy()
    {
        if (IngameGameManager.Instance != null)
        {
            IngameGameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
        }
    }

    private void Update()
    {
        // お菓子フェーズ中かつリスポン有効の場合のみ処理
        if (IngameGameManager.Instance != null && 
            IngameGameManager.Instance.CurrentPhase == IngameGameManager.GamePhase.EatingSnucks && 
            enableRespawn)
        {
            respawnTimer += Time.deltaTime;
            if (respawnTimer >= respawnInterval)
            {
                respawnTimer = 0f;
                CheckAndRespawnSnucks();
            }
        }
    }

    private void HandlePhaseChanged(IngameGameManager.GamePhase newPhase)
    {
        if (newPhase == IngameGameManager.GamePhase.EatingSnucks)
        {
            SpawnSnucks();
        }
        else if (newPhase == IngameGameManager.GamePhase.VideoTransition1)
        {
            // 前半終了時に、もし食べ残しのお菓子があれば綺麗にしておく
            ClearRemainingSnucks();
        }
    }

    /// <summary>
    /// 設定された範囲内に大量のお菓子をランダムにスポーンさせます。
    /// </summary>
    public void SpawnSnucks()
    {
        ClearRemainingSnucks();

        if (snuckPrefabs == null || snuckPrefabs.Length == 0)
        {
            Debug.LogWarning("お菓子のプレハブが登録されていません。");
            return;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            SpawnSingleSnuck();
        }

        Debug.Log($"{spawnCount}個のお菓子を初期スポーンしました。");
    }

    /// <summary>
    /// お菓子が減っているか確認し、不足分を再生成します。
    /// </summary>
    private void CheckAndRespawnSnucks()
    {
        // 既に破棄された（食べられた）オブジェクトをリストから除外
        spawnedSnucks.RemoveAll(snuck => snuck == null);

        int currentCount = spawnedSnucks.Count;
        if (currentCount < spawnCount)
        {
            int amountToSpawn = spawnCount - currentCount;
            for (int i = 0; i < amountToSpawn; i++)
            {
                SpawnSingleSnuck();
            }
            Debug.Log($"お菓子が {amountToSpawn} 個食べられていたため、再生成して補充しました。");
        }
    }

    /// <summary>
    /// 範囲内のランダムな位置に、お菓子を1個スポーンさせます。
    /// </summary>
    private void SpawnSingleSnuck()
    {
        if (snuckPrefabs == null || snuckPrefabs.Length == 0) return;

        // ランダムな位置を計算
        Vector3 randomPos = new Vector3(
            Random.Range(spawnAreaCenter.x - spawnAreaSize.x / 2f, spawnAreaCenter.x + spawnAreaSize.x / 2f),
            Random.Range(spawnAreaCenter.y - spawnAreaSize.y / 2f, spawnAreaCenter.y + spawnAreaSize.y / 2f),
            Random.Range(spawnAreaCenter.z - spawnAreaSize.z / 2f, spawnAreaCenter.z + spawnAreaSize.z / 2f)
        );

        // ランダムにお菓子の種類を選択
        int prefabIndex = Random.Range(0, snuckPrefabs.Length);
        GameObject prefab = snuckPrefabs[prefabIndex];

        if (prefab != null)
        {
            // ランダムな回転を少し加える
            Quaternion randomRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject snuck = Instantiate(prefab, randomPos, randomRot, transform);
            spawnedSnucks.Add(snuck);
        }
    }

    /// <summary>
    /// シーン内に残っているお菓子を削除します。
    /// </summary>
    private void ClearRemainingSnucks()
    {
        foreach (var snuck in spawnedSnucks)
        {
            if (snuck != null)
            {
                Destroy(snuck);
            }
        }
        spawnedSnucks.Clear();
    }

    // デバッグ用：スポーン範囲をエディタのSceneビューに表示
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawCube(spawnAreaCenter, spawnAreaSize);
    }
}
