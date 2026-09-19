using System;

// ═════════════════════════════════════════════════════════════════════════════
//  GameData.cs  —  게임의 "숫자표" 를 전부 모아둔 파일
//
//  ★ 이 파일 하나만 보면 게임의 모든 수치를 알 수 있다.
//    밸런스를 바꾸고 싶으면 다른 파일 말고 여기 숫자만 고치면 된다.
//
//  ★ 모든 숫자는 2026 전국기능경기대회 기획서에 적힌 값 그대로다.
//
//  ── 이 파일에 들어있는 것 ──
//    1) enum      : 종류를 이름으로 구분하는 목록
//    2) 데이터 클래스 : 스킬/장비/아이템/몬스터 한 개의 설명서 모양
//    3) GameData  : 실제 표 (배열)
// ═════════════════════════════════════════════════════════════════════════════


// ─────────────────────────────────────────────────────────────────────────────
//  1) enum : 숫자 대신 이름으로 종류를 구분하게 해주는 문법
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>스킬이 누구를 때리는지.</summary>
public enum TargetType
{
    Single,     // 고른 적 1명
    Adjacent,   // 고른 적 + 양 옆
    All,        // 살아있는 적 전부
    Self        // 나 자신 (버프, 회복)
}

/// <summary>스킬을 얻는 방법.</summary>
public enum GetSkillType
{
    Level,      // 레벨이 되면 자동으로 배움
    Shop        // 상점에서 스킬북 구매
}

/// <summary>스킬의 "이럴 때는 더 세게" 조건.</summary>
public enum SkillBonusType
{
    None,           // 조건 없음
    EnemyDefending, // 급습      : 적이 방어 중일 때
    EnemyLowHp,     // 최후의 일격 : 적 체력 30% 이하일 때
    MyLostHp        // 기사회생   : 내가 잃은 체력이 많을수록
}

/// <summary>장비를 끼는 칸.</summary>
public enum EquipType
{
    Weapon,     // 무기
    Armor       // 갑옷
}

/// <summary>
/// 버프 / 디버프 / 상태이상 종류.
/// ★ 이 순서가 그대로 Unit 의 배열 번호가 된다. 순서를 바꾸면 안 된다!
/// ★ Count 는 "총 몇 종류인지" 를 세기 위해 맨 끝에 넣어둔 것이다.
/// </summary>
public enum EffectType
{
    CritUp,     // 0 : 크리티컬 확률 증가   (노려보기)
    DefUp,      // 1 : 방어력 증가          (가드, 공방일체)
    DefDown,    // 2 : 방어력 감소          (약점 격파)
    AtkUp,      // 3 : 공격력 증가          (힘의 영약)
    AtkDown,    // 4 : 공격력 감소          (2보스 페이즈)
    SkillUp,    // 5 : 스킬 피해 증가       (지식의 영약)
    EvadeUp,    // 6 : 회피율 증가          (회피의 물약)
    SkillSeal,  // 7 : 스킬 봉인            (혼란의 일격)
    Stun,       // 8 : 행동불능             (암흑, 파괴 광선)
    Defend,     // 9 : 몬스터 '방어' 중
    Count       // 10 : ★종류의 개수. 실제 효과가 아니다!
}

/// <summary>지금 어느 화면인지.</summary>
public enum GameState
{
    Menu,       // 시작 메뉴
    Map,        // 지도
    Battle,     // 전투
    Shop,       // 상점 / 휴식
    GameOver,   // 죽음
    GameClear   // 전부 클리어
}

/// <summary>지도 위 방의 종류.</summary>
public enum RoomType
{
    Battle,     // 전투 방
    Shop,       // 상점 및 휴식 방
    Boss        // 보스 방
}

/// <summary>지도 위 방의 진행 상태.</summary>
public enum RoomState
{
    Locked,     // 잠김 (길이 안 이어져서 못 감)
    Open,       // 갈 수 있음
    Now,        // 지금 여기 있음
    Cleared     // 이미 깼음
}

