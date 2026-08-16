using UnityEngine;

/// <summary>
/// BGM / SFX 재생과 음량(0~100%)을 관리한다. 설정은 PlayerPrefs 에 저장된다.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    private const string BgmKey = "TurnBasedGame.BgmVolume";
    private const string SfxKey = "TurnBasedGame.SfxVolume";

    [Header("오디오 소스")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("BGM")]
    public AudioClip mainMenuBgm;
    public AudioClip battleBgm;
    public AudioClip bossBgm;

    [Header("SFX")]
    public AudioClip buttonSfx;
    public AudioClip attackSfx;
    public AudioClip criticalSfx;
    public AudioClip healSfx;
    public AudioClip levelUpSfx;
    public AudioClip victorySfx;
    public AudioClip defeatSfx;

    /// <summary>0 ~ 100</summary>
    public float BgmVolume { get; private set; }
    /// <summary>0 ~ 100</summary>
    public float SfxVolume { get; private set; }

    private void Awake()
    {
        instance = this;

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        BgmVolume = PlayerPrefs.GetFloat(BgmKey, 80f);
        SfxVolume = PlayerPrefs.GetFloat(SfxKey, 80f);
        ApplyVolumes();
    }

    private void Start()
    {
        PlayBgm(mainMenuBgm);
    }

    // ─────────────────────────────────────────────────────────────
    // 음량
    // ─────────────────────────────────────────────────────────────
    public void SetBgmVolume(float percent0To100)
    {
        BgmVolume = Mathf.Clamp(percent0To100, 0f, 100f);
        PlayerPrefs.SetFloat(BgmKey, BgmVolume);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetSfxVolume(float percent0To100)
    {
        SfxVolume = Mathf.Clamp(percent0To100, 0f, 100f);
        PlayerPrefs.SetFloat(SfxKey, SfxVolume);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (bgmSource != null) bgmSource.volume = BgmVolume / 100f;
        if (sfxSource != null) sfxSource.volume = SfxVolume / 100f;
    }

    // ─────────────────────────────────────────────────────────────
    // 재생
    // ─────────────────────────────────────────────────────────────
    public void PlayBgm(AudioClip clip)
    {
        if (bgmSource == null || clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    public void PlaySfx(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, SfxVolume / 100f);
    }

    // 편의 함수 (버튼 OnClick 에 바로 연결할 수 있게 인자 없는 형태로도 제공)
    public void PlayButtonSfx() { PlaySfx(buttonSfx); }
    public void PlayAttackSfx() { PlaySfx(attackSfx); }
    public void PlayCriticalSfx() { PlaySfx(criticalSfx); }
    public void PlayHealSfx() { PlaySfx(healSfx); }
    public void PlayLevelUpSfx() { PlaySfx(levelUpSfx); }

    public void PlayMainMenuBgm() { PlayBgm(mainMenuBgm); }
    public void PlayBattleBgm() { PlayBgm(battleBgm); }
    public void PlayBossBgm() { PlayBgm(bossBgm); }
}
