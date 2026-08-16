using System;
using UnityEngine;

[Serializable]
public class RankingEntry
{
    public string playerName = "---";
    public float clearTime;
}

[Serializable]
public class RankingTable
{
    public RankingEntry[] entries = new RankingEntry[0];
}

/// <summary>
/// 상위 5위 클리어 기록을 PlayerPrefs 에 저장한다. (게임을 껐다 켜도 유지된다)
/// </summary>
public static class RankingManager
{
    public const int MaxEntries = 5;
    private const string SaveKey = "TurnBasedGame.Ranking.v1";

    /// <summary>저장된 랭킹을 시간이 짧은 순으로 돌려준다. 항상 길이 5.</summary>
    public static RankingEntry[] Load()
    {
        RankingEntry[] result = new RankingEntry[MaxEntries];

        RankingTable table = null;
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (!string.IsNullOrEmpty(json))
        {
            try { table = JsonUtility.FromJson<RankingTable>(json); }
            catch (Exception e) { Debug.LogWarning("랭킹 데이터를 읽지 못했습니다 : " + e.Message); }
        }

        for (int i = 0; i < MaxEntries; i++)
        {
            if (table != null && table.entries != null && i < table.entries.Length && table.entries[i] != null)
                result[i] = table.entries[i];
            else
                result[i] = null;
        }

        return result;
    }

    public static void Save(RankingEntry[] entries)
    {
        RankingTable table = new RankingTable();

        int count = 0;
        for (int i = 0; i < entries.Length && i < MaxEntries; i++)
        {
            if (entries[i] != null) count++;
        }

        table.entries = new RankingEntry[count];
        int k = 0;
        for (int i = 0; i < entries.Length && i < MaxEntries; i++)
        {
            if (entries[i] != null) table.entries[k++] = entries[i];
        }

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(table));
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 기록을 등록한다. 5위 안에 들면 순위(0부터)를 돌려주고, 들지 못하면 -1.
    /// </summary>
    public static int Register(string playerName, float clearTime)
    {
        RankingEntry[] current = Load();

        RankingEntry[] merged = new RankingEntry[MaxEntries + 1];
        for (int i = 0; i < MaxEntries; i++) merged[i] = current[i];
        merged[MaxEntries] = new RankingEntry { playerName = playerName, clearTime = clearTime };

        // null 을 뒤로 보내면서 시간 오름차순 정렬
        Array.Sort(merged, delegate (RankingEntry a, RankingEntry b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            return a.clearTime.CompareTo(b.clearTime);
        });

        int rank = -1;
        for (int i = 0; i < MaxEntries; i++)
        {
            if (merged[i] != null && ReferenceEquals(merged[i], merged[i]) &&
                merged[i].playerName == playerName && Mathf.Approximately(merged[i].clearTime, clearTime))
            {
                rank = i;
                break;
            }
        }

        RankingEntry[] top = new RankingEntry[MaxEntries];
        for (int i = 0; i < MaxEntries; i++) top[i] = merged[i];

        Save(top);
        return rank;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }

    /// <summary>초를 "MM:SS" 형태로 바꾼다. 1시간을 넘으면 "HH:MM:SS".</summary>
    public static string FormatTime(float seconds)
    {
        if (seconds < 0f) seconds = 0f;

        int total = Mathf.FloorToInt(seconds);
        int hours = total / 3600;
        int minutes = (total % 3600) / 60;
        int secs = total % 60;

        if (hours > 0) return string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, secs);
        return string.Format("{0:00}:{1:00}", minutes, secs);
    }
}
