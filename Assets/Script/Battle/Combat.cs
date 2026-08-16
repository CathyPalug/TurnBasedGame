using UnityEngine;

/// <summary>한 번의 공격 결과.</summary>
public struct DamageResult
{
    public bool evaded;
    public bool critical;
    public int rawDamage;       // 방어력 적용 전
    public int finalDamage;     // 실제로 들어간 피해
}

/// <summary>
/// 데미지 계산을 한 곳에 모아둔 정적 클래스.
///
/// 계산 순서
///   1) 위력 = 공격자 최종 공격력 * 스킬 배율
///   2) 스킬 피해 증가 버프(지식의 영약) 적용
///   3) 크리티컬 판정 -> 2배
///   4) 방어력 감소 : 위력 * (1 - 방어력/100)
///      단, 원 위력의 10% 아래로는 내려가지 않는다.
///   5) '방어' 상태라면 (50 + 레벨*3)% 추가 감소
/// </summary>
public static class Combat
{
    /// <summary>회피 판정. true 면 공격이 빗나간다.</summary>
    public static bool RollEvade(Entry defender)
    {
        int evade = defender.EvadeChance;
        if (evade <= 0) return false;
        return Random.Range(0, 100) < evade;
    }

    /// <summary>크리티컬 판정.</summary>
    public static bool RollCritical(Entry attacker)
    {
        int crit = attacker.CritChance;
        if (crit <= 0) return false;
        return Random.Range(0, 100) < crit;
    }

    /// <summary>
    /// 공격 하나를 계산한다. 실제 hp 차감은 하지 않는다.
    /// </summary>
    /// <param name="isSkill">스킬 피해인가 (지식의 영약 적용 여부)</param>
    /// <param name="canCrit">크리티컬이 발동할 수 있는 공격인가</param>
    public static DamageResult Calculate(Entry attacker, Entry defender, float powerMultiplier,
                                         bool isSkill, bool canCrit)
    {
        DamageResult result = new DamageResult();

        if (RollEvade(defender))
        {
            result.evaded = true;
            return result;
        }

        float raw = attacker.Atk * powerMultiplier;

        if (isSkill)
        {
            float skillUp = attacker.effects.GetValue(EffectType.SkillDamageUp);
            if (skillUp > 0f) raw *= 1f + skillUp;
        }

        if (canCrit && RollCritical(attacker))
        {
            result.critical = true;
            raw *= GameData.CritMultiplier;
        }

        result.rawDamage = Mathf.Max(1, Mathf.RoundToInt(raw));

        // 방어력에 의한 감소 (최대 감소량 = 원 위력의 90%)
        float defRatio = Mathf.Clamp01(defender.Def / 100f);
        float afterArmor = raw * (1f - defRatio);
        float floor = raw * GameData.MinDamageRatio;
        if (afterArmor < floor) afterArmor = floor;

        // '방어' 상태 추가 감소
        if (defender.IsDefending)
        {
            float reduce = Mathf.Clamp01(defender.effects.GetValue(EffectType.MonsterDefend));
            afterArmor *= 1f - reduce;
        }

        result.finalDamage = Mathf.Max(1, Mathf.RoundToInt(afterArmor));
        return result;
    }

    /// <summary>
    /// 기사회생용 : 자신이 잃은 체력 비율에 따라 min~max 사이 배율을 구한다.
    /// </summary>
    public static float GetLostHpScaledPower(Entry attacker, float minPower, float maxPower)
    {
        float lostRatio = 1f - attacker.HpRatio;
        return Mathf.Lerp(minPower, maxPower, Mathf.Clamp01(lostRatio));
    }
}
