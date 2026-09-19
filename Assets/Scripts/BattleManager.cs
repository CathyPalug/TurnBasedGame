using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ═════════════════════════════════════════════════════════════════════════════
//  BattleManager.cs  —  턴제 전투를 진행하는 "심판"
//
//  ── 라운드 1번의 흐름 (기획서 2-4-1) ──
//    1) 지난 라운드에 켜둔 몬스터 '방어' 를 전부 끈다
//    2) 살아있는 몬스터들이 이번 턴 행동을 미리 정한다 (플레이어가 볼 수 있게)
//    3) '방어' 를 고른 몬스터는 여기서 먼저 발동한다  ← 기획서 필수 사항
//    4) 속도가 빠른 순서대로 한 명씩 행동한다
//    5) 다 행동했으면 1번으로 돌아간다
//
//  ── 대상 고르기 (JRPG 방식) ──
//    마우스로 3D 몬스터를 클릭하는 게 아니라,
//    화면 아래 커맨드창의 "적 이름 버튼" 을 눌러서 고른다.
//    → 레이캐스트 코드가 아예 필요 없어서 훨씬 간단하다.
// ═════════════════════════════════════════════════════════════════════════════


/// <summary>공격 한 번의 결과를 담아 돌려주는 작은 상자.</summary>
public class AttackResult
{
    public bool evaded;     // 적이 피했는가
    public bool critical;   // 크리티컬이 터졌는가
    public int damage;      // 최종 피해량
}


public class BattleManager : MonoBehaviour
{
    /// <summary>어디서든 BattleManager.instance 로 부를 수 있다.</summary>
    public static BattleManager instance;

    // ── 플레이어가 지금 무엇을 고르는 중인지 나타내는 숫자 ──
    // (함수를 변수에 담는 어려운 문법 대신 숫자로 기억한다)
    public const int PICK_NONE = 0;     // 고르는 중이 아님
    public const int PICK_ATTACK = 1;   // 기본 공격할 적을 고르는 중
    public const int PICK_SKILL = 2;    // 스킬 쓸 적을 고르는 중

    [Header("연결")]
    public Player player;               // 플레이어
    public BattleUI battleUI;           // 전투 화면 UI

    [Header("몬스터가 설 자리 (왼쪽부터 순서대로, 최대 4자리)")]
    public Transform[] monsterSlots = new Transform[4];

    [Header("몬스터 3D 프리팹 (GameData.Monsters 와 같은 순서로 10개)")]
    [Tooltip("에셋 : LowPoly Fantasy Monsters Pack")]
    public GameObject[] monsterPrefabs = new GameObject[10];

    [Header("연출 대기 시간 (초)")]
    public float roundDelay = 0.4f;     // 라운드 시작 전 대기
    public float monsterDelay = 0.8f;   // 몬스터가 행동하기 전 대기
    public float endDelay = 1.5f;       // 전투가 끝나고 결과까지 대기

    [Header("지금 상태 (보기용)")]
    public Monster[] monsters = new Monster[0];   // 지금 전투 중인 몬스터들
    public int round;                             // 몇 번째 라운드인가
    public bool battleOn;                         // 전투 중인가
    public bool waitInput;                        // 플레이어 입력을 기다리는 중인가
    public int pickMode = PICK_NONE;              // 지금 무엇을 고르는 중인가

    // ── 이 스크립트 안에서만 쓰는 변수 ──
    private List<Unit> turnList = new List<Unit>();  // 이번 라운드 행동 순서
    private int turnIndex = -1;                      // 지금 몇 번째 차례
    private int pickSkillNum = -1;                   // 고른 뒤 쓸 스킬 번호
    private bool ending;                             // 끝나는 연출 중인가

    private void Awake()
    {
        instance = this;
    }


    // ═════════════════════════════════════════════════════════════
    //  전투 시작 / 종료
    // ═════════════════════════════════════════════════════════════

