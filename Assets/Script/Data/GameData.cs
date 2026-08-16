/// <summary>
/// 기획서의 모든 수치 테이블을 배열로 담아둔 정적 클래스.
/// 스킬 / 장비 / 소모품 / 몬스터는 전부 여기 배열의 인덱스(id)로 참조한다.
/// </summary>
public static class GameData
{
    // ─────────────────────────────────────────────────────────────
    // 플레이어 기본값
    // ─────────────────────────────────────────────────────────────
    public const int BaseMaxHp = 100;
    public const int BaseMaxMp = 50;
    public const int BaseDef = 10;
    public const int BaseAtk = 20;
    public const int BaseSpeed = 10;
    public const int BaseCritChance = 10;   // %
    public const int BaseEvadeChance = 15;  // %
    public const int InventorySize = 6;     // 배낭 6칸
    public const int MaxLevel = 10;

    // 레벨업 시 증가량
    public const int LevelUpHp = 20;
    public const int LevelUpMp = 10;
    public const int LevelUpAtk = 10;

    // 크리티컬 배율
    public const float CritMultiplier = 2f;

    // 방어력으로 줄일 수 있는 최대치 : 원 위력의 10% 아래로는 내려가지 않는다.
    public const float MinDamageRatio = 0.1f;

    // 휴식 시 회복량
    public const float RestHealPercent = 0.3f;

    /// <summary>레벨 N -> N+1 에 필요한 경험치. 인덱스 0 이 1->2.</summary>
    public static readonly int[] RequiredExp =
    {
        50, 90, 140, 200, 280, 380, 500, 650, 850
    };

