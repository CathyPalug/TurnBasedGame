using UnityEngine;

/// <summary>
/// 플레이어 유닛. 능력치 / 성장 / 장비 / 배낭 / 스킬 보유를 모두 담당한다.
/// 장비와 스킬은 GameData 배열의 인덱스(id)로 관리한다.
/// </summary>
public class Player : Entry
{
    [Header("MP")]
    public int mp;
    public int maxMp;

    [Header("성장")]
    public int exp;
    public int gold;

    [Header("기본 확률 (%)")]
    public int baseCritChance = GameData.BaseCritChance;
    public int baseEvadeChance = GameData.BaseEvadeChance;

    [Header("장비 (GameData.Equipments 인덱스, -1 = 없음)")]
    [Tooltip("[0] = 무기, [1] = 갑옷")]
    public int[] equipSlots = new int[2] { -1, -1 };

    [Tooltip("구매해서 보유 중인 장비. 인덱스 = 장비 id")]
    public bool[] ownedEquipments = new bool[GameData.EquipmentCount];

    [Header("스킬 (GameData.Skills 인덱스)")]
    public bool[] skillOwned = new bool[GameData.SkillCount];
    public int[] skillCooldowns = new int[GameData.SkillCount];

    [Header("배낭 (6칸)")]
    public InventorySlot[] inventory = new InventorySlot[GameData.InventorySize];

    [Header("치트")]
    public bool invincible;             // F1
    public int cheatAtkBonus;           // F2

    /// <summary>이번 턴에 추가 행동이 남아있는가 (두 개의 심장).</summary>
    [HideInInspector] public int extraActions;

    private void Awake()
    {
        EnsureArrays();
    }

    // ─────────────────────────────────────────────────────────────
    // 능력치
    // ─────────────────────────────────────────────────────────────
    public int EquipAtkBonus
    {
        get
        {
            EquipmentData w = GameData.GetEquipment(equipSlots[0]);
            EquipmentData a = GameData.GetEquipment(equipSlots[1]);
            return (w != null ? w.atkBonus : 0) + (a != null ? a.atkBonus : 0);
        }
    }

    public int EquipDefBonus
    {
        get
        {
            EquipmentData w = GameData.GetEquipment(equipSlots[0]);
            EquipmentData a = GameData.GetEquipment(equipSlots[1]);
            return (w != null ? w.defBonus : 0) + (a != null ? a.defBonus : 0);
        }
    }

    public int EquipCritBonus
    {
        get
        {
            EquipmentData w = GameData.GetEquipment(equipSlots[0]);
            EquipmentData a = GameData.GetEquipment(equipSlots[1]);
            return (w != null ? w.critBonus : 0) + (a != null ? a.critBonus : 0);
        }
    }

    public override int Atk
    {
        get
        {
            float value = baseAtk + EquipAtkBonus + cheatAtkBonus;
            value *= 1f + effects.GetValue(EffectType.AttackUp);
            value *= 1f - effects.GetValue(EffectType.AttackDown);
            return Mathf.Max(0, Mathf.RoundToInt(value));
        }
    }

    public override int Def
    {
        get
        {
            float value = baseDef + EquipDefBonus;
            value *= 1f + effects.GetValue(EffectType.DefenseUp);
            value *= 1f - effects.GetValue(EffectType.DefenseDown);
            return Mathf.Max(0, Mathf.RoundToInt(value));
        }
    }

    public override int CritChance
    {
        get
        {
            float value = baseCritChance + EquipCritBonus + effects.GetValue(EffectType.CritRateUp);
            return Mathf.Clamp(Mathf.RoundToInt(value), 0, 100);
        }
    }

    public override int EvadeChance
    {
        get
        {
            float multiplier = effects.Has(EffectType.EvadeUp) ? effects.GetValue(EffectType.EvadeUp) : 1f;
            return Mathf.Clamp(Mathf.RoundToInt(baseEvadeChance * multiplier), 0, 100);
        }
    }

    /// <summary>다음 레벨까지 필요한 경험치. 최대 레벨이면 0.</summary>
    public int RequiredExp
    {
        get
        {
            if (level >= GameData.MaxLevel) return 0;
            int index = level - 1;
            if (index < 0 || index >= GameData.RequiredExp.Length) return 0;
            return GameData.RequiredExp[index];
        }
    }

    public bool IsMaxLevel { get { return level >= GameData.MaxLevel; } }

    public float MpRatio { get { return maxMp <= 0 ? 0f : (float)mp / maxMp; } }

