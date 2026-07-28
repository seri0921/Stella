using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Video;

public class EnemyManager : MonoBehaviour
{
    // うさぎの状態
    public enum rabbitState { Normal, Transforming, Monster }
    public rabbitState State = rabbitState.Normal;

    [Header("オブジェクト設定")]
    [Tooltip("通常のうさぎアセット")]
    public GameObject rabbitNormal;
    [Tooltip("モンスター化したうさぎアセット")]
    public GameObject rabbitMonster;
    [Tooltip("モンスターに変身する演出（映像）")]
    public VideoPlayer transformingVideo;

    [Header("ステータス設定")]
    [Tooltip("モンスター化するまでのゴミの数")]
    public int trashThreshold = 20;
    [TooltipAttribute("モンスターの最大HP")]
    public int maxHP = 100;

    [Header("通常時の動きの設定")]
    public float wanderSpeed = 20f;   // 動く速さ
    public float wanderAmount = 0.5f;  // 跳ねる幅

    [Header("モンスター時の動きの設定")]
    public float Monster_wanderSpeed = 10f;   // 動く速さ
    public float Monster_wanderHeight = 0.5f; // 跳ねる高さ

    private int currentHP; // 現在のHP
    private int currentTrashCount = 0;
    private Vector3 startPos;
    private bool Eating = false; // お菓子を食べている最中か判定


    // Start is called before the first frame update
    void Start()
    {
        startPos = transform.position;
        currentHP = maxHP;

        // 初期状態の表示設定
        rabbitNormal.SetActive(true);
        rabbitMonster.SetActive(false);
        if (transformingVideo != null) transformingVideo.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // 状態に応じたアニメーションを計算
        if (State == rabbitState.Normal && Eating)
        {
            // 食べている最中
            float xOffset = Mathf.Sin(Time.time * wanderSpeed) * wanderAmount;
            transform.position = startPos + new Vector3(xOffset, 0, 0);
        }
        else if (State == rabbitState.Monster)
        {
            // モンスター化中：上下に跳ねる（Mathf.Absで常に上方向に数値を変換）
            float yOffset = Mathf.Abs(Mathf.Sin(Time.time * Monster_wanderSpeed)) * Monster_wanderHeight;
            transform.position = startPos + new Vector3(0, yOffset, 0);
        }
        else
        {
            // それ以外の時は定位置に戻す
            transform.position = startPos;
        }
    }


    // お菓子を食べ始めたとき ////////////////////////////////////////////
    public void SetEatingState(bool state)
    {
        Eating = state;
    }

    // ゴミが生成されたときの処理 ////////////////////////////////////////
    public void Add()
    {
        if (State != rabbitState.Normal) return;

        currentTrashCount++;

        // ゴミが
        if (currentTrashCount >= trashThreshold)
        {
            StartCoroutine(TransformToMonsterRoutine());
        }
    }

    // うさぎの状態変化 //////////////////////////////////////////////////
    public void ForceTransform()
    {
        if (State == rabbitState.Normal)
        {
            StartCoroutine(TransformToMonsterRoutine());
        }
    }

    // うさぎの状態変化 //////////////////////////////////////////////////
    private IEnumerator TransformToMonsterRoutine()
    {
        State = rabbitState.Transforming;
        Eating = false;
        rabbitNormal.SetActive(false);

        // 映像を再生
        if (transformingVideo != null)
        {
            transformingVideo.gameObject.SetActive(true);
            transformingVideo.Play();

            // 映像再生が終わるまで待機
            yield return new WaitUntil(() => !transformingVideo.isPlaying);

            transformingVideo.gameObject.SetActive(false);
        }
        else
        {
            // 映像が設定されていない場合は2秒待機
            yield return new WaitForSeconds(2f);
        }

        // モンスターアセットに切り換え
        rabbitMonster.SetActive(true);
        State = rabbitState.Monster;
        // HPをセット
        currentHP = maxHP;
    }

    // ゴミを回収フレーズ //////////////////////////////////////////////////
    public void damageMonster()
    {
        // 通常状態のうさぎは、ダメージを受けない
        if (State != rabbitState.Monster) return;

        // ゴミを一つ回収するごとにモンスターのHPを減らす
        currentHP--;

        // HPが0になったら、うさぎをモンスター状態 → 通常状態に
        if (currentHP <= 0) revertNormal();
    }

    // うさぎが通常状態に戻る /////////////////////////////////////////////
    private void revertNormal()
    {
        State = rabbitState.Normal;
        currentTrashCount = 0;
        rabbitMonster.SetActive(false);
        rabbitNormal.SetActive(true);
        transform.position = startPos;
    }
}
