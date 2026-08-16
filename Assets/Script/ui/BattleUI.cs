using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 화면(Canvas/Battle)의 버튼들을 BattleManager 에 연결한다.
///
/// 인스펙터 연결
///   attackButton / skillButton / itemButton : Select 아래의 공격 / 스킬 / 아이템 버튼
///   listRoot                                : Select/Scroll View
///   listButtons / listLabels                : Scroll View 안의 목록 버튼과 그 텍스트 (같은 순서)
/// </summary>
public class BattleUI : MonoBehaviour
{
    private enum ListMode { None, Skill, Item }

    [Header("행동 버튼")]
    public Button attackButton;
    public Button skillButton;
    public Button itemButton;

    [Header("목록 (스킬 / 아이템 공용)")]
    public GameObject listRoot;
    public Button[] listButtons;
    public Text[] listLabels;

    [Header("안내")]
    public GameObject targetingHint;

    private ListMode listMode = ListMode.None;

    private BattleManager Battle { get { return BattleManager.instance; } }
    private Player ThePlayer { get { return Battle != null ? Battle.player : null; } }

    private void Start()
    {
        if (attackButton != null) attackButton.onClick.AddListener(OnAttackButton);
        if (skillButton != null) skillButton.onClick.AddListener(OnSkillButton);
        if (itemButton != null) itemButton.onClick.AddListener(OnItemButton);

        if (listButtons != null)
        {
            for (int i = 0; i < listButtons.Length; i++)
            {
                if (listButtons[i] == null) continue;
                int index = i;   // 클로저 캡처 주의
                listButtons[i].onClick.AddListener(delegate { OnListButton(index); });
            }
        }

        CloseList();
        if (targetingHint != null) targetingHint.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────
    // BattleManager 가 호출
    // ─────────────────────────────────────────────────────────────
    public void OnBattleStarted()
    {
        CloseList();
        EnablePlayerInput(false);
        RefreshEnemyIntents();
    }

    public void OnBattleEnded(bool win)
    {
        CloseList();
        EnablePlayerInput(false);
        if (targetingHint != null) targetingHint.SetActive(false);
    }

    public void EnablePlayerInput(bool enable)
    {
        if (attackButton != null) attackButton.interactable = enable;
        if (skillButton != null) skillButton.interactable = enable;
        if (itemButton != null) itemButton.interactable = enable;

        if (!enable) CloseList();
    }

    public void OnTargetingStarted()
    {
        CloseList();
        if (targetingHint != null) targetingHint.SetActive(true);
    }

    public void OnTargetingEnded()
    {
        if (targetingHint != null) targetingHint.SetActive(false);
    }

    /// <summary>몬스터 예고(방어 / 파괴 광선) 표시를 갱신한다.</summary>
    public void RefreshEnemyIntents()
    {
        if (Battle == null || Battle.monsters == null) return;

        for (int i = 0; i < Battle.monsters.Length; i++)
        {
            if (Battle.monsters[i] == null) continue;
            EnemyUI ui = Battle.monsters[i].GetComponentInChildren<EnemyUI>(true);
            if (ui != null) ui.Refresh();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 버튼 처리
    // ─────────────────────────────────────────────────────────────
    private void OnAttackButton()
    {
        if (Battle == null || !Battle.waitingForPlayerAction) return;

        CloseList();
        Battle.BeginTargeting(delegate (Monster m) { Battle.PlayerAttack(m); });
    }

    private void OnSkillButton()
    {
        if (Battle == null || !Battle.waitingForPlayerAction) return;

        if (ThePlayer != null && ThePlayer.IsSkillSealed)
        {
            BattleLog.Log("스킬이 봉인되어 사용할 수 없다.");
            return;
        }

        if (listMode == ListMode.Skill) { CloseList(); return; }

        listMode = ListMode.Skill;
        BuildSkillList();
        if (listRoot != null) listRoot.SetActive(true);
    }

    private void OnItemButton()
    {
        if (Battle == null || !Battle.waitingForPlayerAction) return;

        if (listMode == ListMode.Item) { CloseList(); return; }

        listMode = ListMode.Item;
        BuildItemList();
        if (listRoot != null) listRoot.SetActive(true);
    }

    private void OnListButton(int index)
    {
        if (Battle == null || !Battle.waitingForPlayerAction) return;

        if (listMode == ListMode.Skill) UseSkillAt(index);
        else if (listMode == ListMode.Item) UseItemAt(index);
    }

    // ─────────────────────────────────────────────────────────────
    // 목록 구성
    // ─────────────────────────────────────────────────────────────
    private void BuildSkillList()
    {
        Player p = ThePlayer;
        if (p == null || listButtons == null) return;

        for (int i = 0; i < listButtons.Length; i++)
        {
            bool valid = i < GameData.Skills.Length;
            SetSlotActive(i, valid && p.skillOwned[i]);
            if (!valid || !p.skillOwned[i]) continue;

            SkillData s = GameData.Skills[i];
            string reason = p.GetSkillBlockReason(i);
            bool usable = string.IsNullOrEmpty(reason);

            SetLabel(i, usable
                ? string.Format("{0}  (MP {1})", s.skillName, s.mpCost)
                : string.Format("{0}  [{1}]", s.skillName, reason));

            if (listButtons[i] != null) listButtons[i].interactable = usable;
        }
    }

    private void BuildItemList()
    {
        Player p = ThePlayer;
        if (p == null || listButtons == null) return;

        for (int i = 0; i < listButtons.Length; i++)
        {
            bool valid = i < p.inventory.Length && !p.inventory[i].IsEmpty;
            SetSlotActive(i, valid);
            if (!valid) continue;

            ConsumableData c = GameData.GetConsumable(p.inventory[i].itemId);
            SetLabel(i, string.Format("{0} x{1}", c != null ? c.itemName : "?", p.inventory[i].count));
            if (listButtons[i] != null) listButtons[i].interactable = true;
        }
    }

    private void UseSkillAt(int skillId)
    {
        Player p = ThePlayer;
        if (p == null || !p.CanUseSkill(skillId)) return;

        SkillData s = GameData.GetSkill(skillId);
        CloseList();

        bool needsTarget = s.targetType == SkillTargetType.Single ||
                           s.targetType == SkillTargetType.SingleAndAdjacent;

        if (needsTarget)
        {
            int id = skillId;
            Battle.BeginTargeting(delegate (Monster m) { Battle.PlayerUseSkill(id, m); });
        }
        else
        {
            Battle.PlayerUseSkill(skillId, null);
        }
    }

    private void UseItemAt(int slotIndex)
    {
        CloseList();
        Battle.PlayerUseItem(slotIndex);
    }

    private void CloseList()
    {
        listMode = ListMode.None;
        if (listRoot != null) listRoot.SetActive(false);
    }

    private void SetSlotActive(int index, bool active)
    {
        if (listButtons == null || index >= listButtons.Length) return;
        if (listButtons[index] == null) return;

        // 버튼 자체가 아니라 목록 항목 루트를 껐다 켠다.
        Transform root = GetListItemRoot(listButtons[index].transform);
        if (root != null) root.gameObject.SetActive(active);
    }

    /// <summary>Scroll View/Viewport/Content 바로 아래의 항목 루트를 찾는다.</summary>
    private Transform GetListItemRoot(Transform buttonTransform)
    {
        if (listRoot == null) return buttonTransform;

        Transform content = FindContent();
        if (content == null) return buttonTransform;

        Transform cur = buttonTransform;
        while (cur != null && cur.parent != content) cur = cur.parent;
        return cur != null ? cur : buttonTransform;
    }

    private Transform cachedContent;

    private Transform FindContent()
    {
        if (cachedContent != null) return cachedContent;
        if (listRoot == null) return null;

        ScrollRect sr = listRoot.GetComponent<ScrollRect>();
        if (sr != null && sr.content != null) cachedContent = sr.content;
        return cachedContent;
    }

    private void SetLabel(int index, string text)
    {
        if (listLabels == null || index >= listLabels.Length) return;
        if (listLabels[index] != null) listLabels[index].text = text;
    }
}
