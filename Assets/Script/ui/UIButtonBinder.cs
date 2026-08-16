using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 메인 메뉴 / 뒤로 / 옵션 같은 화면 전환 버튼을 코드에서 UIManager 에 연결한다.
///
/// 인스펙터의 OnClick() 에 직접 넣지 않고 여기서 연결하는 이유
///   - 기존에 버튼마다 "패널.SetActive" 가 직접 걸려 있어 UIManager 의 화면 관리와 충돌한다.
///   - 여기서 기존 이벤트를 꺼버리고 필요한 동작만 다시 붙이면 인스펙터를 건드리지 않아도 된다.
/// </summary>
public class UIButtonBinder : MonoBehaviour
{
    [Header("메인 메뉴")]
    public Button startGameButton;
    public Button introduceButton;
    public Button howToPlayButton;
    public Button rankingButton;
    public Button optionButton;
    public Button quitButton;

    [Header("뒤로 가기 (랭킹 / 소개 / 방법)")]
    public Button[] backButtons;

    [Header("옵션")]
    public Button optionCloseButton;

    [Header("배낭 열기 (Status 안의 버튼)")]
    public Button[] inventoryButtons;

    [Header("일시정지 패널")]
    public Button resumeButton;
    public Button pauseOptionButton;
    public Button pauseMainMenuButton;

    private void Awake()
    {
        UIManager ui = UIManager.instance;
        if (ui == null) ui = FindFirstObjectByType<UIManager>();
        if (ui == null)
        {
            Debug.LogError("UIButtonBinder : UIManager 를 찾을 수 없습니다.");
            return;
        }

        Bind(startGameButton, ui.OnClickStartGame);
        Bind(introduceButton, ui.ShowIntroduce);
        Bind(howToPlayButton, ui.ShowHowToPlay);
        Bind(rankingButton, ui.ShowRanking);
        Bind(optionButton, ui.ShowOption);
        Bind(quitButton, ui.OnClickQuit);
        Bind(optionCloseButton, ui.HideOption);

        if (backButtons != null)
        {
            for (int i = 0; i < backButtons.Length; i++) Bind(backButtons[i], ui.OnClickBack);
        }

        if (inventoryButtons != null)
        {
            for (int i = 0; i < inventoryButtons.Length; i++) Bind(inventoryButtons[i], ui.ToggleInventory);
        }

        Bind(resumeButton, ui.OnClickResume);
        Bind(pauseOptionButton, ui.ShowOption);
        Bind(pauseMainMenuButton, ui.OnClickPauseToMainMenu);
    }

    private static void Bind(Button button, UnityAction action)
    {
        if (button == null) return;
        DisablePersistentListeners(button);
        button.onClick.AddListener(action);
    }

    /// <summary>
    /// 인스펙터에 미리 걸려 있던 OnClick() 항목을 런타임에 꺼버린다.
    /// (씬 데이터는 그대로 두고 동작만 막는다)
    /// </summary>
    public static void DisablePersistentListeners(Button button)
    {
        if (button == null) return;

        int count = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
        }
    }
}
