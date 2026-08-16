using UnityEngine;

/// <summary>
/// 몬스터 / 정예 / 보스 공용 유닛.
/// GameData.Monsters 의 데이터로 초기화되며,
/// 매 라운드 시작 시 PlanAction() 으로 이번 턴 행동을 미리 정한다.
/// (기획서 : '방어'는 반드시 먼저 발동하고, 플레이어가 미리 알 수 있어야 한다)
/// </summary>
public class Monster : Entry
{
    [Header("몬스터 데이터")]
    [Tooltip("GameData.Monsters 인덱스")]
    public int monsterId;

    [Header("전투 위치 (인접 판정에 사용)")]
    public int slotIndex;

    [Header("이번 턴 예정 행동")]
    public MonsterActionType plannedAction = MonsterActionType.Attack;
    public int plannedSkillId = -1;

    [Header("AI")]
    [Range(0f, 1f)]
    [Tooltip("스킬이 준비되지 않았을 때 '방어'를 고를 확률")]
    public float defendChance = 0.3f;

    /// <summary>보유 스킬별 남은 쿨타임. skillIds 와 같은 인덱스.</summary>
    [HideInInspector] public int[] skillCooldowns = new int[0];

    private bool phaseTriggered;

    public MonsterData Data { get { return GameData.GetMonster(monsterId); } }

    public bool IsBoss { get { return Data != null && Data.isBoss; } }
    public bool IsElite { get { return Data != null && Data.isElite; } }

    /// <summary>이번 턴에 '방어'를 사용할 예정인가. (플레이어에게 미리 보여줄 정보)</summary>
    public bool WillDefend { get { return plannedAction == MonsterActionType.Defend; } }

