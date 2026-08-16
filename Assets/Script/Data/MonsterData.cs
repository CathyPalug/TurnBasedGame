using System;

/// <summary>
/// 몬스터가 쓰는 스킬 1개의 정의. GameData.MonsterSkills 배열에 담긴다.
/// </summary>
[Serializable]
public class MonsterSkillData
{
    public int id;
    public string skillName;
    public float power;             // 공격력 배율
    public int cooldown;            // 턴

    public bool hasEffect;
    public EffectType effectType = EffectType.None;
    public float effectValue;
    public int effectTurns;

    public float lifestealRatio;    // 영혼 흡수 : 가한 피해의 50% 회복 => 0.5f
    public bool telegraph;          // true 면 사용 예고를 플레이어에게 보여준다

    public string description;
}

/// <summary>
/// 몬스터 종류 1개의 정의. GameData.Monsters 배열에 담긴다.
/// 실제 능력치는 [기본값 + 레벨 * 계수] 공식으로 산출한다.
/// </summary>
[Serializable]
public class MonsterData
{
    public int id;
    public string monsterName;

    public int stageIndex;          // 0 = 1스테이지, 1 = 2스테이지, 2 = 3스테이지
    public int minLevel;
    public int maxLevel;

    public int hpBase;
    public int hpPerLevel;
    public int atkBase;
    public int atkPerLevel;
    public int defense;
    public int speed;
    public int expReward;

    public bool isElite;
    public bool isBoss;

    public int[] skillIds = new int[0];   // GameData.MonsterSkills 인덱스

    // HP 50% 이하 페이즈 변화
    public bool hasPhaseChange;
    public float phaseHealPercent;          // 3보스 : 0.25f
    public int phaseDefDelta;               // 3보스 : -30
    public int phaseAtkDelta;               // 3보스 : +20
    public bool phaseDebuffsPlayerAtk;      // 2보스 : 플레이어 공격력 감소
    public float phasePlayerAtkDownValue;   // 0.3f
    public int phasePlayerAtkDownTurns;     // 4

    public int GetMaxHp(int level)
    {
        return hpBase + hpPerLevel * level;
    }

    public int GetAtk(int level)
    {
        return atkBase + atkPerLevel * level;
    }

    /// <summary>이 몬스터가 '방어'로 줄이는 피해 비율. (50 + 레벨 * 3)%</summary>
    public static float GetDefendReduction(int level)
    {
        return (50f + level * 3f) / 100f;
    }
}
