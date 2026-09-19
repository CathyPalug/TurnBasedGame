using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ═════════════════════════════════════════════════════════════════════════════
//  StageManager.cs  —  3개 스테이지의 지도와 방 이동 담당
//
//  ── 지도 모양 (스테이지마다 똑같은 모양을 쓴다) ──
//
//          [1]──[3]
//         ╱          ╲
//      [0]              [5]상점──[6]보스
//         ╲          ╱
//          [2]──[4]
//
//    0번 : 시작 방 (전투)
//    1~4번 : 전투 방  → 위/아래 두 갈래 길이라 플레이어가 골라서 간다
//    5번 : 상점 및 휴식
//    6번 : 보스   ← 기획서 : 스테이지의 끝은 반드시 보스방, 스테이지당 1개
//
//  ★ 방 버튼과 길(선)은 전부 코드가 자동으로 만든다.
//    → Inspector 에 버튼 21개를 일일이 연결할 필요가 없어서 재현하기 훨씬 쉽다.
// ═════════════════════════════════════════════════════════════════════════════

public class StageManager : MonoBehaviour
{
    public static StageManager instance;

    /// <summary>한 스테이지에 있는 방의 개수.</summary>
    public const int ROOM_COUNT = 7;

    [Header("연결")]
    [Tooltip("방 버튼이 만들어질 자리 (지도 패널 안의 빈 오브젝트)")]
    public RectTransform mapArea;

    [Tooltip("방 버튼으로 쓸 프리팹 (Button + 자식 Text)")]
    public GameObject roomButtonPrefab;

    [Tooltip("길(선)으로 쓸 프리팹 (Image 하나만 있으면 된다)")]
    public GameObject linePrefab;

    [Header("표시용 글자")]
    public Text stageTitleText;    // "STAGE 1" 같은 제목
    public Text battleStageText;   // 전투 화면의 "STAGE 1 - 3F"

    [Header("지금 상태 (보기용)")]
    public int stageNum;    // 지금 스테이지 (0 = 1스테이지)
    public int roomNum;     // 지금 있는 방 번호

    // ── 지도 모양을 정하는 표 (숫자만 바꾸면 지도가 바뀐다) ──

    /// <summary>각 방의 가로 위치</summary>
    private float[] posX = { -360f, -180f, -180f, 0f, 0f, 180f, 360f };

    /// <summary>각 방의 세로 위치</summary>
    private float[] posY = { 0f, 100f, -100f, 100f, -100f, 0f, 0f };

    /// <summary>각 방에서 갈 수 있는 첫 번째 방. -1 이면 없음</summary>
    private int[] nextA = { 1, 3, 4, 5, 5, 6, -1 };

    /// <summary>각 방에서 갈 수 있는 두 번째 방. -1 이면 없음</summary>
    private int[] nextB = { 2, -1, -1, -1, -1, -1, -1 };

    /// <summary>각 방의 종류</summary>
    private RoomType[] roomType =
    {
        RoomType.Battle,  // 0
        RoomType.Battle,  // 1
        RoomType.Battle,  // 2
        RoomType.Battle,  // 3
        RoomType.Battle,  // 4
        RoomType.Shop,    // 5  상점 및 휴식
        RoomType.Boss     // 6  보스
    };

    /// <summary>각 방이 지금 어떤 상태인지 (잠김 / 갈 수 있음 / 지금 여기 / 클리어)</summary>
    private RoomState[] roomState = new RoomState[ROOM_COUNT];

    // 만들어진 버튼과 글자를 기억해 둔다.
    private Button[] roomButtons = new Button[ROOM_COUNT];
    private Text[] roomLabels = new Text[ROOM_COUNT];
    private bool mapBuilt;

    private void Awake()
    {
        instance = this;
    }


    // ═════════════════════════════════════════════════════════════
    //  지도 만들기 (게임 시작할 때 딱 한 번)
    // ═════════════════════════════════════════════════════════════

    private void BuildMap()
    {
        if (mapBuilt) return;               // 이미 만들었으면 또 만들지 않는다
        if (mapArea == null) return;
        if (roomButtonPrefab == null) return;

        mapBuilt = true;

        // ── 1) 먼저 길(선)을 그린다. 버튼보다 먼저 만들어야 선이 뒤에 깔린다.
        for (int i = 0; i < ROOM_COUNT; i++)
        {
            if (nextA[i] >= 0) MakeLine(i, nextA[i]);
            if (nextB[i] >= 0) MakeLine(i, nextB[i]);
        }

        // ── 2) 방 버튼을 만든다.
        for (int i = 0; i < ROOM_COUNT; i++)
        {
            GameObject 오브젝트 = Instantiate(roomButtonPrefab, mapArea);
            오브젝트.name = "Room " + i;

            RectTransform 위치 = 오브젝트.GetComponent<RectTransform>();
            위치.anchoredPosition = new Vector2(posX[i], posY[i]);

            Button 버튼 = 오브젝트.GetComponent<Button>();
            if (버튼 == null) 버튼 = 오브젝트.GetComponentInChildren<Button>();

            Text 글자 = 오브젝트.GetComponentInChildren<Text>();

            roomButtons[i] = 버튼;
            roomLabels[i] = 글자;

            // ★ for 문의 i 를 그대로 쓰면 안 되고, 복사해서 써야 한다.
            //   (안 그러면 모든 버튼이 마지막 번호로 동작한다)
            int 방번호 = i;
            if (버튼 != null)
            {
                버튼.onClick.AddListener(delegate { OnClickRoom(방번호); });
            }
        }
    }