/// <summary>몬스터가 이번 턴에 할 행동.</summary>
public enum MonsterAction
{
    Attack,     // 공격
    Defend,     // 방어
    Skill       // 스킬
}


// ─────────────────────────────────────────────────────────────────────────────
//  2) 데이터 클래스 : "설명서 한 장" 의 모양
//     [Serializable] 은 유니티 화면(Inspector)에서 볼 수 있게 해주는 표시다.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>플레이어 스킬 1개의 설명서.</summary>
[Serializable]
public class SkillData
{
    public string name;             // 스킬 이름
    public int mpCost;              // 쓸 때 드는 MP
    public int coolTime;            // 쿨타임(턴). 0 이면 쿨타임 없음
    public TargetType target;       // 누구를 때리는지
    public float power;             // 공격력 배율. 1.7f = 공격력의 170%

    public SkillBonusType bonusType;// 특별 조건 종류
    public float bonusPower;        // 조건을 만족했을 때 쓰는 배율
    public float minPower;          // 기사회생 : 가장 약할 때 배율
    public float maxPower;          // 기사회생 : 가장 셀 때 배율
    public bool breakDefend;        // 급습 : 적의 방어를 부수는가

    public float healPercent;       // 명상 : 0.3f = 최대 체력 30% 회복

    public EffectType effect;       // 걸어주는 효과 종류
    public bool hasEffect;          // 효과를 거는 스킬인가
    public float effectValue;       // 효과 크기
    public int effectTurn;          // 효과 지속 턴
    public bool effectToMe;         // true = 나에게, false = 적에게

    public bool extraAction;        // 두 개의 심장 : 한 번 더 행동
    public bool freeAction;         // 두 개의 심장 : 턴을 안 쓴다

    public GetSkillType getType;    // 얻는 방법
    public int getLevel;            // 레벨로 배울 때 필요한 레벨
    public int price;               // 상점에서 살 때 가격

    public string info;             // 화면에 보여줄 설명글

    /// <summary>이 스킬이 적에게 피해를 주는 스킬인가.</summary>
    public bool IsAttackSkill()
    {
        if (power > 0f) return true;
        if (maxPower > 0f) return true;     // 기사회생처럼 배율이 변하는 스킬
        return false;
    }
}

/// <summary>장비 1개의 설명서.</summary>
[Serializable]
public class EquipData
{
    public string name;         // 장비 이름
    public EquipType type;      // 무기인지 갑옷인지
    public int atk;             // 올려주는 공격력
    public int def;             // 올려주는 방어력
    public int crit;            // 올려주는 크리티컬 확률(%)
    public int price;           // 가격. 0 이면 상점에서 안 판다
    public bool startEquip;     // 게임 시작할 때 끼고 있는가
    public string info;         // 설명글
}

/// <summary>소모성 아이템 1개의 설명서.</summary>
[Serializable]
public class ItemData
{
    public string name;         // 아이템 이름
    public float healHp;        // 0.2f = 최대 HP 의 20% 회복
    public float healMp;        // 0.2f = 최대 MP 의 20% 회복

    public bool hasEffect;      // 버프를 걸어주는가
    public EffectType effect;   // 어떤 버프인가
    public float effectValue;   // 버프 크기
    public int effectTurn;      // 버프 지속 턴

    public int maxCount;        // 최대 몇 개까지 가질 수 있나
    public int price;           // 가격
    public string info;         // 설명글
}

/// <summary>몬스터가 쓰는 스킬 1개의 설명서.</summary>
[Serializable]
public class MonsterSkillData
{
    public string name;         // 스킬 이름
    public float power;         // 공격력 배율
    public int coolTime;        // 쿨타임(턴)

    public bool hasEffect;      // 나쁜 효과를 거는가
    public EffectType effect;   // 어떤 효과인가
    public int effectTurn;      // 몇 턴 동안인가

    public float drainRate;     // 영혼 흡수 : 0.5f = 준 피해의 50% 회복
    public bool showWarning;    // 파괴 광선 : 미리 예고해야 하는가
}

