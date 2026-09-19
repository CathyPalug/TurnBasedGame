using UnityEngine;

// ═════════════════════════════════════════════════════════════════════════════
//  Unit.cs  —  플레이어와 몬스터가 공통으로 가지는 것
//
//  Player 와 Monster 는 이 Unit 을 "물려받아서(상속)" 만든다.
//      public class Player : Unit
//      public class Monster : Unit
//  이렇게 하면 체력, 공격력, 버프 같은 공통 기능을 두 번 안 써도 된다.
//
//  ★ 버프/디버프는 클래스나 List 를 안 쓰고 "배열 2개" 로만 관리한다.
//    effectValue[번호] : 효과의 크기
//    effectTurn[번호]  : 남은 턴 수 (0 이면 안 걸려 있음)
//    번호는 EffectType 순서를 그대로 쓴다. → (int)EffectType.AtkUp 이 3번
//    이렇게 하면 추가/삭제 코드가 필요 없어서 훨씬 간단하다.
// ═════════════════════════════════════════════════════════════════════════════

public class Unit : MonoBehaviour
{
    [Header("기본 정보")]
    public string unitName = "유닛";    // 화면에 보여줄 이름
    public int level = 1;               // 레벨

    [Header("체력")]
    public int hp;                      // 지금 체력
    public int maxHp;                   // 최대 체력

    [Header("능력치")]
    public int atk;                     // 공격력
    public int def;                     // 방어력
    public int speed;                   // 속도 (높을수록 턴이 먼저 온다)
    public int critRate;                // 크리티컬 확률 (%)
    public int evadeRate;               // 회피 확률 (%)

    [Header("장비로 더해지는 값 (몬스터는 항상 0)")]
    public int bonusAtk;
    public int bonusDef;
    public int bonusCrit;

    [Header("치트 (플레이어만 사용)")]
    public bool noDamage;               // F1 치트 : 켜면 피해를 안 받는다

    [Header("연출")]
    [Tooltip("내 차례일 때 켜지는 화살표 같은 표시")]
    public GameObject turnMark;

    [Tooltip("피해 숫자와 이펙트가 나타날 위치. 비우면 머리 위쯤을 쓴다.")]
    public Transform hitPoint;

    // ── 버프 / 디버프 저장소 (배열 2개) ──────────────────────────────
    // new float[(int)EffectType.Count] = 효과 종류 수만큼 칸을 만든다는 뜻
    [HideInInspector] public float[] effectValue = new float[(int)EffectType.Count];
    [HideInInspector] public int[] effectTurn = new int[(int)EffectType.Count];


    // ═════════════════════════════════════════════════════════════
    //  상태 확인
    // ═════════════════════════════════════════════════════════════

    /// <summary>아직 살아 있는가.</summary>
    public bool IsAlive()
    {
        if (hp > 0) return true;
        return false;
    }

    /// <summary>체력이 몇 % 남았는지 0~1 로 알려준다. (HP바에 쓴다)</summary>
    public float GetHpRate()
    {
        if (maxHp <= 0) return 0f;      // 0으로 나누면 에러가 나므로 먼저 확인
        return (float)hp / maxHp;
    }

    /// <summary>피해 숫자와 이펙트가 나타날 위치.</summary>
    public Vector3 GetHitPos()
    {
        if (hitPoint != null) return hitPoint.position;

        // 따로 정해두지 않았으면 발밑보다 조금 위쪽으로 잡는다.
        return transform.position + new Vector3(0f, 1.5f, 0f);
    }

    /// <summary>"지금 내 차례" 표시를 켜거나 끈다.</summary>
    public void ShowTurnMark(bool on)
    {
        if (turnMark != null) turnMark.SetActive(on);
    }


    // ═════════════════════════════════════════════════════════════
    //  버프 / 디버프
    // ═════════════════════════════════════════════════════════════

    /// <summary>효과를 건다. 이미 걸려 있으면 더 긴 쪽으로 갱신한다.</summary>
    public void AddEffect(EffectType type, float value, int turn)
    {
        int 번호 = (int)type;   // enum 을 배열 번호로 바꾼다

        effectValue[번호] = value;

        // 이미 걸린 게 더 길면 그대로 두고, 새 게 더 길면 새 걸로 바꾼다.
        if (turn > effectTurn[번호])
        {
            effectTurn[번호] = turn;
        }
    }

    /// <summary>이 효과가 지금 걸려 있는가.</summary>
    public bool HasEffect(EffectType type)
    {
        if (effectTurn[(int)type] > 0) return true;
        return false;
    }

    /// <summary>이 효과의 크기를 알려준다. 안 걸려 있으면 0.</summary>
    public float GetEffectValue(EffectType type)
    {
        if (HasEffect(type) == false) return 0f;
        return effectValue[(int)type];
    }

