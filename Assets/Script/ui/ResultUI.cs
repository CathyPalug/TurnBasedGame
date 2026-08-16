using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 오버 / 결과 안내 패널. 확인을 누르면 메인 메뉴로 돌아간다.
/// </summary>
public class ResultUI : MonoBehaviour
{
    public Text messageText;
    public Button confirmButton;
    public Button rankingButton;

    private void Awake()
    {
        if (confirmButton != null) confirmButton.onClick.AddListener(OnClickConfirm);
        if (rankingButton != null) rankingButton.onClick.AddListener(OnClickRanking);
    }

    public void Show(string message)
    {
        if (messageText != null) messageText.text = message;
    }

    public void OnClickConfirm()
    {
        if (UIManager.instance != null) UIManager.instance.HideResult();
        if (GameManager.instance != null) GameManager.instance.ReturnToMainMenu();
    }

    public void OnClickRanking()
    {
        if (UIManager.instance == null) return;
        UIManager.instance.HideResult();
        if (GameManager.instance != null) GameManager.instance.ReturnToMainMenu();
        UIManager.instance.ShowRanking();
    }
}
