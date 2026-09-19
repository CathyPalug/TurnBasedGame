using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ═════════════════════════════════════════════════════════════════════════════
//  BattleUI.cs  —  JRPG 식 전투 화면 UI
//
//  ── 화면 구성 ──
//    위쪽   : 3D 몬스터들 (BattleManager 가 세운다)
//    가운데 : 적 정보 창 (이름 / 레벨 / HP / 방어 예고)
//    아래   : 전투 기록(로그) + 커맨드 창 [공격] [스킬] [아이템]
//
//  ── 목록 창 ──
//    스킬, 아이템, 적 고르기를 전부 "목록 창" 하나로 돌려쓴다.
//    버튼은 코드가 필요한 만큼 만들어 쓰기 때문에 Inspector 연결이 아주 적다.
//
//  ★ 어디서든 BattleUI.Log("글자") 로 전투 기록을 한 줄 남길 수 있다.
// ═════════════════════════════════════════════════════════════════════════════

public class BattleUI : MonoBehaviour
{
    public static BattleUI instance;

    // 목록 창이 지금 무엇을 보여주는지
    private const int LIST_NONE = 0;
    private const int LIST_SKILL = 1;
    private const int LIST_ITEM = 2;
    private const int LIST_TARGET = 3;

    [Header("커맨드 창")]
    public GameObject commandRoot;      // [공격][스킬][아이템] 이 들어있는 창
    public Button attackButton;
    public Button skillButton;
    public Button itemButton;

    [Header("목록 창 (스킬 / 아이템 / 적 고르기 공용)")]
    public GameObject listRoot;         // 목록 창 전체
    public Transform listContent;       // 버튼이 만들어질 자리
    public GameObject listButtonPrefab; // 목록 버튼 프리팹 (Button + 자식 Text)
    public Text listTitleText;          // "스킬 선택" 같은 제목
    public Button listCloseButton;      // 닫기 버튼

    [Header("적 정보 창")]
    public Transform enemyContent;      // 적 정보가 만들어질 자리
    public GameObject enemyInfoPrefab;  // 적 정보 한 줄 프리팹 (Text + Slider)

    [Header("전투 기록 (위에서 아래로, 맨 아래가 최신)")]
    public Text[] logTexts;

    // ── 코드가 만든 것을 기억해 두는 곳 ──
    private List<Button> listButtons = new List<Button>();
    private List<Text> listLabels = new List<Text>();
    private List<GameObject> enemyInfos = new List<GameObject>();
    private List<Text> enemyTexts = new List<Text>();
    private List<Slider> enemySliders = new List<Slider>();

    private int listMode = LIST_NONE;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        // 버튼을 눌렀을 때 할 일을 연결한다.
        if (attackButton != null) attackButton.onClick.AddListener(OnClickAttack);
        if (skillButton != null) skillButton.onClick.AddListener(OnClickSkill);
        if (itemButton != null) itemButton.onClick.AddListener(OnClickItem);
        if (listCloseButton != null) listCloseButton.onClick.AddListener(OnClickCloseList);

