using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Video;

public class EnemyManager : MonoBehaviour
{
    [Header("移動範囲の設定")]
    [Tooltip("右端のX座標")]
    public float leftPos = -5.0f;
    [Tooltip("左端のX座標")]
    public float rightPos = 5.0f;

    [Header("動きの設定")]
    public float move_speed = 1.0f;    // 左右移動する速さ
    public float jump_speed = 0.1f;    // 跳ねる速さ
    public float jump_Height = 0.5f; 　// 跳ねる高さ

    private GameObject rabbitNormal; // うさぎ「ノーマル状態」

    private Vector3 startPos; // うさぎのX・Y・Zの初期位置

    private int direction;  // 移動方向
    private float currentX; // 現在位置

    private bool PlayerEating = false; // お菓子を食べている最中か判定


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
        } else {
            PlayerEating = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // プレイヤーが食べている時
        if (PlayerEating)
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
        if (newPhase == IngameGameManager.GamePhase.EatingSnucks)
        {
            // プレイヤーが食べる
            PlayerEating = true;

            // うさぎの現在地を左端に移動させる
            currentX = leftPos;
            if (rabbitNormal != null) rabbitNormal.SetActive(true);
        }
        else
        {
            // プレイヤーが食べない
            PlayerEating = false;

            // 動画再生中やゲーム終了時など、非表示
            if (rabbitNormal != null) rabbitNormal.SetActive(false);
            transform.position = new Vector3(leftPos, startPos.y, startPos.z);
        }
    }
}
