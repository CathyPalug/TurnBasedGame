using UnityEngine;

/// <summary>
/// 화면(패널) 전환과 일시정지를 담당한다.
///
/// 메인 패널은 서로 배타적이고(StartMenu / Map / Battle / Shop / Ranking / Introduce / HowToPlay),
/// 오버레이 패널(Option / Pause / Inventory / Nickname / Result)은 그 위에 겹쳐 뜬다.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [Header("메인 패널")]
    public GameObject startMenuPanel;
    public GameObject introducePanel;
    public GameObject howToPlayPanel;
    public GameObject rankingPanel;
    public GameObject mapPanel;
    public GameObject battlePanel;
    public GameObject shopPanel;

    [Header("오버레이 패널")]
    public GameObject optionPanel;
    public GameObject pausePanel;
    public GameObject inventoryPanel;
    public GameObject nicknamePanel;
    public GameObject resultPanel;

    [Header("일시정지")]
    public bool IsPaused { get; private set; }

    private GameObject currentMain;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        ShowMainMenu();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) HandleEscape();
    }

    // ─────────────────────────────────────────────────────────────
    // 메인 패널 전환
    // ─────────────────────────────────────────────────────────────
    private void ShowMain(GameObject panel)
    {
        SetActive(startMenuPanel, panel == startMenuPanel);
        SetActive(introducePanel, panel == introducePanel);
        SetActive(howToPlayPanel, panel == howToPlayPanel);
        SetActive(rankingPanel, panel == rankingPanel);
        SetActive(mapPanel, panel == mapPanel);
        SetActive(battlePanel, panel == battlePanel);
        SetActive(shopPanel, panel == shopPanel);

        currentMain = panel;
    }

    public void ShowMainMenu()
    {
        CloseAllOverlays();
        SetPaused(false);
        ShowMain(startMenuPanel);
        if (GameManager.instance != null) GameManager.instance.state = GameState.MainMenu;
    }

    public void ShowIntroduce() { ShowMain(introducePanel); }

    public void ShowHowToPlay() { ShowMain(howToPlayPanel); }

    public void ShowRanking()
    {
        ShowMain(rankingPanel);
        if (RankingUI.instance != null) RankingUI.instance.Refresh();
    }

    public void ShowMap()
    {
        CloseAllOverlays();
        ShowMain(mapPanel);
        if (GameManager.instance != null) GameManager.instance.state = GameState.Map;
        if (StageManager.instance != null) StageManager.instance.UpdateRooms();
    }

    public void ShowBattle()
    {
        CloseAllOverlays();
        ShowMain(battlePanel);
        if (GameManager.instance != null) GameManager.instance.state = GameState.Battle;
    }

    public void ShowShop()
    {
        CloseAllOverlays();
        ShowMain(shopPanel);
        if (GameManager.instance != null) GameManager.instance.state = GameState.Shop;
        if (ShopUI.instance != null) ShopUI.instance.Refresh();
    }

    /// <summary>메인 메뉴의 '게임 시작'.</summary>
    public void OnClickStartGame()
    {
        if (GameManager.instance != null) GameManager.instance.StartNewGame();
    }

    /// <summary>'뒤로' 버튼 : 게임 중이면 지도로, 아니면 메인 메뉴로.</summary>
    public void OnClickBack()
    {
        if (GameManager.instance != null && GameManager.instance.IsPlaying) ShowMap();
        else ShowMainMenu();
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─────────────────────────────────────────────────────────────
    // 오버레이
    // ─────────────────────────────────────────────────────────────
    public void ShowOption() { SetActive(optionPanel, true); }

    public void HideOption() { SetActive(optionPanel, false); }

    public void ToggleInventory()
    {
        if (inventoryPanel == null) return;
        bool next = !inventoryPanel.activeSelf;
        SetActive(inventoryPanel, next);
        if (next && InventoryUI.instance != null) InventoryUI.instance.Refresh();
    }

    public void ShowInventory()
    {
        SetActive(inventoryPanel, true);
        if (InventoryUI.instance != null) InventoryUI.instance.Refresh();
    }

    public void HideInventory() { SetActive(inventoryPanel, false); }

    public void ShowNicknameInput()
    {
        SetActive(nicknamePanel, true);
        if (NicknameInputUI.instance != null) NicknameInputUI.instance.Open();
    }

    public void HideNicknameInput() { SetActive(nicknamePanel, false); }

    public void ShowResult(string message)
    {
        SetActive(resultPanel, true);
        ResultUI ui = resultPanel != null ? resultPanel.GetComponent<ResultUI>() : null;
        if (ui != null) ui.Show(message);
    }

    public void HideResult() { SetActive(resultPanel, false); }

    public void CloseAllOverlays()
    {
        SetActive(optionPanel, false);
        SetActive(pausePanel, false);
        SetActive(inventoryPanel, false);
        SetActive(nicknamePanel, false);
        SetActive(resultPanel, false);
    }

    // ─────────────────────────────────────────────────────────────
    // 일시정지 (ESC)
    // ─────────────────────────────────────────────────────────────
    private void HandleEscape()
    {
        // 옵션이 열려 있으면 옵션만 닫는다.
        if (optionPanel != null && optionPanel.activeSelf)
        {
            HideOption();
            return;
        }

        if (nicknamePanel != null && nicknamePanel.activeSelf) return;

        // 게임 진행 중일 때만 일시정지가 동작한다.
        if (GameManager.instance == null || !GameManager.instance.IsPlaying)
        {
            return;
        }

        SetPaused(!IsPaused);
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        SetActive(pausePanel, paused);

        if (!paused) HideOption();
    }

    public void OnClickResume() { SetPaused(false); }

    public void OnClickPauseToMainMenu()
    {
        SetPaused(false);
        if (GameManager.instance != null) GameManager.instance.ReturnToMainMenu();
    }

    // ─────────────────────────────────────────────────────────────
    private static void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active) go.SetActive(active);
    }
}