    // ─────────────────────────────────────────────────────────────
    // 스킬 (13종) - 인덱스 = id
    // ─────────────────────────────────────────────────────────────
    public static readonly SkillData[] Skills =
    {
        new SkillData
        {
            id = 0, skillName = "베기", mpCost = 30, cooldown = 0,
            targetType = SkillTargetType.Single, power = 1.7f,
            acquireType = SkillAcquireType.Level, acquireLevel = 1,
            description = "지정한 단일 대상 한명에게 공격력의 170%의 피해를 입힙니다."
        },
        new SkillData
        {
            id = 1, skillName = "가르기", mpCost = 35, cooldown = 0,
            targetType = SkillTargetType.SingleAndAdjacent, power = 1.4f,
            acquireType = SkillAcquireType.Level, acquireLevel = 1,
            description = "지정한 단일 대상과 인접한 적들에게 공격력의 140%의 피해를 입힙니다."
        },
        new SkillData
        {
            id = 2, skillName = "노려보기", mpCost = 25, cooldown = 0,
            targetType = SkillTargetType.Self,
            hasEffect = true, effectType = EffectType.CritRateUp,
            effectValue = 25f, effectTurns = 3, effectOnSelf = true,
            acquireType = SkillAcquireType.Level, acquireLevel = 2,
            description = "3턴간 크리티컬 확률이 25% 증가합니다."
        },
        new SkillData
        {
            id = 3, skillName = "명상", mpCost = 45, cooldown = 5,
            targetType = SkillTargetType.Self, healPercentOfMaxHp = 0.3f,
            acquireType = SkillAcquireType.Level, acquireLevel = 2,
            description = "최대 체력의 30%를 회복합니다."
        },
        new SkillData
        {
            id = 4, skillName = "필살기", mpCost = 100, cooldown = 10,
            targetType = SkillTargetType.AllEnemies, power = 3.0f,
            acquireType = SkillAcquireType.Level, acquireLevel = 4,
            description = "모든 대상에게 공격력의 300%의 피해를 입힙니다."
        },
        new SkillData
        {
            id = 5, skillName = "가드", mpCost = 30, cooldown = 0,
            targetType = SkillTargetType.Self,
            hasEffect = true, effectType = EffectType.DefenseUp,
            effectValue = 0.3f, effectTurns = 2, effectOnSelf = true,
            acquireType = SkillAcquireType.ShopBook, price = 200,
            description = "2턴간 방어력이 30% 증가합니다."
        },
        new SkillData
        {
            id = 6, skillName = "기사회생", mpCost = 45, cooldown = 0,
            targetType = SkillTargetType.Single,
            condition = SkillCondition.ScaleByLostHp,
            scaleMinPower = 1.5f, scaleMaxPower = 2.5f,
            acquireType = SkillAcquireType.ShopBook, price = 300,
            description = "지정한 단일 대상에게 자신이 잃은 체력 %에 비례하여 공격력의 150%~250%의 피해를 입힙니다."
        },
        new SkillData
        {
            id = 7, skillName = "약점 격파", mpCost = 55, cooldown = 2,
            targetType = SkillTargetType.Single, power = 1.3f,
            hasEffect = true, effectType = EffectType.DefenseDown,
            effectValue = 0.3f, effectTurns = 3, effectOnSelf = false,
            acquireType = SkillAcquireType.ShopBook, price = 300,
            description = "공격력의 130%의 피해를 입히고 대상의 방어력을 3턴간 30% 감소시킵니다."
        },
        new SkillData
        {
            id = 8, skillName = "화염구", mpCost = 65, cooldown = 0,
            targetType = SkillTargetType.SingleAndAdjacent, power = 1.8f,
            acquireType = SkillAcquireType.ShopBook, price = 350,
            description = "지정된 단일 대상과 인접한 적들에게 공격력의 180%의 피해를 입힙니다."
        },
        new SkillData
        {
            id = 9, skillName = "급습", mpCost = 60, cooldown = 2,
            targetType = SkillTargetType.Single, power = 1.5f,
            condition = SkillCondition.TargetIsDefending, conditionPower = 2.5f,
            breakTargetDefend = true,
            acquireType = SkillAcquireType.ShopBook, price = 350,
            description = "공격력의 150%의 피해를 입힙니다. 대상이 '방어' 상태라면 즉시 해제시키고 250%의 피해를 입힙니다."
        },
        new SkillData
        {
            id = 10, skillName = "최후의 일격", mpCost = 70, cooldown = 1,
            targetType = SkillTargetType.Single, power = 1.7f,
            condition = SkillCondition.TargetHpBelow30, conditionPower = 3.0f,
            acquireType = SkillAcquireType.ShopBook, price = 400,
            description = "공격력의 170%의 피해를 입힙니다. 대상의 체력이 30% 이하라면 300%의 피해를 입힙니다."
        },
        new SkillData
        {
            id = 11, skillName = "공방일체", mpCost = 100, cooldown = 3,
            targetType = SkillTargetType.SingleAndAdjacent, power = 1.0f,
            hasEffect = true, effectType = EffectType.DefenseUp,
            effectValue = 0.3f, effectTurns = 3, effectOnSelf = true,
            acquireType = SkillAcquireType.ShopBook, price = 500,
            description = "지정된 대상 및 인접한 적들에게 공격력의 100%의 피해를 입히고 3턴간 방어력이 30% 증가합니다."
        },
        new SkillData
        {
            id = 12, skillName = "두 개의 심장", mpCost = 0, cooldown = 10,
            targetType = SkillTargetType.Self,
            grantsExtraAction = true, doesNotConsumeTurn = true,
            acquireType = SkillAcquireType.Level, acquireLevel = 6,
            description = "이 스킬은 턴을 소모시키지 않습니다. 이 턴 동안 총 두번 행동할 수 있습니다."
        }
    };

