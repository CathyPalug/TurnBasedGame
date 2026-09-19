using UnityEngine;

// ═════════════════════════════════════════════════════════════════════════════
//  Player.cs  —  플레이어 캐릭터
//
//  Unit 을 물려받으므로 체력, 공격력, 버프 기능은 이미 다 가지고 있다.
//  여기서는 플레이어만 가지는 것(MP, 경험치, 골드, 장비, 배낭, 스킬)을 만든다.
//
//  ★ 배낭 규칙 (기획서 최종수정본 3-4)
//     - 배낭은 4칸이다.
//     - 소모품뿐 아니라 "장비" 도 배낭에 들어간다.
//     - 상점에서 산 물건은 배낭으로 들어가고, 배낭이 꽉 차면 살 수 없다.
//     - 장비를 착용하면 배낭에서 빠진다. 교체하면 벗은 장비가 배낭으로 돌아온다.
//     - 전투 중이 아닐 때만 착용/교체/버리기가 가능하다.
//
//  ★ 배낭은 "배열 3개" 로 관리한다. (클래스를 안 만들어서 더 간단하다)
//       bagKind[칸]  : 0=빈칸, 1=소모품, 2=장비
//       bagNum[칸]   : 아이템 번호 또는 장비 번호
//       bagCount[칸] : 개수 (장비는 항상 1)
//
//  ★ 3D 모델은 다운받은 에셋 "ExplosiveLLC - Warrior Pack Bundle 2 FREE" 의
//     Knight 모델을 쓴다. (에셋 스크립트는 전부 떼고 이 스크립트만 붙인다)
// ═════════════════════════════════════════════════════════════════════════════

public class Player : Unit
{
    [Header("MP (마나)")]
    public int mp;              // 지금 마나
    public int maxMp;           // 최대 마나

    [Header("성장")]
    public int exp;             // 지금 모은 경험치
    public int gold;            // 지금 가진 골드

    [Header("착용 중인 장비 (GameData.Equips 번호. -1 이면 안 낌)")]
    public int weaponNum = -1;  // 무기 칸
    public int armorNum = -1;   // 갑옷 칸

    [Header("스킬")]
    public bool[] hasSkill;     // 배운 스킬인가
    public int[] skillCool;     // 스킬마다 남은 쿨타임

    [Header("배낭 (4칸)")]
    public int[] bagKind;       // 0=빈칸, 1=소모품, 2=장비
    public int[] bagNum;        // 아이템/장비 번호
    public int[] bagCount;      // 개수

    [Header("치트")]
    public int cheatAtk;        // F2 치트로 올린 공격력

    /// <summary>이번 턴에 남은 추가 행동 횟수 (두 개의 심장).</summary>
    [HideInInspector] public int extraTurn;

    private void Awake()
    {
        MakeArrays();
    }

    /// <summary>배열들을 알맞은 크기로 만들어 둔다. (없으면 게임이 죽는다)</summary>
    private void MakeArrays()
    {
        if (hasSkill == null || hasSkill.Length != GameData.Skills.Length)
            hasSkill = new bool[GameData.Skills.Length];

        if (skillCool == null || skillCool.Length != GameData.Skills.Length)
            skillCool = new int[GameData.Skills.Length];

        if (bagKind == null || bagKind.Length != GameData.BAG_SIZE)
            bagKind = new int[GameData.BAG_SIZE];

        if (bagNum == null || bagNum.Length != GameData.BAG_SIZE)
            bagNum = new int[GameData.BAG_SIZE];

        if (bagCount == null || bagCount.Length != GameData.BAG_SIZE)
            bagCount = new int[GameData.BAG_SIZE];
    }


    // ═════════════════════════════════════════════════════════════
    //  새 게임 시작 (기획서 2-1-2)
    // ═════════════════════════════════════════════════════════════