    /// <summary>방 a 와 방 b 를 잇는 길(선)을 그린다.</summary>
    private void MakeLine(int a, int b)
    {
        if (linePrefab == null) return;

        GameObject 선 = Instantiate(linePrefab, mapArea);
        선.name = "Line " + a + "-" + b;

        RectTransform 위치 = 선.GetComponent<RectTransform>();

        Vector2 시작 = new Vector2(posX[a], posY[a]);
        Vector2 끝 = new Vector2(posX[b], posY[b]);

        // 선의 가운데를 두 방의 한가운데에 놓는다.
        위치.anchoredPosition = (시작 + 끝) / 2f;

        // 선의 길이는 두 방 사이의 거리로 한다.
        float 거리 = Vector2.Distance(시작, 끝);
        위치.sizeDelta = new Vector2(거리, 6f);   // 굵기 6

        // 두 방을 잇는 방향으로 선을 돌린다.
        //   Atan2 는 각도를 구해주는 함수. Rad2Deg 는 라디안을 도(°)로 바꾼다.
        Vector2 방향 = 끝 - 시작;
        float 각도 = Mathf.Atan2(방향.y, 방향.x) * Mathf.Rad2Deg;
        위치.localRotation = Quaternion.Euler(0f, 0f, 각도);
    }


    // ═════════════════════════════════════════════════════════════
    //  스테이지 진입
    // ═════════════════════════════════════════════════════════════

    /// <summary>새 게임 : 1스테이지부터 시작.</summary>
    public void StartFromStage1()
    {
        EnterStage(0);
    }

    /// <summary>이 스테이지로 들어간다. (치트 F8~F10 도 이걸 쓴다)</summary>
    public void EnterStage(int num)
    {
        if (num < 0) num = 0;
        if (num > 2) num = 2;

        stageNum = num;

        BuildMap();

        // 모든 방을 잠금 상태로 되돌린다.
        for (int i = 0; i < ROOM_COUNT; i++)
        {
            roomState[i] = RoomState.Locked;
        }

        // 0번 방(시작 방)에서 시작한다.
        roomNum = 0;
        roomState[0] = RoomState.Now;
        OpenNextRooms(0);

        RefreshMap();

        if (UIManager.instance != null) UIManager.instance.ShowMap();
    }

    /// <summary>이 방에서 갈 수 있는 방들을 "갈 수 있음" 으로 바꾼다.</summary>
    private void OpenNextRooms(int from)
    {
        OpenOneRoom(nextA[from]);
        OpenOneRoom(nextB[from]);
    }

    private void OpenOneRoom(int num)
    {
        if (num < 0 || num >= ROOM_COUNT) return;

        // 이미 깬 방은 다시 열지 않는다.
        if (roomState[num] == RoomState.Locked)
        {
            roomState[num] = RoomState.Open;
        }
    }


    // ═════════════════════════════════════════════════════════════
    //  방 이동
    // ═════════════════════════════════════════════════════════════

    /// <summary>지도에서 방 버튼을 눌렀을 때.</summary>
    private void OnClickRoom(int num)
    {
        // 갈 수 있는 방만 들어갈 수 있다.
        if (roomState[num] != RoomState.Open) return;

        if (AudioManager.instance != null) AudioManager.instance.PlayClick();

        roomNum = num;
        roomState[num] = RoomState.Now;
        RefreshMap();

        if (roomType[num] == RoomType.Shop)
        {
            // 상점 및 휴식 방
            if (UIManager.instance != null) UIManager.instance.ShowShop();
        }
        else
        {
            // 전투 방 또는 보스 방
            bool 보스방인가 = (roomType[num] == RoomType.Boss);
            StartRoomBattle(보스방인가);
        }
    }