    // ─────────────────────────────────────────────────────────────
    // 장비 (무기 6 + 갑옷 5) - 인덱스 = id
    // ─────────────────────────────────────────────────────────────
    public static readonly EquipmentData[] Equipments =
    {
        new EquipmentData { id = 0,  equipName = "롱소드",          slot = EquipSlotType.Weapon, atkBonus = 10, price = 0,   equippedAtStart = true,  description = "공격력 10 증가" },
        new EquipmentData { id = 1,  equipName = "대검",            slot = EquipSlotType.Weapon, atkBonus = 20, price = 150, description = "공격력 20 증가" },
        new EquipmentData { id = 2,  equipName = "단검",            slot = EquipSlotType.Weapon, atkBonus = 5,  critBonus = 10, price = 120, description = "공격력 5 증가, 크리티컬 확률 +10%" },
        new EquipmentData { id = 3,  equipName = "도끼",            slot = EquipSlotType.Weapon, atkBonus = 20, critBonus = 20, price = 300, description = "공격력 20 증가, 크리티컬 확률 +20%" },
        new EquipmentData { id = 4,  equipName = "망치",            slot = EquipSlotType.Weapon, atkBonus = 40, price = 450, description = "공격력 40 증가" },
        new EquipmentData { id = 5,  equipName = "마스터 소드",     slot = EquipSlotType.Weapon, atkBonus = 60, critBonus = 30, price = 900, description = "공격력 +60, 크리티컬 확률 +30%" },

        new EquipmentData { id = 6,  equipName = "천 갑옷",         slot = EquipSlotType.Armor, defBonus = 5,  price = 0,   equippedAtStart = true, description = "방어력 5 증가" },
        new EquipmentData { id = 7,  equipName = "가죽 갑옷",       slot = EquipSlotType.Armor, defBonus = 10, price = 100, description = "방어력 10 증가" },
        new EquipmentData { id = 8,  equipName = "사슬 갑옷",       slot = EquipSlotType.Armor, defBonus = 25, price = 250, description = "방어력 25 증가" },
        new EquipmentData { id = 9,  equipName = "무쇠 갑옷",       slot = EquipSlotType.Armor, defBonus = 35, price = 400, description = "방어력 35 증가" },
        new EquipmentData { id = 10, equipName = "풀 플레이트 아머", slot = EquipSlotType.Armor, defBonus = 50, price = 800, description = "방어력 50 증가" }
    };

    // ─────────────────────────────────────────────────────────────
    // 소모성 아이템 (5종) - 인덱스 = id
    // ─────────────────────────────────────────────────────────────
    public static readonly ConsumableData[] Consumables =
    {
        new ConsumableData
        {
            id = 0, itemName = "빨간 포션", healHpPercent = 0.2f,
            maxStack = 5, price = 50, description = "캐릭터의 HP 20% 회복"
        },
        new ConsumableData
        {
            id = 1, itemName = "파란 포션", healMpPercent = 0.2f,
            maxStack = 5, price = 50, description = "캐릭터의 MP 20% 회복"
        },
        new ConsumableData
        {
            id = 2, itemName = "힘의 영약",
            hasEffect = true, effectType = EffectType.AttackUp, effectValue = 0.3f, effectTurns = 5,
            maxStack = 1, price = 200, description = "5턴간 공격력을 30% 증가"
        },
        new ConsumableData
        {
            id = 3, itemName = "지식의 영약",
            hasEffect = true, effectType = EffectType.SkillDamageUp, effectValue = 0.3f, effectTurns = 5,
            maxStack = 1, price = 200, description = "5턴간 스킬 피해 30% 증가"
        },
        new ConsumableData
        {
            id = 4, itemName = "회피의 물약",
            // 기획서에 지속 턴이 명시되어 있지 않아 '전투 종료까지' 로 구현했다.
            hasEffect = true, effectType = EffectType.EvadeUp, effectValue = 2f,
            effectTurns = StatusEffect.UntilBattleEnd,
            maxStack = 1, price = 200, description = "회피율이 2배 증가한다"
        }
    };