    public void ResetPlayer()
    {
        MakeArrays();

        unitName = "용사";
        level = 1;
        exp = 0;
        gold = 0;

        maxHp = GameData.START_HP;        // 100
        maxMp = GameData.START_MP;        // 50
        hp = maxHp;
        mp = maxMp;

        atk = GameData.START_ATK;         // 20
        def = GameData.START_DEF;         // 10
        speed = GameData.START_SPEED;     // 10
        critRate = GameData.START_CRIT;   // 10%
        evadeRate = GameData.START_EVADE; // 15%

        noDamage = false;
        cheatAtk = 0;
        extraTurn = 0;

        ClearEffects();

        // 배운 스킬과 쿨타임 초기화
        for (int i = 0; i < hasSkill.Length; i++) hasSkill[i] = false;
        for (int i = 0; i < skillCool.Length; i++) skillCool[i] = 0;

        // 배낭 전부 비우기
        for (int i = 0; i < bagKind.Length; i++)
        {
            bagKind[i] = GameData.BAG_EMPTY;
            bagNum[i] = -1;
            bagCount[i] = 0;
        }

        // 시작 장비(롱소드 / 천 갑옷)를 바로 착용한다.
        // 착용한 장비는 배낭 칸을 차지하지 않으므로 배낭은 4칸 다 비어있다.
        weaponNum = -1;
        armorNum = -1;
        for (int i = 0; i < GameData.Equips.Length; i++)
        {
            if (GameData.Equips[i].startEquip == false) continue;

            if (GameData.Equips[i].type == EquipType.Weapon) weaponNum = i;
            else armorNum = i;
        }

        // 1레벨에 자동으로 배우는 스킬 습득
        LearnSkill(1);

        UpdateBonus();
    }


    // ═════════════════════════════════════════════════════════════
    //  장비
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// 착용한 장비를 보고 bonusAtk / bonusDef / bonusCrit 를 다시 계산한다.
    /// ★ 장비를 바꾸거나 치트를 쓸 때 이 함수를 꼭 불러야 능력치가 반영된다!
    /// </summary>
    public void UpdateBonus()
    {
        int 공격 = 0;
        int 방어 = 0;
        int 크리 = 0;

        EquipData 무기 = GameData.GetEquip(weaponNum);
        if (무기 != null)
        {
            공격 = 공격 + 무기.atk;
            방어 = 방어 + 무기.def;
            크리 = 크리 + 무기.crit;
        }

        EquipData 갑옷 = GameData.GetEquip(armorNum);
        if (갑옷 != null)
        {
            공격 = 공격 + 갑옷.atk;
            방어 = 방어 + 갑옷.def;
            크리 = 크리 + 갑옷.crit;
        }

        공격 = 공격 + cheatAtk;   // 치트로 올린 공격력

        // Unit 이 가지고 있는 변수에 결과를 넣는다.
        bonusAtk = 공격;
        bonusDef = 방어;
        bonusCrit = 크리;
    }

    /// <summary>
    /// 배낭의 이 칸에 있는 장비를 착용한다.
    /// 착용한 장비는 배낭에서 빠지고, 원래 끼고 있던 장비는 그 칸으로 들어간다.
    /// </summary>
    public bool EquipFromBag(int slot)
    {
        if (slot < 0 || slot >= bagKind.Length) return false;
        if (bagKind[slot] != GameData.BAG_EQUIP) return false;   // 장비가 아니면 못 낀다

        int 새장비 = bagNum[slot];
        EquipData 데이터 = GameData.GetEquip(새장비);
        if (데이터 == null) return false;

        // 지금 끼고 있던 장비 번호를 기억해 둔다.
        int 벗은장비;
        if (데이터.type == EquipType.Weapon)
        {
            벗은장비 = weaponNum;
            weaponNum = 새장비;
        }
        else
        {
            벗은장비 = armorNum;
            armorNum = 새장비;
        }

        // 새 장비는 배낭에서 빼고, 벗은 장비를 그 자리에 넣는다.
        if (벗은장비 >= 0)
        {
            bagKind[slot] = GameData.BAG_EQUIP;
            bagNum[slot] = 벗은장비;
            bagCount[slot] = 1;
        }
        else
        {
            // 원래 아무것도 안 끼고 있었으면 그 칸은 그냥 빈다.
            ClearSlot(slot);
        }

        UpdateBonus();
        return true;
    }


