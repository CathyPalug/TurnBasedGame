using UnityEngine;
using UnityEngine.UI;

// ═════════════════════════════════════════════════════════════════════════════
//  NameInputUI.cs  —  게임 클리어 후 이니셜 입력 화면
//
//  기획서 6) : 닉네임은 버튼 형태로 마우스 클릭을 통하여 입력하고,
//              2개 단어(글자) 이상 입력 시 완료 버튼을 활성화한다.
//
//  ★ 키보드로 타이핑하는 게 아니라, 글자 버튼을 마우스로 눌러서 입력한다.
//    각 버튼의 자식 Text 에 적힌 글자가 그대로 입력된다.
//    (A~Z 버튼 26개를 만들어 letterButtons 에 연결하면 된다)
// ═════════════════════════════════════════════════════════════════════════════

public class NameInputUI : MonoBehaviour
{
    public static NameInputUI instance;

    [Header("표시")]
    public Text displayText;    // 지금까지 입력한 글자
    public Text guideText;      // "2글자 이상 입력하세요"
    public Text clearTimeText;  // 클리어 시간

    [Header("입력 버튼")]
    [Tooltip("A~Z 버튼들. 버튼 안의 Text 글자가 그대로 입력된다.")]
    public Button[] letterButtons;

    public Button backButton;       // 한 글자 지우기
    public Button confirmButton;    // 완료

    [Header("규칙")]
    [Tooltip("완료 버튼이 켜지는 최소 글자 수 (기획서 : 2개 이상)")]
    public int minLength = 2;

    [Tooltip("최대 글자 수")]
    public int maxLength = 6;

    /// <summary>지금까지 입력한 글자.</summary>
    private string nowName = "";

    private void Awake()
    {
        instance = this;
        SetUpButtons();
    }

    /// <summary>버튼들에 할 일을 연결한다.</summary>
    private void SetUpButtons()
    {
        if (letterButtons != null)
        {
            for (int i = 0; i < letterButtons.Length; i++)
            {
                if (letterButtons[i] == null) continue;

                // 버튼 안에 적힌 글자를 읽어온다.
                Text 글자표시 = letterButtons[i].GetComponentInChildren<Text>();
                if (글자표시 == null) continue;

                string 글자 = 글자표시.text.Trim();   // Trim = 앞뒤 공백 제거
                if (글자 == "") continue;

                // ★ 반복문 변수를 그대로 쓰면 안 되므로 복사해서 쓴다.
                string 이글자 = 글자;
                letterButtons[i].onClick.AddListener(delegate { AddLetter(이글자); });
            }
        }

        if (backButton != null) backButton.onClick.AddListener(DeleteLetter);
        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
    }

    /// <summary>화면이 열릴 때 초기화한다.</summary>
    public void Open()
    {
        nowName = "";

        if (clearTimeText != null && GameManager.instance != null)
        {
            clearTimeText.text = "CLEAR TIME   "
                + RankingManager.TimeToText(GameManager.instance.playTime);
        }

        if (guideText != null)
        {
            guideText.text = minLength + "글자 이상 입력하세요";
        }

        Redraw();
    }

    /// <summary>글자를 하나 추가한다.</summary>
    public void AddLetter(string letter)
    {
        if (nowName.Length >= maxLength) return;   // 꽉 찼으면 무시

        nowName = nowName + letter;

        if (AudioManager.instance != null) AudioManager.instance.PlayClick();
        Redraw();
    }

    /// <summary>마지막 글자를 하나 지운다.</summary>
    public void DeleteLetter()
    {
        if (nowName.Length == 0) return;

        // Substring(0, 길이-1) = 맨 뒤 한 글자를 뺀 나머지
        nowName = nowName.Substring(0, nowName.Length - 1);

        if (AudioManager.instance != null) AudioManager.instance.PlayClick();
        Redraw();
    }

    /// <summary>완료 버튼 : 랭킹에 등록한다.</summary>
    public void Confirm()
    {
        // 기획서 : 2글자 이상일 때만 완료할 수 있다.
        if (nowName.Length < minLength) return;

        float 클리어시간 = 0f;
        if (GameManager.instance != null) 클리어시간 = GameManager.instance.playTime;

        int 등수 = RankingManager.AddRecord(nowName, 클리어시간);

        if (등수 >= 0) Debug.Log("랭킹 등록! " + (등수 + 1) + "위");
        else Debug.Log("아쉽게도 5위 안에 들지 못했습니다.");

        // 랭킹 화면으로 넘어간다.
        if (UIManager.instance != null)
        {
            UIManager.instance.CloseNameInput();
            UIManager.instance.ShowRanking();
        }

        if (RankingUI.instance != null) RankingUI.instance.Refresh();
    }

    /// <summary>화면을 다시 그린다.</summary>
    private void Redraw()
    {
        if (displayText != null) displayText.text = nowName;

        // 기획서 : 2글자 이상 입력하면 완료 버튼이 켜진다.
        if (confirmButton != null)
        {
            confirmButton.interactable = (nowName.Length >= minLength);
        }
    }
}
