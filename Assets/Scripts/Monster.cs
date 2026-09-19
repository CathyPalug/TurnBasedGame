using UnityEngine;

// ═════════════════════════════════════════════════════════════════════════════
//  Monster.cs  —  일반 몬스터 / 정예 / 보스가 전부 이 스크립트 하나를 쓴다
//
//  어떤 몬스터가 될지는 GameData.Monsters 의 번호(dataNum)로 정해진다.
//
//  ── 몬스터의 한 라운드 흐름 (기획서 2-4-1) ──
//    1) PlanAction()   : 이번 턴에 뭘 할지 미리 정한다
//                        → 플레이어가 "이 적은 방어할 거구나" 를 미리 알 수 있다
//    2) StartDefend()  : 방어를 골랐으면 라운드 맨 앞에서 먼저 발동한다 (기획서 필수!)
//    3) DoTurn()       : 자기 차례가 오면 정해둔 행동을 실제로 한다
//
//  ★ 3D 모델은 다운받은 에셋 "LowPoly Fantasy Monsters Pack" 의 프리팹을 쓴다.
//     (에셋에 붙어있는 스크립트는 전부 떼어내고 이 스크립트만 붙인다)
// ═════════════════════════════════════════════════════════════════════════════

public class Monster : Unit
{
    [Header("몬스터 종류")]
    [Tooltip("GameData.Monsters 배열의 번호")]
    public int dataNum;

    [Header("서 있는 자리 (왼쪽부터 0, 1, 2, 3). 양 옆 판정에 쓴다")]
    public int slot;

    [Header("이번 턴에 하기로 정한 행동")]
    public MonsterAction plan = MonsterAction.Attack;
    public int planSkillNum = -1;

    [Header("AI")]
    [Range(0f, 1f)]
    [Tooltip("쓸 스킬이 없을 때 '방어'를 고를 확률. 0.3 = 30%")]
    public float defendChance = 0.3f;

    /// <summary>보유 스킬마다 남은 쿨타임. MonsterData.skills 와 같은 순서.</summary>
    [HideInInspector] public int[] skillCool = new int[0];

    /// <summary>페이즈 변화를 이미 했는가. (한 번만 발동한다)</summary>
    private bool phaseDone;


    // ═════════════════════════════════════════════════════════════
    //  정보 확인
    // ═════════════════════════════════════════════════════════════

    /// <summary>이 몬스터의 설명서를 가져온다.</summary>
    public MonsterData GetData()
    {
        return GameData.GetMonster(dataNum);
    }

    /// <summary>보스인가.</summary>
    public bool IsBoss()
    {
        MonsterData 데이터 = GetData();
        if (데이터 == null) return false;
        return 데이터.isBoss;
    }

    /// <summary>이번 턴에 '방어'를 할 예정인가. (플레이어에게 미리 보여줄 정보)</summary>
    public bool WillDefend()
    {
        if (plan == MonsterAction.Defend) return true;
        return false;
    }

    /// <summary>
    /// 이번 턴에 미리 예고해야 하는 스킬 이름. (파괴 광선)
    /// 예고할 스킬이 없으면 빈 글자("").
    /// </summary>
    public string GetWarningSkillName()
    {
        if (plan != MonsterAction.Skill) return "";

        MonsterSkillData 스킬 = GameData.GetMonsterSkill(planSkillNum);
        if (스킬 == null) return "";
        if (스킬.showWarning == false) return "";

        return 스킬.name;
    }


    // ═════════════════════════════════════════════════════════════
    //  만들어질 때 초기화
    // ═════════════════════════════════════════════════════════════

