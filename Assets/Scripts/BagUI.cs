using UnityEngine;
using UnityEngine.UI;

// ═════════════════════════════════════════════════════════════════════════════
//  BagUI.cs  —  배낭(보관함) 화면
//
//  기획서 최종수정본 3-4
//    - 배낭은 4칸이다.
//    - 소모품과 장비를 같이 보관한다.
//    - 전투 중이 아닐 때 장비를 착용 / 교체할 수 있다.
//    - 전투 중이 아닐 때 아이템을 버릴 수 있다.
//
//  ── 화면 조작 ──
//    1) 배낭 칸 버튼을 눌러 칸을 고른다
//    2) 아래의 [사용/착용] 또는 [버리기] 버튼을 누른다
// ═════════════════════════════════════════════════════════════════════════════

public class BagUI : MonoBehaviour
{
    public static BagUI instance;

    [Header("배낭 칸 (4개를 순서대로 연결)")]
    public Button[] slotButtons = new Button[GameData.BAG_SIZE];
    public Text[] slotTexts = new Text[GameData.BAG_SIZE];

    [Header("아래쪽 버튼")]
    public Button useButton;    // 사용 / 착용
    public Text useButtonText;  // 그 버튼의 글자
    public Button dropButton;   // 버리기
    public Button closeButton;  // 닫기

    [Header("표시")]
    public Text infoText;       // 고른 칸의 설명
    public Text messageText;    // 안내 문구
    public Text weaponText;     // 착용 중인 무기
    public Text armorText;      // 착용 중인 갑옷

    /// <summary>지금 고른 배낭 칸. -1 이면 아무것도 안 고름.</summary>
    private int pickedSlot = -1;

    private void Awake()
    {
        instance = this;

        // 배낭 칸 버튼에 할 일을 연결한다.
        if (slotButtons != null)
        {
            for (int i = 0; i < slotButtons.Length; i++)
            {
                if (slotButtons[i] == null) continue;

                // ★ 반복문 변수를 그대로 쓰면 안 되므로 복사해서 쓴다.
                int 칸번호 = i;
                slotButtons[i].onClick.AddListener(delegate { PickSlot(칸번호); });
            }
        }

        if (useButton != null) useButton.onClick.AddListener(OnClickUse);
        if (dropButton != null) dropButton.onClick.AddListener(OnClickDrop);
        if (closeButton != null) closeButton.onClick.AddListener(OnClickClose);
    }

    private void OnEnable()
    {
        pickedSlot = -1;
        Refresh();
    }

    /// <summary>지금 전투 중인가. (전투 중에는 장비 교체와 버리기를 막는다)</summary>
    private bool IsInBattle()
    {
        if (BattleManager.instance == null) return false;
        return BattleManager.instance.battleOn;
    }

    private Player GetPlayer()
    {
        if (GameManager.instance == null) return null;
        return GameManager.instance.player;
    }


    // ═════════════════════════════════════════════════════════════
    //  화면 그리기
    // ═════════════════════════════════════════════════════════════

    public void Refresh()
    {
        Player 플레이어 = GetPlayer();
        if (플레이어 == null) return;

        // ── 배낭 4칸 글자 채우기
        for (int i = 0; i < GameData.BAG_SIZE; i++)
        {
            if (i < slotTexts.Length && slotTexts[i] != null)
            {
                string 글자 = 플레이어.GetSlotText(i);

                // 지금 고른 칸은 화살표로 표시한다.
                if (i == pickedSlot) 글자 = "▶ " + 글자;

                slotTexts[i].text = 글자;
            }

            if (i < slotButtons.Length && slotButtons[i] != null)
            {
                // 빈 칸은 고를 수 없다.
                slotButtons[i].interactable = (플레이어.bagKind[i] != GameData.BAG_EMPTY);
            }
        }

        // ── 착용 중인 장비
        if (weaponText != null)
        {
            EquipData 무기 = GameData.GetEquip(플레이어.weaponNum);
            if (무기 != null) weaponText.text = "무기 : " + 무기.name + " (" + 무기.info + ")";
            else weaponText.text = "무기 : 없음";
        }

        if (armorText != null)
        {
            EquipData 갑옷 = GameData.GetEquip(플레이어.armorNum);
            if (갑옷 != null) armorText.text = "갑옷 : " + 갑옷.name + " (" + 갑옷.info + ")";
            else armorText.text = "갑옷 : 없음";
        }

        RefreshButtons(플레이어);
    }

