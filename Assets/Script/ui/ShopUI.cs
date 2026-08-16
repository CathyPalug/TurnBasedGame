using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 및 휴식 화면.
/// 장비 / 스킬북 / 소모성 아이템을 골드로 구매하고, 휴식으로 체력을 30% 회복한다.
///
/// 목록 버튼은 itemButtonPrefab 을 contentRoot 아래에 런타임 생성해서 만든다.
/// (버튼 프리팹은 자식 어딘가에 Button 과 Text 가 있으면 된다)
/// </summary>
public class ShopUI : MonoBehaviour
{
    private enum ShopEntryKind { Equipment, SkillBook, Consumable }

    private class ShopEntry
    {
        public ShopEntryKind kind;
        public int id;
        public string label;
        public int price;
        public bool purchasable;
        public string blockReason;
    }

    public static ShopUI instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [Header("목록")]
    [Tooltip("Scroll View/Viewport/Content")]
    public Transform contentRoot;
    [Tooltip("목록 한 줄로 쓸 버튼 프리팹 (Assets/Prefabs/btn 1.prefab 등)")]
    public GameObject itemButtonPrefab;

    [Header("표시")]
    public Text goldText;
    public Text messageText;

    [Header("버튼")]
    public Button restButton;
    public Button leaveButton;

    [Header("휴식")]
    [Tooltip("한 번 방문할 때 휴식 가능 횟수")]
    public int restPerVisit = 1;

    private readonly List<GameObject> spawnedRoots = new List<GameObject>();
    private readonly List<Button> spawnedButtons = new List<Button>();
    private readonly List<Text> spawnedLabels = new List<Text>();
    private readonly List<ShopEntry> entries = new List<ShopEntry>();
    private int restUsed;

    /// <summary>
    /// 목록용 버튼 프리팹에서 실제로 눌리는 Button 과 라벨 Text 를 찾아준다.
    /// (버튼 프리팹이 여러 겹으로 되어 있어도 Text 를 감싸고 있는 Button 을 고른다)
    /// </summary>
    public static void FindButtonAndLabel(GameObject root, out Button button, out Text label)
    {
        label = root.GetComponentInChildren<Text>(true);
        button = label != null ? label.GetComponentInParent<Button>() : null;
        if (button == null) button = root.GetComponentInChildren<Button>(true);
    }

