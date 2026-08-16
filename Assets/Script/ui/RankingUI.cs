using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 랭킹 화면(Canvas/Ranking). 상위 5위의 이니셜과 클리어 시간을 표시한다.
/// nameTexts / timeTexts / indexTexts 는 1위부터 순서대로 연결한다.
/// </summary>
public class RankingUI : MonoBehaviour
{
    public static RankingUI instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [Header("랭킹 줄 (1위 -> 5위 순서)")]
    public Text[] nameTexts = new Text[RankingManager.MaxEntries];
    public Text[] timeTexts = new Text[RankingManager.MaxEntries];
    public Text[] indexTexts = new Text[RankingManager.MaxEntries];

    [Header("빈 칸 표시")]
    public string emptyName = "- - -";
    public string emptyTime = "--:--";

    private void Awake()
    {
        instance = this;
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        RankingEntry[] entries = RankingManager.Load();

        for (int i = 0; i < RankingManager.MaxEntries; i++)
        {
            if (indexTexts != null && i < indexTexts.Length && indexTexts[i] != null)
                indexTexts[i].text = (i + 1).ToString("00");

            bool has = entries[i] != null;

            if (nameTexts != null && i < nameTexts.Length && nameTexts[i] != null)
                nameTexts[i].text = has ? entries[i].playerName : emptyName;

            if (timeTexts != null && i < timeTexts.Length && timeTexts[i] != null)
                timeTexts[i].text = has ? RankingManager.FormatTime(entries[i].clearTime) : emptyTime;
        }
    }

    /// <summary>랭킹 초기화 버튼용.</summary>
    public void OnClickClearRanking()
    {
        RankingManager.Clear();
        Refresh();
    }
}