/// <summary>몬스터 종류 1개의 설명서.</summary>
[Serializable]
public class MonsterData
{
    public string name;         // 몬스터 이름
    public int stage;           // 어느 스테이지에 나오나 (0 = 1스테이지)
    public int minLevel;        // 나올 수 있는 최소 레벨
    public int maxLevel;        // 나올 수 있는 최대 레벨

    public int hpBase;          // 체력 기본값
    public int hpPerLevel;      // 레벨 1당 늘어나는 체력
    public int atkBase;         // 공격력 기본값
    public int atkPerLevel;     // 레벨 1당 늘어나는 공격력
    public int def;             // 방어력
    public int speed;           // 속도 (턴 순서)
    public int exp;             // 잡으면 주는 경험치 기준값

    public bool isElite;        // 정예 몬스터인가
    public bool isBoss;         // 보스인가

    public int[] skills;        // 이 몬스터가 쓰는 스킬 번호 목록

    // ── 보스 전용 : 체력 50% 이하가 되면 한 번 강해진다 ──
    public bool hasPhase;           // 페이즈 변화를 하는가
    public float phaseHeal;         // 3보스 : 0.25f = 체력 25% 회복
    public int phaseDefAdd;         // 3보스 : 방어력 -30
    public int phaseAtkAdd;         // 3보스 : 공격력 +20
    public bool phaseCursePlayer;   // 2보스 : 플레이어 공격력을 깎는가
    public float phaseCurseValue;   // 0.3f = 30% 감소
    public int phaseCurseTurn;      // 4턴 동안

    /// <summary>이 레벨일 때의 최대 체력. 공식 = 기본값 + (레벨 x 레벨당증가)</summary>
    public int GetMaxHp(int level)
    {
        // 예) 고블린 레벨 3 이면 : 50 + (3 x 10) = 80
        return hpBase + (hpPerLevel * level);
    }

    /// <summary>이 레벨일 때의 공격력. 공식 = 기본값 + (레벨 x 레벨당증가)</summary>
    public int GetAtk(int level)
    {
        // 예) 고블린 레벨 3 이면 : 8 + (3 x 2) = 14
        return atkBase + (atkPerLevel * level);
    }
}


// ─────────────────────────────────────────────────────────────────────────────
//  3) GameData : 실제 표
//     static 이라서 오브젝트를 만들지 않고 GameData.Skills 처럼 바로 쓴다.
// ─────────────────────────────────────────────────────────────────────────────

public static class GameData
{
    // ══ 플레이어 시작 능력치 (기획서 2-1-2) ══
    public const int START_HP = 100;
    public const int START_MP = 50;
    public const int START_DEF = 10;
    public const int START_ATK = 20;
    public const int START_SPEED = 10;
    public const int START_CRIT = 10;    // 크리티컬 확률 10%
    public const int START_EVADE = 15;   // 회피율 15%
    public const int BAG_SIZE = 4;       // 배낭 4칸 ★최종수정본 기준 (구버전은 6칸이었다)
    public const int MAX_LEVEL = 10;     // 최대 레벨 10

    // ══ 배낭 한 칸이 무엇을 담고 있는지 나타내는 값 ══
    // 기획서 최종본 : 소모품뿐 아니라 장비도 배낭에 보관한다.
    public const int BAG_EMPTY = 0;      // 빈 칸
    public const int BAG_ITEM = 1;       // 소모성 아이템이 들어있음
    public const int BAG_EQUIP = 2;      // 장비가 들어있음

    // ══ 레벨업 시 증가량 (기획서 2-2-2) ══
    public const int LEVELUP_HP = 20;
    public const int LEVELUP_MP = 10;
    public const int LEVELUP_ATK = 10;

    // ══ 전투 공식에 쓰는 값 ══
    public const float CRIT_DAMAGE = 2f;      // 크리티컬은 2배
    public const float MIN_DAMAGE_RATE = 0.1f;// 방어력으로 아무리 깎아도 원래 위력의 10%는 들어간다
    public const float REST_HEAL = 0.3f;      // 휴식하면 체력 30% 회복
    public const int FOREVER = 999;           // "전투 끝날 때까지" 를 뜻하는 턴 수

