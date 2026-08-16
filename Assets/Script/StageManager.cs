using System;
using UnityEngine;
using UnityEngine.UI;

public enum RoomStatus
{
    Lock,       // 아직 갈 수 없음
    Ready,      // 이동 가능
    Playing,    // 현재 위치
    Clear       // 클리어 완료
}

/// <summary>
/// 지도의 방 1개.
/// roomButton 은 실제로 클릭되는 Button, 그 0번 자식이 표시 Text, 1번 자식이 현재 위치 마커다.
/// </summary>
[Serializable]
public class Room
{
    public RoomType roomType = RoomType.Battle;
    public RoomStatus roomStatus = RoomStatus.Lock;
    public Button roomButton;

    [Tooltip("이 방을 클리어하면 열리는 방 인덱스들")]
    public int[] nextRoomIndex = new int[0];

    [Tooltip("전투 방에 등장할 몬스터 수. 0 이면 2~3마리 무작위.")]
    public int monsterCount;

    /// <summary>
    /// 방 버튼의 글자는 '방 종류'가 아니라 '진행 상태'를 나타낸다.
    ///   -  : 잠김 (경로가 이어지지 않아 갈 수 없음)
    ///   ?  : 아직 못 간 방
    ///   O  : 지금 플레이 중
    ///   X  : 클리어 완료
    /// </summary>
    public void UpdateRoom(string lockLabel, string readyLabel, string playingLabel, string clearLabel)
    {
        if (roomButton == null) return;

        Text label = GetLabel();
        GameObject marker = GetMarker();

        switch (roomStatus)
        {
            case RoomStatus.Lock:
                if (label != null) label.text = lockLabel;
                if (marker != null) marker.SetActive(false);
                roomButton.interactable = false;
                break;

            case RoomStatus.Ready:
                if (label != null) label.text = readyLabel;
                if (marker != null) marker.SetActive(false);
                roomButton.interactable = true;
                break;

            case RoomStatus.Playing:
                if (label != null) label.text = playingLabel;
                if (marker != null) marker.SetActive(true);
                roomButton.interactable = true;
                break;

            case RoomStatus.Clear:
                if (label != null) label.text = clearLabel;
                if (marker != null) marker.SetActive(false);
                roomButton.interactable = false;
                break;
        }
    }

    private Text GetLabel()
    {
        if (roomButton.transform.childCount < 1) return null;
        return roomButton.transform.GetChild(0).GetComponent<Text>();
    }

    private GameObject GetMarker()
    {
        if (roomButton.transform.childCount < 2) return null;
        return roomButton.transform.GetChild(1).gameObject;
    }
}

/// <summary>스테이지 1개의 지도.</summary>
[Serializable]
public class StageMap
{
    public string stageName = "STAGE";
    [Tooltip("이 스테이지 지도의 루트 오브젝트 (Canvas/MAP/Stage1 등)")]
    public GameObject mapRoot;
    public Room[] rooms = new Room[0];
    public int startRoomIndex;

    public int BossRoomIndex
    {
        get
        {
            for (int i = 0; i < rooms.Length; i++)
            {
                if (rooms[i].roomType == RoomType.Boss) return i;
            }
            return -1;
        }
    }
}

/// <summary>
/// 3개 스테이지의 지도와 방 진행을 관리한다.
/// 방 클리어 -> 이어진 방 해금 -> 보스 처치 -> 다음 스테이지.
/// </summary>
public class StageManager : MonoBehaviour
{
    private static StageManager cached;

