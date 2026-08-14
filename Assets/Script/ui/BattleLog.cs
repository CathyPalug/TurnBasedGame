using UnityEngine;
using UnityEngine.UI;

public class BattleLog : MonoBehaviour
{
    public Text[] txt_Logs;
    public void AddBattleLog(string txt)
    {
        for (int i = 0; i < 3; i++)
        {
            txt_Logs[i].text = txt_Logs[i+1].text;
        }

        txt_Logs[3].text = $"{txt}";
    }

    public void ClearBattleLog()
    {
        for (int i = 0; i < 4; i++)
        {
            txt_Logs[i].text = $"";
        }
    }
}
