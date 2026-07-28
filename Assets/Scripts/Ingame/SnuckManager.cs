// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.InputSystem;

// public class SnuckManager : MonoBehaviour
// {
//     // ゲームの進行状況を管理するための状態
//     public enum GamePhase
//     {
//         Eating,   // お菓子を食べる
//         Fighting, // モンスター化したうさぎと戦う
//         End       // 終了
//     }

//     [Header("フェーズ管理")]
//     public GamePhase Phase = GamePhase.Eating;
//     [TooltipAttribute("各フェーズの制限時間")]
//     public float phaseTimeLimit = 90f;

//     [Header("プレハブの設定")]
//     [Tooltip("出現するオブジェクト")]
//     [SerializeField] public GameObject[] snuckPrefabs; // お菓子の配列
//     [SerializeField] public GameObject[] trashPrefabs; // ゴミの配列

//     [Header("演出の設定")]
//     [Tooltip("右クリックしてからお菓子が消えるまでに、かかる時間")]
//     [SerializeField] public float destroyTime = 2.0f;

//     [Header("位置の設定（仮）")]
//     public Vector3 spawnPosition = new Vector3(0, 0, 0);
//     public Vector3 trashPosition = new Vector3(0, -3f, 0);

//     [Header("連携スクリプト")]
//     public EnemyManager enemyManager;

//     private bool running = false;   // 実行中（否）
//     private GameObject snuckState;  // お菓子の状態
//     private float elapsedTime = 0f; // 経過した時間を計る
//     private float currentTimer;     // 現在の残り自国

//     // Start is called before the first frame update
//     void Start()
//     {
//         Phase = GamePhase.Eating;
//         currentTimer = phaseTimeLimit;
//         spawn_snuck();
//     }

//     // Update is called once per frame
//     void Update()
//     {
//         if (Phase == GamePhase.Eating)
//         {
//             // タイマーを減らす
//             currentTimer -= Time.deltaTime;

//             // 右クリックを押したら、お菓子を食べる
//             if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
//             {
//                 if (snuckState != null && !running) StartCoroutine(destroy_snuckANDfeature_trush());
//             }

//             // 1分半経過したらフェーズ切り換え
//             if (currentTimer <= 0) StartFightingPhase();
//         }
//         else if (Phase == GamePhase.Fighting)
//         {
//             // タイマーを減らす
//             currentTimer -= Time.deltaTime;

//             if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
//             {
//                 if (enemyManager != null) enemyManager.damageMonster();
//             }
//             else if (currentTimer <= 0)
//             {
//                 Phase = GamePhase.End;
//             }
//         }
//     }

//     // お菓子を出現させる /////////////////////////////////////////////////////////////////////
//     public void spawn_snuck()
//     {    
//         // お菓子の配列からランダムに1つ選ぶ
//         int randomIndex = Random.Range(0, snuckPrefabs.Length);
//         snuckState = Instantiate(snuckPrefabs[randomIndex], spawnPosition, Quaternion.identity);
//     }

//     // お菓子を消して、ゴミを生成 ////////////////////////////////////////////////////////////
//     private IEnumerator destroy_snuckANDfeature_trush()
//     {
//         running = true;
//         // 食べている処理をEnemyManagerへ
//         if (enemyManager != null) enemyManager.SetEatingState(true);

//         // だんだんお菓子を消す（小さくする）アニメーション
//         Vector3 initialScale = snuckState.transform.localScale;

//         float elapsedTime = 0f;

//         // 経過した時間よりも、"お菓子が消えるまで"の時間がかかっていたら
//         while (elapsedTime < destroyTime)
//         {
//             // t(elapsedTime / destroyTime)による、aとbの間の線形補間（2つの与えられた値の間のある割合の値を見つける）
//             float scaleRatio = Mathf.Lerp(1f, 0f, elapsedTime / destroyTime);

//             snuckState.transform.localScale = initialScale * scaleRatio;

//             // 経過した時間
//             elapsedTime += Time.deltaTime;

//             // yield return：ここで一旦処理止め、null：次のフレームで続きを実行
//             yield return null;
//         }

//         // お菓子を削除
//         if (snuckState != null) Destroy(snuckState);
//         // ゴミを生成
//         if (trashPrefabs.Length > 0)
//         {
//             int trashIndex = Random.Range(0, trashPrefabs.Length);
//             Instantiate(trashPrefabs[trashIndex], trashPosition, Quaternion.identity); // Quaternion.identity：回転させない

//             // ゴミが増えた処理をEnemyManagerへ
//             if (enemyManager != null) enemyManager.Add();
//         }

//         if (enemyManager != null) enemyManager.SetEatingState(false);

//         // 次のお菓子が出るまで待つ
//         yield return new WaitForSeconds(1.5f);

//         spawn_snuck();

//         running = false;
//     }

//     private void StartFightingPhase()
//     {
//         Phase = GamePhase.Fighting;
//         currentTimer = phaseTimeLimit;

//         // 画面に残っているお菓子があれば消す
//         if (snuckState == null) Destroy(snuckState);
//         running = false;

//         // 1分半経ったら、ゴミの数に関係なく強制的にモンスターを変身させる
//         if (enemyManager != null) enemyManager.ForceTransform();
//     }
// }