    /// <summary>
    /// MAP 패널이 꺼져 있을 때는 Awake 가 돌지 않으므로, 없으면 씬에서 직접 찾아온다.
    /// </summary>
    public static StageManager instance
    {
        get
        {
            if (cached == null)
                cached = FindFirstObjectByType<StageManager>(FindObjectsInactive.Include);
            return cached;
        }
        private set { cached = value; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cached = null;
    }

    [Header("스테이지 (3개)")]
    public StageMap[] stages = new StageMap[3];

    [Header("방 종류 자동 배치")]
    [Tooltip("켜면 보스 방 바로 앞의 방을 상점/휴식으로, 나머지를 전투로 자동 지정한다.")]
    public bool autoAssignRoomTypes = true;

    [Header("표시")]
    [Tooltip("지도 화면의 'STAGE - 1' 텍스트")]
    public Text stageTitleText;
    [Tooltip("전투 화면의 'STAGE 1-3F' 텍스트")]
    public Text battleStageText;

    [Header("방 상태 라벨")]
    [Tooltip("잠김 : 경로가 이어지지 않아 갈 수 없는 방")]
    public string lockLabel = "-";
    [Tooltip("아직 못 간 방")]
    public string readyLabel = "?";
    [Tooltip("지금 플레이 중인 방")]
    public string playingLabel = "O";
    [Tooltip("클리어 완료한 방")]
    public string clearLabel = "X";

    [Header("런타임 (읽기용)")]
    public int currentStageIndex;
    public int currentRoomIndex;

    private bool listenersBound;

    private void Awake()
    {
        instance = this;
    }

    private void OnEnable()
    {
        BindButtons();
        UpdateRooms();
    }

    private void Start()
    {
        BindButtons();
        UpdateRooms();
    }

    // ─────────────────────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────────────────────
    private void BindButtons()
    {
        if (listenersBound) return;
        listenersBound = true;

        for (int s = 0; s < stages.Length; s++)
        {
            if (stages[s] == null || stages[s].rooms == null) continue;

            for (int r = 0; r < stages[s].rooms.Length; r++)
            {
                Button b = stages[s].rooms[r].roomButton;
                if (b == null) continue;

                // 인스펙터에 남아 있는 "패널.SetActive" 배선을 끄고 방 이동으로 바꾼다.
                UIButtonBinder.DisablePersistentListeners(b);

                int stageIndex = s;
                int roomIndex = r;
                b.onClick.AddListener(delegate { OnRoomButtonClicked(stageIndex, roomIndex); });
            }
        }
    }

    /// <summary>새 게임 시작 시 모든 스테이지를 초기 상태로 되돌린다.</summary>
    public void ResetAllStages()
    {
        for (int s = 0; s < stages.Length; s++)
        {
            if (stages[s] == null || stages[s].rooms == null) continue;

            for (int r = 0; r < stages[s].rooms.Length; r++)
            {
                stages[s].rooms[r].roomStatus = RoomStatus.Lock;
            }
        }

        currentStageIndex = 0;
        EnterStage(0);
    }

    /// <summary>해당 스테이지로 진입한다. (치트 F8~F10 도 이걸 쓴다)</summary>
    public void EnterStage(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= stages.Length) return;

        currentStageIndex = stageIndex;
        StageMap map = stages[stageIndex];

        if (map != null && map.rooms != null)
        {
            for (int r = 0; r < map.rooms.Length; r++)
            {
                map.rooms[r].roomStatus = RoomStatus.Lock;
            }

            int start = Mathf.Clamp(map.startRoomIndex, 0, Mathf.Max(0, map.rooms.Length - 1));
            currentRoomIndex = start;
            if (map.rooms.Length > 0)
            {
                map.rooms[start].roomStatus = RoomStatus.Playing;
                UnlockNextRooms(start);
            }
        }

        UpdateRooms();

        if (UIManager.instance != null) UIManager.instance.ShowMap();
    }

    // ─────────────────────────────────────────────────────────────
    // 방 이동 / 클리어
    // ─────────────────────────────────────────────────────────────
    private void OnRoomButtonClicked(int stageIndex, int roomIndex)
    {
        if (stageIndex != currentStageIndex) return;
        EnterRoom(roomIndex);
    }

    public void EnterRoom(int roomIndex)
    {
        StageMap map = CurrentStage;
        if (map == null || roomIndex < 0 || roomIndex >= map.rooms.Length) return;

        Room room = map.rooms[roomIndex];
        if (room.roomStatus == RoomStatus.Lock || room.roomStatus == RoomStatus.Clear) return;

        currentRoomIndex = roomIndex;
        room.roomStatus = RoomStatus.Playing;
        UpdateRooms();

        switch (room.roomType)
        {
            case RoomType.ShopRest:
                if (UIManager.instance != null) UIManager.instance.ShowShop();
                break;

            case RoomType.Boss:
                StartBattleRoom(room, true);
                break;

            default:
                StartBattleRoom(room, false);
                break;
        }
    }

    private void StartBattleRoom(Room room, bool isBoss)
    {
        if (BattleManager.instance == null)
        {
            Debug.LogError("StageManager : BattleManager 가 없습니다.");
            return;
        }

        int[] ids;
        if (isBoss)
        {
            int bossId = GameData.GetBossId(currentStageIndex);
            ids = new int[] { bossId };
        }
        else
        {
            int[] pool = GameData.GetNormalMonsterIds(currentStageIndex);
            if (pool.Length == 0)
            {
                Debug.LogError("StageManager : 등장 가능한 몬스터가 없습니다. stage=" + currentStageIndex);
                return;
            }

            int count = room.monsterCount > 0 ? room.monsterCount : UnityEngine.Random.Range(2, 4);
            count = Mathf.Clamp(count, 1, BattleManager.instance.monsterSlots.Length);

            ids = new int[count];
            for (int i = 0; i < count; i++)
            {
                ids[i] = pool[UnityEngine.Random.Range(0, pool.Length)];
            }
        }

        UpdateBattleStageText();

        if (UIManager.instance != null) UIManager.instance.ShowBattle();
        BattleManager.instance.StartBattle(ids, null);
    }