    /// <summary>몬스터 번호 목록을 받아 전투를 시작한다.</summary>
    public void StartBattle(int[] monsterNums)
    {
        if (player == null)
        {
            Debug.LogError("BattleManager : player 가 연결되지 않았습니다!");
            return;
        }

        RemoveAllMonsters();
        MakeMonsters(monsterNums);

        // 플레이어를 전투 시작 상태로 만든다.
        player.ResetCool();
        player.ClearEffects();
        player.UpdateBonus();
        player.extraTurn = 0;
        player.ShowTurnMark(false);

        round = 0;
        battleOn = true;
        ending = false;
        waitInput = false;
        pickMode = PICK_NONE;

        if (battleUI != null) battleUI.OnBattleStart();
        BattleUI.Log("── 전투 시작! ──");

        // 보스가 있으면 보스 음악, 아니면 전투 음악
        if (AudioManager.instance != null)
        {
            if (HasBoss()) AudioManager.instance.PlayBossBgm();
            else AudioManager.instance.PlayBattleBgm();
        }

        // 코루틴 : 정해진 시간만큼 기다렸다가 다음 줄을 실행하게 해주는 유니티 기능
        StartCoroutine(WaitAndStartRound());
    }

    /// <summary>몬스터들을 화면에 만들어 세운다.</summary>
    private void MakeMonsters(int[] monsterNums)
    {
        // 자리보다 몬스터가 많으면 자리 수만큼만 만든다.
        int 개수 = Mathf.Min(monsterNums.Length, monsterSlots.Length);
        monsters = new Monster[개수];

        for (int i = 0; i < 개수; i++)
        {
            int 번호 = monsterNums[i];

            if (번호 < 0 || 번호 >= monsterPrefabs.Length || monsterPrefabs[번호] == null)
            {
                Debug.LogError("BattleManager : 몬스터 프리팹이 비어 있습니다. 번호=" + 번호);
                continue;
            }

            Transform 자리 = monsterSlots[i];

            // Instantiate = 프리팹을 복사해서 화면에 만드는 함수
            GameObject 오브젝트 = Instantiate(monsterPrefabs[번호], 자리.position, 자리.rotation, 자리);

            Monster 몬스터 = 오브젝트.GetComponent<Monster>();
            if (몬스터 == null) 몬스터 = 오브젝트.AddComponent<Monster>();

            몬스터.SetupRandom(번호, i);
            monsters[i] = 몬스터;
        }
    }

