using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Scene_autoTransition : MonoBehaviour
{
    [Header("シーン遷移の設定")]
    [Tooltip("次シーンへ移行する時間")]
    [SerializeField] float wait_time = 30f;
    [Tooltip("移行先のシーン名")]
    [SerializeField] string next_Scene = "Start";

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Transition_Routine());
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    /// <summary>
    /// 自動的にスタートシーンに戻る処理
    /// </summary>
    public IEnumerator Transition_Routine()
    {
        // 設定した時間、待機
        yield return new WaitForSeconds(wait_time);
        // 次のシーンへ移行
        SceneManager.LoadScene(next_Scene);
    }
}