    // ─────────────────────────────────────────────────────────────
    // 몬스터 스킬 - 인덱스 = id
    // ─────────────────────────────────────────────────────────────
    public static readonly MonsterSkillData[] MonsterSkills =
    {
        new MonsterSkillData
        {
            id = 0, skillName = "강타", power = 1.5f, cooldown = 3,
            description = "공격력의 150%의 피해를 입힌다."
        },
        new MonsterSkillData
        {
            id = 1, skillName = "내려찍기", power = 1.8f, cooldown = 8,
            description = "공격력의 180%의 피해를 입힌다."
        },
        new MonsterSkillData
        {
            id = 2, skillName = "혼란의 일격", power = 1.2f, cooldown = 5,
            hasEffect = true, effectType = EffectType.SkillSeal, effectTurns = 1,
            description = "공격력의 120%의 피해를 입히고 1턴간 스킬을 사용할 수 없게 한다."
        },
        new MonsterSkillData
        {
            id = 3, skillName = "혼신의 일격", power = 2.0f, cooldown = 9,
            description = "공격력의 200%의 피해를 입힌다."
        },
        new MonsterSkillData
        {
            id = 4, skillName = "암흑", power = 1.2f, cooldown = 7,
            hasEffect = true, effectType = EffectType.Stun, effectTurns = 1,
            description = "공격력의 120%의 피해를 입히고 1턴간 행동할 수 없게 한다."
        },
        new MonsterSkillData
        {
            id = 5, skillName = "영혼 흡수", power = 1.7f, cooldown = 4,
            lifestealRatio = 0.5f,
            description = "공격력의 170%의 피해를 입히고 가한 피해의 50%를 회복한다."
        },
        new MonsterSkillData
        {
            id = 6, skillName = "파괴 광선", power = 3.0f, cooldown = 10,
            hasEffect = true, effectType = EffectType.Stun, effectTurns = 3,
            telegraph = true,
            description = "공격력의 300%의 피해를 입히고 3턴간 '행동불능' 상태가 됩니다."
        }
    };

    // ─────────────────────────────────────────────────────────────
    // 몬스터 - 인덱스 = id
    // ─────────────────────────────────────────────────────────────
    public static readonly MonsterData[] Monsters =
    {
        new MonsterData
        {
            id = 0, monsterName = "1스테이지 몬스터 1", stageIndex = 0,
            minLevel = 1, maxLevel = 3,
            hpBase = 50, hpPerLevel = 10, atkBase = 8, atkPerLevel = 2,
            defense = 0, speed = 8, expReward = 20
        },
        new MonsterData
        {
            id = 1, monsterName = "1스테이지 몬스터 2", stageIndex = 0,
            minLevel = 1, maxLevel = 3,
            hpBase = 40, hpPerLevel = 10, atkBase = 10, atkPerLevel = 2,
            defense = 0, speed = 11, expReward = 30
        },
        new MonsterData
        {
            id = 2, monsterName = "2스테이지 몬스터 1", stageIndex = 1,
            minLevel = 4, maxLevel = 6,
            hpBase = 60, hpPerLevel = 10, atkBase = 11, atkPerLevel = 2,
            defense = 0, speed = 11, expReward = 50
        },
        new MonsterData
        {
            id = 3, monsterName = "2스테이지 몬스터 2", stageIndex = 1,
            minLevel = 4, maxLevel = 6,
            hpBase = 50, hpPerLevel = 10, atkBase = 14, atkPerLevel = 3,
            defense = 0, speed = 13, expReward = 60
        },
        new MonsterData
        {
            id = 4, monsterName = "3스테이지 몬스터 1", stageIndex = 2,
            minLevel = 7, maxLevel = 9,
            hpBase = 70, hpPerLevel = 10, atkBase = 13, atkPerLevel = 2,
            defense = 25, speed = 13, expReward = 80
        },
        new MonsterData
        {
            id = 5, monsterName = "3스테이지 몬스터 2", stageIndex = 2,
            minLevel = 7, maxLevel = 9,
            hpBase = 60, hpPerLevel = 10, atkBase = 17, atkPerLevel = 2,
            defense = 10, speed = 17, expReward = 90
        },
        new MonsterData
        {
            id = 6, monsterName = "3스테이지 정예 몬스터", stageIndex = 2,
            minLevel = 9, maxLevel = 9,
            hpBase = 100, hpPerLevel = 8, atkBase = 17, atkPerLevel = 2,
            defense = 40, speed = 15, expReward = 120,
            isElite = true, skillIds = new int[] { 0 }
        },
        new MonsterData
        {
            id = 7, monsterName = "1스테이지 보스", stageIndex = 0,
            minLevel = 4, maxLevel = 4,
            hpBase = 250, hpPerLevel = 0, atkBase = 20, atkPerLevel = 0,
            defense = 30, speed = 12, expReward = 150,
            isBoss = true, skillIds = new int[] { 1 }
        },
        new MonsterData
        {
            id = 8, monsterName = "2스테이지 보스", stageIndex = 1,
            minLevel = 7, maxLevel = 7,
            hpBase = 500, hpPerLevel = 0, atkBase = 30, atkPerLevel = 0,
            defense = 45, speed = 14, expReward = 300,
            isBoss = true, skillIds = new int[] { 2, 3 },
            hasPhaseChange = true,
            phaseDebuffsPlayerAtk = true,
            phasePlayerAtkDownValue = 0.3f,
            phasePlayerAtkDownTurns = 4
        },
        new MonsterData
        {
            id = 9, monsterName = "3스테이지 보스", stageIndex = 2,
            minLevel = 11, maxLevel = 11,
            hpBase = 800, hpPerLevel = 0, atkBase = 40, atkPerLevel = 0,
            defense = 60, speed = 18, expReward = 600,
            isBoss = true, skillIds = new int[] { 4, 5, 6 },
            hasPhaseChange = true,
            phaseHealPercent = 0.25f,
            phaseDefDelta = -30,
            phaseAtkDelta = 20
        }
    };

