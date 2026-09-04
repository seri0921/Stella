using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Video;

public class EnemyManager : MonoBehaviour
{
    [Header("キャラクターの設定")]
    [Tooltip("うさぎ「ノーマル状態」")]
    [SerializeField] private GameObject rabbitNormal;
    [Tooltip("うさぎ「ゴミモンスター化」")]
    [SerializeField] private GameObject trashMonster;

    [Header("うさぎの移動範囲")]
    [Tooltip("右端のX座標")]
    [SerializeField] float leftPos = -5.0f;
    [Tooltip("左端のX座標")]
    [SerializeField] float rightPos = 5.0f;

    [Header("うさぎの動き")]
    [SerializeField] float move_speed = 1.0f;    // 左右移動する速さ
    [SerializeField] float jump_speed = 0.1f;    // 跳ねる速さ
    [SerializeField] float jump_Height = 0.5f; 　// 跳ねる高さ

    [Header("うさぎのランダム移動のタイミング")]
    [Tooltip("止まっている時間の最小・最大")]
    [SerializeField] float wait_timeMIN = 1.0f;
    [SerializeField] float wait_timeMAX = 3.0f;
    [Tooltip("動き続ける時間の最小・最大")]
    [SerializeField] float move_timeMIN = 1.5f;
    [SerializeField] float move_timeMAX = 4.0f;

    private Coroutine randomCoroutine;

    private Vector3 startPos; // うさぎのX・Y・Zの初期位置

    private int direction;  // 移動方向
    private float currentX; // 現在位置

    private bool ActivePhase = false;  // アクティブ状態（お菓子を食べているか、ゴミを捨てているか）
    private bool EnemyMove = false;    // 動いているかどうか判定

    // Start is called before the first frame update
    void Start()
    {
        // Unity上のうさぎの位置を記録し、X座標を指定
        startPos = new Vector3(leftPos, transform.position.y, transform.position.z);

        // 現在のX位置のみ記録
        currentX = startPos.x;

        // IngameGameManagerのイベント購読
        if (IngameGameManager.Instance != null) {
            IngameGameManager.Instance.OnPhaseChanged += OnPhaseChanged;
            OnPhaseChanged(IngameGameManager.Instance.CurrentPhase);
        } else {
            ActivePhase = true;
            Start_Random();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // プレイヤーが食べている時
        if (ActivePhase && EnemyMove)
        {
            // 左右に少しずつ跳ねて移動
            currentX += direction * move_speed * Time.deltaTime;

            if (currentX >= rightPos) {
                currentX = rightPos;
                // 右向きに進む
                direction = -1;
            } else if(currentX <= leftPos) {
                currentX = leftPos;
                // 左向きに進む
                direction = 1;
            }

            if (currentX == rightPos) {
                // 右向きに進む時の角度
                transform.rotation = Quaternion.Euler(-90, 90, 180);
            }
            else if (currentX == leftPos) {
                // 左向きに進む時の角度
                transform.rotation = Quaternion.Euler(-90, 90, 0);
            }

            // 跳ねる
            float yOffset = Mathf.Abs(Mathf.Sin(Time.time * jump_speed)) * jump_Height;

            transform.position = new Vector3(currentX, startPos.y + yOffset, startPos.z);
        }
    }

    // フェーズが変わったときの処理
    private void OnPhaseChanged(IngameGameManager.GamePhase newPhase)
    {
        // うさぎが動く
        if (newPhase == IngameGameManager.GamePhase.EatingSnucks)
        {
            // プレイヤーが食べる
            ActivePhase = true;
            // うさぎの現在地を左端に移動させる
            currentX = leftPos;

            if (rabbitNormal != null) rabbitNormal.SetActive(true);
            if (trashMonster != null) trashMonster.SetActive(false);

            currentX = leftPos;
            direction = 1;
            transform.rotation = Quaternion.Euler(-90, 90, 0);
            Start_Random();
        }
        // うさぎ（ゴミモンスター）は動かない
        else if(newPhase == IngameGameManager.GamePhase.CleaningTrash)
        {
            ActivePhase = false;
            Stop_Random();

            if (rabbitNormal != null) rabbitNormal.SetActive(false);
            if (trashMonster != null) trashMonster.SetActive(true);

            transform.position = new Vector3(0f, startPos.y, startPos.z);
            transform.rotation = Quaternion.Euler(-90, 90, 0);
        }
        else
        {
            ActivePhase = false;
            Stop_Random();

            if (rabbitNormal != null) rabbitNormal.SetActive(false);
            if (trashMonster != null) trashMonster.SetActive(true);

            transform.position = new Vector3(leftPos, startPos.y, startPos.z);
        }
    }

    private IEnumerator Move_Random()
    {
        while (ActivePhase)
        {
            // ランダムに停止
            EnemyMove = false;
            transform.position = new Vector3(currentX, startPos.y, startPos.z);

            float wait_duration = Random.Range(wait_timeMIN, wait_timeMAX);
            yield return new WaitForSeconds(wait_duration);

            if (!ActivePhase) break;


            // ランダムに移動
            EnemyMove = true;
            Start_MovementSE();
            float move_duration = Random.Range(move_timeMIN, move_timeMAX);
            yield return new WaitForSeconds(move_duration);

            EnemyMove = false;
            Stop_MovementSE();
        }
        EnemyMove = false;
        Stop_MovementSE();
    }

    private void Start_Random()
    {
        if (randomCoroutine != null) StopCoroutine(randomCoroutine);

        randomCoroutine = StartCoroutine(Move_Random());
    }
    private void Stop_Random()
    {
        if (randomCoroutine != null)
        {
            StopCoroutine(randomCoroutine);
            randomCoroutine = null;
        }
        EnemyMove = false;
        Stop_MovementSE();
    }

    private void Start_MovementSE()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE_RabbitMovement();
        }
    }

    private void Stop_MovementSE()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopSE_RabbitMovement();
        }
    }
}
