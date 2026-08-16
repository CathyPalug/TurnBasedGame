using System;

/// <summary>
/// 플레이어 스킬 1개의 정의. GameData.Skills 배열에 담긴다.
/// </summary>
[Serializable]
public class SkillData
{
    public int id;
    public string skillName;
    public int mpCost;
    public int cooldown;                    // 0 이면 쿨타임 없음

    public SkillTargetType targetType = SkillTargetType.Single;

    public float power;                     // 공격력 배율 (1.7f = 170%)
    public SkillCondition condition = SkillCondition.None;
    public float conditionPower;            // 조건 충족 시 대체 배율
    public float scaleMinPower;             // 기사회생 하한 배율
    public float scaleMaxPower;             // 기사회생 상한 배율
    public bool breakTargetDefend;          // 급습 : 대상의 '방어' 해제

    public float healPercentOfMaxHp;        // 명상 : 0.3f = 최대 체력 30% 회복

    public bool hasEffect;
    public EffectType effectType = EffectType.None;
    public float effectValue;               // 0.3f = 30%, 25f = 25%p 등 효과별 의미
    public int effectTurns;
    public bool effectOnSelf = true;        // false 면 대상에게 건다

    public bool grantsExtraAction;          // 두 개의 심장 : 이번 턴 1회 추가 행동
    public bool doesNotConsumeTurn;         // 두 개의 심장 : 턴을 소모하지 않음

    public SkillAcquireType acquireType = SkillAcquireType.Level;
    public int acquireLevel = 1;            // acquireType == Level 일 때 습득 레벨
    public int price;                       // acquireType == ShopBook 일 때 스킬북 가격

    public string description;

    public bool DealsDamage
    {
        get { return power > 0f || scaleMaxPower > 0f; }
    }
}
