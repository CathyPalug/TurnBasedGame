using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public List<GameObject> entryList = new List<GameObject>();

    public int currentRoomIndex, currentTurn;
    public TextMeshProUGUI turnText;
    public StageManager stageManager;

    private void OnEnable()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            entryList.Add(go);
        }
        entryList.Add(GameManager.instance.player.gameObject);

        SortEntry();

        
    }
    void SortEntry()
    {
        entryList.Sort((a, b) =>
        {
            if (a == null || b == null) return 0;

            Entry entryA = a.GetComponent<Entry>();
            Entry entryB = b.GetComponent<Entry>();

            float speedA = entryA != null ? entryA.speed : 0;
            float speedB = entryB != null ? entryB.speed : 0;

            return speedB.CompareTo(speedA);
        });
    }

    public void NextTurn()
    {
        if (entryList.Count == 0) return;

        currentTurn = (currentTurn + 1) % entryList.Count;

        ShowCurrentTurn();

        if (entryList[currentTurn].CompareTag("Player"))
        {
            for (int i =0; i < entryList.Count; i++)
            {
                if (i != currentTurn)
                {
                    //entryList[i].GetComponent<Enemy>
                }
            }
        }
    }

    void ShowCurrentTurn()
    {
        if (entryList.Count == 0) return;

        for (int i = 0; i < entryList.Count; i++)
        {
            if (entryList[i] == null) continue;

            Entry entry = entryList[i].GetComponent<Entry>();
            if (entry != null && entry.turnMark != null)
            {
                entry.turnMark.SetActive(i == currentTurn);
            }
        }

        GameObject currentObj = entryList[currentTurn];
        if (currentObj != null)
        {
            if (currentObj.CompareTag("Player"))
            {
                turnText.text = "PLAYER";
            }
            else
            {
                turnText.text = "ENEMY";
            }
        }
    }
    public void RoomFinish(bool win)
    {
        if (win)
        {

        }
        else
        {

        }
    }
}