    // ─────────────────────────────────────────────────────────────
    // 배열 길이 (배열 필드 초기화용)
    // ─────────────────────────────────────────────────────────────
    public static readonly int SkillCount = Skills.Length;
    public static readonly int EquipmentCount = Equipments.Length;
    public static readonly int ConsumableCount = Consumables.Length;
    public static readonly int MonsterCount = Monsters.Length;

    // ─────────────────────────────────────────────────────────────
    // 조회 헬퍼
    // ─────────────────────────────────────────────────────────────
    public static SkillData GetSkill(int id)
    {
        return (id >= 0 && id < Skills.Length) ? Skills[id] : null;
    }

    public static EquipmentData GetEquipment(int id)
    {
        return (id >= 0 && id < Equipments.Length) ? Equipments[id] : null;
    }

    public static ConsumableData GetConsumable(int id)
    {
        return (id >= 0 && id < Consumables.Length) ? Consumables[id] : null;
    }

    public static MonsterData GetMonster(int id)
    {
        return (id >= 0 && id < Monsters.Length) ? Monsters[id] : null;
    }

    public static MonsterSkillData GetMonsterSkill(int id)
    {
        return (id >= 0 && id < MonsterSkills.Length) ? MonsterSkills[id] : null;
    }

    /// <summary>해당 스테이지의 보스 몬스터 id. 없으면 -1.</summary>
    public static int GetBossId(int stageIndex)
    {
        for (int i = 0; i < Monsters.Length; i++)
        {
            if (Monsters[i].isBoss && Monsters[i].stageIndex == stageIndex) return i;
        }
        return -1;
    }

    /// <summary>해당 스테이지에서 등장 가능한 일반/정예 몬스터 id 목록.</summary>
    public static int[] GetNormalMonsterIds(int stageIndex)
    {
        int count = 0;
        for (int i = 0; i < Monsters.Length; i++)
        {
            if (!Monsters[i].isBoss && Monsters[i].stageIndex == stageIndex) count++;
        }

        int[] result = new int[count];
        int k = 0;
        for (int i = 0; i < Monsters.Length; i++)
        {
            if (!Monsters[i].isBoss && Monsters[i].stageIndex == stageIndex) result[k++] = i;
        }
        return result;
    }
}
