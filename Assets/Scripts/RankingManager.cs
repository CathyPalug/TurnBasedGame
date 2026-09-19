using UnityEngine;

// ═════════════════════════════════════════════════════════════════════════════
//  RankingManager.cs  —  클리어 시간 랭킹 상위 5위 저장/불러오기
//
//  기획서 6) : 랭킹 정보는 게임을 끈 이후에도 저장되어야 한다.
//  → PlayerPrefs 를 쓴다. PlayerPrefs 는 유니티가 제공하는 "간단한 저장통" 이다.
//    껐다 켜도 값이 남아있다.
//
//  ★ 저장 방식을 아주 단순하게 만들었다.
//    이름은 이름끼리, 시간은 시간끼리 따로따로 5개씩 저장한다.
//      Rank_Name_0, Rank_Name_1 ... Rank_Name_4
//      Rank_Time_0, Rank_Time_1 ... Rank_Time_4
//    (JSON 같은 어려운 것 없이 for 문만으로 처리된다)
// ═════════════════════════════════════════════════════════════════════════════

public static class RankingManager
{
    /// <summary>랭킹은 5위까지만 저장한다.</summary>
    public const int MAX_RANK = 5;

    private const string NAME_KEY = "Rank_Name_";
    private const string TIME_KEY = "Rank_Time_";

    /// <summary>등수(0~4)의 이름을 읽는다. 비어 있으면 "".</summary>
    public static string GetName(int rank)
    {
        return PlayerPrefs.GetString(NAME_KEY + rank, "");
    }

    /// <summary>등수(0~4)의 클리어 시간(초)을 읽는다. 비어 있으면 0.</summary>
    public static float GetTime(int rank)
    {
        return PlayerPrefs.GetFloat(TIME_KEY + rank, 0f);
    }

    /// <summary>이 등수에 기록이 들어있는가.</summary>
    public static bool HasRecord(int rank)
    {
        if (GetName(rank) == "") return false;
        return true;
    }

    /// <summary>
    /// 새 기록을 넣는다. 5위 안에 들면 그 등수(0부터)를 돌려주고, 못 들면 -1.
    ///
    /// 방법 : 1위부터 차례로 내려가면서 "내 시간이 더 빠른 자리" 를 찾는다.
    ///        찾으면 그 아래 기록들을 한 칸씩 뒤로 밀고 그 자리에 넣는다.
    /// </summary>
    public static int AddRecord(string playerName, float clearTime)
    {
        // 1) 내가 들어갈 자리를 찾는다.
        int 내자리 = -1;

        for (int i = 0; i < MAX_RANK; i++)
        {
            // 빈 자리면 바로 거기에 들어간다.
            if (HasRecord(i) == false)
            {
                내자리 = i;
                break;
            }

            // 내 시간이 더 짧으면(= 더 빠르면) 그 자리를 차지한다.
            if (clearTime < GetTime(i))
            {
                내자리 = i;
                break;
            }
        }

        // 5위 안에 못 들었다.
        if (내자리 < 0) return -1;

        // 2) 내 자리 아래의 기록들을 한 칸씩 뒤로 민다.
        //    ★ 뒤에서부터 밀어야 값이 덮어써지지 않는다.
        for (int i = MAX_RANK - 1; i > 내자리; i--)
        {
            string 위이름 = GetName(i - 1);
            float 위시간 = GetTime(i - 1);

            PlayerPrefs.SetString(NAME_KEY + i, 위이름);
            PlayerPrefs.SetFloat(TIME_KEY + i, 위시간);
        }

        // 3) 내 기록을 넣는다.
        PlayerPrefs.SetString(NAME_KEY + 내자리, playerName);
        PlayerPrefs.SetFloat(TIME_KEY + 내자리, clearTime);
        PlayerPrefs.Save();

        return 내자리;
    }

    /// <summary>랭킹을 전부 지운다.</summary>
    public static void ClearAll()
    {
        for (int i = 0; i < MAX_RANK; i++)
        {
            PlayerPrefs.DeleteKey(NAME_KEY + i);
            PlayerPrefs.DeleteKey(TIME_KEY + i);
        }
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 초를 "분:초" 모양 글자로 바꾼다. 예) 125초 → "02:05"
    /// </summary>
    public static string TimeToText(float seconds)
    {
        if (seconds < 0f) seconds = 0f;

        int 전체초 = Mathf.FloorToInt(seconds);
        int 분 = 전체초 / 60;
        int 초 = 전체초 % 60;   // % 는 나머지를 구하는 기호

        // "00" 은 두 자리로 맞춰서 보여달라는 뜻이다. (5 → "05")
        return 분.ToString("00") + ":" + 초.ToString("00");
    }
}