    /// <summary>레벨업에 필요한 경험치. 0번 칸이 1→2레벨, 1번 칸이 2→3레벨 …</summary>
    public static readonly int[] NeedExp =
    {
        50, 90, 140, 200, 280, 380, 500, 650, 850
    };

    // ═════════════════════════════════════════════════════════════
    //  스킬 13종 (기획서 2-3-1)  —  배열 순서 = 스킬 번호
    // ═════════════════════════════════════════════════════════════
    public static readonly SkillData[] Skills =
    {
        // 0
        new SkillData {
            name = "베기", mpCost = 30, coolTime = 0,
            target = TargetType.Single, power = 1.7f,
            getType = GetSkillType.Level, getLevel = 1,
            info = "적 1명에게 공격력의 170% 피해"
        },
        // 1
        new SkillData {
            name = "가르기", mpCost = 35, coolTime = 0,
            target = TargetType.Adjacent, power = 1.4f,
            getType = GetSkillType.Level, getLevel = 1,
            info = "적과 양 옆에게 공격력의 140% 피해"
        },
        // 2
        new SkillData {
            name = "노려보기", mpCost = 25, coolTime = 0,
            target = TargetType.Self,
            hasEffect = true, effect = EffectType.CritUp,
            effectValue = 25f, effectTurn = 3, effectToMe = true,
            getType = GetSkillType.Level, getLevel = 2,
            info = "3턴간 크리티컬 확률 25% 증가"
        },
        // 3
        new SkillData {
            name = "명상", mpCost = 45, coolTime = 5,
            target = TargetType.Self, healPercent = 0.3f,
            getType = GetSkillType.Level, getLevel = 2,
            info = "최대 체력의 30% 회복"
        },
        // 4
        new SkillData {
            name = "필살기", mpCost = 100, coolTime = 10,
            target = TargetType.All, power = 3.0f,
            getType = GetSkillType.Level, getLevel = 4,
            info = "모든 적에게 공격력의 300% 피해"
        },
        // 5
        new SkillData {
            name = "가드", mpCost = 30, coolTime = 0,
            target = TargetType.Self,
            hasEffect = true, effect = EffectType.DefUp,
            effectValue = 0.3f, effectTurn = 2, effectToMe = true,
            getType = GetSkillType.Shop, price = 200,
            info = "2턴간 방어력 30% 증가"
        },
        // 6
        new SkillData {
            name = "기사회생", mpCost = 45, coolTime = 0,
            target = TargetType.Single,
            bonusType = SkillBonusType.MyLostHp, minPower = 1.5f, maxPower = 2.5f,
            getType = GetSkillType.Shop, price = 300,
            info = "잃은 체력에 비례해 150%~250% 피해"
        },
        // 7
        new SkillData {
            name = "약점 격파", mpCost = 55, coolTime = 2,
            target = TargetType.Single, power = 1.3f,
            hasEffect = true, effect = EffectType.DefDown,
            effectValue = 0.3f, effectTurn = 3, effectToMe = false,
            getType = GetSkillType.Shop, price = 300,
            info = "130% 피해 + 적 방어력 3턴간 30% 감소"
        },
        // 8
        new SkillData {
            name = "화염구", mpCost = 65, coolTime = 0,
            target = TargetType.Adjacent, power = 1.8f,
            getType = GetSkillType.Shop, price = 350,
            info = "적과 양 옆에게 공격력의 180% 피해"
        },
        // 9
        new SkillData {
            name = "급습", mpCost = 60, coolTime = 2,
            target = TargetType.Single, power = 1.5f,
            bonusType = SkillBonusType.EnemyDefending, bonusPower = 2.5f, breakDefend = true,
            getType = GetSkillType.Shop, price = 350,
            info = "150% 피해. 적이 방어 중이면 방어를 깨고 250% 피해"
        },
        // 10
        new SkillData {
            name = "최후의 일격", mpCost = 70, coolTime = 1,
            target = TargetType.Single, power = 1.7f,
            bonusType = SkillBonusType.EnemyLowHp, bonusPower = 3.0f,
            getType = GetSkillType.Shop, price = 400,
            info = "170% 피해. 적 체력 30% 이하면 300% 피해"
        },
        // 11
        new SkillData {
            name = "공방일체", mpCost = 100, coolTime = 3,
            target = TargetType.Adjacent, power = 1.0f,
            hasEffect = true, effect = EffectType.DefUp,
            effectValue = 0.3f, effectTurn = 3, effectToMe = true,
            getType = GetSkillType.Shop, price = 500,
            info = "적과 양 옆에게 100% 피해 + 3턴간 방어력 30% 증가"
        },
        // 12
        new SkillData {
            name = "두 개의 심장", mpCost = 0, coolTime = 10,
            target = TargetType.Self,
            extraAction = true, freeAction = true,
            getType = GetSkillType.Level, getLevel = 6,
            info = "턴을 쓰지 않고, 이번 턴에 두 번 행동"
        }
    };

