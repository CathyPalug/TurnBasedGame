using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 로그 4줄 표시. 어디서든 BattleLog.Log(...) 로 한 줄 추가할 수 있다.
/// </summary>
public class BattleLog : MonoBehaviour
{
    private static BattleLog cached;

    /// <summary>패널이 꺼져 있어 Awake 가 돌지 않았어도 씬에서 찾아온다.</summary>
    public static BattleLog Instance
    {
        get
        {
            if (cached == null)
                cached = FindFirstObjectByType<BattleLog>(FindObjectsInactive.Include);
            return cached;
        }
        private set { cached = value; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cached = null;
    }

    [Tooltip("위에서 아래 순서로 넣는다. 마지막 요소가 가장 최근 로그.")]
    public Text[] txt_Logs;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (cached == this) cached = null;
    }

    /// <summary>인스턴스가 없어도 안전하게 호출할 수 있는 정적 진입점.</summary>
    public static void Log(string txt)
    {
        if (Instance != null) Instance.AddBattleLog(txt);
        else Debug.Log("[BattleLog] " + txt);
    }

    public void AddBattleLog(string txt)
    {
        if (txt_Logs == null || txt_Logs.Length == 0) return;

        for (int i = 0; i < txt_Logs.Length - 1; i++)
        {
            if (txt_Logs[i] == null || txt_Logs[i + 1] == null) continue;
            txt_Logs[i].text = txt_Logs[i + 1].text;
        }

        Text last = txt_Logs[txt_Logs.Length - 1];
        if (last != null) last.text = txt;
    }

    public void ClearBattleLog()
    {
        if (txt_Logs == null) return;
        for (int i = 0; i < txt_Logs.Length; i++)
        {
            if (txt_Logs[i] != null) txt_Logs[i].text = string.Empty;
        }
    }
}