    // ═════════════════════════════════════════════════════════════
    //  성장 (기획서 2-2)
    // ═════════════════════════════════════════════════════════════

    public bool IsMaxLevel()
    {
        if (level >= GameData.MAX_LEVEL) return true;
        return false;
    }

    /// <summary>다음 레벨까지 필요한 경험치. 최고 레벨이면 0.</summary>
    public int GetNeedExp()
    {
        if (IsMaxLevel()) return 0;

        int 칸 = level - 1;   // 레벨 1이면 표의 0번 칸
        if (칸 < 0 || 칸 >= GameData.NeedExp.Length) return 0;

        return GameData.NeedExp[칸];
    }

    /// <summary>경험치를 얻는다. 충분히 모이면 자동으로 레벨업.</summary>
    public void AddExp(int amount)
    {
        if (amount <= 0) return;
        if (IsMaxLevel()) return;

        exp = exp + amount;

        // 한 번에 여러 레벨이 오를 수 있으니 while 로 반복한다.
        while (true)
        {
            if (IsMaxLevel()) break;

            int 필요 = GetNeedExp();
            if (필요 <= 0) break;
            if (exp < 필요) break;

            exp = exp - 필요;    // 기획서 : 필요 경험치만큼 빼고 레벨업
            LevelUp();
        }

        if (IsMaxLevel()) exp = 0;
    }

    /// <summary>레벨 1 올리기. (기획서 : HP+20 MP+10 공격+10)</summary>
    public void LevelUp()
    {
        if (IsMaxLevel()) return;

        level = level + 1;

        maxHp = maxHp + GameData.LEVELUP_HP;
        maxMp = maxMp + GameData.LEVELUP_MP;
        atk = atk + GameData.LEVELUP_ATK;

        // 늘어난 만큼 지금 체력/마나도 채워준다.
        hp = Mathf.Min(maxHp, hp + GameData.LEVELUP_HP);
        mp = Mathf.Min(maxMp, mp + GameData.LEVELUP_MP);

        LearnSkill(level);
        UpdateBonus();

        BattleUI.Log("레벨 업! Lv." + level + "  (HP+20 MP+10 공격+10)");

        if (AudioManager.instance != null) AudioManager.instance.PlayLevelUp();
    }

    /// <summary>F5 치트 : 강제 레벨업.</summary>
    public void CheatLevelUp()
    {
        if (IsMaxLevel()) return;
        exp = 0;
        LevelUp();
    }

    /// <summary>이 레벨이 되면 자동으로 배우는 스킬 습득. (1, 2, 4, 6 레벨)</summary>
    public void LearnSkill(int nowLevel)
    {
        for (int i = 0; i < GameData.Skills.Length; i++)
        {
            SkillData 스킬 = GameData.Skills[i];

            if (스킬.getType != GetSkillType.Level) continue;   // 상점 스킬 제외
            if (스킬.getLevel != nowLevel) continue;            // 다른 레벨 제외
            if (hasSkill[i]) continue;                          // 이미 배움

            hasSkill[i] = true;
            BattleUI.Log("스킬 습득 : " + 스킬.name);
        }
    }


    // ═════════════════════════════════════════════════════════════
    //  스킬
    // ═════════════════════════════════════════════════════════════

    /// <summary>스킬을 못 쓰는 이유. 쓸 수 있으면 빈 글자("").</summary>
    public string GetSkillBlockReason(int skillNum)
    {
        SkillData 스킬 = GameData.GetSkill(skillNum);

        if (스킬 == null) return "없는 스킬";
        if (hasSkill[skillNum] == false) return "미습득";
        if (IsSkillSealed()) return "스킬 봉인";
        if (mp < 스킬.mpCost) return "MP 부족";
        if (skillCool[skillNum] > 0) return "쿨타임 " + skillCool[skillNum] + "턴";

        return "";
    }