    // ═════════════════════════════════════════════════════════════
    //  장비 11종 (기획서 3-1)  —  0~5번 무기, 6~10번 갑옷
    // ═════════════════════════════════════════════════════════════
    public static readonly EquipData[] Equips =
    {
        new EquipData { name = "롱소드",         type = EquipType.Weapon, atk = 10, price = 0,   startEquip = true, info = "공격력 +10" },
        new EquipData { name = "대검",           type = EquipType.Weapon, atk = 20, price = 150, info = "공격력 +20" },
        new EquipData { name = "단검",           type = EquipType.Weapon, atk = 5,  crit = 10, price = 120, info = "공격력 +5, 크리티컬 +10%" },
        new EquipData { name = "도끼",           type = EquipType.Weapon, atk = 20, crit = 20, price = 300, info = "공격력 +20, 크리티컬 +20%" },
        new EquipData { name = "망치",           type = EquipType.Weapon, atk = 40, price = 450, info = "공격력 +40" },
        new EquipData { name = "마스터 소드",     type = EquipType.Weapon, atk = 60, crit = 30, price = 900, info = "공격력 +60, 크리티컬 +30%" },

        new EquipData { name = "천 갑옷",         type = EquipType.Armor, def = 5,  price = 0,   startEquip = true, info = "방어력 +5" },
        new EquipData { name = "가죽 갑옷",       type = EquipType.Armor, def = 10, price = 100, info = "방어력 +10" },
        new EquipData { name = "사슬 갑옷",       type = EquipType.Armor, def = 25, price = 250, info = "방어력 +25" },
        new EquipData { name = "무쇠 갑옷",       type = EquipType.Armor, def = 35, price = 400, info = "방어력 +35" },
        new EquipData { name = "풀 플레이트 아머", type = EquipType.Armor, def = 50, price = 800, info = "방어력 +50" }
    };

    // ═════════════════════════════════════════════════════════════
    //  소모성 아이템 5종 (기획서 3-2)
    // ═════════════════════════════════════════════════════════════
    public static readonly ItemData[] Items =
    {
        new ItemData { name = "빨간 포션", healHp = 0.2f, maxCount = 5, price = 50, info = "HP 20% 회복" },
        new ItemData { name = "파란 포션", healMp = 0.2f, maxCount = 5, price = 50, info = "MP 20% 회복" },

        new ItemData { name = "힘의 영약", hasEffect = true, effect = EffectType.AtkUp,
                       effectValue = 0.3f, effectTurn = 5, maxCount = 1, price = 200,
                       info = "5턴간 공격력 30% 증가" },

        new ItemData { name = "지식의 영약", hasEffect = true, effect = EffectType.SkillUp,
                       effectValue = 0.3f, effectTurn = 5, maxCount = 1, price = 200,
                       info = "5턴간 스킬 피해 30% 증가" },

        // 기획서에 지속 턴이 안 적혀 있어서 '전투 끝까지' 로 정했다.
        new ItemData { name = "회피의 물약", hasEffect = true, effect = EffectType.EvadeUp,
                       effectValue = 2f, effectTurn = FOREVER, maxCount = 1, price = 200,
                       info = "회피율 2배 (전투 끝까지)" }
    };

