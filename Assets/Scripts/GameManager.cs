using UnityEngine;

// ═════════════════════════════════════════════════════════════════════════════
//  GameManager.cs  —  게임 전체 흐름과 치트키 담당
//
//  하는 일
//    1) 새 게임 시작 / 게임 오버 / 게임 클리어 처리
//    2) 클리어 시간 재기 (랭킹에 쓴다)
//    3) 치트키 F1 ~ F10  (기획서 10번, 채점 때 심사위원이 이걸로 확인한다!)
// ═════════════════════════════════════════════════════════════════════════════

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("연결")]
    public Player player;

    [Header("지금 상태 (보기용)")]
    public GameState state = GameState.Menu;

    [Tooltip("게임 시작부터 지금까지 걸린 시간(초). 일시정지 중에는 안 늘어난다.")]
    public float playTime;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        // 게임을 실제로 하는 중일 때만 시간을 잰다.
        // (Time.deltaTime 은 일시정지 중에 0 이 되므로 자동으로 멈춘다)
        if (IsPlaying())
        {
            playTime = playTime + Time.deltaTime;
        }

        CheckCheatKeys();
    }

    /// <summary>메뉴나 결과 화면이 아니라 실제로 게임을 진행 중인가.</summary>
    public bool IsPlaying()
    {
        if (state == GameState.Map) return true;
        if (state == GameState.Battle) return true;
        if (state == GameState.Shop) return true;
        return false;
    }


    // ═════════════════════════════════════════════════════════════
    //  게임 흐름
    // ═════════════════════════════════════════════════════════════

    /// <summary>메뉴에서 [게임 시작] 을 눌렀을 때. (기획서 7번)</summary>
    public void StartNewGame()
    {
        if (player == null)
        {
            Debug.LogError("GameManager : player 가 연결되지 않았습니다!");
            return;
        }

        // 스테이지 1, 레벨 1 로 시작한다.
        player.ResetPlayer();
        player.gameObject.SetActive(true);

        playTime = 0f;

        if (BattleUI.instance != null) BattleUI.instance.ClearLog();
        if (StageManager.instance != null) StageManager.instance.StartFromStage1();

        state = GameState.Map;
        if (UIManager.instance != null) UIManager.instance.ShowMap();
    }

    /// <summary>플레이어가 죽었을 때. (기획서 8번 : 사망하면 게임 종료)</summary>
    public void GameOver()
    {
        // 이미 끝난 상태면 두 번 처리하지 않는다.
        if (state == GameState.GameOver) return;
        if (state == GameState.GameClear) return;

        state = GameState.GameOver;

        int 도달스테이지 = 1;
        if (StageManager.instance != null) 도달스테이지 = StageManager.instance.stageNum + 1;

        string 내용 = "GAME OVER\n\n"
                    + "도달 스테이지 : " + 도달스테이지 + "\n"
                    + "생존 시간 : " + RankingManager.TimeToText(playTime);

        if (UIManager.instance != null) UIManager.instance.ShowResult(내용, false);
    }

    /// <summary>3스테이지 보스까지 잡았을 때.</summary>
    public void GameClear()
    {
        if (state == GameState.GameClear) return;

        state = GameState.GameClear;

        // 클리어 시간을 랭킹에 등록하기 위해 이니셜 입력 화면을 띄운다.
        if (UIManager.instance != null) UIManager.instance.ShowNameInput();
    }

    /// <summary>메인 메뉴로 돌아간다.</summary>
    public void GoToMenu()
    {
        state = GameState.Menu;

        if (BattleManager.instance != null) BattleManager.instance.StopBattle();
        if (UIManager.instance != null) UIManager.instance.ShowMenu();
        if (AudioManager.instance != null) AudioManager.instance.PlayMenuBgm();
    }


    // ═════════════════════════════════════════════════════════════
    //  치트키 (기획서 10번)
    //  ★ 채점할 때 심사위원이 이 키로 기능을 확인하므로 반드시 다 동작해야 한다.
    // ═════════════════════════════════════════════════════════════

    private void CheckCheatKeys()
    {
        // Input.GetKeyDown = 그 키를 "누른 순간" 딱 한 번 true 가 된다
        if (Input.GetKeyDown(KeyCode.F1)) Cheat_NoDamage();
        else if (Input.GetKeyDown(KeyCode.F2)) Cheat_AtkUp();
        else if (Input.GetKeyDown(KeyCode.F3)) Cheat_FullHp();
        else if (Input.GetKeyDown(KeyCode.F4)) Cheat_FullMp();
        else if (Input.GetKeyDown(KeyCode.F5)) Cheat_LevelUp();
        else if (Input.GetKeyDown(KeyCode.F6)) Cheat_KillAll();
        else if (Input.GetKeyDown(KeyCode.F7)) Cheat_GoMenu();
        else if (Input.GetKeyDown(KeyCode.F8)) Cheat_GoStage(0);
        else if (Input.GetKeyDown(KeyCode.F9)) Cheat_GoStage(1);
        else if (Input.GetKeyDown(KeyCode.F10)) Cheat_GoStage(2);
    }

    /// <summary>F1 : 플레이어 무적 (한 번 더 누르면 해제)</summary>
    public void Cheat_NoDamage()
    {
        if (player == null) return;

        // ! 는 "반대로 바꾼다" 는 뜻이다. true → false, false → true
        player.noDamage = !player.noDamage;

        if (player.noDamage) BattleUI.Log("[치트] 무적 ON");
        else BattleUI.Log("[치트] 무적 OFF");
    }

    /// <summary>F2 : 공격력 100 증가</summary>
    public void Cheat_AtkUp()
    {
        if (player == null) return;

        player.cheatAtk = player.cheatAtk + 100;
        player.UpdateBonus();   // 보너스를 다시 계산해야 실제로 올라간다

        BattleUI.Log("[치트] 공격력 +100  (지금 공격력 " + player.GetFinalAtk() + ")");
    }

    /// <summary>F3 : HP 최대 회복</summary>
    public void Cheat_FullHp()
    {
        if (player == null) return;
        player.FullHp();
        BattleUI.Log("[치트] HP 최대 회복");
    }

    /// <summary>F4 : MP 최대 회복</summary>
    public void Cheat_FullMp()
    {
        if (player == null) return;
        player.FullMp();
        BattleUI.Log("[치트] MP 최대 회복");
    }

    /// <summary>F5 : 레벨 1 업</summary>
    public void Cheat_LevelUp()
    {
        if (player == null) return;
        player.CheatLevelUp();
        BattleUI.Log("[치트] 레벨 업 → Lv." + player.level);
    }

    /// <summary>F6 : 현재 전투의 모든 적 제거</summary>
    public void Cheat_KillAll()
    {
        if (BattleManager.instance == null) return;

        BattleUI.Log("[치트] 모든 적 제거");
        BattleManager.instance.CheatKillAll();
    }

    /// <summary>F7 : 메인화면으로</summary>
    public void Cheat_GoMenu()
    {
        GoToMenu();
    }

    /// <summary>F8 / F9 / F10 : 해당 스테이지로 이동</summary>
    public void Cheat_GoStage(int stageNum)
    {
        if (StageManager.instance == null) return;

        // 메뉴에서 눌렀다면 게임을 먼저 시작한다.
        if (IsPlaying() == false)
        {
            StartNewGame();
        }

        if (BattleManager.instance != null) BattleManager.instance.StopBattle();

        state = GameState.Map;
        StageManager.instance.EnterStage(stageNum);

        BattleUI.Log("[치트] " + (stageNum + 1) + "스테이지 이동");
    }
}
