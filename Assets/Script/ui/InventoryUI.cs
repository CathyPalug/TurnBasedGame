using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 배낭(6칸)과 착용 장비를 보여준다.
/// 칸을 클릭하면 아이템을 사용한다. (전투 중에는 턴을 소모한다)
/// 장비 목록에서 보유 장비를 클릭하면 바꿔 낄 수 있다.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [Header("배낭 6칸")]
    public Button[] slotButtons = new Button[GameData.InventorySize];
    public Text[] slotLabels = new Text[GameData.InventorySize];

    [Header("착용 장비")]
    public Text weaponText;
    public Text armorText;

    [Header("보유 장비 교체 목록")]
    public Transform equipListRoot;
    public GameObject equipButtonPrefab;

    [Header("능력치 요약")]
    public Text statText;

    [Header("닫기")]
    public Button closeButton;

    private readonly System.Collections.Generic.List<GameObject> equipRoots = new System.Collections.Generic.List<GameObject>();
    private readonly System.Collections.Generic.List<Button> equipButtons = new System.Collections.Generic.List<Button>();
    private readonly System.Collections.Generic.List<Text> equipLabels = new System.Collections.Generic.List<Text>();
    private readonly System.Collections.Generic.List<int> equipIds = new System.Collections.Generic.List<int>();

    private Player ThePlayer
    {
        get { return GameManager.instance != null ? GameManager.instance.player : null; }
    }

    private void Awake()
    {
        instance = this;

        if (slotButtons != null)
        {
            for (int i = 0; i < slotButtons.Length; i++)
            {
                if (slotButtons[i] == null) continue;
                int index = i;
                slotButtons[i].onClick.AddListener(delegate { OnClickSlot(index); });
            }
        }

        if (closeButton != null) closeButton.onClick.AddListener(OnClickClose);
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        Player p = ThePlayer;
        if (p == null) return;

        // 배낭
        for (int i = 0; i < slotButtons.Length; i++)
        {
            bool has = i < p.inventory.Length && !p.inventory[i].IsEmpty;

            if (slotButtons[i] != null) slotButtons[i].interactable = has;

            if (i < slotLabels.Length && slotLabels[i] != null)
            {
                if (has)
                {
                    ConsumableData c = GameData.GetConsumable(p.inventory[i].itemId);
                    slotLabels[i].text = string.Format("{0}\nx{1}", c != null ? c.itemName : "?", p.inventory[i].count);
                }
                else
                {
                    slotLabels[i].text = "빈 칸";
                }
            }
        }

        // 착용 장비
        EquipmentData w = p.GetEquipped(EquipSlotType.Weapon);
        EquipmentData a = p.GetEquipped(EquipSlotType.Armor);
        if (weaponText != null) weaponText.text = string.Format("무기 : {0}", w != null ? w.equipName : "-");
        if (armorText != null) armorText.text = string.Format("갑옷 : {0}", a != null ? a.equipName : "-");

        if (statText != null)
        {
            statText.text = string.Format("공격력 {0}   방어력 {1}   속도 {2}\n크리티컬 {3}%   회피 {4}%",
                p.Atk, p.Def, p.Speed, p.CritChance, p.EvadeChance);
        }

        RefreshEquipList(p);
    }

    private void RefreshEquipList(Player p)
    {
        if (equipListRoot == null || equipButtonPrefab == null) return;

        equipIds.Clear();
        for (int i = 0; i < GameData.Equipments.Length; i++)
        {
            if (p.ownedEquipments[i]) equipIds.Add(i);
        }

        while (equipButtons.Count < equipIds.Count)
        {
            GameObject go = Instantiate(equipButtonPrefab, equipListRoot);
            go.name = "EquipEntry " + equipButtons.Count;

            Button b;
            Text t;
            ShopUI.FindButtonAndLabel(go, out b, out t);
            if (b == null) { Destroy(go); break; }

            int index = equipButtons.Count;
            b.onClick.AddListener(delegate { OnClickEquip(index); });

            equipRoots.Add(go);
            equipButtons.Add(b);
            equipLabels.Add(t);
        }

        for (int i = 0; i < equipButtons.Count; i++)
        {
            bool used = i < equipIds.Count;
            equipRoots[i].SetActive(used);
            if (!used) continue;

            EquipmentData e = GameData.GetEquipment(equipIds[i]);
            bool equipped = p.IsEquipped(equipIds[i]);

            if (equipLabels[i] != null)
            {
                equipLabels[i].text = string.Format("{0}{1}", e.equipName, equipped ? "  (착용중)" : string.Empty);
            }
            equipButtons[i].interactable = !equipped;
        }
    }

    private void OnClickSlot(int index)
    {
        Player p = ThePlayer;
        if (p == null) return;
        if (index < 0 || index >= p.inventory.Length || p.inventory[index].IsEmpty) return;

        // 전투 중이고 내 턴이면 턴을 소모하며 사용한다.
        BattleManager bm = BattleManager.instance;
        if (bm != null && bm.battleActive)
        {
            if (!bm.waitingForPlayerAction) return;
            bm.PlayerUseItem(index);
        }
        else
        {
            p.UseItemSlot(index);
        }

        Refresh();
    }

    private void OnClickEquip(int index)
    {
        Player p = ThePlayer;
        if (p == null) return;
        if (index < 0 || index >= equipIds.Count) return;

        p.Equip(equipIds[index]);
        Refresh();
    }

    public void OnClickClose()
    {
        if (UIManager.instance != null) UIManager.instance.HideInventory();
        else gameObject.SetActive(false);
    }
}
