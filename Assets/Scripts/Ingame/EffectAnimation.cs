using System.Collections;
using UnityEngine;

public class EffectAnimation : MonoBehaviour
{
    [Header("エフェクトアニメーション設定")]
    [Tooltip("0スケールから元のサイズまで拡大する時間（秒）")]
    [SerializeField] [Min(0f)] private float appearDuration = 0.25f;
    [Tooltip("エフェクトを表示する時間（秒）")]
    [SerializeField] [Min(0f)] private float displayDuration = 1.0f;

    private Vector3 targetScale;

    private void Awake()
    {
        targetScale = transform.localScale;
        transform.localScale = Vector3.zero;
    }

    private void Start()
    {
        StartCoroutine(PlayAnimation());
    }

    private IEnumerator PlayAnimation()
    {
        Destroy(gameObject, Mathf.Max(0f, displayDuration));

        if (appearDuration <= 0f)
        {
            transform.localScale = targetScale;
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < appearDuration)
        {
            if (this == null) yield break;

            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / appearDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, easedT);
            yield return null;
        }

        if (this != null)
        {
            transform.localScale = targetScale;
        }
    }
}