    /// <summary>몬스터 번호와 레벨을 정해서 세팅한다.</summary>
    public void Setup(int num, int myLevel, int mySlot)
    {
        dataNum = num;
        slot = mySlot;

        MonsterData 데이터 = GetData();
        if (데이터 == null)
        {
            Debug.LogError("Monster.Setup : 없는 몬스터 번호 → " + num);
            return;
        }

        // 레벨은 이 몬스터가 나올 수 있는 범위 안으로 맞춘다.
        level = Mathf.Clamp(myLevel, 데이터.minLevel, 데이터.maxLevel);
        unitName = 데이터.name;

        maxHp = 데이터.GetMaxHp(level);
        hp = maxHp;

        atk = 데이터.GetAtk(level);
        def = 데이터.def;
        speed = 데이터.speed;

        // 몬스터는 크리티컬도 회피도 하지 않는다. 장비도 없다.
        critRate = 0;
        evadeRate = 0;
        bonusAtk = 0;
        bonusDef = 0;
        bonusCrit = 0;

        // 전투 시작하자마자 최강 스킬을 맞으면 너무 아프므로
        // 쿨타임을 절반쯤 채워둔 상태로 시작한다.
        int 스킬수 = 0;
        if (데이터.skills != null) 스킬수 = 데이터.skills.Length;

        skillCool = new int[스킬수];
        for (int i = 0; i < 스킬수; i++)
        {
            MonsterSkillData 스킬 = GameData.GetMonsterSkill(데이터.skills[i]);

            if (스킬 == null)
            {
                skillCool[i] = 0;
            }
            else
            {
                // 쿨타임의 절반. 단 최소 1턴은 기다리게 한다.
                skillCool[i] = Mathf.Max(1, Mathf.CeilToInt(스킬.coolTime * 0.5f));
            }
        }

        phaseDone = false;
        plan = MonsterAction.Attack;
        planSkillNum = -1;

        ClearEffects();
        ShowTurnMark(false);
    }

    /// <summary>레벨을 무작위로 정해서 세팅한다.</summary>
    public void SetupRandom(int num, int mySlot)
    {
        MonsterData 데이터 = GameData.GetMonster(num);
        if (데이터 == null) return;

        // Random.Range 는 정수일 때 뒤 숫자를 포함하지 않으므로 +1 해준다.
        int 레벨 = Random.Range(데이터.minLevel, 데이터.maxLevel + 1);
        Setup(num, 레벨, mySlot);
    }


    // ═════════════════════════════════════════════════════════════
    //  1) 이번 턴 행동 정하기 (라운드 시작 때 호출)
    // ═════════════════════════════════════════════════════════════

    public void PlanAction()
    {
        // 죽어 있으면 아무것도 안 한다.
        if (IsAlive() == false)
        {
            plan = MonsterAction.Attack;
            planSkillNum = -1;
            return;
        }

        planSkillNum = -1;

        // 1순위 : 쿨타임이 끝난 스킬이 있으면 스킬을 쓴다.
        //         (기획서 : 스킬이 있으면 패턴 중간중간 반드시 써야 한다)
        int 쓸칸 = FindReadySkill();
        if (쓸칸 >= 0)
        {
            plan = MonsterAction.Skill;
            planSkillNum = GetData().skills[쓸칸];
            return;
        }

        // 2순위 : 스킬이 없으면 확률로 방어 또는 공격.
        // Random.value 는 0.0 ~ 1.0 사이 실수를 준다.
        if (Random.value < defendChance) plan = MonsterAction.Defend;
        else plan = MonsterAction.Attack;
    }

    /// <summary>
    /// 지금 쓸 수 있는 스킬 중 가장 센 것의 "칸 번호" 를 찾는다. 없으면 -1.
    /// (쿨타임이 길수록 센 스킬이라고 보고 고른다)
    /// </summary>
    private int FindReadySkill()
    {
        MonsterData 데이터 = GetData();
        if (데이터 == null) return -1;
        if (데이터.skills == null || 데이터.skills.Length == 0) return -1;

        int 가장센칸 = -1;
        int 가장긴쿨 = -1;

        for (int i = 0; i < 데이터.skills.Length; i++)
        {
            if (skillCool[i] > 0) continue;   // 아직 쿨타임 중

            MonsterSkillData 스킬 = GameData.GetMonsterSkill(데이터.skills[i]);
            if (스킬 == null) continue;

            if (스킬.coolTime > 가장긴쿨)
            {
                가장긴쿨 = 스킬.coolTime;
                가장센칸 = i;
            }
        }

        return 가장센칸;
    }


    // ═════════════════════════════════════════════════════════════
    //  2) 방어는 라운드 맨 앞에서 먼저 발동한다 (기획서 필수)
    // ═════════════════════════════════════════════════════════════