    public bool CanUseSkill(int skillNum)
    {
        if (GetSkillBlockReason(skillNum) == "") return true;
        return false;
    }

    public void UseMp(int amount)
    {
        mp = Mathf.Clamp(mp - amount, 0, maxMp);
    }

    /// <summary>마나 회복. 진짜로 회복된 양을 돌려준다.</summary>
    public int HealMp(int amount)
    {
        int 회복전 = mp;
        mp = Mathf.Clamp(mp + amount, 0, maxMp);
        return mp - 회복전;
    }

    public void StartCool(int skillNum)
    {
        SkillData 스킬 = GameData.GetSkill(skillNum);
        if (스킬 == null) return;
        if (스킬.coolTime <= 0) return;

        skillCool[skillNum] = 스킬.coolTime;
    }

    /// <summary>내 턴이 시작될 때 쿨타임을 1씩 줄인다.</summary>
    public void TickCool()
    {
        for (int i = 0; i < skillCool.Length; i++)
        {
            if (skillCool[i] > 0) skillCool[i] = skillCool[i] - 1;
        }
    }

    public void ResetCool()
    {
        for (int i = 0; i < skillCool.Length; i++) skillCool[i] = 0;
    }


    // ═════════════════════════════════════════════════════════════
    //  배낭 (기획서 최종수정본 3-4)
    // ═════════════════════════════════════════════════════════════

    /// <summary>배낭 한 칸을 완전히 비운다.</summary>
    private void ClearSlot(int slot)
    {
        bagKind[slot] = GameData.BAG_EMPTY;
        bagNum[slot] = -1;
        bagCount[slot] = 0;
    }

    /// <summary>빈 칸의 번호를 찾는다. 없으면 -1.</summary>
    public int FindEmptySlot()
    {
        for (int i = 0; i < bagKind.Length; i++)
        {
            if (bagKind[i] == GameData.BAG_EMPTY) return i;
        }
        return -1;
    }

    /// <summary>배낭에 빈 칸이 있는가.</summary>
    public bool HasEmptySlot()
    {
        if (FindEmptySlot() >= 0) return true;
        return false;
    }

    /// <summary>이 소모품을 몇 개 가지고 있는지 센다.</summary>
    public int CountItem(int itemNum)
    {
        int 합계 = 0;

        for (int i = 0; i < bagKind.Length; i++)
        {
            if (bagKind[i] != GameData.BAG_ITEM) continue;
            if (bagNum[i] != itemNum) continue;

            합계 = 합계 + bagCount[i];
        }

        return 합계;
    }

    /// <summary>
    /// 소모품을 배낭에 넣는다.
    /// 기획서 : 최대 누적 개수를 넘었거나 배낭이 꽉 찼으면 획득하지 못한다.
    /// </summary>
    public bool AddItemToBag(int itemNum)
    {
        ItemData 아이템 = GameData.GetItem(itemNum);
        if (아이템 == null) return false;

        // 이미 최대 개수만큼 가지고 있으면 더 못 받는다.
        if (CountItem(itemNum) >= 아이템.maxCount) return false;

        // 1) 같은 아이템이 있는 칸에 하나 더 쌓는다.
        for (int i = 0; i < bagKind.Length; i++)
        {
            if (bagKind[i] != GameData.BAG_ITEM) continue;
            if (bagNum[i] != itemNum) continue;

            bagCount[i] = bagCount[i] + 1;
            return true;
        }

        // 2) 없으면 빈 칸에 새로 넣는다.
        int 빈칸 = FindEmptySlot();
        if (빈칸 < 0) return false;   // 배낭이 꽉 찼다

        bagKind[빈칸] = GameData.BAG_ITEM;
        bagNum[빈칸] = itemNum;
        bagCount[빈칸] = 1;
        return true;
    }