    /// <summary>이 방의 전투를 시작한다.</summary>
    private void StartRoomBattle(bool isBoss)
    {
        if (BattleManager.instance == null)
        {
            Debug.LogError("StageManager : BattleManager 가 없습니다!");
            return;
        }

        int[] 나올몬스터들;

        if (isBoss)
        {
            // 보스방에는 보스 1마리만 나온다.
            int 보스번호 = GameData.FindBoss(stageNum);
            나올몬스터들 = new int[] { 보스번호 };
        }
        else
        {
            // 이 스테이지에 나올 수 있는 몬스터 목록을 만든다.
            List<int> 후보 = new List<int>();

            for (int i = 0; i < GameData.Monsters.Length; i++)
            {
                if (GameData.Monsters[i].isBoss) continue;              // 보스 제외
                if (GameData.Monsters[i].stage != stageNum) continue;   // 다른 스테이지 제외

                후보.Add(i);
            }

            if (후보.Count == 0)
            {
                Debug.LogError("StageManager : 나올 몬스터가 없습니다. 스테이지=" + stageNum);
                return;
            }

            // 2~3마리를 무작위로 뽑는다. (Random.Range 는 뒤 숫자를 포함하지 않는다)
            int 마리수 = Random.Range(2, 4);
            나올몬스터들 = new int[마리수];

            for (int i = 0; i < 마리수; i++)
            {
                나올몬스터들[i] = 후보[Random.Range(0, 후보.Count)];
            }
        }

        RefreshBattleTitle();

        if (UIManager.instance != null) UIManager.instance.ShowBattle();
        BattleManager.instance.StartBattle(나올몬스터들);
    }

    /// <summary>전투에서 이겼거나 상점에서 나왔을 때 호출된다.</summary>
    public void ClearRoom()
    {
        roomState[roomNum] = RoomState.Cleared;

        bool 보스방이었나 = (roomType[roomNum] == RoomType.Boss);

        OpenNextRooms(roomNum);
        RefreshMap();

        if (보스방이었나)
        {
            OnBossCleared();
            return;
        }

        if (UIManager.instance != null) UIManager.instance.ShowMap();
    }

    /// <summary>보스를 잡았을 때. (기획서 : 보스 처치 = 스테이지 클리어)</summary>
    private void OnBossCleared()
    {
        BattleUI.Log("★ 스테이지 " + (stageNum + 1) + " 클리어! ★");

        // 3스테이지(번호 2)까지 깼으면 게임 클리어
        if (stageNum >= 2)
        {
            if (GameManager.instance != null) GameManager.instance.GameClear();
            return;
        }

        // 아니면 다음 스테이지로
        EnterStage(stageNum + 1);
    }

    /// <summary>상점에서 [나가기] 를 눌렀을 때.</summary>
    public void LeaveShop()
    {
        ClearRoom();
    }


    // ═════════════════════════════════════════════════════════════
    //  지도 표시 갱신
    // ═════════════════════════════════════════════════════════════

    /// <summary>방 버튼의 글자와 색, 누를 수 있는지를 다시 정한다.</summary>
    public void RefreshMap()
    {
        BuildMap();

        for (int i = 0; i < ROOM_COUNT; i++)
        {
            if (roomButtons[i] == null) continue;

            // 방 종류에 따른 이름
            string 이름 = "전투";
            if (roomType[i] == RoomType.Shop) 이름 = "상점";
            if (roomType[i] == RoomType.Boss) 이름 = "보스";

            // 상태에 따른 표시
            string 상태표시 = "";
            bool 누를수있나 = false;
            Color 색 = Color.white;

            if (roomState[i] == RoomState.Locked)
            {
                상태표시 = "?";
                누를수있나 = false;
                색 = new Color(0.4f, 0.4f, 0.4f);      // 회색 : 아직 못 감
            }
            else if (roomState[i] == RoomState.Open)
            {
                상태표시 = "▶";
                누를수있나 = true;
                색 = new Color(1f, 0.95f, 0.6f);       // 노랑 : 지금 갈 수 있음
            }
            else if (roomState[i] == RoomState.Now)
            {
                상태표시 = "●";
                누를수있나 = false;
                색 = new Color(0.5f, 0.9f, 1f);        // 하늘색 : 지금 여기
            }
            else // Cleared
            {
                상태표시 = "✔";
                누를수있나 = false;
                색 = new Color(0.5f, 0.5f, 0.5f);      // 어두운 색 : 이미 깼음
            }

            if (roomLabels[i] != null)
            {
                roomLabels[i].text = 이름 + "\n" + 상태표시;
            }

            roomButtons[i].interactable = 누를수있나;

            Image 그림 = roomButtons[i].GetComponent<Image>();
            if (그림 != null) 그림.color = 색;
        }

        if (stageTitleText != null)
        {
            stageTitleText.text = "STAGE " + (stageNum + 1);
        }

        RefreshBattleTitle();
    }

    private void RefreshBattleTitle()
    {
        if (battleStageText == null) return;

        battleStageText.text = "STAGE " + (stageNum + 1) + " - " + (roomNum + 1) + "F";
    }
}