    public float ExpRatio
    {
        get
        {
            int req = RequiredExp;
            return req <= 0 ? 1f : Mathf.Clamp01((float)exp / req);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 새 게임 초기화
    // ─────────────────────────────────────────────────────────────
    public void ResetToNewGame()
    {
        EnsureArrays();

        entryName = "플레이어";
        level = 1;
        exp = 0;
        gold = 0;

        maxHp = GameData.BaseMaxHp;
        maxMp = GameData.BaseMaxMp;
        hp = maxHp;
        mp = maxMp;

        SetBaseStats(GameData.BaseAtk, GameData.BaseDef, GameData.BaseSpeed);
        baseCritChance = GameData.BaseCritChance;
        baseEvadeChance = GameData.BaseEvadeChance;

        invincible = false;
        cheatAtkBonus = 0;
        extraActions = 0;

        effects.ClearAll();

        for (int i = 0; i < skillOwned.Length; i++) skillOwned[i] = false;
        for (int i = 0; i < skillCooldowns.Length; i++) skillCooldowns[i] = 0;
        for (int i = 0; i < ownedEquipments.Length; i++) ownedEquipments[i] = false;
        for (int i = 0; i < inventory.Length; i++) inventory[i].Clear();

        // 시작 장비 자동 장착 (롱소드 / 천 갑옷)
        equipSlots[0] = -1;
        equipSlots[1] = -1;
        for (int i = 0; i < GameData.Equipments.Length; i++)
        {
            if (!GameData.Equipments[i].equippedAtStart) continue;
            ownedEquipments[i] = true;
            Equip(i);
        }

        // 1레벨 자동 습득 스킬
        LearnSkillsForLevel(1);
    }

    private void EnsureArrays()
    {
        if (equipSlots == null || equipSlots.Length != 2) equipSlots = new int[2] { -1, -1 };
        if (ownedEquipments == null || ownedEquipments.Length != GameData.EquipmentCount)
            ownedEquipments = new bool[GameData.EquipmentCount];
        if (skillOwned == null || skillOwned.Length != GameData.SkillCount)
            skillOwned = new bool[GameData.SkillCount];
        if (skillCooldowns == null || skillCooldowns.Length != GameData.SkillCount)
            skillCooldowns = new int[GameData.SkillCount];

        if (inventory == null || inventory.Length != GameData.InventorySize)
            inventory = new InventorySlot[GameData.InventorySize];
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i] == null) inventory[i] = new InventorySlot();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 성장
    // ─────────────────────────────────────────────────────────────
    /// <summary>몬스터 처치 보상. 경험치 = 몬스터 경험치 * 몬스터 레벨, 골드 = 그 값의 50%.</summary>
    public void GainKillReward(MonsterData data, int monsterLevel, out int gainedExp, out int gainedGold)
    {
        gainedExp = data.expReward * monsterLevel;
        gainedGold = gainedExp / 2;

        GainExp(gainedExp);
        gold += gainedGold;
    }

    public void GainExp(int amount)
    {
        if (amount <= 0) return;
        if (IsMaxLevel) return;

        exp += amount;

        while (!IsMaxLevel && exp >= RequiredExp && RequiredExp > 0)
        {
            exp -= RequiredExp;
            LevelUp();
        }

        if (IsMaxLevel) exp = 0;
    }

    public void LevelUp()
    {
        if (IsMaxLevel) return;

        level++;

        maxHp += GameData.LevelUpHp;
        maxMp += GameData.LevelUpMp;
        baseAtk += GameData.LevelUpAtk;

        hp = Mathf.Min(maxHp, hp + GameData.LevelUpHp);
        mp = Mathf.Min(maxMp, mp + GameData.LevelUpMp);

        LearnSkillsForLevel(level);

        if (BattleLog.Instance != null)
            BattleLog.Instance.AddBattleLog(string.Format("레벨 업! Lv.{0} (HP+{1} MP+{2} 공격력+{3})",
                level, GameData.LevelUpHp, GameData.LevelUpMp, GameData.LevelUpAtk));
    }

    /// <summary>치트 F5 : 경험치와 무관하게 강제 레벨업.</summary>
    public void ForceLevelUp()
    {
        if (IsMaxLevel) return;
        exp = 0;
        LevelUp();
    }

    /// <summary>해당 레벨에서 자동 획득하는 스킬을 배운다. (1, 2, 4, 6 레벨)</summary>
    public void LearnSkillsForLevel(int targetLevel)
    {
        for (int i = 0; i < GameData.Skills.Length; i++)
        {
            SkillData s = GameData.Skills[i];
            if (s.acquireType != SkillAcquireType.Level) continue;
            if (s.acquireLevel != targetLevel) continue;
            if (skillOwned[i]) continue;

            skillOwned[i] = true;
            if (BattleLog.Instance != null)
                BattleLog.Instance.AddBattleLog(string.Format("스킬 습득 : {0}", s.skillName));
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 스킬
    // ─────────────────────────────────────────────────────────────
    public bool CanUseSkill(int skillId)
    {
        SkillData s = GameData.GetSkill(skillId);
        if (s == null) return false;
        if (!skillOwned[skillId]) return false;
        if (IsSkillSealed) return false;
        if (mp < s.mpCost) return false;
        if (skillCooldowns[skillId] > 0) return false;
        return true;
    }

    /// <summary>사용할 수 없는 이유. 사용 가능하면 빈 문자열.</summary>
    public string GetSkillBlockReason(int skillId)
    {
        SkillData s = GameData.GetSkill(skillId);
        if (s == null) return "없는 스킬";
        if (!skillOwned[skillId]) return "미습득";
        if (IsSkillSealed) return "스킬 봉인";
        if (mp < s.mpCost) return "MP 부족";
        if (skillCooldowns[skillId] > 0) return string.Format("쿨타임 {0}턴", skillCooldowns[skillId]);
        return string.Empty;
    }

    public void ConsumeMp(int amount)
    {
        mp = Mathf.Clamp(mp - amount, 0, maxMp);
    }

    public int RecoverMp(int amount)
    {
        int before = mp;
        mp = Mathf.Clamp(mp + amount, 0, maxMp);
        return mp - before;
    }

    public void StartSkillCooldown(int skillId)
    {
        SkillData s = GameData.GetSkill(skillId);
        if (s == null || s.cooldown <= 0) return;
        skillCooldowns[skillId] = s.cooldown;
    }

    public void TickSkillCooldowns()
    {
        for (int i = 0; i < skillCooldowns.Length; i++)
        {
            if (skillCooldowns[i] > 0) skillCooldowns[i]--;
        }
    }

    public void ResetSkillCooldowns()
    {
        for (int i = 0; i < skillCooldowns.Length; i++) skillCooldowns[i] = 0;
    }

    // ─────────────────────────────────────────────────────────────
    // 장비
    // ─────────────────────────────────────────────────────────────
    public void Equip(int equipmentId)
    {
        EquipmentData e = GameData.GetEquipment(equipmentId);
        if (e == null) return;
        if (!ownedEquipments[equipmentId]) return;

        int slotIndex = (e.slot == EquipSlotType.Weapon) ? 0 : 1;
        equipSlots[slotIndex] = equipmentId;
    }

    public bool IsEquipped(int equipmentId)
    {
        return equipSlots[0] == equipmentId || equipSlots[1] == equipmentId;
    }

    public EquipmentData GetEquipped(EquipSlotType slot)
    {
        return GameData.GetEquipment(equipSlots[slot == EquipSlotType.Weapon ? 0 : 1]);
    }

    // ─────────────────────────────────────────────────────────────
    // 배낭
    // ─────────────────────────────────────────────────────────────
    public int CountItem(int itemId)
    {
        int total = 0;
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i].itemId == itemId) total += inventory[i].count;
        }
        return total;
    }