    private Player ThePlayer
    {
        get { return GameManager.instance != null ? GameManager.instance.player : null; }
    }

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        if (restButton != null) restButton.onClick.AddListener(OnClickRest);
        if (leaveButton != null) leaveButton.onClick.AddListener(OnClickLeave);
    }

    private void OnEnable()
    {
        restUsed = 0;
        Refresh();
    }

    // ─────────────────────────────────────────────────────────────
    // 목록 구성
    // ─────────────────────────────────────────────────────────────
    public void Refresh()
    {
        Player p = ThePlayer;
        if (p == null) return;

        BuildEntries(p);
        EnsureButtons(entries.Count);

        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            bool used = i < entries.Count;
            spawnedRoots[i].SetActive(used);
            if (!used) continue;

            ShopEntry e = entries[i];
            spawnedButtons[i].interactable = e.purchasable;

            if (i < spawnedLabels.Count && spawnedLabels[i] != null)
            {
                spawnedLabels[i].text = e.purchasable
                    ? string.Format("{0}   -   {1} G", e.label, e.price)
                    : string.Format("{0}   -   {1} G  [{2}]", e.label, e.price, e.blockReason);
            }
        }

        if (goldText != null) goldText.text = string.Format("보유 골드 : {0} G", p.gold);
        if (restButton != null) restButton.interactable = restUsed < restPerVisit;
    }

    private void BuildEntries(Player p)
    {
        entries.Clear();

        // 1) 장비
        for (int i = 0; i < GameData.Equipments.Length; i++)
        {
            EquipmentData e = GameData.Equipments[i];
            if (e.price <= 0) continue;               // 시작 장비는 판매하지 않는다
            if (p.ownedEquipments[i]) continue;       // 이미 보유

            ShopEntry entry = new ShopEntry
            {
                kind = ShopEntryKind.Equipment,
                id = i,
                price = e.price,
                label = string.Format("[{0}] {1} ({2})",
                    e.slot == EquipSlotType.Weapon ? "무기" : "갑옷", e.equipName, e.description)
            };

            entry.purchasable = p.gold >= e.price;
            entry.blockReason = entry.purchasable ? string.Empty : "골드 부족";
            entries.Add(entry);
        }

        // 2) 스킬북
        for (int i = 0; i < GameData.Skills.Length; i++)
        {
            SkillData s = GameData.Skills[i];
            if (s.acquireType != SkillAcquireType.ShopBook) continue;
            if (p.skillOwned[i]) continue;

            ShopEntry entry = new ShopEntry
            {
                kind = ShopEntryKind.SkillBook,
                id = i,
                price = s.price,
                label = string.Format("[스킬북] {0} (MP {1}{2})",
                    s.skillName, s.mpCost, s.cooldown > 0 ? ", 쿨 " + s.cooldown + "턴" : string.Empty)
            };

            entry.purchasable = p.gold >= s.price;
            entry.blockReason = entry.purchasable ? string.Empty : "골드 부족";
            entries.Add(entry);
        }

        // 3) 소모성 아이템
        for (int i = 0; i < GameData.Consumables.Length; i++)
        {
            ConsumableData c = GameData.Consumables[i];

            ShopEntry entry = new ShopEntry
            {
                kind = ShopEntryKind.Consumable,
                id = i,
                price = c.price,
                label = string.Format("[아이템] {0} ({1}) {2}/{3}",
                    c.itemName, c.description, p.CountItem(i), c.maxStack)
            };

            if (p.gold < c.price)
            {
                entry.purchasable = false;
                entry.blockReason = "골드 부족";
            }
            else if (p.CountItem(i) >= c.maxStack)
            {
                entry.purchasable = false;
                entry.blockReason = "최대 개수";
            }
            else if (p.CountItem(i) == 0 && !p.HasEmptySlot())
            {
                entry.purchasable = false;
                entry.blockReason = "배낭 가득";
            }
            else
            {
                entry.purchasable = true;
            }

            entries.Add(entry);
        }
    }

    /// <summary>필요한 수만큼 목록 버튼을 만들어 둔다.</summary>
    private void EnsureButtons(int needed)
    {
        if (contentRoot == null || itemButtonPrefab == null) return;

        while (spawnedButtons.Count < needed)
        {
            GameObject go = Instantiate(itemButtonPrefab, contentRoot);
            go.name = "ShopEntry " + spawnedButtons.Count;

            Button b;
            Text t;
            FindButtonAndLabel(go, out b, out t);
            if (b == null) { Destroy(go); break; }

            int index = spawnedButtons.Count;
            b.onClick.AddListener(delegate { OnClickEntry(index); });

            spawnedRoots.Add(go);
            spawnedButtons.Add(b);
            spawnedLabels.Add(t);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 구매
    // ─────────────────────────────────────────────────────────────
    private void OnClickEntry(int index)
    {
        if (index < 0 || index >= entries.Count) return;

        Player p = ThePlayer;
        if (p == null) return;

        ShopEntry e = entries[index];
        if (!e.purchasable) { SetMessage(e.blockReason); return; }
        if (p.gold < e.price) { SetMessage("골드가 부족합니다."); return; }

        switch (e.kind)
        {
            case ShopEntryKind.Equipment:
            {
                EquipmentData data = GameData.GetEquipment(e.id);
                p.gold -= e.price;
                p.ownedEquipments[e.id] = true;
                p.Equip(e.id);
                SetMessage(string.Format("{0} 구매 & 장착!", data.equipName));
                break;
            }

            case ShopEntryKind.SkillBook:
            {
                SkillData data = GameData.GetSkill(e.id);
                p.gold -= e.price;
                p.skillOwned[e.id] = true;
                SetMessage(string.Format("스킬북 [{0}] 습득!", data.skillName));
                break;
            }

            case ShopEntryKind.Consumable:
            {
                ConsumableData data = GameData.GetConsumable(e.id);
                if (!p.AddItem(e.id))
                {
                    SetMessage("배낭이 가득 찼거나 최대 개수를 넘었습니다.");
                    break;
                }
                p.gold -= e.price;
                SetMessage(string.Format("{0} 구매!", data.itemName));
                break;
            }
        }

        Refresh();
        if (InventoryUI.instance != null) InventoryUI.instance.Refresh();
    }

    // ─────────────────────────────────────────────────────────────
    // 휴식 / 나가기
    // ─────────────────────────────────────────────────────────────
    public void OnClickRest()
    {
        Player p = ThePlayer;
        if (p == null) return;

        if (restUsed >= restPerVisit)
        {
            SetMessage("이미 휴식했습니다.");
            return;
        }

        restUsed++;
        int healed = p.Rest();
        SetMessage(string.Format("휴식 - HP {0} 회복 ({1}/{2})", healed, p.hp, p.maxHp));
        Refresh();
    }

    public void OnClickLeave()
    {
        if (StageManager.instance != null) StageManager.instance.LeaveShopRoom();
    }

    private void SetMessage(string msg)
    {
        if (messageText != null) messageText.text = msg;
        Debug.Log("[상점] " + msg);
    }
}
