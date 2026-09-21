using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirPollutionManager : MonoBehaviour
{
    [Header("エフェクトの設定")]
    [Tooltip("エフェクトが表示されている時間")]
    [SerializeField] float waitTime = 0.5f;
    [Tooltip("フェードアウトにかかる時間")]
    [SerializeField] float fadeDuration = 0.3f;

    private SpriteRenderer spriteRenderer;

    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 生成されたらフェードアウトのコルーチンを開始
        if (spriteRenderer == null) StartCoroutine(FadeOutRoutine());
    }
    
    private IEnumerator FadeOutRoutine()
    {
        // 指定した時間待機
        yield return new WaitForSeconds(waitTime);

        float elapsedTime = 0f;
        Color initialColor = spriteRenderer.color;

        // 徐々に透明に
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(initialColor.a, 0f, elapsedTime / fadeDuration);
            spriteRenderer.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
            yield return null;
        }
        
        // エフェクトを削除
        Destroy(gameObject);
    }
}
