using System;
using UnityEngine;
using UnityEngine.UI;

public enum RoomStatus
{
    Lock,
    Ready,
    Playing,
    Clear
}

[Serializable]
public class Room
{
    public RoomStatus roomStatus;
    public Button roomButton;
    public int[] nextRoomIndex;

    public void UpdateRoom()
    {
        switch (roomStatus)
        {
            case RoomStatus.Lock:
                {
                    roomButton.transform.GetChild(0).GetComponent<Text>().text = "-";
                    roomButton.transform.GetChild(1).gameObject.SetActive(false);
                    roomButton.interactable = false;
                    break;
                }
            case RoomStatus.Ready:
                {
                    roomButton.transform.GetChild(0).GetComponent<Text>().text = "?";
                    roomButton.transform.GetChild(1).gameObject.SetActive(false);
                    roomButton.interactable = true;
                    break;
                }
            case RoomStatus.Playing:
                {
                    roomButton.transform.GetChild(0).GetComponent<Text>().text = "O";
                    roomButton.transform.GetChild(1).gameObject.SetActive(true);
                    roomButton.interactable = true;
                    break;
                }
            case RoomStatus.Clear:
                {
                    roomButton.transform.GetChild(0).GetComponent<Text>().text = "X";
                    roomButton.transform.GetChild(1).gameObject.SetActive(false);
                    roomButton.interactable = false;
                    break;
                }
        }
    }
}
public class StageManager : MonoBehaviour
{
    public static StageManager instance;

    public Room[] roomList;
    public GameObject[] stageList;
    public int currentRoomIndex, currentStageIndex;

    private void Awake()
    {
        instance = this;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateRooms();
    }

    private void OnEnable()
    {
        UpdateRooms();
    }

    void UpdateRooms()
    {
        for (int i = 0; i < roomList.Length; i++)
        {
            roomList[i].UpdateRoom();
        }

        for (int i = 0; i < stageList.Length; i++)
        {
            if (currentStageIndex == i)
            {
                stageList[i].SetActive(true);
            }
            else
            {
                stageList[i].SetActive(false);
            }
        }
    }
}