    /// <summary>이번 턴에 사용을 예고해야 하는 스킬. 없으면 null. (예: 파괴 광선)</summary>
    public MonsterSkillData TelegraphedSkill
    {
        get
        {
            if (plannedAction != MonsterActionType.Skill) return null;
            MonsterSkillData s = GameData.GetMonsterSkill(plannedSkillId);
            return (s != null && s.telegraph) ? s : null;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────────────────────
    public void Setup(int id, int monsterLevel, int slot)
    {
        monsterId = id;
        slotIndex = slot;

        MonsterData data = Data;
        if (data == null)
        {
            Debug.LogError("Monster.Setup : 잘못된 monsterId " + id);
            return;
        }

        level = Mathf.Clamp(monsterLevel, data.minLevel, data.maxLevel);
        entryName = data.monsterName;

        maxHp = data.GetMaxHp(level);
        hp = maxHp;
        SetBaseStats(data.GetAtk(level), data.defense, data.speed);

        // 전투 시작하자마자 최상위 스킬을 쓰지 않도록 절반 정도 쿨타임을 준 채로 시작한다.
        // (기획서 : 스킬은 패턴 중간중간 사용해야 한다)
        skillCooldowns = new int[data.skillIds != null ? data.skillIds.Length : 0];
        for (int i = 0; i < skillCooldowns.Length; i++)
        {
            MonsterSkillData s = GameData.GetMonsterSkill(data.skillIds[i]);
            skillCooldowns[i] = (s == null) ? 0 : Mathf.Max(1, Mathf.CeilToInt(s.cooldown * 0.5f));
        }

        phaseTriggered = false;
        plannedAction = MonsterActionType.Attack;
        plannedSkillId = -1;

        effects.ClearAll();
        ShowTurnMark(false);
    }

    /// <summary>레벨 범위 안에서 무작위 레벨로 생성.</summary>
    public void SetupRandomLevel(int id, int slot)
    {
        MonsterData data = GameData.GetMonster(id);
        if (data == null) return;
        Setup(id, Random.Range(data.minLevel, data.maxLevel + 1), slot);
    }

    // ─────────────────────────────────────────────────────────────
    // 행동 결정 (라운드 시작 시 호출)
    // ─────────────────────────────────────────────────────────────
    public void PlanAction()
    {
        if (!IsAlive)
        {
            plannedAction = MonsterActionType.Attack;
            plannedSkillId = -1;
            return;
        }

        MonsterData data = Data;
        plannedSkillId = -1;

        // 1) 쿨타임이 끝난 스킬이 있으면 우선 사용한다.
        //    정예 / 보스는 반드시 스킬을 패턴에 섞어 쓴다.
        int readySkill = PickReadySkill();
        if (readySkill >= 0)
        {
            plannedAction = MonsterActionType.Skill;
            plannedSkillId = data.skillIds[readySkill];
            return;
        }

        // 2) 스킬이 없으면 공격 / 방어 중 하나
        plannedAction = (Random.value < defendChance) ? MonsterActionType.Defend : MonsterActionType.Attack;
    }

    /// <summary>사용 가능한 스킬의 '보유 인덱스'를 돌려준다. 없으면 -1.</summary>
    private int PickReadySkill()
    {
        MonsterData data = Data;
        if (data == null || data.skillIds == null || data.skillIds.Length == 0) return -1;

        // 위력이 큰(쿨타임이 긴) 스킬을 우선한다.
        int best = -1;
        int bestCooldown = -1;
        for (int i = 0; i < data.skillIds.Length; i++)
        {
            if (skillCooldowns[i] > 0) continue;
            MonsterSkillData s = GameData.GetMonsterSkill(data.skillIds[i]);
            if (s == null) continue;
            if (s.cooldown > bestCooldown)
            {
                bestCooldown = s.cooldown;
                best = i;
            }
        }
        return best;
    }

    /// <summary>
    /// '방어'는 라운드 맨 앞에서 먼저 발동한다. BattleManager 가 호출한다.
    /// </summary>
    public void ApplyPlannedDefend()
    {
        if (plannedAction != MonsterActionType.Defend || !IsAlive) return;

        float reduction = MonsterData.GetDefendReduction(level);
        // 라운드가 끝날 때 BattleManager 가 직접 제거하므로 턴 감소 대상에서 뺀다.
        AddEffect(EffectType.MonsterDefend, reduction, StatusEffect.UntilBattleEnd, "방어");

        BattleLog.Log(string.Format("{0}이(가) 방어 태세를 취했다. (받는 피해 {1}% 감소)",
            entryName, Mathf.RoundToInt(reduction * 100f)));
    }

    // ─────────────────────────────────────────────────────────────
    // 행동 실행
    // ─────────────────────────────────────────────────────────────
    public void ExecuteTurn(Player target)
    {
        if (!IsAlive || target == null) return;

        if (IsStunned)
        {
            BattleLog.Log(string.Format("{0}은(는) 행동불능 상태다.", entryName));
            return;
        }

        switch (plannedAction)
        {
            case MonsterActionType.Defend:
                // 라운드 시작 때 이미 발동했으므로 여기서는 아무것도 하지 않는다.
                break;

            case MonsterActionType.Skill:
                UseSkill(target);
                break;

            default:
                BasicAttack(target);
                break;
        }
    }

    private void BasicAttack(Player target)
    {
        DamageResult r = Combat.Calculate(this, target, 1.0f, false, false);

        if (r.evaded)
        {
            BattleEffects.Miss(target);
            BattleLog.Log(string.Format("{0}의 공격 - {1}이(가) 회피했다!", entryName, target.entryName));
            return;
        }

        int dealt = target.ApplyDamage(r.finalDamage);
        BattleEffects.Hit(target, dealt, false);
        BattleLog.Log(string.Format("{0}의 공격 - {1}에게 {2} 피해", entryName, target.entryName, dealt));
    }

    private void UseSkill(Player target)
    {
        MonsterData data = Data;
        MonsterSkillData skill = GameData.GetMonsterSkill(plannedSkillId);
        if (skill == null)
        {
            BasicAttack(target);
            return;
        }

        // 쿨타임 시작
        for (int i = 0; i < data.skillIds.Length; i++)
        {
            if (data.skillIds[i] == plannedSkillId) skillCooldowns[i] = skill.cooldown;
        }

        DamageResult r = Combat.Calculate(this, target, skill.power, true, false);

        if (r.evaded)
        {
            BattleEffects.Miss(target);
            BattleLog.Log(string.Format("{0}의 [{1}] - {2}이(가) 회피했다!", entryName, skill.skillName, target.entryName));
            return;
        }

        int dealt = target.ApplyDamage(r.finalDamage);
        // 몬스터 스킬은 기본 공격보다 크게 연출한다
        BattleEffects.Hit(target, dealt, true);
        string log = string.Format("{0}의 [{1}] - {2}에게 {3} 피해", entryName, skill.skillName, target.entryName, dealt);

        if (skill.lifestealRatio > 0f)
        {
            int healed = Heal(Mathf.RoundToInt(dealt * skill.lifestealRatio));
            BattleEffects.Heal(this, healed);
            log += string.Format(" (자신 {0} 회복)", healed);
        }

        if (skill.hasEffect && skill.effectType != EffectType.None)
        {
            target.AddEffect(skill.effectType, skill.effectValue, skill.effectTurns, skill.skillName);
            log += string.Format(" ({0} {1}턴)", GetEffectLabel(skill.effectType), skill.effectTurns);
        }

        BattleLog.Log(log);
    }

    private static string GetEffectLabel(EffectType type)
    {
        switch (type)
        {
            case EffectType.SkillSeal: return "스킬 봉인";
            case EffectType.Stun: return "행동불능";
            case EffectType.AttackDown: return "공격력 감소";
            case EffectType.DefenseDown: return "방어력 감소";
            default: return type.ToString();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 턴 종료 처리
    // ─────────────────────────────────────────────────────────────
    public override void TickEffects()
    {
        base.TickEffects();

        for (int i = 0; i < skillCooldowns.Length; i++)
        {
            if (skillCooldowns[i] > 0) skillCooldowns[i]--;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 피해 / 페이즈 변화
    // ─────────────────────────────────────────────────────────────
    public override int ApplyDamage(int amount)
    {
        int dealt = base.ApplyDamage(amount);
        CheckPhaseChange();
        return dealt;
    }

    /// <summary>보스 HP 50% 이하 페이즈 변화. 한 번만 발동한다.</summary>
    private void CheckPhaseChange()
    {
        MonsterData data = Data;
        if (data == null || !data.hasPhaseChange || phaseTriggered) return;
        if (!IsAlive) return;
        if (HpRatio > 0.5f) return;

        phaseTriggered = true;

        if (data.phaseHealPercent > 0f)
        {
            int healed = Heal(Mathf.RoundToInt(maxHp * data.phaseHealPercent));
            BattleEffects.Heal(this, healed);
            BattleEffects.Shake(0.25f, 0.4f);
            BattleLog.Log(string.Format("{0}이(가) 각성했다! HP {1} 회복", entryName, healed));
        }

        if (data.phaseDefDelta != 0 || data.phaseAtkDelta != 0)
        {
            BaseDef = Mathf.Max(0, BaseDef + data.phaseDefDelta);
            BaseAtk = Mathf.Max(0, BaseAtk + data.phaseAtkDelta);
            BattleLog.Log(string.Format("{0} : 방어력 {1}, 공격력 {2}",
                entryName,
                data.phaseDefDelta >= 0 ? "+" + data.phaseDefDelta : data.phaseDefDelta.ToString(),
                data.phaseAtkDelta >= 0 ? "+" + data.phaseAtkDelta : data.phaseAtkDelta.ToString()));
        }

        if (data.phaseDebuffsPlayerAtk && BattleManager.instance != null && BattleManager.instance.player != null)
        {
            BattleManager.instance.player.AddEffect(
                EffectType.AttackDown, data.phasePlayerAtkDownValue, data.phasePlayerAtkDownTurns, entryName);

            BattleLog.Log(string.Format("{0}의 위압! 플레이어의 공격력이 {1}턴간 {2}% 감소한다.",
                entryName, data.phasePlayerAtkDownTurns, Mathf.RoundToInt(data.phasePlayerAtkDownValue * 100f)));
        }
    }
}