    /// <summary>화면의 몬스터를 전부 지운다.</summary>
    private void RemoveAllMonsters()
    {
        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null) Destroy(monsters[i].gameObject);
        }
        monsters = new Monster[0];
    }

    /// <summary>전투를 끝낸다.</summary>
    public void EndBattle(bool win)
    {
        if (battleOn == false) return;

        battleOn = false;
        ending = false;
        waitInput = false;
        pickMode = PICK_NONE;
        StopAllCoroutines();

        player.ClearEffects();
        player.ShowTurnMark(false);

        if (win) BattleUI.Log("── 전투 승리! ──");
        else BattleUI.Log("── 전투 패배... ──");

        if (battleUI != null) battleUI.OnBattleEnd();

        if (win)
        {
            // 이겼으면 이 방을 클리어 처리한다.
            if (StageManager.instance != null) StageManager.instance.ClearRoom();
        }
        else
        {
            // 졌으면 게임 오버.
            if (GameManager.instance != null) GameManager.instance.GameOver();
        }
    }

    /// <summary>승패를 따지지 않고 전투를 강제로 끝낸다. (치트로 스테이지 이동 등)</summary>
    public void StopBattle()
    {
        if (battleOn == false) return;

        battleOn = false;
        ending = false;
        waitInput = false;
        pickMode = PICK_NONE;
        StopAllCoroutines();

        player.ClearEffects();
        player.ShowTurnMark(false);
        RemoveAllMonsters();

        if (battleUI != null) battleUI.OnBattleEnd();
    }


    // ═════════════════════════════════════════════════════════════
    //  라운드 진행
    // ═════════════════════════════════════════════════════════════

    private IEnumerator WaitAndStartRound()
    {
        // yield return new WaitForSeconds(x) = x초 기다렸다가 아래 줄부터 계속
        yield return new WaitForSeconds(roundDelay);
        StartRound();
    }

    private void StartRound()
    {
        if (battleOn == false) return;

        round = round + 1;

        // 1) 지난 라운드의 '방어' 를 전부 끈다.
        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null) monsters[i].RemoveEffect(EffectType.Defend);
        }

        // 2) 살아있는 몬스터들이 이번 턴 행동을 미리 정한다.
        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null && monsters[i].IsAlive()) monsters[i].PlanAction();
        }

        // 3) '방어' 를 고른 몬스터는 지금 바로 방어를 켠다. (기획서 : 반드시 먼저 발동)
        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null && monsters[i].IsAlive()) monsters[i].StartDefend();
        }

        MakeTurnOrder();
        turnIndex = -1;      // NextTurn 에서 +1 되어 0번부터 시작한다

        if (battleUI != null) battleUI.RefreshEnemies();

        NextTurn();
    }

    /// <summary>속도가 빠른 순서로 이번 라운드의 행동 순서를 만든다. (기획서 : 속도가 턴 순서 결정)</summary>
    private void MakeTurnOrder()
    {
        turnList.Clear();

        // 살아있는 유닛만 넣는다.
        if (player != null && player.IsAlive()) turnList.Add(player);

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null && monsters[i].IsAlive()) turnList.Add(monsters[i]);
        }

        // ★ 선택 정렬 : 앞에서부터 한 칸씩 보면서 가장 빠른 유닛을 앞으로 보낸다.
        //   (어려운 정렬 함수 대신 for 두 번으로 직접 정렬한다)
        for (int i = 0; i < turnList.Count; i++)
        {
            int 가장빠른칸 = i;

            for (int j = i + 1; j < turnList.Count; j++)
            {
                if (turnList[j].speed > turnList[가장빠른칸].speed)
                {
                    가장빠른칸 = j;
                }
            }

            // 찾은 유닛과 i번 칸을 서로 바꾼다.
            Unit 임시 = turnList[i];
            turnList[i] = turnList[가장빠른칸];
            turnList[가장빠른칸] = 임시;
        }
    }

    /// <summary>다음 유닛에게 턴을 넘긴다.</summary>
    public void NextTurn()
    {
        if (battleOn == false) return;
        if (CheckBattleEnd()) return;

        turnIndex = turnIndex + 1;

        // 모두가 행동했으면 다음 라운드로.
        if (turnIndex >= turnList.Count)
        {
            StartCoroutine(WaitAndStartRound());
            return;
        }

        Unit 이번차례 = turnList[turnIndex];

        // 차례가 오기 전에 죽었으면 건너뛴다.
        if (이번차례 == null || 이번차례.IsAlive() == false)
        {
            NextTurn();
            return;
        }

        // 지금 차례인 유닛만 표시를 켠다.
        for (int i = 0; i < turnList.Count; i++)
        {
            turnList[i].ShowTurnMark(turnList[i] == 이번차례);
        }

        if (이번차례 == player) StartPlayerTurn();
        else StartCoroutine(MonsterTurn(이번차례 as Monster));
    }


    // ═════════════════════════════════════════════════════════════
    //  플레이어 턴
    // ═════════════════════════════════════════════════════════════

    private void StartPlayerTurn()
    {
        player.TickCool();   // 스킬 쿨타임 1 줄이기

        // 행동불능(스턴)이면 이번 턴을 통째로 쉰다.
        if (player.IsStunned())
        {
            BattleUI.Log("행동불능! 이번 턴을 쉰다.");
            EndPlayerTurn();
            return;
        }

        player.extraTurn = 0;
        waitInput = true;

        if (battleUI != null) battleUI.ShowCommand(true);
    }

    /// <summary>플레이어가 행동 하나를 끝냈을 때 호출한다.</summary>
    /// <param name="useTurn">이 행동이 턴을 소모하는가</param>
    public void PlayerActionDone(bool useTurn)
    {
        if (battleOn == false) return;
        if (CheckBattleEnd()) return;

        // '두 개의 심장' 처럼 턴을 안 쓰는 행동이면 계속 입력을 받는다.
        if (useTurn == false)
        {
            if (battleUI != null) battleUI.ShowCommand(true);
            return;
        }

        // 추가 행동이 남아 있으면 한 번 더 행동할 수 있다.
        if (player.extraTurn > 0)
        {
            player.extraTurn = player.extraTurn - 1;
            BattleUI.Log("추가 행동! 한 번 더 행동할 수 있다.");

            if (battleUI != null) battleUI.ShowCommand(true);
            return;
        }

        EndPlayerTurn();
    }

    private void EndPlayerTurn()
    {
        waitInput = false;
        pickMode = PICK_NONE;

        if (battleUI != null) battleUI.ShowCommand(false);

        player.TickEffects();   // 버프의 남은 턴 1 줄이기
        NextTurn();
    }


    // ═════════════════════════════════════════════════════════════
    //  몬스터 턴
    // ═════════════════════════════════════════════════════════════

    private IEnumerator MonsterTurn(Monster m)
    {
        yield return new WaitForSeconds(monsterDelay);

        // 기다리는 사이에 전투가 끝났을 수도 있다.
        if (battleOn == false) yield break;

        if (m != null && m.IsAlive())
        {
            DoMonsterAction(m);

            m.TickEffects();
            m.TickCool();

            if (battleUI != null) battleUI.RefreshEnemies();
        }

        yield return new WaitForSeconds(monsterDelay * 0.4f);

        NextTurn();
    }

    /// <summary>몬스터가 정해둔 행동을 실제로 한다.</summary>
    private void DoMonsterAction(Monster m)
    {
        // 행동불능이면 쉰다.
        if (m.IsStunned())
        {
            BattleUI.Log(m.unitName + " 은(는) 행동불능 상태다.");
            return;
        }

        if (m.plan == MonsterAction.Defend)
        {
            // 방어는 라운드 시작 때 이미 켜졌으므로 여기서는 할 일이 없다.
            return;
        }

        if (m.plan == MonsterAction.Skill)
        {
            MonsterUseSkill(m);
            return;
        }

        // 기본 공격 (기획서 : 공격력의 100%)
        AttackResult 결과 = Attack(m, player, 1.0f, false, false);

        if (결과.evaded)
        {
            BattleUI.Log(m.unitName + " 의 공격 → 회피!");
            return;
        }

        BattleUI.Log(m.unitName + " 의 공격 → " + 결과.damage + " 피해");
    }

    /// <summary>몬스터가 스킬을 쓴다.</summary>
    private void MonsterUseSkill(Monster m)
    {
        MonsterSkillData 스킬 = GameData.GetMonsterSkill(m.planSkillNum);

        // 스킬 정보가 없으면 그냥 평타
        if (스킬 == null)
        {
            AttackResult 평타 = Attack(m, player, 1.0f, false, false);
            BattleUI.Log(m.unitName + " 의 공격 → " + 평타.damage + " 피해");
            return;
        }

        m.StartCool(m.planSkillNum);

        AttackResult 결과 = Attack(m, player, 스킬.power, true, false);

        if (결과.evaded)
        {
            BattleUI.Log(m.unitName + " 의 [" + 스킬.name + "] → 회피!");
            return;
        }

        string 기록 = m.unitName + " 의 [" + 스킬.name + "] → " + 결과.damage + " 피해";

        // 영혼 흡수 : 준 피해의 50% 만큼 자기 체력 회복
        if (스킬.drainRate > 0f)
        {
            int 회복 = m.HealHp(Mathf.RoundToInt(결과.damage * 스킬.drainRate));
            if (DamagePopup.instance != null) DamagePopup.instance.ShowHeal(m, 회복);
            기록 = 기록 + " (자신 " + 회복 + " 회복)";
        }

        // 나쁜 효과(스턴, 스킬봉인)를 건다.
        if (스킬.hasEffect)
        {
            player.AddEffect(스킬.effect, 0f, 스킬.effectTurn);
            기록 = 기록 + " (" + GetEffectName(스킬.effect) + " " + 스킬.effectTurn + "턴)";
        }

        BattleUI.Log(기록);
    }

    /// <summary>효과 종류를 한글 이름으로 바꿔준다.</summary>
    public static string GetEffectName(EffectType type)
    {
        if (type == EffectType.SkillSeal) return "스킬 봉인";
        if (type == EffectType.Stun) return "행동불능";
        if (type == EffectType.AtkDown) return "공격력 감소";
        if (type == EffectType.DefDown) return "방어력 감소";
        if (type == EffectType.AtkUp) return "공격력 증가";
        if (type == EffectType.DefUp) return "방어력 증가";
        if (type == EffectType.CritUp) return "크리티컬 증가";
        if (type == EffectType.SkillUp) return "스킬 피해 증가";
        if (type == EffectType.EvadeUp) return "회피 증가";
        if (type == EffectType.Defend) return "방어";
        return "";
    }


    // ═════════════════════════════════════════════════════════════
    //  ★ 피해 계산 (기획서의 핵심 공식)
    //
    //   1) 위력 = 공격하는 쪽 최종 공격력 x 스킬 배율
    //   2) 스킬이면 '지식의 영약' 버프만큼 더 세게
    //   3) 크리티컬이 터지면 2배
    //   4) 맞는 쪽 방어력 % 만큼 깎기
    //      단, 원래 위력의 10% 아래로는 절대 안 내려간다
    //   5) 맞는 쪽이 '방어' 중이면 (50 + 레벨x3)% 더 깎기
    // ═════════════════════════════════════════════════════════════

    /// <summary>공격을 계산하고 실제로 체력까지 깎는다.</summary>
    /// <param name="power">위력 배율. 1.0f = 공격력 그대로</param>
    /// <param name="isSkill">스킬 공격인가</param>
    /// <param name="canCrit">크리티컬이 터질 수 있는가</param>
    public AttackResult Attack(Unit 공격자, Unit 방어자, float power, bool isSkill, bool canCrit)
    {
        AttackResult 결과 = new AttackResult();

        // ── 0단계 : 회피 판정
        // 0~99 중 아무 숫자나 뽑아서, 회피율보다 작으면 피한 것이다.
        // 예) 회피율 15 → 0~14 가 나오면 성공 = 100번 중 15번 = 15%
        int 회피율 = 방어자.GetFinalEvade();
        if (회피율 > 0 && Random.Range(0, 100) < 회피율)
        {
            결과.evaded = true;
            if (DamagePopup.instance != null) DamagePopup.instance.ShowMiss(방어자);
            return 결과;
        }

        // ── 1단계 : 기본 위력
        float 위력 = 공격자.GetFinalAtk() * power;

        // ── 2단계 : 스킬 피해 증가 버프 (지식의 영약)
        if (isSkill)
        {
            float 스킬증가 = 공격자.GetEffectValue(EffectType.SkillUp);
            위력 = 위력 * (1f + 스킬증가);
        }

        // ── 3단계 : 크리티컬 판정 → 2배
        int 크리율 = 공격자.GetFinalCrit();
        if (canCrit && 크리율 > 0 && Random.Range(0, 100) < 크리율)
        {
            결과.critical = true;
            위력 = 위력 * GameData.CRIT_DAMAGE;
        }

        // ── 4단계 : 방어력만큼 깎기 (방어력 30 이면 30% 감소)
        float 방어비율 = Mathf.Clamp01(방어자.GetFinalDef() / 100f);
        float 최종 = 위력 * (1f - 방어비율);

        //    아무리 방어력이 높아도 원래 위력의 10% 는 반드시 들어간다.
        float 최소피해 = 위력 * GameData.MIN_DAMAGE_RATE;
        if (최종 < 최소피해) 최종 = 최소피해;

        // ── 5단계 : '방어' 중이면 한 번 더 크게 깎인다.
        if (방어자.IsDefending())
        {
            float 방어감소 = Mathf.Clamp01(방어자.GetEffectValue(EffectType.Defend));
            최종 = 최종 * (1f - 방어감소);
        }

        // 최소 1은 들어가게 해서 "0 피해" 가 안 뜨게 한다.
        int 피해량 = Mathf.Max(1, Mathf.RoundToInt(최종));

        // ── 실제로 체력을 깎는다.
        결과.damage = 방어자.TakeDamage(피해량);

        // ── 연출
        if (DamagePopup.instance != null)
        {
            DamagePopup.instance.ShowDamage(방어자, 결과.damage, 결과.critical);
        }

        if (AudioManager.instance != null)
        {
            if (결과.critical) AudioManager.instance.PlayCritical();
            else AudioManager.instance.PlayHit();
        }

        // ── 몬스터가 맞았다면 보스 페이즈 변화를 확인한다.
        Monster 맞은몬스터 = 방어자 as Monster;   // as = 이 타입이면 바꿔주고 아니면 null
        if (맞은몬스터 != null) 맞은몬스터.CheckPhase();

        return 결과;
    }


    // ═════════════════════════════════════════════════════════════
    //  플레이어 커맨드 (BattleUI 버튼이 부른다)
    // ═════════════════════════════════════════════════════════════

    /// <summary>[공격] 버튼 → 때릴 적을 고르게 한다.</summary>
    public void StartAttack()
    {
        if (waitInput == false) return;

        pickSkillNum = -1;
        StartPick(PICK_ATTACK);
    }

    /// <summary>스킬 목록에서 스킬 하나를 골랐을 때.</summary>
    public void StartSkill(int skillNum)
    {
        if (waitInput == false) return;

        SkillData 스킬 = GameData.GetSkill(skillNum);
        if (스킬 == null) return;

        // 못 쓰는 스킬이면 이유를 알려주고 끝낸다.
        if (player.CanUseSkill(skillNum) == false)
        {
            BattleUI.Log("[" + 스킬.name + "] " + player.GetSkillBlockReason(skillNum));
            return;
        }

        // 적을 고를 필요가 있는 스킬인지 확인한다.
        bool 적을골라야하나 = false;
        if (스킬.target == TargetType.Single) 적을골라야하나 = true;
        if (스킬.target == TargetType.Adjacent) 적을골라야하나 = true;

        pickSkillNum = skillNum;

        if (적을골라야하나)
        {
            StartPick(PICK_SKILL);
        }
        else
        {
            // 자신에게 쓰는 스킬이나 전체 공격은 바로 실행한다.
            UseSkill(skillNum, null);
        }
    }

    /// <summary>아이템 목록에서 아이템 하나를 골랐을 때. (턴을 쓴다)</summary>
    public void UseItem(int bagSlot)
    {
        if (waitInput == false) return;

        if (player.UseItem(bagSlot) == false) return;   // 실패하면 턴도 안 넘긴다

        if (battleUI != null) battleUI.RefreshEnemies();
        PlayerActionDone(true);
    }

    /// <summary>적을 고르기 시작한다.</summary>
    private void StartPick(int mode)
    {
        // 살아있는 적이 몇 마리인지 센다.
        int 살아있는수 = 0;
        int 마지막칸 = -1;

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] == null || monsters[i].IsAlive() == false) continue;
            살아있는수 = 살아있는수 + 1;
            마지막칸 = i;
        }

        if (살아있는수 == 0) return;

        // 적이 한 마리뿐이면 고를 필요가 없으니 바로 실행한다.
        if (살아있는수 == 1)
        {
            DoPickedAction(mode, monsters[마지막칸]);
            return;
        }

        // 두 마리 이상이면 플레이어가 직접 고르게 한다.
        pickMode = mode;
        if (battleUI != null) battleUI.ShowTargetList(true);
    }

    /// <summary>적 이름 버튼을 눌렀을 때 (BattleUI 가 부른다).</summary>
    public void PickTarget(int slotIndex)
    {
        if (pickMode == PICK_NONE) return;
        if (slotIndex < 0 || slotIndex >= monsters.Length) return;
        if (monsters[slotIndex] == null || monsters[slotIndex].IsAlive() == false) return;

        int 모드 = pickMode;
        pickMode = PICK_NONE;

        if (battleUI != null) battleUI.ShowTargetList(false);

        DoPickedAction(모드, monsters[slotIndex]);
    }

    /// <summary>적 고르기를 취소한다.</summary>
    public void CancelPick()
    {
        pickMode = PICK_NONE;
        pickSkillNum = -1;

        if (battleUI != null) battleUI.ShowTargetList(false);
    }

    /// <summary>고른 적에게 실제 행동을 실행한다.</summary>
    private void DoPickedAction(int mode, Monster target)
    {
        if (mode == PICK_ATTACK) BasicAttack(target);
        else if (mode == PICK_SKILL) UseSkill(pickSkillNum, target);

        pickSkillNum = -1;
    }


    // ═════════════════════════════════════════════════════════════
    //  플레이어의 실제 행동
    // ═════════════════════════════════════════════════════════════

    /// <summary>기본 공격. (공격력 100%, 크리티컬 가능)</summary>
    private void BasicAttack(Monster target)
    {
        if (target == null || target.IsAlive() == false) return;

        AttackResult 결과 = Attack(player, target, 1.0f, false, true);

        if (결과.evaded)
        {
            BattleUI.Log("공격 → " + target.unitName + " 이(가) 회피!");
        }
        else
        {
            string 크리 = "";
            if (결과.critical) 크리 = "  ★크리티컬!";
            BattleUI.Log("공격 → " + target.unitName + " 에게 " + 결과.damage + " 피해" + 크리);

            CheckMonsterDead(target);
        }

        if (battleUI != null) battleUI.RefreshEnemies();
        PlayerActionDone(true);
    }

    /// <summary>스킬을 쓴다.</summary>
    private void UseSkill(int skillNum, Monster target)
    {
        SkillData 스킬 = GameData.GetSkill(skillNum);
        if (스킬 == null) return;

        player.UseMp(스킬.mpCost);
        player.StartCool(skillNum);

        BattleUI.Log("[" + 스킬.name + "] 사용!  (MP -" + 스킬.mpCost + ")");

        // ── 1) 회복 스킬 (명상)
        if (스킬.healPercent > 0f)
        {
            int 회복 = player.HealHp(Mathf.RoundToInt(player.maxHp * 스킬.healPercent));
            if (DamagePopup.instance != null) DamagePopup.instance.ShowHeal(player, 회복);
            BattleUI.Log("HP " + 회복 + " 회복");

            if (AudioManager.instance != null) AudioManager.instance.PlayHeal();
        }

        // ── 2) 나에게 거는 버프 (노려보기, 가드)
        if (스킬.hasEffect && 스킬.effectToMe)
        {
            player.AddEffect(스킬.effect, 스킬.effectValue, 스킬.effectTurn);
            BattleUI.Log(GetEffectName(스킬.effect) + " " + 스킬.effectTurn + "턴");
        }

        // ── 3) 추가 행동 (두 개의 심장)
        if (스킬.extraAction)
        {
            player.extraTurn = player.extraTurn + 1;
            BattleUI.Log("이번 턴에 한 번 더 행동할 수 있다!");
        }

        // ── 4) 피해를 주는 스킬이면 대상들을 때린다.
        if (스킬.IsAttackSkill())
        {
            List<Monster> 맞을적들 = FindTargets(스킬, target);

            for (int i = 0; i < 맞을적들.Count; i++)
            {
                SkillDamage(스킬, 맞을적들[i]);
            }
        }

        // ── 5) 적에게 거는 디버프 (약점 격파)
        if (스킬.hasEffect && 스킬.effectToMe == false && target != null)
        {
            target.AddEffect(스킬.effect, 스킬.effectValue, 스킬.effectTurn);
            BattleUI.Log(target.unitName + " : " + GetEffectName(스킬.effect) + " " + 스킬.effectTurn + "턴");
        }

        if (battleUI != null) battleUI.RefreshEnemies();

        // '두 개의 심장' 은 턴을 안 쓴다.
        PlayerActionDone(스킬.freeAction == false);
    }

    /// <summary>스킬이 적 하나에게 주는 피해를 계산한다.</summary>
    private void SkillDamage(SkillData 스킬, Monster target)
    {
        if (target == null || target.IsAlive() == false) return;

        float 배율 = 스킬.power;

        // ── 특별 조건이 있으면 배율이 바뀐다.
        if (스킬.bonusType == SkillBonusType.MyLostHp)
        {
            // 기사회생 : 내가 잃은 체력이 많을수록 세진다. (150% ~ 250%)
            float 잃은비율 = 1f - player.GetHpRate();
            배율 = Mathf.Lerp(스킬.minPower, 스킬.maxPower, Mathf.Clamp01(잃은비율));
        }
        else if (스킬.bonusType == SkillBonusType.EnemyDefending)
        {
            // 급습 : 적이 방어 중이면 방어를 깨고 더 세게
            if (target.IsDefending())
            {
                배율 = 스킬.bonusPower;

                if (스킬.breakDefend)
                {
                    target.RemoveEffect(EffectType.Defend);
                    BattleUI.Log(target.unitName + " 의 방어를 무너뜨렸다!");
                }
            }
        }
        else if (스킬.bonusType == SkillBonusType.EnemyLowHp)
        {
            // 최후의 일격 : 적 체력이 30% 이하면 더 세게
            if (target.GetHpRate() <= 0.3f) 배율 = 스킬.bonusPower;
        }

        AttackResult 결과 = Attack(player, target, 배율, true, true);

        if (결과.evaded)
        {
            BattleUI.Log(target.unitName + " 이(가) 회피!");
            return;
        }

        string 크리 = "";
        if (결과.critical) 크리 = "  ★크리티컬!";
        BattleUI.Log(target.unitName + " 에게 " + 결과.damage + " 피해" + 크리);

        CheckMonsterDead(target);
    }

    /// <summary>스킬의 대상 범위를 실제 몬스터 목록으로 바꾼다.</summary>
    private List<Monster> FindTargets(SkillData 스킬, Monster target)
    {
        List<Monster> 목록 = new List<Monster>();

        if (스킬.target == TargetType.All)
        {
            // 살아있는 적 전부
            for (int i = 0; i < monsters.Length; i++)
            {
                if (monsters[i] != null && monsters[i].IsAlive()) 목록.Add(monsters[i]);
            }
        }
        else if (스킬.target == TargetType.Adjacent)
        {
            // 고른 적 + 왼쪽 1칸 + 오른쪽 1칸
            if (target != null)
            {
                for (int i = target.slot - 1; i <= target.slot + 1; i++)
                {
                    if (i < 0 || i >= monsters.Length) continue;   // 배열 밖은 건너뛴다
                    if (monsters[i] == null || monsters[i].IsAlive() == false) continue;

                    목록.Add(monsters[i]);
                }
            }
        }
        else if (스킬.target == TargetType.Single)
        {
            // 고른 적 1명만
            if (target != null && target.IsAlive()) 목록.Add(target);
        }

        return 목록;
    }


    // ═════════════════════════════════════════════════════════════
    //  죽음 / 승패 판정
    // ═════════════════════════════════════════════════════════════

    /// <summary>몬스터가 죽었는지 확인하고, 죽었으면 보상을 준다. (기획서 2-2-1)</summary>
    private void CheckMonsterDead(Monster m)
    {
        if (m == null) return;
        if (m.IsAlive()) return;

        MonsterData 데이터 = m.GetData();

        // 경험치 = 몬스터 경험치 x 몬스터 레벨
        int 경험치 = 데이터.exp * m.level;

        // 골드 = 경험치 획득량의 50%
        int 골드 = 경험치 / 2;

        player.AddExp(경험치);
        player.gold = player.gold + 골드;

        BattleUI.Log(m.unitName + " 처치!  경험치 +" + 경험치 + ", 골드 +" + 골드);

        m.ShowTurnMark(false);
        m.gameObject.SetActive(false);   // 화면에서 숨긴다
    }

    /// <summary>전투가 끝났는지 확인한다. 끝났으면 true.</summary>
    private bool CheckBattleEnd()
    {
        if (battleOn == false) return false;
        if (ending) return true;

        // 플레이어가 죽었으면 패배
        if (player == null || player.IsAlive() == false)
        {
            ending = true;
            waitInput = false;
            pickMode = PICK_NONE;
            if (battleUI != null) battleUI.ShowCommand(false);

            StartCoroutine(WaitAndEnd(false));
            return true;
        }

        // 적이 전부 죽었으면 승리
        if (AllMonstersDead())
        {
            ending = true;
            waitInput = false;
            pickMode = PICK_NONE;
            if (battleUI != null) battleUI.ShowCommand(false);

            StartCoroutine(WaitAndEnd(true));
            return true;
        }

        return false;
    }

    private IEnumerator WaitAndEnd(bool win)
    {
        yield return new WaitForSeconds(endDelay);
        EndBattle(win);
    }

    /// <summary>몬스터가 전부 죽었는가.</summary>
    public bool AllMonstersDead()
    {
        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null && monsters[i].IsAlive()) return false;
        }
        return true;
    }

    /// <summary>살아있는 보스가 있는가.</summary>
    public bool HasBoss()
    {
        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] == null || monsters[i].IsAlive() == false) continue;
            if (monsters[i].IsBoss()) return true;
        }
        return false;
    }

    /// <summary>F6 치트 : 지금 전투의 모든 적 제거.</summary>
    public void CheatKillAll()
    {
        if (battleOn == false) return;

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] == null || monsters[i].IsAlive() == false) continue;

            monsters[i].TakeDamage(monsters[i].hp);   // 남은 체력만큼 피해 = 반드시 죽음
            CheckMonsterDead(monsters[i]);
        }

        if (battleUI != null) battleUI.RefreshEnemies();
        CheckBattleEnd();
    }
}
