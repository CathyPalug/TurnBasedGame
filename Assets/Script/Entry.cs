using UnityEngine;

/// <summary>
/// 전투에 참여하는 모든 유닛(플레이어 / 몬스터)의 공통 베이스.
/// 턴 순서는 Speed 로 결정된다.
/// </summary>
public abstract class Entry : MonoBehaviour
{
    [Header("공통 정보")]
    public string entryName = "Unit";
    public int level = 1;

    [Header("체력")]
    public int hp;
    public int maxHp;

    [Header("기본 능력치")]
    [SerializeField] protected int baseAtk;
    [SerializeField] protected int baseDef;
    [SerializeField] protected int baseSpeed;

    [Header("연출")]
    [Tooltip("이번 턴 대상임을 나타내는 표시. 씬의 자식 오브젝트를 넣으면 된다.")]
    public GameObject turnMark;

    [Tooltip("타격 이펙트가 터질 위치. 비워두면 오브젝트 위쪽을 쓴다.")]
    public Transform effectPoint;

    /// <summary>타격 / 피해 숫자가 나타날 월드 좌표.</summary>
    public Vector3 EffectPosition
    {
        get { return effectPoint != null ? effectPoint.position : transform.position + Vector3.up * 1.3f; }
    }

    /// <summary>진행 중인 버프 / 디버프 / 상태이상.</summary>
    public StatusEffectHolder effects = new StatusEffectHolder();

    public bool IsAlive { get { return hp > 0; } }

    /// <summary>버프/디버프가 반영된 최종 공격력.</summary>
    public virtual int Atk
    {
        get
        {
            float value = baseAtk;
            value *= 1f + effects.GetValue(EffectType.AttackUp);
            value *= 1f - effects.GetValue(EffectType.AttackDown);
            return Mathf.Max(0, Mathf.RoundToInt(value));
        }
    }

    /// <summary>버프/디버프가 반영된 최종 방어력.</summary>
    public virtual int Def
    {
        get
        {
            float value = baseDef;
            value *= 1f + effects.GetValue(EffectType.DefenseUp);
            value *= 1f - effects.GetValue(EffectType.DefenseDown);
            return Mathf.Max(0, Mathf.RoundToInt(value));
        }
    }

    public virtual int Speed { get { return baseSpeed; } }

    public virtual int CritChance { get { return 0; } }

    public virtual int EvadeChance { get { return 0; } }

    /// <summary>'행동불능' 상태인가.</summary>
    public bool IsStunned { get { return effects.Has(EffectType.Stun); } }

    /// <summary>스킬이 봉인된 상태인가.</summary>
    public bool IsSkillSealed { get { return effects.Has(EffectType.SkillSeal); } }

    /// <summary>이번 턴 '방어' 중인가.</summary>
    public bool IsDefending { get { return effects.Has(EffectType.MonsterDefend); } }

    public float HpRatio { get { return maxHp <= 0 ? 0f : (float)hp / maxHp; } }

    public void SetBaseStats(int atk, int def, int speed)
    {
        baseAtk = atk;
        baseDef = def;
        baseSpeed = speed;
    }

    public int BaseAtk { get { return baseAtk; } set { baseAtk = value; } }
    public int BaseDef { get { return baseDef; } set { baseDef = value; } }
    public int BaseSpeed { get { return baseSpeed; } set { baseSpeed = value; } }

    /// <summary>실제로 hp 를 깎는다. 실제로 깎인 양을 돌려준다.</summary>
    public virtual int ApplyDamage(int amount)
    {
        if (amount < 0) amount = 0;
        int before = hp;
        hp = Mathf.Max(0, hp - amount);
        OnHpChanged();
        return before - hp;
    }

    /// <summary>회복. 실제 회복량을 돌려준다.</summary>
    public virtual int Heal(int amount)
    {
        if (amount < 0) amount = 0;
        int before = hp;
        hp = Mathf.Min(maxHp, hp + amount);
        OnHpChanged();
        return hp - before;
    }

    public void AddEffect(EffectType type, float value, int turns, string sourceName)
    {
        effects.Add(type, value, turns, sourceName);
    }

    /// <summary>이 유닛의 턴이 끝날 때 버프 지속시간을 1 줄인다.</summary>
    public virtual void TickEffects()
    {
        effects.TickAll();
    }

    public virtual void OnBattleEnd()
    {
        effects.ClearBattleEffects();
        ShowTurnMark(false);
    }

    public void ShowTurnMark(bool show)
    {
        if (turnMark != null) turnMark.SetActive(show);
    }

    protected virtual void OnHpChanged() { }
}