    public bool HasEmptySlot()
    {
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i].IsEmpty) return true;
        }
        return false;
    }

    /// <summary>
    /// 아이템을 배낭에 넣는다.
    /// 배낭이 가득 찼거나 최대 누적 개수를 넘으면 획득하지 못하고 false 를 돌려준다.
    /// </summary>
    public bool AddItem(int itemId)
    {
        ConsumableData c = GameData.GetConsumable(itemId);
        if (c == null) return false;

        if (CountItem(itemId) >= c.maxStack) return false;

        // 이미 같은 아이템이 있는 칸에 누적
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i].itemId == itemId)
            {
                inventory[i].count++;
                return true;
            }
        }

        // 빈 칸에 새로 추가
        for (int i = 0; i < inventory.Length; i++)
        {
            if (!inventory[i].IsEmpty) continue;
            inventory[i].itemId = itemId;
            inventory[i].count = 1;
            return true;
        }

        return false;   // 배낭 가득 참
    }

    /// <summary>배낭 칸의 아이템을 사용한다.</summary>
    public bool UseItemSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventory.Length) return false;
        if (inventory[slotIndex].IsEmpty) return false;

        ConsumableData c = GameData.GetConsumable(inventory[slotIndex].itemId);
        if (c == null) return false;

        string log = string.Format("{0} 사용", c.itemName);

        if (c.healHpPercent > 0f)
        {
            int healed = Heal(Mathf.RoundToInt(maxHp * c.healHpPercent));
            BattleEffects.Heal(this, healed);
            log += string.Format(" - HP {0} 회복", healed);
        }

        if (c.healMpPercent > 0f)
        {
            int recovered = RecoverMp(Mathf.RoundToInt(maxMp * c.healMpPercent));
            BattleEffects.Heal(this, recovered);
            log += string.Format(" - MP {0} 회복", recovered);
        }

        if (c.hasEffect)
        {
            AddEffect(c.effectType, c.effectValue, c.effectTurns, c.itemName);
            log += string.Format(" - {0}", c.description);
        }

        inventory[slotIndex].count--;
        if (inventory[slotIndex].count <= 0) inventory[slotIndex].Clear();

        if (BattleLog.Instance != null) BattleLog.Instance.AddBattleLog(log);
        return true;
    }

    // ─────────────────────────────────────────────────────────────
    // 피해 / 회복
    // ─────────────────────────────────────────────────────────────
    public override int ApplyDamage(int amount)
    {
        if (invincible) return 0;   // 치트 F1
        return base.ApplyDamage(amount);
    }

    /// <summary>휴식 : 최대 체력의 30% 회복.</summary>
    public int Rest()
    {
        return Heal(Mathf.RoundToInt(maxHp * GameData.RestHealPercent));
    }

    public void FullHeal()
    {
        hp = maxHp;
    }

    public void FullMp()
    {
        mp = maxMp;
    }
}