    /// <summary>장비를 배낭에 넣는다. 배낭이 꽉 차 있으면 실패한다.</summary>
    public bool AddEquipToBag(int equipNum)
    {
        if (GameData.GetEquip(equipNum) == null) return false;

        // 장비는 쌓이지 않으므로 무조건 빈 칸이 필요하다.
        int 빈칸 = FindEmptySlot();
        if (빈칸 < 0) return false;

        bagKind[빈칸] = GameData.BAG_EQUIP;
        bagNum[빈칸] = equipNum;
        bagCount[빈칸] = 1;
        return true;
    }

    /// <summary>배낭 한 칸을 사용한다. (소모품만 사용 가능)</summary>
    public bool UseItem(int slot)
    {
        if (slot < 0 || slot >= bagKind.Length) return false;
        if (bagKind[slot] != GameData.BAG_ITEM) return false;

        ItemData 아이템 = GameData.GetItem(bagNum[slot]);
        if (아이템 == null) return false;

        string 기록 = 아이템.name + " 사용";

        // HP 회복
        if (아이템.healHp > 0f)
        {
            int 회복 = HealHp(Mathf.RoundToInt(maxHp * 아이템.healHp));
            기록 = 기록 + " → HP +" + 회복;
            if (DamagePopup.instance != null) DamagePopup.instance.ShowHeal(this, 회복);
        }

        // MP 회복
        if (아이템.healMp > 0f)
        {
            int 회복 = HealMp(Mathf.RoundToInt(maxMp * 아이템.healMp));
            기록 = 기록 + " → MP +" + 회복;
        }

        // 버프
        if (아이템.hasEffect)
        {
            AddEffect(아이템.effect, 아이템.effectValue, 아이템.effectTurn);
            기록 = 기록 + " → " + 아이템.info;
        }

        // 하나 소모
        bagCount[slot] = bagCount[slot] - 1;
        if (bagCount[slot] <= 0) ClearSlot(slot);

        BattleUI.Log(기록);
        if (AudioManager.instance != null) AudioManager.instance.PlayHeal();

        return true;
    }

    /// <summary>배낭 한 칸을 버린다. (전투 중이 아닐 때만 UI 에서 부른다)</summary>
    public bool DropSlot(int slot)
    {
        if (slot < 0 || slot >= bagKind.Length) return false;
        if (bagKind[slot] == GameData.BAG_EMPTY) return false;

        // 여러 개 쌓여 있으면 하나만 버린다.
        bagCount[slot] = bagCount[slot] - 1;
        if (bagCount[slot] <= 0) ClearSlot(slot);

        return true;
    }

    /// <summary>배낭 한 칸을 화면에 보여줄 글자로 만든다. 빈 칸이면 "- 빈 칸 -".</summary>
    public string GetSlotText(int slot)
    {
        if (slot < 0 || slot >= bagKind.Length) return "- 빈 칸 -";

        if (bagKind[slot] == GameData.BAG_ITEM)
        {
            ItemData 아이템 = GameData.GetItem(bagNum[slot]);
            if (아이템 == null) return "- 빈 칸 -";
            return 아이템.name + " x" + bagCount[slot];
        }

        if (bagKind[slot] == GameData.BAG_EQUIP)
        {
            EquipData 장비 = GameData.GetEquip(bagNum[slot]);
            if (장비 == null) return "- 빈 칸 -";

            string 종류 = "갑옷";
            if (장비.type == EquipType.Weapon) 종류 = "무기";

            return "[" + 종류 + "] " + 장비.name;
        }

        return "- 빈 칸 -";
    }


    // ═════════════════════════════════════════════════════════════
    //  기타
    // ═════════════════════════════════════════════════════════════

    /// <summary>상점에서 휴식. 최대 체력의 30% 회복. (기획서 4-2)</summary>
    public int Rest()
    {
        return HealHp(Mathf.RoundToInt(maxHp * GameData.REST_HEAL));
    }

    /// <summary>F3 치트 : 체력 최대 회복.</summary>
    public void FullHp() { hp = maxHp; }

    /// <summary>F4 치트 : 마나 최대 회복.</summary>
    public void FullMp() { mp = maxMp; }
}