    public void StartDefend()
    {
        if (plan != MonsterAction.Defend) return;
        if (IsAlive() == false) return;

        float 감소 = GameData.GetDefendCut(level);   // (50 + 레벨x3) %

        // 이 효과는 다음 라운드 시작 때 BattleManager 가 직접 지우므로
        // 턴이 지나도 안 사라지게 아주 긴 턴수로 걸어둔다.
        AddEffect(EffectType.Defend, 감소, GameData.FOREVER);

        BattleUI.Log(unitName + " 이(가) 방어! 받는 피해 " + Mathf.RoundToInt(감소 * 100f) + "% 감소");
    }


    // ═════════════════════════════════════════════════════════════
    //  3) 턴 종료 처리
    // ═════════════════════════════════════════════════════════════

    /// <summary>스킬 쿨타임을 1씩 줄인다.</summary>
    public void TickCool()
    {
        for (int i = 0; i < skillCool.Length; i++)
        {
            if (skillCool[i] > 0) skillCool[i] = skillCool[i] - 1;
        }
    }

    /// <summary>스킬을 썼을 때 그 스킬의 쿨타임을 시작한다.</summary>
    public void StartCool(int monsterSkillNum)
    {
        MonsterData 데이터 = GetData();
        if (데이터 == null || 데이터.skills == null) return;

        MonsterSkillData 스킬 = GameData.GetMonsterSkill(monsterSkillNum);
        if (스킬 == null) return;

        for (int i = 0; i < 데이터.skills.Length; i++)
        {
            if (데이터.skills[i] == monsterSkillNum)
            {
                skillCool[i] = 스킬.coolTime;
            }
        }
    }


    // ═════════════════════════════════════════════════════════════
    //  보스 페이즈 변화 (기획서 2-4-2)
    //  ★ BattleManager 가 몬스터에게 피해를 준 직후에 이 함수를 불러준다.
    // ═════════════════════════════════════════════════════════════

    public void CheckPhase()
    {
        MonsterData 데이터 = GetData();

        // 아래 중 하나라도 걸리면 페이즈 변화를 하지 않는다.
        if (데이터 == null) return;
        if (데이터.hasPhase == false) return;   // 페이즈가 없는 몬스터
        if (phaseDone) return;                   // 이미 한 번 했다
        if (IsAlive() == false) return;          // 죽었다
        if (GetHpRate() > 0.5f) return;          // 아직 체력이 반 이상

        phaseDone = true;   // 두 번 발동하지 않게 표시

        // ── (3보스) 체력 25% 회복
        if (데이터.phaseHeal > 0f)
        {
            int 회복 = HealHp(Mathf.RoundToInt(maxHp * 데이터.phaseHeal));
            if (DamagePopup.instance != null) DamagePopup.instance.ShowHeal(this, 회복);
            BattleUI.Log(unitName + " 각성! HP " + 회복 + " 회복");
        }

        // ── (3보스) 방어력 -30, 공격력 +20
        if (데이터.phaseDefAdd != 0 || 데이터.phaseAtkAdd != 0)
        {
            def = Mathf.Max(0, def + 데이터.phaseDefAdd);
            atk = Mathf.Max(0, atk + 데이터.phaseAtkAdd);

            BattleUI.Log(unitName + " : 방어력 " + 데이터.phaseDefAdd + ", 공격력 +" + 데이터.phaseAtkAdd);
        }

        // ── (2보스) 플레이어 공격력을 4턴간 30% 감소
        if (데이터.phaseCursePlayer && BattleManager.instance != null)
        {
            Player 플레이어 = BattleManager.instance.player;
            if (플레이어 != null)
            {
                플레이어.AddEffect(EffectType.AtkDown, 데이터.phaseCurseValue, 데이터.phaseCurseTurn);

                BattleUI.Log(unitName + " 의 위압! 플레이어 공격력이 "
                    + 데이터.phaseCurseTurn + "턴간 "
                    + Mathf.RoundToInt(데이터.phaseCurseValue * 100f) + "% 감소");
            }
        }
    }
}
