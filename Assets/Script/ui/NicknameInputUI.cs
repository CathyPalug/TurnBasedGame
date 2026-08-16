using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 랭킹 등록용 이니셜 입력. 키보드가 아니라 버튼 클릭으로 글자를 입력한다.
/// 기획서 : "2개 단어 이상 입력 시 완료 버튼을 활성화한다"
/// -> minLength(기본 2) 글자 이상일 때만 완료 버튼이 켜진다.
/// </summary>
public class NicknameInputUI : MonoBehaviour
{
    public static NicknameInputUI instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [Header("표시")]
    public Text displayText;
    public Text guideText;
    public Text clearTimeText;

    [Header("입력 버튼")]
    [Tooltip("각 버튼의 자식 Text 에 적힌 글자가 그대로 입력된다.")]
    public Button[] letterButtons;
    public Button backspaceButton;
    public Button confirmButton;

    [Header("규칙")]
    [Tooltip("완료 버튼이 켜지는 최소 글자 수")]
    public int minLength = 2;
    public int maxLength = 8;

    private string current = string.Empty;

    private void Awake()
    {
        instance = this;
        BindButtons();
    }

    private void BindButtons()
    {
        if (letterButtons != null)
        {
            for (int i = 0; i < letterButtons.Length; i++)
            {
                if (letterButtons[i] == null) continue;

                Text label = letterButtons[i].GetComponentInChildren<Text>(true);
                string letter = label != null ? label.text.Trim() : string.Empty;
                if (string.IsNullOrEmpty(letter)) continue;

                string captured = letter;
                letterButtons[i].onClick.AddListener(delegate { Append(captured); });
            }
        }

        if (backspaceButton != null) backspaceButton.onClick.AddListener(Backspace);
        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
    }

    public void Open()
    {
        current = string.Empty;

        if (clearTimeText != null && GameManager.instance != null)
        {
            clearTimeText.text = string.Format("CLEAR TIME  {0}",
                RankingManager.FormatTime(GameManager.instance.playTime));
        }

        if (guideText != null)
        {
            guideText.text = string.Format("{0}글자 이상 입력하세요", minLength);
        }

        UpdateView();
    }

    public void Append(string letter)
    {
        if (current.Length >= maxLength) return;
        current += letter;
        UpdateView();
    }

    public void Backspace()
    {
        if (current.Length == 0) return;
        current = current.Substring(0, current.Length - 1);
        UpdateView();
    }

    public void Confirm()
    {
        if (current.Length < minLength) return;

        float time = GameManager.instance != null ? GameManager.instance.playTime : 0f;
        int rank = RankingManager.Register(current, time);

        if (RankingUI.instance != null) RankingUI.instance.Refresh();

        if (UIManager.instance != null)
        {
            UIManager.instance.HideNicknameInput();
            UIManager.instance.ShowRanking();
        }

        Debug.Log(string.Format("랭킹 등록 : {0} / {1} / 순위 {2}",
            current, RankingManager.FormatTime(time), rank < 0 ? "순위권 밖" : (rank + 1).ToString()));
    }

    private void UpdateView()
    {
        if (displayText != null) displayText.text = current;
        if (confirmButton != null) confirmButton.interactable = current.Length >= minLength;
    }
}