    /// <summary>이 효과가 몇 턴 남았는지 알려준다.</summary>
    public int GetEffectTurn(EffectType type)
    {
        return effectTurn[(int)type];
    }

    /// <summary>이 효과를 강제로 지운다. (급습이 방어를 부술 때 등)</summary>
    public void RemoveEffect(EffectType type)
    {
        effectTurn[(int)type] = 0;
        effectValue[(int)type] = 0f;
    }

    /// <summary>내 턴이 끝날 때 호출. 모든 효과의 남은 턴을 1씩 줄인다.</summary>
    public void TickEffects()
    {
        for (int i = 0; i < effectTurn.Length; i++)
        {
            if (effectTurn[i] > 0)
            {
                effectTurn[i] = effectTurn[i] - 1;
            }
        }
    }

    /// <summary>걸린 효과를 전부 지운다. (전투 시작 / 종료 때)</summary>
    public void ClearEffects()
    {
        for (int i = 0; i < effectTurn.Length; i++)
        {
            effectTurn[i] = 0;
            effectValue[i] = 0f;
        }
    }

    /// <summary>행동불능(스턴) 상태인가.</summary>
    public bool IsStunned()
    {
        return HasEffect(EffectType.Stun);
    }

    /// <summary>스킬이 봉인된 상태인가.</summary>
    public bool IsSkillSealed()
    {
        return HasEffect(EffectType.SkillSeal);
    }

    /// <summary>지금 '방어' 중인가.</summary>
    public bool IsDefending()
    {
        return HasEffect(EffectType.Defend);
    }


    // ═════════════════════════════════════════════════════════════
    //  능력치 계산 (버프/디버프까지 반영한 최종 값)
    // ═════════════════════════════════════════════════════════════

    /// <summary>최종 공격력.</summary>
    public int GetFinalAtk()
    {
        // 1) 기본 공격력 + 장비 보너스
        float 값 = atk + bonusAtk;

        // 2) 공격력 증가 버프 (힘의 영약 0.3 이면 30% 증가)
        값 = 값 * (1f + GetEffectValue(EffectType.AtkUp));

        // 3) 공격력 감소 디버프 (2보스 위압 0.3 이면 30% 감소)
        값 = 값 * (1f - GetEffectValue(EffectType.AtkDown));

        // 4) 소수점 반올림 + 음수 막기
        int 결과 = Mathf.RoundToInt(값);
        return Mathf.Max(0, 결과);
    }

    /// <summary>최종 방어력.</summary>
    public int GetFinalDef()
    {
        float 값 = def + bonusDef;

        값 = 값 * (1f + GetEffectValue(EffectType.DefUp));    // 가드
        값 = 값 * (1f - GetEffectValue(EffectType.DefDown));  // 약점 격파

        int 결과 = Mathf.RoundToInt(값);
        return Mathf.Max(0, 결과);
    }

    /// <summary>최종 크리티컬 확률 (%). 0~100 사이로 잘라준다.</summary>
    public int GetFinalCrit()
    {
        // 노려보기는 "25" 처럼 퍼센트 숫자를 그대로 더한다.
        float 값 = critRate + bonusCrit + GetEffectValue(EffectType.CritUp);

        return Mathf.Clamp(Mathf.RoundToInt(값), 0, 100);
    }

    /// <summary>최종 회피 확률 (%). 0~100 사이로 잘라준다.</summary>
    public int GetFinalEvade()
    {
        // 회피의 물약은 "2배" 처럼 곱하기로 동작한다.
        float 배수 = 1f;
        if (HasEffect(EffectType.EvadeUp))
        {
            배수 = GetEffectValue(EffectType.EvadeUp);
        }

        float 값 = evadeRate * 배수;
        return Mathf.Clamp(Mathf.RoundToInt(값), 0, 100);
    }


    // ═════════════════════════════════════════════════════════════
    //  피해 / 회복
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// 체력을 깎는다. 진짜로 깎인 양을 돌려준다.
    /// (체력이 5 남았는데 100 피해를 받으면 실제로 깎인 건 5 이다)
    /// </summary>
    public int TakeDamage(int amount)
    {
        if (noDamage) return 0;         // F1 무적 치트
        if (amount < 0) amount = 0;     // 음수 피해는 회복이 되어버리므로 막는다

        int 맞기전 = hp;
        hp = Mathf.Max(0, hp - amount); // 체력은 0 아래로 안 내려간다

        return 맞기전 - hp;
    }

    /// <summary>체력을 회복한다. 진짜로 회복된 양을 돌려준다.</summary>
    public int HealHp(int amount)
    {
        if (amount < 0) amount = 0;

        int 회복전 = hp;
        hp = Mathf.Min(maxHp, hp + amount);  // 최대 체력을 넘지 않는다

        return hp - 회복전;
    }
}