    // ═════════════════════════════════════════════════════════════
    //  몬스터 스킬 7종 (기획서 2-4-2)
    // ═════════════════════════════════════════════════════════════
    public static readonly MonsterSkillData[] MonsterSkills =
    {
        // 0 : 정예 몬스터용
        new MonsterSkillData { name = "강타", power = 1.5f, coolTime = 3 },

        // 1 : 1스테이지 보스
        new MonsterSkillData { name = "내려찍기", power = 1.8f, coolTime = 8 },

        // 2, 3 : 2스테이지 보스
        new MonsterSkillData { name = "혼란의 일격", power = 1.2f, coolTime = 5,
                               hasEffect = true, effect = EffectType.SkillSeal, effectTurn = 1 },
        new MonsterSkillData { name = "혼신의 일격", power = 2.0f, coolTime = 9 },

        // 4, 5, 6 : 3스테이지 보스
        new MonsterSkillData { name = "암흑", power = 1.2f, coolTime = 7,
                               hasEffect = true, effect = EffectType.Stun, effectTurn = 1 },
        new MonsterSkillData { name = "영혼 흡수", power = 1.7f, coolTime = 4, drainRate = 0.5f },
        new MonsterSkillData { name = "파괴 광선", power = 3.0f, coolTime = 10,
                               hasEffect = true, effect = EffectType.Stun, effectTurn = 3,
                               showWarning = true }
    };

    // ═════════════════════════════════════════════════════════════
    //  몬스터 10종 (기획서 2-4-1)
    //  ★ 이 배열 순서가 BattleManager 의 monsterPrefabs 순서와 같아야 한다!
    // ═════════════════════════════════════════════════════════════
    public static readonly MonsterData[] Monsters =
    {
        // 0 : 1스테이지 몬스터 1  (에셋 : goblin)
        new MonsterData { name = "고블린", stage = 0, minLevel = 1, maxLevel = 3,
                          hpBase = 50, hpPerLevel = 10, atkBase = 8, atkPerLevel = 2,
                          def = 0, speed = 8, exp = 20, skills = new int[0] },

        // 1 : 1스테이지 몬스터 2  (에셋 : wolf)
        new MonsterData { name = "늑대", stage = 0, minLevel = 1, maxLevel = 3,
                          hpBase = 40, hpPerLevel = 10, atkBase = 10, atkPerLevel = 2,
                          def = 0, speed = 11, exp = 30, skills = new int[0] },

        // 2 : 2스테이지 몬스터 1  (에셋 : Hobgoblin)
        new MonsterData { name = "홉고블린", stage = 1, minLevel = 4, maxLevel = 6,
                          hpBase = 60, hpPerLevel = 10, atkBase = 11, atkPerLevel = 2,
                          def = 0, speed = 11, exp = 50, skills = new int[0] },

        // 3 : 2스테이지 몬스터 2  (에셋 : wolf)
        new MonsterData { name = "사나운 늑대", stage = 1, minLevel = 4, maxLevel = 6,
                          hpBase = 50, hpPerLevel = 10, atkBase = 14, atkPerLevel = 3,
                          def = 0, speed = 13, exp = 60, skills = new int[0] },

        // 4 : 3스테이지 몬스터 1  (에셋 : troll)
        new MonsterData { name = "트롤", stage = 2, minLevel = 7, maxLevel = 9,
                          hpBase = 70, hpPerLevel = 10, atkBase = 13, atkPerLevel = 2,
                          def = 25, speed = 13, exp = 80, skills = new int[0] },

        // 5 : 3스테이지 몬스터 2  (에셋 : Hobgoblin)
        new MonsterData { name = "광폭한 홉고블린", stage = 2, minLevel = 7, maxLevel = 9,
                          hpBase = 60, hpPerLevel = 10, atkBase = 17, atkPerLevel = 2,
                          def = 10, speed = 17, exp = 90, skills = new int[0] },

        // 6 : 3스테이지 정예 몬스터 — 기획서상 스킬 1개 필수
        new MonsterData { name = "정예 트롤", stage = 2, minLevel = 9, maxLevel = 9,
                          hpBase = 100, hpPerLevel = 8, atkBase = 17, atkPerLevel = 2,
                          def = 40, speed = 15, exp = 120,
                          isElite = true, skills = new int[] { 0 } },

        // 7 : 1스테이지 보스
        new MonsterData { name = "고블린 군주", stage = 0, minLevel = 4, maxLevel = 4,
                          hpBase = 250, hpPerLevel = 0, atkBase = 20, atkPerLevel = 0,
                          def = 30, speed = 12, exp = 150,
                          isBoss = true, skills = new int[] { 1 } },

        // 8 : 2스테이지 보스 — HP 절반이면 플레이어 공격력 4턴간 30% 감소
        new MonsterData { name = "홉고블린 대장", stage = 1, minLevel = 7, maxLevel = 7,
                          hpBase = 500, hpPerLevel = 0, atkBase = 30, atkPerLevel = 0,
                          def = 45, speed = 14, exp = 300,
                          isBoss = true, skills = new int[] { 2, 3 },
                          hasPhase = true, phaseCursePlayer = true,
                          phaseCurseValue = 0.3f, phaseCurseTurn = 4 },

        // 9 : 3스테이지 보스 — HP 절반이면 25% 회복 + 방어 -30 + 공격 +20
        new MonsterData { name = "고대 트롤 왕", stage = 2, minLevel = 11, maxLevel = 11,
                          hpBase = 800, hpPerLevel = 0, atkBase = 40, atkPerLevel = 0,
                          def = 60, speed = 18, exp = 600,
                          isBoss = true, skills = new int[] { 4, 5, 6 },
                          hasPhase = true, phaseHeal = 0.25f,
                          phaseDefAdd = -30, phaseAtkAdd = 20 }
    };

