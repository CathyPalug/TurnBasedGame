using UnityEngine;
using UnityEngine.UI;

// ═════════════════════════════════════════════════════════════════════════════
//  RankingUI.cs  —  랭킹 화면 (상위 5위)
//
//  기획서 6) : 게임 종료 시 랭킹 UI 가 표시되며, 상위 5위까지 클리어 시간을 표기.
//              랭킹 정보는 게임을 끈 이후에도 저장되어야 한다.
//
//  ★ 실제 저장은 RankingManager 가 PlayerPrefs 로 해준다.
//    여기서는 읽어와서 화면에 보여주기만 한다.
// ═════════════════════════════════════════════════════════════════════════════

public class RankingUI : MonoBehaviour
{
    public static RankingUI instance;

    [Header("랭킹 줄 (1위부터 5위 순서로 연결한다)")]
    public Text[] rankTexts = new Text[RankingManager.MAX_RANK];

    [Header("랭킹 초기화 버튼 (없어도 됨)")]
    public Button clearButton;

    private void Awake()
    {
        instance = this;

        if (clearButton != null) clearButton.onClick.AddListener(OnClickClear);
    }

    // 화면이 켜질 때마다 자동으로 새로 그린다.
    private void OnEnable()
    {
        Refresh();
    }

    /// <summary>저장된 랭킹을 읽어서 화면에 그린다.</summary>
    public void Refresh()
    {
        if (rankTexts == null) return;

        for (int i = 0; i < rankTexts.Length; i++)
        {
            if (rankTexts[i] == null) continue;

            // 등수는 0부터 세므로 화면에는 +1 해서 보여준다.
            string 등수 = (i + 1) + "위   ";

            if (RankingManager.HasRecord(i))
            {
                string 이름 = RankingManager.GetName(i);
                string 시간 = RankingManager.TimeToText(RankingManager.GetTime(i));

                rankTexts[i].text = 등수 + 이름 + "        " + 시간;
            }
            else
            {
                // 기록이 없는 자리
                rankTexts[i].text = 등수 + "- - -" + "        --:--";
            }
        }
    }

    /// <summary>랭킹 초기화 버튼.</summary>
    public void OnClickClear()
    {
        RankingManager.ClearAll();
        Refresh();
    }
}
