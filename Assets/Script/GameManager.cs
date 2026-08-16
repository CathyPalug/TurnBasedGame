using UnityEngine;

/// <summary>
/// 게임 전체 상태, 클리어 시간 측정, 치트키(F1~F10)를 담당한다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [Header("참조")]
    public Player player;

    [Header("상태 (읽기용)")]
    public GameState state = GameState.MainMenu;

    [Tooltip("게임 시작부터 지금까지 경과한 초. 일시정지 중에는 멈춘다.")]
    public float playTime;

    /// <summary>메인 메뉴 / 결과 화면이 아닌, 실제로 게임을 진행 중인가.</summary>
    public bool IsPlaying
    {
        get
        {
            return state == GameState.Map || state == GameState.Battle || state == GameState.Shop;
        }
    }

    /// <summary>치트 F1 상태. (Player.invincible 과 동기화)</summary>
    public bool isDebugF1;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        if (IsPlaying) playTime += Time.deltaTime;

        HandleCheatKeys();
    }

    // ─────────────────────────────────────────────────────────────
    // 게임 흐름
    // ─────────────────────────────────────────────────────────────
    public void StartNewGame()
    {
        if (player == null)
        {
            Debug.LogError("GameManager : player 가 연결되어 있지 않습니다.");
            return;
        }

        player.ResetToNewGame();
        player.gameObject.SetActive(true);

        playTime = 0f;
        isDebugF1 = false;

        if (BattleLog.Instance != null) BattleLog.Instance.ClearBattleLog();

        if (StageManager.instance != null) StageManager.instance.ResetAllStages();

        state = GameState.Map;
        if (UIManager.instance != null) UIManager.instance.ShowMap();
    }

    public void GameOver()
    {
        if (state == GameState.GameOver || state == GameState.GameClear) return;

        state = GameState.GameOver;
        BattleLog.Log("플레이어가 쓰러졌다... GAME OVER");

        if (UIManager.instance != null)
        {
            UIManager.instance.ShowResult(string.Format("GAME OVER\n\n도달 스테이지 : {0}\n생존 시간 : {1}",
                StageManager.instance != null ? StageManager.instance.currentStageIndex + 1 : 1,
                RankingManager.FormatTime(playTime)));
        }
    }

    public void GameClear()
    {
        if (state == GameState.GameClear) return;

        state = GameState.GameClear;
        BattleLog.Log(string.Format("모든 스테이지 클리어! 기록 {0}", RankingManager.FormatTime(playTime)));

        // 클리어 시간을 랭킹에 등록하기 위해 닉네임 입력을 띄운다.
        if (UIManager.instance != null) UIManager.instance.ShowNicknameInput();
    }

    /// <summary>결과 화면 / 랭킹 등록이 끝난 뒤 메인 메뉴로.</summary>
    public void ReturnToMainMenu()
    {
        state = GameState.MainMenu;

        if (BattleManager.instance != null) BattleManager.instance.AbortBattle();

        if (UIManager.instance != null) UIManager.instance.ShowMainMenu();
    }

    // ─────────────────────────────────────────────────────────────
    // 치트키
    // ─────────────────────────────────────────────────────────────
    private void HandleCheatKeys()
    {
        if (Input.GetKeyDown(KeyCode.F1)) CheatInvincible();
        else if (Input.GetKeyDown(KeyCode.F2)) CheatAttackUp();
        else if (Input.GetKeyDown(KeyCode.F3)) CheatFullHp();
        else if (Input.GetKeyDown(KeyCode.F4)) CheatFullMp();
        else if (Input.GetKeyDown(KeyCode.F5)) CheatLevelUp();
        else if (Input.GetKeyDown(KeyCode.F6)) CheatKillAllEnemies();
        else if (Input.GetKeyDown(KeyCode.F7)) CheatMainMenu();
        else if (Input.GetKeyDown(KeyCode.F8)) CheatGoToStage(0);
        else if (Input.GetKeyDown(KeyCode.F9)) CheatGoToStage(1);
        else if (Input.GetKeyDown(KeyCode.F10)) CheatGoToStage(2);
    }

    /// <summary>F1 : 플레이어 무적 토글</summary>
    public void CheatInvincible()
    {
        if (player == null) return;
        isDebugF1 = !isDebugF1;
        player.invincible = isDebugF1;
        BattleLog.Log(string.Format("[치트] 무적 {0}", isDebugF1 ? "ON" : "OFF"));
    }

    /// <summary>F2 : 캐릭터 공격력 증가 100</summary>
    public void CheatAttackUp()
    {
        if (player == null) return;
        player.cheatAtkBonus += 100;
        BattleLog.Log(string.Format("[치트] 공격력 +100 (현재 {0})", player.Atk));
    }

    /// <summary>F3 : HP 최대 회복</summary>
    public void CheatFullHp()
    {
        if (player == null) return;
        player.FullHeal();
        BattleLog.Log("[치트] HP 최대 회복");
    }

    /// <summary>F4 : MP 최대 회복</summary>
    public void CheatFullMp()
    {
        if (player == null) return;
        player.FullMp();
        BattleLog.Log("[치트] MP 최대 회복");
    }

    /// <summary>F5 : 플레이어 레벨 1 업</summary>
    public void CheatLevelUp()
    {
        if (player == null) return;
        player.ForceLevelUp();
        BattleLog.Log(string.Format("[치트] 레벨 업 -> Lv.{0}", player.level));
    }

    /// <summary>F6 : 현재 전투의 모든 적 제거</summary>
    public void CheatKillAllEnemies()
    {
        if (BattleManager.instance == null) return;
        BattleLog.Log("[치트] 현재 전투의 모든 적 제거");
        BattleManager.instance.KillAllMonsters();
    }

    /// <summary>F7 : 메인화면</summary>
    public void CheatMainMenu()
    {
        ReturnToMainMenu();
    }

    /// <summary>F8 / F9 / F10 : 해당 스테이지로 이동</summary>
    public void CheatGoToStage(int stageIndex)
    {
        if (StageManager.instance == null) return;

        // 메인 메뉴에서 눌렀다면 게임을 먼저 시작한다.
        if (!IsPlaying)
        {
            StartNewGame();
        }

        if (BattleManager.instance != null) BattleManager.instance.AbortBattle();

        state = GameState.Map;
        StageManager.instance.EnterStage(stageIndex);
        BattleLog.Log(string.Format("[치트] 스테이지 {0} 이동", stageIndex + 1));
    }
}
