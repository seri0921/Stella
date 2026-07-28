using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SnuckObject : MonoBehaviour
{
    [Header("ゴミの設定")]
    [SerializeField] private GameObject[] trashPrefabs; // 生成されるゴミプレハブ（複数からランダム）
    [SerializeField] private float eatAnimDuration = 0.3f; // 食べられた時の縮小アニメーション時間

    private bool isEaten = false;

    /// <summary>
    /// お菓子を食べる（タップ）処理。
    /// </summary>
    public void Eat()
    {
        if (isEaten) return;
        isEaten = true;

        StartCoroutine(EatRoutine());
    }

    private IEnumerator EatRoutine()
    {
        Vector3 initialScale = transform.localScale;
        float elapsedTime = 0f;

        // だんだん小さくなるアニメーション
        while (elapsedTime < eatAnimDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / eatAnimDuration;
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, t);
            yield return null;
        }

        // ゴミを生成
        SpawnTrash();

        // 自身を破棄
        Destroy(gameObject);
    }

    private void SpawnTrash()
    {
        if (trashPrefabs == null || trashPrefabs.Length == 0) return;

        // ランダムにゴミプレハブを選択
        int index = Random.Range(0, trashPrefabs.Length);
        GameObject selectedTrash = trashPrefabs[index];

        if (selectedTrash != null)
        {
            // お菓子の位置からゴミを生成
            Instantiate(selectedTrash, transform.position, Quaternion.identity);
        }
    }

    // デバッグ用：マウスクリックでお菓子を食べる
    private void OnMouseDown()
    {
        // 開発時の確認用に、クリックでも食べられるようにします
        // ただし、Eatingフェーズ中のみ有効
        if (IngameGameManager.Instance != null && IngameGameManager.Instance.CurrentPhase == IngameGameManager.GamePhase.EatingSnucks)
        {
            Eat();
        }
    }
}
