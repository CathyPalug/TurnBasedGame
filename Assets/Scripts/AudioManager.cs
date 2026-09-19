using UnityEngine;

// ═════════════════════════════════════════════════════════════════════════════
//  AudioManager.cs  —  배경음악(BGM)과 효과음(SFX) 담당
//
//  기획서 6) : 설정 화면에서 BGM / SFX 음량을 0~100% 로 조절할 수 있어야 한다.
//  음량은 PlayerPrefs 에 저장되어 게임을 껐다 켜도 그대로 유지된다.
//
//  ★ 소리 파일(AudioClip)은 Inspector 에 끌어다 놓기만 하면 된다.
//    비어 있으면 그냥 소리가 안 날 뿐 에러는 나지 않게 만들어 두었다.
//
//  ★ 지금 연결된 에셋
//     - 버튼 클릭음 : Alebardium "Bloodlines UI" 의 Click Button SFX.wav
//     - 나머지(BGM, 타격음 등)는 에셋을 받으면 Inspector 에 넣기만 하면 된다.
// ═════════════════════════════════════════════════════════════════════════════

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    // PlayerPrefs 에 저장할 때 쓰는 이름표
    private const string BGM_KEY = "BgmVolume";
    private const string SFX_KEY = "SfxVolume";

    [Header("소리를 내보내는 장치 (없으면 자동으로 만든다)")]
    public AudioSource bgmSource;   // 배경음악용
    public AudioSource sfxSource;   // 효과음용

    [Header("배경음악")]
    public AudioClip menuBgm;       // 메뉴 화면
    public AudioClip battleBgm;     // 일반 전투
    public AudioClip bossBgm;       // 보스 전투

    [Header("효과음")]
    public AudioClip clickSfx;      // 버튼 클릭
    public AudioClip hitSfx;        // 공격이 맞음
    public AudioClip criticalSfx;   // 크리티컬
    public AudioClip healSfx;       // 회복
    public AudioClip levelUpSfx;    // 레벨업

    [Header("음량 (0 ~ 100)")]
    public float bgmVolume = 80f;
    public float sfxVolume = 80f;

    private void Awake()
    {
        instance = this;

        // AudioSource 가 연결 안 되어 있으면 여기서 직접 만들어 붙인다.
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;          // 배경음악은 계속 반복
            bgmSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        // 저장해 둔 음량을 불러온다. 저장된 게 없으면 80 을 쓴다.
        bgmVolume = PlayerPrefs.GetFloat(BGM_KEY, 80f);
        sfxVolume = PlayerPrefs.GetFloat(SFX_KEY, 80f);
        ApplyVolume();
    }

    private void Start()
    {
        PlayMenuBgm();
    }


    // ═════════════════════════════════════════════════════════════
    //  음량 조절 (설정 화면에서 부른다)
    // ═════════════════════════════════════════════════════════════

    /// <summary>BGM 음량을 0~100 사이 값으로 정한다.</summary>
    public void SetBgmVolume(float value)
    {
        bgmVolume = Mathf.Clamp(value, 0f, 100f);

        PlayerPrefs.SetFloat(BGM_KEY, bgmVolume);
        PlayerPrefs.Save();     // 지금 바로 파일에 저장

        ApplyVolume();
    }

    /// <summary>SFX 음량을 0~100 사이 값으로 정한다.</summary>
    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp(value, 0f, 100f);

        PlayerPrefs.SetFloat(SFX_KEY, sfxVolume);
        PlayerPrefs.Save();

        ApplyVolume();
    }

    /// <summary>0~100 값을 유니티가 쓰는 0~1 값으로 바꿔서 넣어준다.</summary>
    private void ApplyVolume()
    {
        if (bgmSource != null) bgmSource.volume = bgmVolume / 100f;
        if (sfxSource != null) sfxSource.volume = sfxVolume / 100f;
    }


    // ═════════════════════════════════════════════════════════════
    //  재생
    // ═════════════════════════════════════════════════════════════

    /// <summary>배경음악을 바꾼다. 이미 같은 곡이 나오고 있으면 아무것도 안 한다.</summary>
    public void PlayBgm(AudioClip clip)
    {
        if (bgmSource == null) return;
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    /// <summary>효과음을 한 번 재생한다.</summary>
    public void PlaySfx(AudioClip clip)
    {
        if (sfxSource == null) return;
        if (clip == null) return;

        // PlayOneShot = 여러 소리가 겹쳐서 나도 괜찮게 재생해주는 함수
        sfxSource.PlayOneShot(clip, sfxVolume / 100f);
    }

    // ── 버튼 OnClick 에 바로 연결할 수 있도록 인자 없는 함수로도 만들어 둔다 ──
    public void PlayMenuBgm() { PlayBgm(menuBgm); }
    public void PlayBattleBgm() { PlayBgm(battleBgm); }
    public void PlayBossBgm() { PlayBgm(bossBgm); }

    public void PlayClick() { PlaySfx(clickSfx); }
    public void PlayHit() { PlaySfx(hitSfx); }
    public void PlayCritical() { PlaySfx(criticalSfx); }
    public void PlayHeal() { PlaySfx(healSfx); }
    public void PlayLevelUp() { PlaySfx(levelUpSfx); }
}
