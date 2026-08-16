using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 턴제 전투의 흐름을 관리한다.
///
/// 라운드 구조
///   1) 지난 라운드의 '방어'를 해제한다.
///   2) 살아있는 모든 몬스터가 이번 턴 행동을 미리 정한다. (플레이어가 볼 수 있게 예고)
///   3) '방어'를 고른 몬스터는 여기서 먼저 발동한다. (기획서 : 방어는 반드시 먼저 발동)
///   4) 속도가 높은 순서대로 한 명씩 행동한다.
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [Header("참조")]
    public Player player;
    public Camera targetingCamera;

    [Header("몬스터 배치")]
    [Tooltip("몬스터가 설 위치. 왼쪽부터 순서대로. 배열 길이가 최대 등장 수가 된다.")]
    public Transform[] monsterSlots = new Transform[4];

    [Tooltip("GameData.Monsters 와 같은 인덱스의 3D 프리팹. 비어 있으면 defaultMonsterPrefab 을 쓴다.")]
    public GameObject[] monsterPrefabs = new GameObject[10];

    public GameObject defaultMonsterPrefab;

    [Header("UI")]
    public Text turnText;
    public BattleUI battleUI;

    [Header("연출")]
    [Tooltip("몬스터 행동 사이의 대기 시간(초)")]
    public float monsterActionDelay = 0.8f;
    public float roundStartDelay = 0.3f;

    [Tooltip("마지막 적을 쓰러뜨린(막타) 뒤 결과 화면으로 넘어가기까지 대기 시간(초)")]
    public float battleEndDelay = 1.8f;

    [Tooltip("쓰러진 몬스터가 화면에서 사라지기까지 대기 시간(초)")]
    public float monsterFadeDelay = 0.7f;

    // ── 런타임 상태 ────────────────────────────────────────────────
    [Header("런타임 (읽기용)")]
    public Monster[] monsters = new Monster[0];
    public int round;
    public bool battleActive;
    public bool waitingForPlayerAction;

    private readonly List<Entry> turnOrder = new List<Entry>();
    private int turnIndex = -1;

    private bool targetingMode;
    private Action<Monster> onTargetPicked;
    private int highlightedTarget = -1;

    /// <summary>막타 이후 결과로 넘어가기를 기다리는 중인가.</summary>
    private bool battleEnding;

    /// <summary>전투가 끝났을 때 호출된다. (bool win)</summary>
    public Action<bool> onBattleFinished;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        if (targetingMode) UpdateTargeting();
    }

    // ─────────────────────────────────────────────────────────────
    // 전투 시작 / 종료
    // ─────────────────────────────────────────────────────────────
    /// <summary>몬스터 id 배열로 전투를 시작한다. levels 가 null 이면 레벨 범위 안에서 무작위.</summary>
    public void StartBattle(int[] monsterIds, int[] levels)
    {
        if (player == null)
        {
            Debug.LogError("BattleManager : player 가 연결되어 있지 않습니다.");
            return;
        }

        ClearMonsters();
        SpawnMonsters(monsterIds, levels);

        player.ResetSkillCooldowns();
        player.effects.ClearBattleEffects();
        player.extraActions = 0;
        player.ShowTurnMark(false);

        round = 0;
        battleActive = true;
        battleEnding = false;
        waitingForPlayerAction = false;

        if (BattleLog.Instance != null) BattleLog.Instance.ClearBattleLog();
        BattleLog.Log("전투 시작!");

        if (battleUI != null) battleUI.OnBattleStarted();

        StartCoroutine(BeginRoundRoutine());
    }

    private void SpawnMonsters(int[] monsterIds, int[] levels)
    {
        int count = Mathf.Min(monsterIds.Length, monsterSlots.Length);
        monsters = new Monster[count];

        for (int i = 0; i < count; i++)
        {
            int id = monsterIds[i];
            GameObject prefab = GetMonsterPrefab(id);
            if (prefab == null)
            {
                Debug.LogError("BattleManager : 몬스터 프리팹이 없습니다. id=" + id);
                continue;
            }

            Transform slot = monsterSlots[i];
            GameObject go = Instantiate(prefab, slot.position, slot.rotation, slot);

            Monster m = go.GetComponent<Monster>();
            if (m == null) m = go.AddComponent<Monster>();

            if (levels != null && i < levels.Length) m.Setup(id, levels[i], i);
            else m.SetupRandomLevel(id, i);

            monsters[i] = m;
        }
    }

    private GameObject GetMonsterPrefab(int monsterId)
    {
        if (monsterPrefabs != null && monsterId >= 0 && monsterId < monsterPrefabs.Length && monsterPrefabs[monsterId] != null)
            return monsterPrefabs[monsterId];
        return defaultMonsterPrefab;
    }

    private void ClearMonsters()
    {
        if (monsters == null) return;
        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null) Destroy(monsters[i].gameObject);
        }
        monsters = new Monster[0];
    }

    /// <summary>
    /// 승패 판정 없이 전투를 강제로 끝낸다. (치트 스테이지 이동, 메인 메뉴 복귀 등)
    /// </summary>
    public void AbortBattle()
    {
        if (!battleActive) return;

        battleActive = false;
        battleEnding = false;
        waitingForPlayerAction = false;
        targetingMode = false;
        StopAllCoroutines();

        if (player != null) player.OnBattleEnd();
        ClearMonsters();

        if (battleUI != null) battleUI.OnBattleEnded(false);
    }

    public void EndBattle(bool win)
    {
        if (!battleActive) return;

        battleActive = false;
        battleEnding = false;
        waitingForPlayerAction = false;
        targetingMode = false;
        StopAllCoroutines();

        if (player != null) player.OnBattleEnd();
        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null) monsters[i].OnBattleEnd();
        }

        BattleLog.Log(win ? "전투 승리!" : "전투 패배...");

        if (battleUI != null) battleUI.OnBattleEnded(win);

        RoomFinish(win);

        if (onBattleFinished != null) onBattleFinished(win);
    }

    /// <summary>전투 결과를 스테이지 진행에 반영한다.</summary>
    public void RoomFinish(bool win)
    {
        if (win)
        {
            if (StageManager.instance != null) StageManager.instance.OnRoomCleared();
        }
        else
        {
            if (GameManager.instance != null) GameManager.instance.GameOver();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 라운드 / 턴 진행
    // ─────────────────────────────────────────────────────────────
    private IEnumerator BeginRoundRoutine()
    {
        yield return new WaitForSeconds(roundStartDelay);
        BeginRound();
    }

    private void BeginRound()
    {
        if (!battleActive) return;

        round++;

        // 1) 지난 라운드의 '방어' 해제
        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null) monsters[i].effects.Remove(EffectType.MonsterDefend);
        }

        // 2) 이번 라운드 행동 예고
        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null && monsters[i].IsAlive) monsters[i].PlanAction();
        }

        // 3) '방어'는 먼저 발동
        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null && monsters[i].IsAlive) monsters[i].ApplyPlannedDefend();
        }

        BuildTurnOrder();
        turnIndex = -1;

        if (battleUI != null) battleUI.RefreshEnemyIntents();

        NextTurn();
    }

    /// <summary>속도가 높은 순서로 턴 순서를 만든다.</summary>
    private void BuildTurnOrder()
    {
        turnOrder.Clear();

        if (player != null && player.IsAlive) turnOrder.Add(player);
        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null && monsters[i].IsAlive) turnOrder.Add(monsters[i]);
        }

        turnOrder.Sort(delegate (Entry a, Entry b)
        {
            int cmp = b.Speed.CompareTo(a.Speed);
            if (cmp != 0) return cmp;
            // 속도가 같으면 플레이어가 먼저
            bool aIsPlayer = a is Player;
            bool bIsPlayer = b is Player;
            if (aIsPlayer && !bIsPlayer) return -1;
            if (!aIsPlayer && bIsPlayer) return 1;
            return 0;
        });
    }

    public void NextTurn()
    {
        if (!battleActive) return;

        if (CheckBattleEnd()) return;

        turnIndex++;

        // 이번 라운드의 모든 유닛이 행동했으면 다음 라운드
        if (turnIndex >= turnOrder.Count)
        {
            StartCoroutine(BeginRoundRoutine());
            return;
        }

        Entry current = turnOrder[turnIndex];
        if (current == null || !current.IsAlive)
        {
            NextTurn();
            return;
        }

        ShowCurrentTurn(current);

        if (current is Player) StartPlayerTurn();
        else StartCoroutine(MonsterTurnRoutine(current as Monster));
    }

    private void ShowCurrentTurn(Entry current)
    {
        for (int i = 0; i < turnOrder.Count; i++)
        {
            if (turnOrder[i] != null) turnOrder[i].ShowTurnMark(turnOrder[i] == current);
        }

        if (turnText != null) turnText.text = (current is Player) ? "PLAYER" : "ENEMY";
    }

    // ── 플레이어 턴 ────────────────────────────────────────────────
    private void StartPlayerTurn()
    {
        player.TickSkillCooldowns();

        if (player.IsStunned)
        {
            BattleLog.Log("행동불능 상태라 이번 턴을 넘긴다.");
            EndPlayerTurn();
            return;
        }

        player.extraActions = 0;
        waitingForPlayerAction = true;
        if (battleUI != null) battleUI.EnablePlayerInput(true);
    }

    /// <summary>플레이어가 행동을 하나 끝냈을 때 호출한다.</summary>
    /// <param name="consumesTurn">턴을 소모하는 행동인가</param>
    public void OnPlayerActionDone(bool consumesTurn)
    {
        if (!battleActive) return;

        if (CheckBattleEnd()) return;

        if (!consumesTurn)
        {
            // 두 개의 심장처럼 턴을 소모하지 않는 행동
            if (battleUI != null) battleUI.EnablePlayerInput(true);
            return;
        }

        if (player.extraActions > 0)
        {
            player.extraActions--;
            BattleLog.Log("추가 행동! 한 번 더 행동할 수 있다.");
            if (battleUI != null) battleUI.EnablePlayerInput(true);
            return;
        }

        EndPlayerTurn();
    }

    private void EndPlayerTurn()
    {
        waitingForPlayerAction = false;
        targetingMode = false;
        if (battleUI != null) battleUI.EnablePlayerInput(false);

        player.TickEffects();
        NextTurn();
    }

    // ── 몬스터 턴 ─────────────────────────────────────────────────
    private IEnumerator MonsterTurnRoutine(Monster m)
    {
        yield return new WaitForSeconds(monsterActionDelay);

        if (!battleActive) yield break;

        if (m != null && m.IsAlive)
        {
            m.ExecuteTurn(player);
            m.TickEffects();
            if (battleUI != null) battleUI.RefreshEnemyIntents();
        }

        yield return new WaitForSeconds(monsterActionDelay * 0.4f);

        NextTurn();
    }

    // ─────────────────────────────────────────────────────────────
    // 플레이어 행동 실행
    // ─────────────────────────────────────────────────────────────
    /// <summary>기본 공격 (공격력 100%, 크리티컬 가능).</summary>
    public void PlayerAttack(Monster target)
    {
        if (!battleActive) return;
        if (target == null || !target.IsAlive) return;

        DamageResult r = Combat.Calculate(player, target, 1.0f, false, true);

        if (r.evaded)
        {
            BattleEffects.Miss(target);
            BattleLog.Log(string.Format("공격 - {0}이(가) 회피했다!", target.entryName));
        }
        else
        {
            int dealt = target.ApplyDamage(r.finalDamage);
            BattleEffects.Hit(target, dealt, r.critical);
            BattleLog.Log(string.Format("공격 - {0}에게 {1} 피해{2}",
                target.entryName, dealt, r.critical ? " (크리티컬!)" : string.Empty));
            CheckMonsterDeath(target);
        }

        OnPlayerActionDone(true);
    }

    /// <summary>스킬 사용. target 은 단일/인접 대상 스킬일 때만 필요하다.</summary>
    public void PlayerUseSkill(int skillId, Monster target)
    {
        if (!battleActive) return;

        SkillData s = GameData.GetSkill(skillId);
        if (s == null) return;

        if (!player.CanUseSkill(skillId))
        {
            BattleLog.Log(string.Format("[{0}] 사용 불가 : {1}", s.skillName, player.GetSkillBlockReason(skillId)));
            return;
        }

        player.ConsumeMp(s.mpCost);
        player.StartSkillCooldown(skillId);

        BattleLog.Log(string.Format("[{0}] 사용! (MP -{1})", s.skillName, s.mpCost));

        // 회복
        if (s.healPercentOfMaxHp > 0f)
        {
            int healed = player.Heal(Mathf.RoundToInt(player.maxHp * s.healPercentOfMaxHp));
            BattleEffects.Heal(player, healed);
            BattleLog.Log(string.Format("HP {0} 회복", healed));
        }

        // 자신에게 거는 버프
        if (s.hasEffect && s.effectOnSelf)
        {
            player.AddEffect(s.effectType, s.effectValue, s.effectTurns, s.skillName);
        }

        // 추가 행동
        if (s.grantsExtraAction)
        {
            player.extraActions += 1;
            BattleLog.Log("이번 턴 동안 두 번 행동할 수 있다!");
        }

        // 피해
        if (s.DealsDamage)
        {
            Monster[] targets = GetSkillTargets(s, target);
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null || !targets[i].IsAlive) continue;
                ApplySkillDamage(s, targets[i]);
            }
        }

        // 대상에게 거는 디버프
        if (s.hasEffect && !s.effectOnSelf && target != null)
        {
            target.AddEffect(s.effectType, s.effectValue, s.effectTurns, s.skillName);
            BattleLog.Log(string.Format("{0} : {1} {2}턴", target.entryName, s.skillName, s.effectTurns));
        }

        OnPlayerActionDone(!s.doesNotConsumeTurn);
    }

    private void ApplySkillDamage(SkillData s, Monster target)
    {
        float power = s.power;

        switch (s.condition)
        {
            case SkillCondition.ScaleByLostHp:
                power = Combat.GetLostHpScaledPower(player, s.scaleMinPower, s.scaleMaxPower);
                break;

            case SkillCondition.TargetIsDefending:
                if (target.IsDefending)
                {
                    power = s.conditionPower;
                    if (s.breakTargetDefend)
                    {
                        target.effects.Remove(EffectType.MonsterDefend);
                        BattleLog.Log(string.Format("{0}의 방어를 무너뜨렸다!", target.entryName));
                    }
                }
                break;

            case SkillCondition.TargetHpBelow30:
                if (target.HpRatio <= 0.3f) power = s.conditionPower;
                break;
        }

        DamageResult r = Combat.Calculate(player, target, power, true, true);

        if (r.evaded)
        {
            BattleEffects.Miss(target);
            BattleLog.Log(string.Format("{0}이(가) 회피했다!", target.entryName));
            return;
        }

        int dealt = target.ApplyDamage(r.finalDamage);
        BattleEffects.Hit(target, dealt, r.critical);
        BattleLog.Log(string.Format("{0}에게 {1} 피해{2}",
            target.entryName, dealt, r.critical ? " (크리티컬!)" : string.Empty));

        CheckMonsterDeath(target);
    }

    /// <summary>스킬 대상 범위를 실제 몬스터 배열로 바꾼다.</summary>
    public Monster[] GetSkillTargets(SkillData s, Monster target)
    {
        List<Monster> list = new List<Monster>();

        switch (s.targetType)
        {
            case SkillTargetType.AllEnemies:
                for (int i = 0; i < monsters.Length; i++)
                {
                    if (monsters[i] != null && monsters[i].IsAlive) list.Add(monsters[i]);
                }
                break;

            case SkillTargetType.SingleAndAdjacent:
                if (target != null)
                {
                    int c = target.slotIndex;
                    for (int i = c - 1; i <= c + 1; i++)
                    {
                        if (i < 0 || i >= monsters.Length) continue;
                        if (monsters[i] != null && monsters[i].IsAlive) list.Add(monsters[i]);
                    }
                }
                break;

            case SkillTargetType.Single:
                if (target != null && target.IsAlive) list.Add(target);
                break;
        }

        return list.ToArray();
    }

    /// <summary>배낭 칸의 아이템을 사용한다. (턴을 소모한다)</summary>
    public void PlayerUseItem(int slotIndex)
    {
        if (!battleActive) return;
        if (!player.UseItemSlot(slotIndex)) return;
        OnPlayerActionDone(true);
    }

    // ─────────────────────────────────────────────────────────────
    // 사망 / 승패 판정
    // ─────────────────────────────────────────────────────────────
    private void CheckMonsterDeath(Monster m)
    {
        if (m == null || m.IsAlive) return;

        MonsterData data = m.Data;
        int gainedExp, gainedGold;
        player.GainKillReward(data, m.level, out gainedExp, out gainedGold);

        BattleLog.Log(string.Format("{0} 처치! 경험치 +{1}, 골드 +{2}", m.entryName, gainedExp, gainedGold));

        m.ShowTurnMark(false);
        BattleEffects.Death(m);
        StartCoroutine(HideMonsterRoutine(m));
    }

    /// <summary>쓰러진 몬스터를 바로 지우지 않고 잠깐 두었다가 사라지게 한다.</summary>
    private IEnumerator HideMonsterRoutine(Monster m)
    {
        yield return new WaitForSeconds(monsterFadeDelay);
        if (m != null) m.gameObject.SetActive(false);
    }

    private bool CheckBattleEnd()
    {
        if (!battleActive) return false;
        if (battleEnding) return true;

        if (player == null || !player.IsAlive)
        {
            BeginEndBattle(false);
            return true;
        }

        if (AllMonstersDead())
        {
            BeginEndBattle(true);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 막타 연출을 볼 수 있도록 잠깐 기다렸다가 전투를 끝낸다.
    /// </summary>
    private void BeginEndBattle(bool win)
    {
        battleEnding = true;
        waitingForPlayerAction = false;
        targetingMode = false;

        if (battleUI != null)
        {
            battleUI.EnablePlayerInput(false);
            battleUI.OnTargetingEnded();
        }

        StartCoroutine(EndBattleRoutine(win));
    }

    private IEnumerator EndBattleRoutine(bool win)
    {
        yield return new WaitForSeconds(battleEndDelay);
        EndBattle(win);
    }

    public bool AllMonstersDead()
    {
        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null && monsters[i].IsAlive) return false;
        }
        return true;
    }

    public Monster GetFirstAliveMonster()
    {
        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null && monsters[i].IsAlive) return monsters[i];
        }
        return null;
    }

    public bool HasBossAlive()
    {
        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null && monsters[i].IsAlive && monsters[i].IsBoss) return true;
        }
        return false;
    }

    /// <summary>치트 F6 : 현재 전투의 모든 적 제거.</summary>
    public void KillAllMonsters()
    {
        if (!battleActive) return;

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] == null || !monsters[i].IsAlive) continue;
            monsters[i].ApplyDamage(monsters[i].hp);
            CheckMonsterDeath(monsters[i]);
        }

        CheckBattleEnd();
    }

    // ─────────────────────────────────────────────────────────────
    // 대상 지정
    // ─────────────────────────────────────────────────────────────
    /// <summary>몬스터 클릭(또는 숫자키)으로 대상을 고르게 한다.</summary>
    public void BeginTargeting(Action<Monster> callback)
    {
        // 살아있는 적이 하나뿐이면 바로 확정
        int aliveCount = 0;
        Monster onlyOne = null;
        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] == null || !monsters[i].IsAlive) continue;
            aliveCount++;
            onlyOne = monsters[i];
        }

        if (aliveCount == 0) return;
        if (aliveCount == 1)
        {
            callback(onlyOne);
            return;
        }

        targetingMode = true;
        onTargetPicked = callback;
        highlightedTarget = -1;

        BattleLog.Log("대상을 선택하세요. (적 클릭 또는 숫자키 1~4)");
        if (battleUI != null) battleUI.OnTargetingStarted();
    }

    public void CancelTargeting()
    {
        targetingMode = false;
        onTargetPicked = null;
        SetHighlight(-1);
        if (battleUI != null) battleUI.OnTargetingEnded();
    }

    private void UpdateTargeting()
    {
        // 숫자키로 선택
        for (int i = 0; i < monsters.Length && i < 9; i++)
        {
            if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;
            if (monsters[i] != null && monsters[i].IsAlive) PickTarget(monsters[i]);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelTargeting();
            return;
        }

        Camera cam = targetingCamera != null ? targetingCamera : Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Monster hovered = null;

        if (Physics.Raycast(ray, out hit, 500f))
        {
            hovered = hit.collider.GetComponentInParent<Monster>();
            if (hovered != null && !hovered.IsAlive) hovered = null;
        }

        SetHighlight(hovered != null ? hovered.slotIndex : -1);

        if (Input.GetMouseButtonDown(0) && hovered != null) PickTarget(hovered);
    }

    private void SetHighlight(int slot)
    {
        if (highlightedTarget == slot) return;
        highlightedTarget = slot;

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] == null) continue;
            EnemyUI ui = monsters[i].GetComponentInChildren<EnemyUI>(true);
            if (ui != null) ui.SetHighlight(i == slot);
        }
    }

    private void PickTarget(Monster m)
    {
        Action<Monster> callback = onTargetPicked;

        targetingMode = false;
        onTargetPicked = null;
        SetHighlight(-1);
        if (battleUI != null) battleUI.OnTargetingEnded();

        if (callback != null) callback(m);
    }
}
