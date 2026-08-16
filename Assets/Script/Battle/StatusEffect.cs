using System;
using System.Collections.Generic;

/// <summary>
/// 진행 중인 버프 / 디버프 / 상태이상 1개.
/// 남은 턴이 0 이 되면 자동으로 해제된다.
/// </summary>
[Serializable]
public class StatusEffect
{
    /// <summary>전투가 끝날 때까지 유지되는 효과의 turnsLeft 값.</summary>
    public const int UntilBattleEnd = -1;

    public EffectType type;
    public float value;
    public int turnsLeft;
    public string sourceName;

    public StatusEffect(EffectType type, float value, int turns, string sourceName)
    {
        this.type = type;
        this.value = value;
        this.turnsLeft = turns;
        this.sourceName = sourceName;
    }

    public bool IsExpired
    {
        get { return turnsLeft == 0; }
    }

    /// <summary>1턴 경과. 전투 종료까지 지속되는 효과는 줄지 않는다.</summary>
    public void Tick()
    {
        if (turnsLeft > 0) turnsLeft--;
    }
}

/// <summary>
/// StatusEffect 목록을 관리하고 합계를 계산해 주는 헬퍼.
/// </summary>
[Serializable]
public class StatusEffectHolder
{
    private readonly List<StatusEffect> effects = new List<StatusEffect>();

    public IReadOnlyList<StatusEffect> Effects
    {
        get { return effects; }
    }

    /// <summary>같은 종류의 효과가 있으면 남은 턴을 갱신하고, 없으면 새로 추가한다.</summary>
    public void Add(EffectType type, float value, int turns, string sourceName)
    {
        if (type == EffectType.None) return;

        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i].type != type) continue;

            effects[i].value = value;
            // UntilBattleEnd(-1) 가 더 강한 지속으로 취급된다.
            if (turns == StatusEffect.UntilBattleEnd || effects[i].turnsLeft == StatusEffect.UntilBattleEnd)
                effects[i].turnsLeft = StatusEffect.UntilBattleEnd;
            else
                effects[i].turnsLeft = UnityEngine.Mathf.Max(effects[i].turnsLeft, turns);

            effects[i].sourceName = sourceName;
            return;
        }

        effects.Add(new StatusEffect(type, value, turns, sourceName));
    }

    public bool Has(EffectType type)
    {
        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i].type == type) return true;
        }
        return false;
    }

    public float GetValue(EffectType type)
    {
        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i].type == type) return effects[i].value;
        }
        return 0f;
    }

    public int GetTurnsLeft(EffectType type)
    {
        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i].type == type) return effects[i].turnsLeft;
        }
        return 0;
    }

    public void Remove(EffectType type)
    {
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            if (effects[i].type == type) effects.RemoveAt(i);
        }
    }

    /// <summary>모든 효과의 남은 턴을 1 줄이고 만료된 것을 제거한다. 제거된 효과 목록을 돌려준다.</summary>
    public List<StatusEffect> TickAll()
    {
        List<StatusEffect> expired = new List<StatusEffect>();

        for (int i = effects.Count - 1; i >= 0; i--)
        {
            effects[i].Tick();
            if (effects[i].IsExpired)
            {
                expired.Add(effects[i]);
                effects.RemoveAt(i);
            }
        }
        return expired;
    }

    public void ClearAll()
    {
        effects.Clear();
    }

    /// <summary>전투 종료 시 호출. 전투용 버프를 전부 지운다.</summary>
    public void ClearBattleEffects()
    {
        effects.Clear();
    }
}