        CloseList();
        ShowCommand(false);
    }


    // ═════════════════════════════════════════════════════════════
    //  전투 기록 (로그)
    // ═════════════════════════════════════════════════════════════

    /// <summary>어디서든 부를 수 있는 기록 함수. BattleUI 가 없어도 에러가 안 난다.</summary>
    public static void Log(string text)
    {
        if (instance != null) instance.AddLog(text);
        else Debug.Log("[전투] " + text);
    }

    /// <summary>기록을 한 줄 추가한다. 기존 줄들은 한 칸씩 위로 밀린다.</summary>
    public void AddLog(string text)
    {
        if (logTexts == null || logTexts.Length == 0) return;

        // 위로 한 칸씩 밀기 : 0번 ← 1번, 1번 ← 2번 ...
        for (int i = 0; i < logTexts.Length - 1; i++)
        {
            if (logTexts[i] == null || logTexts[i + 1] == null) continue;
            logTexts[i].text = logTexts[i + 1].text;
        }

        // 맨 아래 줄에 새 기록을 넣는다.
        Text 마지막줄 = logTexts[logTexts.Length - 1];
        if (마지막줄 != null) 마지막줄.text = text;
    }

    /// <summary>기록을 전부 지운다.</summary>
    public void ClearLog()
    {
        if (logTexts == null) return;

        for (int i = 0; i < logTexts.Length; i++)
        {
            if (logTexts[i] != null) logTexts[i].text = "";
        }
    }


    // ═════════════════════════════════════════════════════════════
    //  BattleManager 가 부르는 함수들
    // ═════════════════════════════════════════════════════════════

    public void OnBattleStart()
    {
        ClearLog();
        CloseList();
        ShowCommand(false);
        RefreshEnemies();
    }

    public void OnBattleEnd()
    {
        CloseList();
        ShowCommand(false);
    }

    /// <summary>
    /// 커맨드 창의 버튼을 누를 수 있게/없게 만든다.
    /// ★ 창 자체는 계속 보여준다. 창이 사라졌다 나타나면 화면이 깜빡여서 보기 안 좋다.
    ///   내 차례가 아닐 때는 버튼만 회색으로 잠긴다.
    /// </summary>
    public void ShowCommand(bool on)
    {
        if (commandRoot != null && commandRoot.activeSelf == false) commandRoot.SetActive(true);

        if (attackButton != null) attackButton.interactable = on;
        if (skillButton != null) skillButton.interactable = on;
        if (itemButton != null) itemButton.interactable = on;

        if (on == false) CloseList();
    }

    /// <summary>적 고르기 목록을 켜거나 끈다.</summary>
    public void ShowTargetList(bool on)
    {
        if (on) BuildTargetList();
        else CloseList();
    }


    // ═════════════════════════════════════════════════════════════
    //  적 정보 창
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// 적들의 이름 / 레벨 / HP / 이번 턴 예고를 다시 그린다.
    /// 기획서 : 적이 '방어' 를 쓸 것과 '파괴 광선' 예고를 플레이어가 알 수 있어야 한다.
    /// </summary>
    public void RefreshEnemies()
    {
        BattleManager 전투 = BattleManager.instance;
        if (전투 == null) return;
        if (enemyContent == null || enemyInfoPrefab == null) return;

        // 필요한 만큼 정보 줄을 만들어 둔다.
        while (enemyInfos.Count < 전투.monsters.Length)
        {
            GameObject 줄 = Instantiate(enemyInfoPrefab, enemyContent);
            줄.name = "EnemyInfo " + enemyInfos.Count;

            enemyInfos.Add(줄);
            enemyTexts.Add(줄.GetComponentInChildren<Text>());
            enemySliders.Add(줄.GetComponentInChildren<Slider>());
        }

        // 한 줄씩 내용을 채운다.
        for (int i = 0; i < enemyInfos.Count; i++)
        {
            // 몬스터가 없는 줄은 숨긴다.
            bool 쓰는줄인가 = (i < 전투.monsters.Length && 전투.monsters[i] != null);

            enemyInfos[i].SetActive(쓰는줄인가);
            if (쓰는줄인가 == false) continue;

            Monster 몬스터 = 전투.monsters[i];

            // ── 이름과 상태 글자 만들기
            string 글자 = (i + 1) + ". " + 몬스터.unitName + " Lv." + 몬스터.level;

            if (몬스터.IsAlive() == false)
            {
                글자 = 글자 + "   [쓰러짐]";
            }
            else
            {
                글자 = 글자 + "   HP " + 몬스터.hp + " / " + 몬스터.maxHp;

                // 이번 턴 예고 (기획서 필수 사항)
                // ★ 이모지(🛡 ⚠)는 한글 폰트에 그림이 없어서 화면에 안 나온다.
                //   그래서 [ ] 로 감싼 글자로 표시한다.
                string 예고 = 몬스터.GetWarningSkillName();

                if (예고 != "") 글자 = 글자 + "   ◆[" + 예고 + " 준비!]";
                else if (몬스터.WillDefend()) 글자 = 글자 + "   ■[방어]";
            }

            if (enemyTexts[i] != null) enemyTexts[i].text = 글자;

            // ── HP 바
            if (enemySliders[i] != null)
            {
                enemySliders[i].minValue = 0f;
                enemySliders[i].maxValue = 1f;
                enemySliders[i].value = 몬스터.GetHpRate();
            }
        }
    }


    // ═════════════════════════════════════════════════════════════
    //  커맨드 버튼
    // ═════════════════════════════════════════════════════════════

    private void OnClickAttack()
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayClick();

        CloseList();
        if (BattleManager.instance != null) BattleManager.instance.StartAttack();
    }

    private void OnClickSkill()
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayClick();

        // 이미 스킬 목록이 열려 있으면 닫는다. (같은 버튼을 두 번 누르면 닫힘)
        if (listMode == LIST_SKILL) { CloseList(); return; }

        Player 플레이어 = GetPlayer();
        if (플레이어 != null && 플레이어.IsSkillSealed())
        {
            Log("스킬이 봉인되어 쓸 수 없다!");
            return;
        }

        BuildSkillList();
    }

    private void OnClickItem()
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayClick();

        if (listMode == LIST_ITEM) { CloseList(); return; }

        BuildItemList();
    }


    // ═════════════════════════════════════════════════════════════
    //  목록 창 만들기
    // ═════════════════════════════════════════════════════════════

    /// <summary>목록 버튼을 필요한 개수만큼 만들어 둔다.</summary>
    private void MakeListButtons(int need)
    {
        if (listContent == null || listButtonPrefab == null) return;

        while (listButtons.Count < need)
        {
            GameObject 오브젝트 = Instantiate(listButtonPrefab, listContent);
            오브젝트.name = "ListButton " + listButtons.Count;

            // 목록 창 너비에 맞춘다.
            RectTransform 크기 = 오브젝트.GetComponent<RectTransform>();
            크기.sizeDelta = new Vector2(600f, 50f);

            Button 버튼 = 오브젝트.GetComponent<Button>();
            if (버튼 == null) 버튼 = 오브젝트.GetComponentInChildren<Button>();

            Text 글자 = 오브젝트.GetComponentInChildren<Text>();

            // ★ for/while 변수를 그대로 쓰면 안 되므로 복사해서 쓴다.
            int 칸번호 = listButtons.Count;
            if (버튼 != null)
            {
                버튼.onClick.AddListener(delegate { OnClickListButton(칸번호); });
            }

            listButtons.Add(버튼);
            listLabels.Add(글자);
        }

        // 필요 없는 버튼은 숨긴다.
        for (int i = 0; i < listButtons.Count; i++)
        {
            if (listButtons[i] == null) continue;
            listButtons[i].gameObject.SetActive(i < need);
        }
    }

    /// <summary>목록 버튼 한 칸의 글자와 누를 수 있는지를 정한다.</summary>
    private void SetListButton(int index, string text, bool canClick)
    {
        if (index < 0 || index >= listButtons.Count) return;

        if (listLabels[index] != null) listLabels[index].text = text;
        if (listButtons[index] != null) listButtons[index].interactable = canClick;
    }

    /// <summary>스킬 목록을 만든다.</summary>
    private void BuildSkillList()
    {
        Player 플레이어 = GetPlayer();
        if (플레이어 == null) return;

        listMode = LIST_SKILL;
        if (listRoot != null) listRoot.SetActive(true);
        if (listTitleText != null) listTitleText.text = "스킬 선택";

        MakeListButtons(GameData.Skills.Length);

        for (int i = 0; i < GameData.Skills.Length; i++)
        {
            SkillData 스킬 = GameData.Skills[i];

            // 아직 안 배운 스킬은 회색으로 "???" 만 보여준다.
            if (플레이어.hasSkill[i] == false)
            {
                SetListButton(i, "??? (미습득)", false);
                continue;
            }

            string 막는이유 = 플레이어.GetSkillBlockReason(i);
            bool 쓸수있나 = (막는이유 == "");

            string 글자 = 스킬.name + "   MP " + 스킬.mpCost;
            if (쓸수있나 == false) 글자 = 글자 + "   [" + 막는이유 + "]";

            SetListButton(i, 글자, 쓸수있나);
        }
    }

    /// <summary>아이템 목록(배낭)을 만든다.</summary>
    private void BuildItemList()
    {
        Player 플레이어 = GetPlayer();
        if (플레이어 == null) return;

        listMode = LIST_ITEM;
        if (listRoot != null) listRoot.SetActive(true);
        if (listTitleText != null) listTitleText.text = "아이템 선택";

        MakeListButtons(GameData.BAG_SIZE);

        for (int i = 0; i < GameData.BAG_SIZE; i++)
        {
            // 전투 중에는 소모품만 쓸 수 있다. (장비는 전투 중 교체 불가 — 기획서)
            bool 쓸수있나 = (플레이어.bagKind[i] == GameData.BAG_ITEM);

            string 글자 = 플레이어.GetSlotText(i);
            if (플레이어.bagKind[i] == GameData.BAG_EQUIP)
            {
                글자 = 글자 + "   [전투 중 교체 불가]";
            }

            SetListButton(i, 글자, 쓸수있나);
        }
    }

    /// <summary>적 고르기 목록을 만든다.</summary>
    private void BuildTargetList()
    {
        BattleManager 전투 = BattleManager.instance;
        if (전투 == null) return;

        listMode = LIST_TARGET;
        if (listRoot != null) listRoot.SetActive(true);
        if (listTitleText != null) listTitleText.text = "대상 선택";

        MakeListButtons(전투.monsters.Length);

        for (int i = 0; i < 전투.monsters.Length; i++)
        {
            Monster 몬스터 = 전투.monsters[i];

            if (몬스터 == null)
            {
                SetListButton(i, "-", false);
                continue;
            }

            // 죽은 적은 고를 수 없다.
            if (몬스터.IsAlive() == false)
            {
                SetListButton(i, 몬스터.unitName + " (쓰러짐)", false);
                continue;
            }

            string 글자 = (i + 1) + ". " + 몬스터.unitName
                        + "  HP " + 몬스터.hp + "/" + 몬스터.maxHp;

            SetListButton(i, 글자, true);
        }
    }

    /// <summary>목록에서 버튼 하나를 눌렀을 때.</summary>
    private void OnClickListButton(int index)
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayClick();

        BattleManager 전투 = BattleManager.instance;
        if (전투 == null) return;

        if (listMode == LIST_SKILL)
        {
            CloseList();
            전투.StartSkill(index);
        }
        else if (listMode == LIST_ITEM)
        {
            CloseList();
            전투.UseItem(index);
        }
        else if (listMode == LIST_TARGET)
        {
            CloseList();
            전투.PickTarget(index);
        }
    }

    /// <summary>
    /// 목록 창을 그냥 닫기만 한다.
    /// ★ 여기서 BattleManager.CancelPick() 을 부르면 안 된다.
    ///   CancelPick 이 다시 이 함수를 부르기 때문에 무한 반복에 빠진다.
    /// </summary>
    public void CloseList()
    {
        listMode = LIST_NONE;
        if (listRoot != null) listRoot.SetActive(false);
    }

    /// <summary>목록 창의 [닫기] 버튼을 눌렀을 때.</summary>
    private void OnClickCloseList()
    {
        // 적을 고르는 중이었다면 고르기 자체를 취소해야 한다.
        // (CancelPick 안에서 목록 창도 같이 닫아준다)
        if (listMode == LIST_TARGET && BattleManager.instance != null)
        {
            listMode = LIST_NONE;
            BattleManager.instance.CancelPick();
            return;
        }

        CloseList();
    }


    // ═════════════════════════════════════════════════════════════

    /// <summary>플레이어를 가져온다. 없으면 null.</summary>
    private Player GetPlayer()
    {
        if (BattleManager.instance == null) return null;
        return BattleManager.instance.player;
    }
}
