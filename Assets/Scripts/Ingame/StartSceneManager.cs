using UnityEngine;
using UnityEngine.SceneManagement;

public class StartSceneManager : MonoBehaviour
{
    [Header("遷移先シーン名")]
    [SerializeField] private string gameSceneName = "Ingame";

    /// <summary>
    /// ゲームシーンへの遷移を行います。
    /// UIボタンやKinectのインタラクションから呼び出します。
    /// </summary>
    public void StartGame()
    {
        Debug.Log($"ゲームを開始します。シーン {gameSceneName} をロード中...");
        SceneManager.LoadScene(gameSceneName);
    }
}