    /// <summary>고른 칸에 맞게 아래 버튼들의 글자와 켜짐 상태를 정한다.</summary>
    private void RefreshButtons(Player 플레이어)
    {
        bool 뭔가골랐나 = (pickedSlot >= 0 && 플레이어.bagKind[pickedSlot] != GameData.BAG_EMPTY);

        if (뭔가골랐나 == false)
        {
            if (useButtonText != null) useButtonText.text = "사용";
            if (useButton != null) useButton.interactable = false;
            if (dropButton != null) dropButton.interactable = false;
            if (infoText != null) infoText.text = "배낭 칸을 골라주세요.";
            return;
        }

        // ── 소모품을 골랐을 때
        if (플레이어.bagKind[pickedSlot] == GameData.BAG_ITEM)
        {
            ItemData 아이템 = GameData.GetItem(플레이어.bagNum[pickedSlot]);

            if (useButtonText != null) useButtonText.text = "사용";
            if (useButton != null) useButton.interactable = true;
            if (infoText != null && 아이템 != null) infoText.text = 아이템.name + " : " + 아이템.info;
        }
        // ── 장비를 골랐을 때
        else
        {
            EquipData 장비 = GameData.GetEquip(플레이어.bagNum[pickedSlot]);

            if (useButtonText != null) useButtonText.text = "착용";

            // 기획서 : 전투 중이 아닐 때만 착용/교체할 수 있다.
            if (useButton != null) useButton.interactable = (IsInBattle() == false);

            if (infoText != null && 장비 != null) infoText.text = 장비.name + " : " + 장비.info;
        }

        // 기획서 : 전투 중이 아닐 때만 버릴 수 있다.
        if (dropButton != null) dropButton.interactable = (IsInBattle() == false);
    }


    // ═════════════════════════════════════════════════════════════
    //  버튼 동작
    // ═════════════════════════════════════════════════════════════

    /// <summary>배낭 칸을 고른다.</summary>
    private void PickSlot(int slot)
    {
        pickedSlot = slot;

        if (AudioManager.instance != null) AudioManager.instance.PlayClick();
        Refresh();
    }

    /// <summary>[사용] 또는 [착용] 버튼.</summary>
    private void OnClickUse()
    {
        Player 플레이어 = GetPlayer();
        if (플레이어 == null) return;
        if (pickedSlot < 0) return;

        // ── 소모품이면 사용
        if (플레이어.bagKind[pickedSlot] == GameData.BAG_ITEM)
        {
            // 전투 중이라면 턴을 소모하며 써야 하므로 BattleManager 를 거친다.
            if (IsInBattle())
            {
                if (BattleManager.instance.waitInput == false)
                {
                    SetMessage("지금은 내 차례가 아닙니다.");
                    return;
                }

                BattleManager.instance.UseItem(pickedSlot);
            }
            else
            {
                플레이어.UseItem(pickedSlot);
            }

            SetMessage("아이템을 사용했습니다.");
        }
        // ── 장비면 착용
        else if (플레이어.bagKind[pickedSlot] == GameData.BAG_EQUIP)
        {
            if (IsInBattle())
            {
                SetMessage("전투 중에는 장비를 바꿀 수 없습니다.");
                return;
            }

            if (플레이어.EquipFromBag(pickedSlot)) SetMessage("장비를 착용했습니다.");
        }

        Refresh();
    }

    /// <summary>[버리기] 버튼.</summary>
    private void OnClickDrop()
    {
        Player 플레이어 = GetPlayer();
        if (플레이어 == null) return;
        if (pickedSlot < 0) return;

        if (IsInBattle())
        {
            SetMessage("전투 중에는 버릴 수 없습니다.");
            return;
        }

        if (플레이어.DropSlot(pickedSlot)) SetMessage("아이템을 버렸습니다.");

        Refresh();
    }

    /// <summary>[닫기] 버튼.</summary>
    private void OnClickClose()
    {
        if (UIManager.instance != null) UIManager.instance.CloseBag();
        else gameObject.SetActive(false);
    }

    private void SetMessage(string text)
    {
        if (messageText != null) messageText.text = text;
    }
}
