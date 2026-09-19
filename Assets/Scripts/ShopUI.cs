using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ═════════════════════════════════════════════════════════════════════════════
//  ShopUI.cs  —  상점 및 휴식 화면
//
//  기획서 4-2 : 상점에서 아이템이나 스킬북을 구매하거나
//               휴식(체력 30% 회복)을 할 수 있다.
//  기획서 3-1 : 구입한 아이템은 배낭으로 들어간다.
//               배낭이 가득 차 있다면 구매가 불가능하다.
//
//  ★ 파는 물건 목록은 코드가 자동으로 만든다.
//    장비 → 스킬북 → 소모품 순서로 줄을 세운다.
// ═════════════════════════════════════════════════════════════════════════════

public class ShopUI : MonoBehaviour
{
    public static ShopUI instance;

    // 파는 물건의 종류
    private const int SELL_EQUIP = 0;   // 장비
    private const int SELL_SKILL = 1;   // 스킬북
    private const int SELL_ITEM = 2;    // 소모품

    [Header("목록")]
    [Tooltip("물건 버튼이 만들어질 자리")]
    public Transform listContent;

    [Tooltip("물건 한 줄로 쓸 버튼 프리팹 (Button + 자식 Text)")]
    public GameObject listButtonPrefab;

    [Header("표시")]
    public Text goldText;       // 보유 골드
    public Text messageText;    // 안내 문구
    public Text bagText;        // 배낭 남은 칸

    [Header("버튼")]
    public Button restButton;   // 휴식
    public Button bagButton;    // 배낭 열기
    public Button leaveButton;  // 나가기

    [Header("휴식")]
    [Tooltip("한 번 방문할 때 쉴 수 있는 횟수")]
    public int restPerVisit = 1;

    // ── 파는 물건 목록을 기억해 두는 배열 3개 ──
    // (클래스를 만들지 않고 배열 3개로 관리해서 더 간단하게 했다)
    private List<int> sellKind = new List<int>();   // 종류 (장비/스킬북/소모품)
    private List<int> sellNum = new List<int>();    // 번호
    private List<int> sellPrice = new List<int>();  // 가격

    private List<Button> listButtons = new List<Button>();
    private List<Text> listLabels = new List<Text>();

    private int restUsed;   // 이번 방문에 몇 번 쉬었나

    private void Awake()
    {
        instance = this;

        if (restButton != null) restButton.onClick.AddListener(OnClickRest);
        if (leaveButton != null) leaveButton.onClick.AddListener(OnClickLeave);
        if (bagButton != null) bagButton.onClick.AddListener(OnClickBag);
    }

    private void OnEnable()
    {
        restUsed = 0;       // 방에 새로 들어올 때마다 휴식 횟수를 되돌린다
        SetMessage("무엇을 도와드릴까요?");
        Refresh();
    }

    private Player GetPlayer()
    {
        if (GameManager.instance == null) return null;
        return GameManager.instance.player;
    }


    // ═════════════════════════════════════════════════════════════
    //  목록 만들기
    // ═════════════════════════════════════════════════════════════

    public void Refresh()
    {
        Player 플레이어 = GetPlayer();
        if (플레이어 == null) return;

        BuildSellList(플레이어);
        MakeButtons(sellKind.Count);

        for (int i = 0; i < listButtons.Count; i++)
        {
            bool 쓰는줄인가 = (i < sellKind.Count);

            if (listButtons[i] != null) listButtons[i].gameObject.SetActive(쓰는줄인가);
            if (쓰는줄인가 == false) continue;

            // 이 물건을 살 수 있는지 확인하고, 못 사면 이유를 붙인다.
            string 못사는이유 = GetBlockReason(플레이어, i);
            bool 살수있나 = (못사는이유 == "");

            string 글자 = GetItemLabel(i) + "   -   " + sellPrice[i] + " G";
            if (살수있나 == false) 글자 = 글자 + "   [" + 못사는이유 + "]";

            if (listLabels[i] != null) listLabels[i].text = 글자;
            if (listButtons[i] != null) listButtons[i].interactable = 살수있나;
        }

        // 보유 골드
        if (goldText != null) goldText.text = "보유 골드 : " + 플레이어.gold + " G";

        // 배낭 남은 칸
        if (bagText != null)
        {
            int 빈칸수 = 0;
            for (int i = 0; i < GameData.BAG_SIZE; i++)
            {
                if (플레이어.bagKind[i] == GameData.BAG_EMPTY) 빈칸수 = 빈칸수 + 1;
            }
            bagText.text = "배낭 : " + (GameData.BAG_SIZE - 빈칸수) + " / " + GameData.BAG_SIZE;
        }

        // 휴식 버튼
        if (restButton != null) restButton.interactable = (restUsed < restPerVisit);
    }

