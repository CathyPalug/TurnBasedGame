using UnityEngine;
using UnityEngine.UI;

// ═════════════════════════════════════════════════════════════════════════════
//  UIManager.cs  —  화면(패널) 전환과 일시정지 담당
//
//  ── 화면 종류 ──
//   [메인 화면] 한 번에 하나만 켜진다
//     menuPanel(메뉴) / introPanel(게임소개) / howToPanel(게임방법)
//     rankingPanel(랭킹) / mapPanel(지도) / battlePanel(전투) / shopPanel(상점)
//
//   [겹쳐 뜨는 화면] 위에 덮어서 뜬다
//     pausePanel(일시정지) / optionPanel(설정) / bagPanel(배낭)
//     namePanel(이니셜 입력) / resultPanel(결과)
//
//  ── ESC 규칙 (기획서 6번) ──
//     설정이 열려 있으면 → 설정만 닫는다
//     그 외 게임 진행 중이면 → 일시정지 켜기/끄기 (시간이 멈춘다)
// ═════════════════════════════════════════════════════════════════════════════

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("메인 화면 (한 번에 하나만 켜진다)")]
    public GameObject menuPanel;      // 시작 메뉴
    public GameObject introPanel;     // 게임 소개
    public GameObject howToPanel;     // 게임 방법
    public GameObject rankingPanel;   // 랭킹
    public GameObject mapPanel;       // 지도
    public GameObject battlePanel;    // 전투
    public GameObject shopPanel;      // 상점 및 휴식

    [Header("겹쳐 뜨는 화면")]
    public GameObject pausePanel;     // 일시정지
    public GameObject optionPanel;    // 설정 (BGM/SFX 음량)
    public GameObject bagPanel;       // 배낭
    public GameObject namePanel;      // 이니셜 입력
    public GameObject resultPanel;    // 결과 (게임오버 등)

    [Header("결과 화면 안의 글자")]
    public Text resultText;

    [Header("설정 화면 (기획서 : BGM/SFX 0~100%)")]
    public Slider bgmSlider;
    public Slider sfxSlider;
    public Text bgmValueText;
    public Text sfxValueText;

    /// <summary>지금 일시정지 중인가.</summary>
    public bool isPaused;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        SetUpOptionSliders();
        ShowMenu();
    }

    private void Update()
    {
        // ESC 키 처리 (기획서 6번)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnEscape();
        }
    }


    // ═════════════════════════════════════════════════════════════
    //  메인 화면 전환
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// 메인 화면 중 하나만 켜고 나머지는 전부 끈다.
    /// 켜고 싶은 패널을 넣으면 그것만 켜진다.
    /// </summary>
    private void ShowOnly(GameObject target)
    {
        SetOn(menuPanel, menuPanel == target);
        SetOn(introPanel, introPanel == target);
        SetOn(howToPanel, howToPanel == target);
        SetOn(rankingPanel, rankingPanel == target);
        SetOn(mapPanel, mapPanel == target);
        SetOn(battlePanel, battlePanel == target);
        SetOn(shopPanel, shopPanel == target);
    }

    /// <summary>오브젝트를 켜거나 끈다. null 이어도 에러가 안 나게 확인한다.</summary>
    private void SetOn(GameObject go, bool on)
    {
        if (go == null) return;
        if (go.activeSelf == on) return;   // 이미 그 상태면 안 건드린다

        go.SetActive(on);
    }

    public void ShowMenu()
    {
        CloseAllPopups();
        SetPause(false);
        ShowOnly(menuPanel);

        if (GameManager.instance != null) GameManager.instance.state = GameState.Menu;
    }

    public void ShowIntro() { ShowOnly(introPanel); }

    public void ShowHowTo() { ShowOnly(howToPanel); }

    public void ShowRanking()
    {
        ShowOnly(rankingPanel);
        if (RankingUI.instance != null) RankingUI.instance.Refresh();
    }

    public void ShowMap()
    {
        CloseAllPopups();
        ShowOnly(mapPanel);

        if (GameManager.instance != null) GameManager.instance.state = GameState.Map;
        if (StageManager.instance != null) StageManager.instance.RefreshMap();
        if (AudioManager.instance != null) AudioManager.instance.PlayMenuBgm();
    }

    public void ShowBattle()
    {
        CloseAllPopups();
        ShowOnly(battlePanel);

        if (GameManager.instance != null) GameManager.instance.state = GameState.Battle;
    }

    public void ShowShop()
    {
        CloseAllPopups();
        ShowOnly(shopPanel);

        if (GameManager.instance != null) GameManager.instance.state = GameState.Shop;
        if (ShopUI.instance != null) ShopUI.instance.Refresh();
    }

    /// <summary>[뒤로] 버튼 : 게임 중이면 지도로, 아니면 메뉴로.</summary>
    public void OnClickBack()
    {
        if (GameManager.instance != null && GameManager.instance.IsPlaying()) ShowMap();
        else ShowMenu();
    }

    /// <summary>[게임 시작] 버튼.</summary>
    public void OnClickStart()
    {
        if (GameManager.instance != null) GameManager.instance.StartNewGame();
    }

    /// <summary>[종료] 버튼.</summary>
    public void OnClickQuit()
    {
        // 에디터에서 테스트 중일 때는 플레이 모드를 끄고,
        // 진짜 빌드된 게임에서는 창을 닫는다.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }


    // ═════════════════════════════════════════════════════════════
    //  겹쳐 뜨는 화면
    // ═════════════════════════════════════════════════════════════

    public void CloseAllPopups()
    {
        SetOn(pausePanel, false);
        SetOn(optionPanel, false);
        SetOn(bagPanel, false);
        SetOn(namePanel, false);
        SetOn(resultPanel, false);
    }

    /// <summary>배낭을 열거나 닫는다.</summary>
    public void ToggleBag()
    {
        if (bagPanel == null) return;

        bool 켤까 = !bagPanel.activeSelf;
        SetOn(bagPanel, 켤까);

        if (켤까 && BagUI.instance != null) BagUI.instance.Refresh();
    }

    public void CloseBag() { SetOn(bagPanel, false); }

    public void ShowOption()
    {
        SetOn(optionPanel, true);
        RefreshOptionSliders();
    }

    public void CloseOption() { SetOn(optionPanel, false); }

    /// <summary>이니셜 입력 화면을 띄운다. (게임 클리어 시)</summary>
    public void ShowNameInput()
    {
        SetOn(namePanel, true);
        if (NameInputUI.instance != null) NameInputUI.instance.Open();
    }

    public void CloseNameInput() { SetOn(namePanel, false); }

    /// <summary>결과 화면을 띄운다.</summary>
    public void ShowResult(string message, bool win)
    {
        SetOn(resultPanel, true);
        if (resultText != null) resultText.text = message;
    }

    public void CloseResult() { SetOn(resultPanel, false); }

    /// <summary>결과 화면의 [확인] 버튼 : 메뉴로 돌아간다.</summary>
    public void OnClickResultOk()
    {
        CloseResult();
        if (GameManager.instance != null) GameManager.instance.GoToMenu();
    }


    // ═════════════════════════════════════════════════════════════
    //  일시정지 (기획서 6번)
    // ═════════════════════════════════════════════════════════════

    private void OnEscape()
    {
        // 1) 설정이 열려 있으면 설정만 닫는다.
        if (optionPanel != null && optionPanel.activeSelf)
        {
            CloseOption();
            return;
        }

        // 2) 이니셜 입력 중에는 일시정지를 막는다. (입력이 꼬이기 때문)
        if (namePanel != null && namePanel.activeSelf) return;

        // 3) 게임 진행 중일 때만 일시정지가 동작한다.
        if (GameManager.instance == null) return;
        if (GameManager.instance.IsPlaying() == false) return;

        SetPause(!isPaused);   // 켜져 있으면 끄고, 꺼져 있으면 켠다
    }

    /// <summary>일시정지를 켜거나 끈다. 켜면 게임 시간이 완전히 멈춘다.</summary>
    public void SetPause(bool on)
    {
        isPaused = on;

        // Time.timeScale = 게임 시간의 속도.
        //   1 이면 보통 속도, 0 이면 완전히 멈춤.
        if (on) Time.timeScale = 0f;
        else Time.timeScale = 1f;

        SetOn(pausePanel, on);

        // 일시정지를 끄면 설정 화면도 같이 닫는다.
        if (on == false) CloseOption();
    }

    /// <summary>일시정지 화면의 [계속하기] 버튼.</summary>
    public void OnClickResume() { SetPause(false); }

    /// <summary>일시정지 화면의 [메인 메뉴] 버튼.</summary>
    public void OnClickPauseToMenu()
    {
        SetPause(false);
        if (GameManager.instance != null) GameManager.instance.GoToMenu();
    }


    // ═════════════════════════════════════════════════════════════
    //  설정 화면 (BGM / SFX 음량)
    // ═════════════════════════════════════════════════════════════

    /// <summary>슬라이더를 0~100 범위로 맞추고, 움직였을 때 할 일을 연결한다.</summary>
    private void SetUpOptionSliders()
    {
        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 100f;
            bgmSlider.wholeNumbers = true;   // 소수점 없이 정수만
            bgmSlider.onValueChanged.AddListener(OnBgmSliderMoved);
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 100f;
            sfxSlider.wholeNumbers = true;
            sfxSlider.onValueChanged.AddListener(OnSfxSliderMoved);
        }

        RefreshOptionSliders();
    }

    /// <summary>지금 저장된 음량 값을 슬라이더에 표시한다.</summary>
    private void RefreshOptionSliders()
    {
        if (AudioManager.instance == null) return;

        if (bgmSlider != null) bgmSlider.value = AudioManager.instance.bgmVolume;
        if (sfxSlider != null) sfxSlider.value = AudioManager.instance.sfxVolume;

        ShowVolumeText();
    }

    private void OnBgmSliderMoved(float value)
    {
        if (AudioManager.instance != null) AudioManager.instance.SetBgmVolume(value);
        ShowVolumeText();
    }

    private void OnSfxSliderMoved(float value)
    {
        if (AudioManager.instance != null) AudioManager.instance.SetSfxVolume(value);
        ShowVolumeText();
    }

    /// <summary>슬라이더 옆에 "80%" 처럼 숫자를 보여준다.</summary>
    private void ShowVolumeText()
    {
        if (bgmValueText != null && bgmSlider != null)
            bgmValueText.text = Mathf.RoundToInt(bgmSlider.value) + "%";

        if (sfxValueText != null && sfxSlider != null)
            sfxValueText.text = Mathf.RoundToInt(sfxSlider.value) + "%";
    }
}
