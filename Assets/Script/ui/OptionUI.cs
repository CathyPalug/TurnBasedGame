using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 화면. BGM / SFX 음량을 0~100% 로 조절한다.
/// 기존 Option 패널의 Slider 2개를 그대로 연결해서 쓴다.
/// </summary>
public class OptionUI : MonoBehaviour
{
    [Header("슬라이더 (0 ~ 100)")]
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("값 표시")]
    public Text bgmValueText;
    public Text sfxValueText;

    [Header("제목 표시")]
    public Text bgmTitleText;
    public Text sfxTitleText;
    public string bgmTitle = "BGM·배경음";
    public string sfxTitle = "SFX·효과음";

    [Header("+ / - 버튼")]
    public Button bgmPlusButton;
    public Button bgmMinusButton;
    public Button sfxPlusButton;
    public Button sfxMinusButton;
    public float stepAmount = 5f;

    [Header("닫기")]
    public Button closeButton;

    private bool initialized;

    private void Awake()
    {
        SetupSlider(bgmSlider);
        SetupSlider(sfxSlider);

        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);

        if (bgmPlusButton != null) bgmPlusButton.onClick.AddListener(delegate { Step(bgmSlider, stepAmount); });
        if (bgmMinusButton != null) bgmMinusButton.onClick.AddListener(delegate { Step(bgmSlider, -stepAmount); });
        if (sfxPlusButton != null) sfxPlusButton.onClick.AddListener(delegate { Step(sfxSlider, stepAmount); });
        if (sfxMinusButton != null) sfxMinusButton.onClick.AddListener(delegate { Step(sfxSlider, -stepAmount); });

        if (closeButton != null) closeButton.onClick.AddListener(OnClickClose);

        if (bgmTitleText != null) bgmTitleText.text = bgmTitle;
        if (sfxTitleText != null) sfxTitleText.text = sfxTitle;
    }

    private void OnEnable()
    {
        PullFromAudioManager();
    }

    private static void SetupSlider(Slider s)
    {
        if (s == null) return;
        s.minValue = 0f;
        s.maxValue = 100f;
        s.wholeNumbers = true;
    }

    private void PullFromAudioManager()
    {
        if (AudioManager.instance == null) return;

        initialized = false;
        if (bgmSlider != null) bgmSlider.value = AudioManager.instance.BgmVolume;
        if (sfxSlider != null) sfxSlider.value = AudioManager.instance.SfxVolume;
        initialized = true;

        UpdateValueTexts();
    }

    private void OnBgmChanged(float value)
    {
        if (AudioManager.instance != null && initialized) AudioManager.instance.SetBgmVolume(value);
        UpdateValueTexts();
    }

    private void OnSfxChanged(float value)
    {
        if (AudioManager.instance != null && initialized) AudioManager.instance.SetSfxVolume(value);
        UpdateValueTexts();
    }

    private void Step(Slider s, float amount)
    {
        if (s == null) return;
        s.value = Mathf.Clamp(s.value + amount, s.minValue, s.maxValue);
    }

    private void UpdateValueTexts()
    {
        if (bgmValueText != null && bgmSlider != null) bgmValueText.text = string.Format("{0}%", Mathf.RoundToInt(bgmSlider.value));
        if (sfxValueText != null && sfxSlider != null) sfxValueText.text = string.Format("{0}%", Mathf.RoundToInt(sfxSlider.value));
    }

    public void OnClickClose()
    {
        if (UIManager.instance != null) UIManager.instance.HideOption();
        else gameObject.SetActive(false);
    }
}