    /// <summary>파는 물건 목록을 만든다.</summary>
    private void BuildSellList(Player 플레이어)
    {
        sellKind.Clear();
        sellNum.Clear();
        sellPrice.Clear();

        // ── 1) 장비
        for (int i = 0; i < GameData.Equips.Length; i++)
        {
            EquipData 장비 = GameData.Equips[i];

            if (장비.price <= 0) continue;              // 시작 장비는 안 판다
            if (HasEquipAlready(플레이어, i)) continue;  // 이미 가지고 있으면 안 판다

            sellKind.Add(SELL_EQUIP);
            sellNum.Add(i);
            sellPrice.Add(장비.price);
        }

        // ── 2) 스킬북
        for (int i = 0; i < GameData.Skills.Length; i++)
        {
            SkillData 스킬 = GameData.Skills[i];

            if (스킬.getType != GetSkillType.Shop) continue;   // 레벨로 배우는 스킬은 안 판다
            if (플레이어.hasSkill[i]) continue;                 // 이미 배웠으면 안 판다

            sellKind.Add(SELL_SKILL);
            sellNum.Add(i);
            sellPrice.Add(스킬.price);
        }

        // ── 3) 소모품 (항상 판다)
        for (int i = 0; i < GameData.Items.Length; i++)
        {
            sellKind.Add(SELL_ITEM);
            sellNum.Add(i);
            sellPrice.Add(GameData.Items[i].price);
        }
    }

    /// <summary>이 장비를 이미 착용 중이거나 배낭에 가지고 있는가.</summary>
    private bool HasEquipAlready(Player 플레이어, int equipNum)
    {
        if (플레이어.weaponNum == equipNum) return true;
        if (플레이어.armorNum == equipNum) return true;

        for (int i = 0; i < GameData.BAG_SIZE; i++)
        {
            if (플레이어.bagKind[i] != GameData.BAG_EQUIP) continue;
            if (플레이어.bagNum[i] == equipNum) return true;
        }

        return false;
    }

    /// <summary>목록 한 줄에 보여줄 이름을 만든다.</summary>
    private string GetItemLabel(int index)
    {
        int 번호 = sellNum[index];

        if (sellKind[index] == SELL_EQUIP)
        {
            EquipData 장비 = GameData.GetEquip(번호);
            if (장비 == null) return "?";

            string 종류 = "갑옷";
            if (장비.type == EquipType.Weapon) 종류 = "무기";

            return "[" + 종류 + "] " + 장비.name + " (" + 장비.info + ")";
        }

        if (sellKind[index] == SELL_SKILL)
        {
            SkillData 스킬 = GameData.GetSkill(번호);
            if (스킬 == null) return "?";

            return "[스킬북] " + 스킬.name + " (MP " + 스킬.mpCost + ")";
        }

        // 소모품
        ItemData 아이템 = GameData.GetItem(번호);
        if (아이템 == null) return "?";

        Player 플레이어 = GetPlayer();
        int 가진개수 = 0;
        if (플레이어 != null) 가진개수 = 플레이어.CountItem(번호);

        return "[아이템] " + 아이템.name + " (" + 아이템.info + ") "
             + 가진개수 + "/" + 아이템.maxCount;
    }

    /// <summary>이 물건을 못 사는 이유. 살 수 있으면 빈 글자("").</summary>
    private string GetBlockReason(Player 플레이어, int index)
    {
        // 돈이 모자라면 무조건 못 산다.
        if (플레이어.gold < sellPrice[index]) return "골드 부족";

        // 스킬북은 배낭 칸이 필요 없다. (배우면 끝)
        if (sellKind[index] == SELL_SKILL) return "";

        // 장비는 반드시 빈 칸이 하나 필요하다.
        if (sellKind[index] == SELL_EQUIP)
        {
            if (플레이어.HasEmptySlot() == false) return "배낭 가득";
            return "";
        }

        // 소모품
        int 번호 = sellNum[index];
        ItemData 아이템 = GameData.GetItem(번호);
        if (아이템 == null) return "오류";

        // 기획서 : 최대 누적 개수를 넘으면 획득하지 못한다.
        if (플레이어.CountItem(번호) >= 아이템.maxCount) return "최대 개수";

        // 이미 가지고 있는 아이템이면 그 칸에 쌓으므로 빈 칸이 필요 없다.
        if (플레이어.CountItem(번호) > 0) return "";

        // 처음 사는 아이템이면 빈 칸이 필요하다.
        if (플레이어.HasEmptySlot() == false) return "배낭 가득";

        return "";
    }

