using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 自動でAudioSourceが付く（抜け防止）
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [Header("BGMの設定")]
    [Tooltip("お菓子を食べるBGM")]
    [SerializeField] AudioClip eatingBGM;
    [Tooltip("ゴミを捨てるBGM")]
    [SerializeField] AudioClip dumpingBGM;
    [Tooltip("通常の音量")]
    [SerializeField] [Range(0f, 1f)] float maxVolume = 1.0f;
    [Tooltip("フェーズ終了前に音量小さくする秒間")]
    [SerializeField] float Fade_Duration = 3.0f;

    [Header("SEの設定")]
    [Tooltip("お菓子を食べている時の効果音")]
    public AudioClip eatingSE;

    private AudioSource BGM_Source;
    private AudioSource SE_Source;

    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        // インスタンスの初期化
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        BGM_Source = GetComponent<AudioSource>();
        if (BGM_Source == null)
        {
            BGM_Source = gameObject.AddComponent<AudioSource>();
        }

        BGM_Source.playOnAwake = false;
        BGM_Source.loop = true;
        BGM_Source.volume = maxVolume;

        // SE用のAudioSourceを自動で追加して設定
        SE_Source = gameObject.AddComponent<AudioSource>();
        SE_Source.playOnAwake = false;
        SE_Source.loop = false;

        if (IngameGameManager.Instance != null) {
            IngameGameManager.Instance.OnPhaseChanged += OnPhaseChanged;
            IngameGameManager.Instance.OnTimerUpdated += OnTimerUpdated; // 別スクリプトから、残り時間の更新処理を取得

            OnPhaseChanged(IngameGameManager.Instance.CurrentPhase);
        }
    }

    // オブジェクトが破壊されたり、シーンを移動した際にイベント解除する処理
    private void OnDestroy()
    {
        if (IngameGameManager.Instance != null)
        {
            IngameGameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
            IngameGameManager.Instance.OnTimerUpdated -= OnTimerUpdated;
        }
    }
    // BGMを再生する処理
    private void PlayBGM(AudioClip clip)
    {
        // クリップが設定されていなければ、何もない
        if (clip == null) return;
        BGM_Source.clip = clip;

        BGM_Source.volume = maxVolume;
        BGM_Source.Play();
    }

    // BGMを停止する処理
    private void StopBGM()
    {
        if (BGM_Source.isPlaying) BGM_Source.Stop();
    }

    // 食べているときの効果音を鳴らす処理
    public void PlaySE_eating()
    {
        if (eatingSE != null && SE_Source != null)
        {
            // PlayOneShot：連続で食べても音が途切れず重なって再生
            SE_Source.PlayOneShot(eatingSE);
        }
    }

    // フェーズが切り替わる処理
    private void OnPhaseChanged(IngameGameManager.GamePhase newPhase)
    {
        switch (newPhase)
        {
            // お菓子を食べる
            case IngameGameManager.GamePhase.EatingSnucks:
                PlayBGM(eatingBGM);
                break;
            // ゴミを捨てる
            case IngameGameManager.GamePhase.CleaningTrash:
                PlayBGM(dumpingBGM);
                break;
            // それ以外
            default:
                StopBGM();
                break;
        }
    }

    // 毎フレーム残り時間が通知されるときの処理
    private void OnTimerUpdated(float currentTime)
    {
        // BGMが鳴っている時だけ計算する
        if (BGM_Source.isPlaying)
        {
            // 残り時間がフェードアウト設定時間（例：3秒）を切ったら、徐々に音量を下げる
            if (currentTime <= Fade_Duration && currentTime > 0)
            {
                // 残り時間の割合（1.0 〜 0.0）を計算して、最大音量に掛ける
                float volumeRatio = currentTime / Fade_Duration;
                BGM_Source.volume = maxVolume * volumeRatio;
            }
            else if (currentTime > Fade_Duration)
            {
                // まだ時間がたっぷりある場合は元の音量をキープ
                BGM_Source.volume = maxVolume;
            }
        }
    }
}