    // ═════════════════════════════════════════════════════════════
    //  표에서 데이터를 찾아주는 함수들
    //  ★ 배열 밖 번호를 넣으면 게임이 죽으므로 항상 범위를 먼저 확인한다.
    // ═════════════════════════════════════════════════════════════

    public static SkillData GetSkill(int num)
    {
        if (num < 0 || num >= Skills.Length) return null;
        return Skills[num];
    }

    public static EquipData GetEquip(int num)
    {
        if (num < 0 || num >= Equips.Length) return null;
        return Equips[num];
    }

    public static ItemData GetItem(int num)
    {
        if (num < 0 || num >= Items.Length) return null;
        return Items[num];
    }

    public static MonsterData GetMonster(int num)
    {
        if (num < 0 || num >= Monsters.Length) return null;
        return Monsters[num];
    }

    public static MonsterSkillData GetMonsterSkill(int num)
    {
        if (num < 0 || num >= MonsterSkills.Length) return null;
        return MonsterSkills[num];
    }

    /// <summary>이 스테이지의 보스 몬스터 번호를 찾는다. 없으면 -1.</summary>
    public static int FindBoss(int stage)
    {
        for (int i = 0; i < Monsters.Length; i++)
        {
            if (Monsters[i].isBoss == false) continue;
            if (Monsters[i].stage != stage) continue;
            return i;
        }
        return -1;
    }

    /// <summary>
    /// 몬스터가 '방어' 했을 때 피해가 몇 % 줄어드는지. (기획서 : 50 + 레벨 x 3 %)
    /// 예) 레벨 5 → (50 + 15) / 100 = 0.65 (65% 감소)
    /// </summary>
    public static float GetDefendCut(int level)
    {
        float percent = 50f + (level * 3f);
        return percent / 100f;
    }
}