    /// <summary>전투 승리 또는 상점/휴식 종료 시 호출된다.</summary>
    public void OnRoomCleared()
    {
        StageMap map = CurrentStage;
        if (map == null) return;
        if (currentRoomIndex < 0 || currentRoomIndex >= map.rooms.Length) return;

        Room room = map.rooms[currentRoomIndex];
        room.roomStatus = RoomStatus.Clear;

        bool wasBoss = room.roomType == RoomType.Boss;

        UnlockNextRooms(currentRoomIndex);
        UpdateRooms();

        if (wasBoss)
        {
            OnBossDefeated();
            return;
        }

        if (UIManager.instance != null) UIManager.instance.ShowMap();
    }

    private void OnBossDefeated()
    {
        BattleLog.Log(string.Format("스테이지 {0} 클리어!", currentStageIndex + 1));

        if (currentStageIndex >= stages.Length - 1)
        {
            if (GameManager.instance != null) GameManager.instance.GameClear();
            return;
        }

        EnterStage(currentStageIndex + 1);
    }

    private void UnlockNextRooms(int roomIndex)
    {
        StageMap map = CurrentStage;
        if (map == null) return;
        if (roomIndex < 0 || roomIndex >= map.rooms.Length) return;

        int[] next = map.rooms[roomIndex].nextRoomIndex;
        if (next == null) return;

        for (int i = 0; i < next.Length; i++)
        {
            int n = next[i];
            if (n < 0 || n >= map.rooms.Length) continue;
            if (map.rooms[n].roomStatus == RoomStatus.Lock) map.rooms[n].roomStatus = RoomStatus.Ready;
        }
    }

    /// <summary>상점/휴식 방에서 나갈 때 호출한다.</summary>
    public void LeaveShopRoom()
    {
        OnRoomCleared();
    }

    // ─────────────────────────────────────────────────────────────
    // 표시 갱신
    // ─────────────────────────────────────────────────────────────
    public void UpdateRooms()
    {
        ApplyAutoRoomTypes();

        for (int s = 0; s < stages.Length; s++)
        {
            StageMap map = stages[s];
            if (map == null) continue;

            if (map.mapRoot != null) map.mapRoot.SetActive(s == currentStageIndex);

            if (map.rooms == null) continue;
            for (int r = 0; r < map.rooms.Length; r++)
            {
                map.rooms[r].UpdateRoom(lockLabel, readyLabel, playingLabel, clearLabel);
            }
        }

        if (stageTitleText != null)
        {
            stageTitleText.text = string.Format("STAGE - {0}", currentStageIndex + 1);
        }

        UpdateBattleStageText();
    }

    /// <summary>
    /// 방 종류를 규칙대로 자동 지정한다.
    ///   보스 방      : 인스펙터에 지정된 방 (스테이지당 1개, 지도의 끝)
    ///   보스 앞 방   : 상점 / 휴식
    ///   나머지       : 전투 (잡몹)
    /// </summary>
    public void ApplyAutoRoomTypes()
    {
        if (!autoAssignRoomTypes) return;

        for (int s = 0; s < stages.Length; s++)
        {
            StageMap map = stages[s];
            if (map == null || map.rooms == null) continue;

            int boss = map.BossRoomIndex;
            if (boss < 0) continue;

            for (int r = 0; r < map.rooms.Length; r++)
            {
                if (r == boss) continue;
                map.rooms[r].roomType = LeadsTo(map.rooms[r], boss) ? RoomType.ShopRest : RoomType.Battle;
            }
        }
    }

    private static bool LeadsTo(Room room, int targetIndex)
    {
        if (room.nextRoomIndex == null) return false;

        for (int i = 0; i < room.nextRoomIndex.Length; i++)
        {
            if (room.nextRoomIndex[i] == targetIndex) return true;
        }
        return false;
    }

    private void UpdateBattleStageText()
    {
        if (battleStageText == null) return;
        battleStageText.text = string.Format("STAGE {0}-{1}F", currentStageIndex + 1, currentRoomIndex + 1);
    }

    public StageMap CurrentStage
    {
        get
        {
            if (stages == null || currentStageIndex < 0 || currentStageIndex >= stages.Length) return null;
            return stages[currentStageIndex];
        }
    }

    public Room CurrentRoom
    {
        get
        {
            StageMap map = CurrentStage;
            if (map == null || map.rooms == null) return null;
            if (currentRoomIndex < 0 || currentRoomIndex >= map.rooms.Length) return null;
            return map.rooms[currentRoomIndex];
        }
    }
}
