using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;
using UnityEngine.InputSystem;

public class SnuckManager : MonoBehaviour
{
    [Header("プレハブの設定")]
    [Tooltip("出現するオブジェクト")]
    [SerializeField] public GameObject[] snuckPrefabs; // お菓子の配列
    [SerializeField] public GameObject[] trashPrefabs; // ゴミの配列

    [Header("演出の設定")]
    [Tooltip("右クリックしてからお菓子が消えるまでに、かかる時間")]
    [SerializeField] public float destroyTime = 2.0f;

    [Header("位置の設定（仮）")]
    public Vector3 spawnPosition = new Vector3(0, 0, 0);
    public Vector3 trashPosition = new Vector3(0, -3f, 0);

    private bool running = false;   // 実行中（否）
    private GameObject snuckState;  // お菓子の状態
    private float elapsedTime = 0f; // 経過した時間を計る

    // Start is called before the first frame update
    void Start()
    {
        spawn_snuck();
    }

    // Update is called once per frame
    void Update()
    {
        // 右クリックを押したら、お菓子を食べる
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (snuckState != null && !running) StartCoroutine(destroy_snuckANDfeature_trush());
        }
    }

    // お菓子を出現させる /////////////////////////////////////////////////////////////////////
    public void spawn_snuck()
    {    
        // お菓子の配列からランダムに1つ選ぶ
        int randomIndex = Random.Range(0, snuckPrefabs.Length);
        snuckState = Instantiate(snuckPrefabs[randomIndex], spawnPosition, Quaternion.identity);
    }

    // お菓子を消して、ゴミを生成 ////////////////////////////////////////////////////////////
    private IEnumerator destroy_snuckANDfeature_trush()
    {
        running = true;

        // だんだんお菓子を消す（小さくする）アニメーション
        Vector3 initialScale = snuckState.transform.localScale;

        // 経過した時間よりも、"お菓子が消えるまで"の時間がかかっていたら
        while (elapsedTime < destroyTime)
        {
            // t(elapsedTime / destroyTime)による、aとbの間の線形補間（2つの与えられた値の間のある割合の値を見つける）
            float scaleRatio = Mathf.Lerp(1f, 0f, elapsedTime / destroyTime);

            snuckState.transform.localScale = initialScale * scaleRatio;

            // 経過した時間
            elapsedTime += Time.deltaTime;

            // yield return：ここで一旦処理止め、null：次のフレームで続きを実行
            yield return null;
        }

        // お菓子を削除
        if (snuckState != null) Destroy(snuckState);
        // ゴミを生成
        if (trashPrefabs.Length > 0)
        {
            int trashIndex = Random.Range(0, trashPrefabs.Length);
            Instantiate(trashPrefabs[trashIndex], trashPosition, Quaternion.identity); // Quaternion.identity：回転させない
        }

        // 次のお菓子が出るまで待つ
        yield return new WaitForSeconds(1.5f);

        spawn_snuck();

        running = false;
    }
}