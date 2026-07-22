//using System.Collections;
//using System.Collections.Generic;
//using TMPro;
//using UnityEngine;

//public class FairyManager : MonoBehaviour
//{
//    [Header("UI設定")]
//    [Tooltip("セリフを表示するテキストコンポーネント")]
//    [SerializeField] private TextMeshProUGUI dialogueText;

//    [TooltipAttribute("セリフを画面に表示しておく時間（秒）")]
//    [SerializeField] private float

//    // 抽出した妖精のセリフ一覧
//    private readonly string[] dialogues =
//    {
//        "いっぱい食べよう！",
//        "うさぎを助けるためにごみをきれいにしよう！",
//        "もう少し！"
//    };

//    // セリフの種類を分かりやすく
//    public enum DialogueType
//    {
//        EatLots,    // 誘導シーン：「いっぱい食べよう！」
//        CleanTrash, // 掃除説明：「うさぎを助けるためにごみをきれいにしよう！」
//        CheerUp     // 応援演出：「もう少し！」
//    }

//    private Coroutine hideCoroutine;

//    // Start is called before the first frame update
//    void Start()
//    {
//        // 初期状態ではセリフを非表示
//        if (dialogueText == null)
//        {
//            dialogueText.gameObject.SetActive(false);
//        }
//    }

//    /// <summary>
//    /// 指定された場面のセリフを表示します。
//    /// 他のスクリプト（ゲーム進行管理など）からこのメソッドを呼び出してください。
//    /// </summary>
//    /// <param name="type">表示したいセリフの種類</param>
//    public void ShowDialogue(DialogueType type)
//    {
//        if (dialogueText == null) return;

//        // セリフのテキストを更新して表示
//        dialogueText.text = dialogues[(int)type];
//        dialogueText.gameObject.SetActive(true);

//        // 既に非表示にするコルーチンが動いていれば停止してリセットする
//        if (hideCoroutine != null)
//        {
//            StopCoroutine(hideCoroutine);
//        }

//        // 一定時間後に非表示にするコルーチンを開始
//        hideCoroutine = StartCoroutine(HideDialogueAfterDelay(displayDuration));
//    }

//    // 一定時間待機してからテキストを非表示にする処理
//    private IEnumerator HideDialogueAfterDelay(float delay)
//    {
//        yield return new WaitForSeconds(delay);
//        dialogueText.gameObject.SetActive(false);
//        hideCoroutine = null;
//    }
//}
