/// <summary>
/// 게임 전반에서 쓰는 열거형 모음.
/// </summary>

// 스킬이 노리는 대상 범위
public enum SkillTargetType
{
    Single,             // 지정한 단일 대상
    SingleAndAdjacent,  // 지정한 대상 + 좌우 인접
    AllEnemies,         // 모든 대상
    Self                // 자신
}

// 스킬 습득 방법
public enum SkillAcquireType
{
    Level,      // 특정 레벨 달성 시 자동 획득
    ShopBook    // 상점에서 스킬북 구매
}

// 스킬의 조건부 배율 조건
public enum SkillCondition
{
    None,
    TargetIsDefending,  // 급습 : 대상이 '방어' 상태
    TargetHpBelow30,    // 최후의 일격 : 대상 체력 30% 이하
    ScaleByLostHp       // 기사회생 : 자신이 잃은 체력 %에 비례
}

// 장비 착용 칸
public enum EquipSlotType
{
    Weapon,
    Armor
}

// 버프 / 디버프 / 상태이상 종류
public enum EffectType
{
    None,
    CritRateUp,     // 노려보기
    DefenseUp,      // 가드, 공방일체
    DefenseDown,    // 약점 격파
    AttackUp,       // 힘의 영약
    AttackDown,     // 2스테이지 보스 페이즈
    SkillDamageUp,  // 지식의 영약
    EvadeUp,        // 회피의 물약
    SkillSeal,      // 혼란의 일격 : 스킬 사용 불가
    Stun,           // 암흑 / 파괴 광선 : 행동 불가
    MonsterDefend   // 몬스터 '방어'
}

// 전체 게임 상태
public enum GameState
{
    MainMenu,
    Map,
    Battle,
    Shop,
    GameOver,
    GameClear
}

// 지도상의 방 종류
public enum RoomType
{
    Battle,
    ShopRest,
    Boss
}

// 몬스터가 이번 턴에 할 행동
public enum MonsterActionType
{
    Attack,
    Defend,
    Skill
}

// 플레이어가 전투 중 고른 행동
public enum PlayerActionType
{
    None,
    Attack,
    Skill,
    Item
}
