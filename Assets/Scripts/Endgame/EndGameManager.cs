using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndGameManager : MonoBehaviour
{
    [Header("動きの設定")]
    [Tooltip("初期位置")]
    [SerializeField] float initialY = 10.0f;
    [Tooltip("下りてくるのに時間がかかる")]
    [SerializeField] float drop_time = 1.5f;

    private Vector3 stopPos; // 止まる位置

    // Start is called before the first frame update
    void Start()
    {
        // 現在位置を止まる位置として記録
        stopPos = transform.position;
        transform.position = new Vector3(transform.position.x, initialY, transform.position.z);
        //
        StartCoroutine(Drop_Routine());
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private IEnumerator Drop_Routine()
    {
        Vector3 initialPos = transform.position;
        float elapsed_time = 0f;

        while (elapsed_time < drop_time)
        {
            // 経過時間に合わせて 0.0（開始） 〜 1.0（終了） の割合を作る
            float t = elapsed_time / drop_time;
            // スピードを調整し、止まる直前にフワッと減速させる計算
            float smooth = t * (2f - t);
            // 開始位置から目的地まで、滑らかに移動させる
            transform.position = Vector3.Lerp(initialPos, stopPos, smooth);

            elapsed_time += Time.deltaTime;
            yield return null;
        }
        // 最後にズレを直して、最終位置に合わせる
        transform.position = stopPos;
    }
}