    /// <summary>목록 버튼을 필요한 개수만큼 만들어 둔다.</summary>
    private void MakeButtons(int need)
    {
        if (listContent == null || listButtonPrefab == null) return;

        while (listButtons.Count < need)
        {
            GameObject 오브젝트 = Instantiate(listButtonPrefab, listContent);
            오브젝트.name = "ShopItem " + listButtons.Count;

            // 상점은 글이 길어서 버튼을 넓게 만든다.
            RectTransform 크기 = 오브젝트.GetComponent<RectTransform>();
            크기.sizeDelta = new Vector2(950f, 50f);

            Button 버튼 = 오브젝트.GetComponent<Button>();
            if (버튼 == null) 버튼 = 오브젝트.GetComponentInChildren<Button>();

            Text 글자 = 오브젝트.GetComponentInChildren<Text>();

            // ★ 반복문 변수를 그대로 쓰면 안 되므로 복사해서 쓴다.
            int 줄번호 = listButtons.Count;
            if (버튼 != null)
            {
                버튼.onClick.AddListener(delegate { OnClickBuy(줄번호); });
            }

            listButtons.Add(버튼);
            listLabels.Add(글자);
        }
    }


    // ═════════════════════════════════════════════════════════════
    //  구매
    // ═════════════════════════════════════════════════════════════

    private void OnClickBuy(int index)
    {
        Player 플레이어 = GetPlayer();
        if (플레이어 == null) return;
        if (index < 0 || index >= sellKind.Count) return;

        // 한 번 더 확인한다. (버튼을 누르는 사이에 상황이 바뀌었을 수도 있다)
        string 못사는이유 = GetBlockReason(플레이어, index);
        if (못사는이유 != "")
        {
            SetMessage("구매 불가 : " + 못사는이유);
            return;
        }

        int 번호 = sellNum[index];
        int 가격 = sellPrice[index];

        if (sellKind[index] == SELL_EQUIP)
        {
            EquipData 장비 = GameData.GetEquip(번호);

            // 기획서 : 산 장비는 배낭으로 들어간다.
            if (플레이어.AddEquipToBag(번호) == false)
            {
                SetMessage("배낭이 가득 차서 살 수 없습니다.");
                return;
            }

            플레이어.gold = 플레이어.gold - 가격;
            SetMessage(장비.name + " 구매! 배낭에서 착용할 수 있습니다.");
        }
        else if (sellKind[index] == SELL_SKILL)
        {
            SkillData 스킬 = GameData.GetSkill(번호);

            플레이어.gold = 플레이어.gold - 가격;
            플레이어.hasSkill[번호] = true;

            SetMessage("스킬북 [" + 스킬.name + "] 습득!");
        }
        else
        {
            ItemData 아이템 = GameData.GetItem(번호);

            if (플레이어.AddItemToBag(번호) == false)
            {
                SetMessage("배낭이 가득 찼거나 최대 개수입니다.");
                return;
            }

            플레이어.gold = 플레이어.gold - 가격;
            SetMessage(아이템.name + " 구매!");
        }

        if (AudioManager.instance != null) AudioManager.instance.PlayClick();

        Refresh();
        if (BagUI.instance != null) BagUI.instance.Refresh();
    }


    // ═════════════════════════════════════════════════════════════
    //  휴식 / 배낭 / 나가기
    // ═════════════════════════════════════════════════════════════

    /// <summary>휴식 : 최대 체력의 30% 회복. (기획서 4-2)</summary>
    public void OnClickRest()
    {
        Player 플레이어 = GetPlayer();
        if (플레이어 == null) return;

        if (restUsed >= restPerVisit)
        {
            SetMessage("이미 쉬었습니다.");
            return;
        }

        restUsed = restUsed + 1;

        int 회복 = 플레이어.Rest();
        SetMessage("휴식! HP " + 회복 + " 회복  (" + 플레이어.hp + " / " + 플레이어.maxHp + ")");

        if (AudioManager.instance != null) AudioManager.instance.PlayHeal();
        Refresh();
    }

    /// <summary>배낭 열기.</summary>
    public void OnClickBag()
    {
        if (UIManager.instance != null) UIManager.instance.ToggleBag();
    }

    /// <summary>나가기 : 이 방을 클리어 처리하고 지도로 돌아간다.</summary>
    public void OnClickLeave()
    {
        if (StageManager.instance != null) StageManager.instance.LeaveShop();
    }

    private void SetMessage(string text)
    {
        if (messageText != null) messageText.text = text;
    }
}
