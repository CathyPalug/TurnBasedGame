#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 기획서 요구사항 중 씬에 없던 패널(상점 / 배낭 / 일시정지 / 결과 / 닉네임 입력)을
/// 기존 UI와 같은 톤으로 만들어 주는 에디터 도구.
/// 메뉴 : Tools/TurnBasedGame/Build Missing Panels
/// </summary>
public static class GameUIBuilder
{
    private static Font Font
    {
        get { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
    }

    private static Sprite UiSprite
    {
        get { return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); }
    }

    private static readonly Color PanelBg = new Color(0.04f, 0.03f, 0.05f, 1f);
    private static readonly Color BoxBg = new Color(0.11f, 0.09f, 0.12f, 0.98f);
    private static readonly Color Accent = new Color(0.82f, 0.16f, 0.16f);
    private static readonly Color TextColor = new Color(0.93f, 0.90f, 0.85f);

    // ─────────────────────────────────────────────────────────────
    // 기본 생성 헬퍼
    // ─────────────────────────────────────────────────────────────
    public static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        return (RectTransform)go.transform;
    }

    public static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    public static Image AddImage(RectTransform rt, Color color, bool raycast)
    {
        // Graphic 은 한 오브젝트에 하나만 붙일 수 있으므로 이미 있으면 재사용한다.
        Image img = rt.GetComponent<Image>();
        if (img == null) img = rt.gameObject.AddComponent<Image>();
        img.sprite = UiSprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    public static Text MakeText(string name, Transform parent, string content, int size,
                                TextAnchor anchor, Color color)
    {
        RectTransform rt = NewRect(name, parent);
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = Font;
        t.fontSize = size;
        t.alignment = anchor;
        t.color = color;
        t.text = content;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    public static Button MakeButton(string name, Transform parent, string label, int fontSize,
                                    Vector2 size, out Text labelText)
    {
        RectTransform rt = NewRect(name, parent);
        rt.sizeDelta = size;

        Image img = AddImage(rt, BoxBg, true);

        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;

        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.3f, 1.1f, 1.1f, 1f);
        cb.pressedColor = new Color(0.7f, 0.6f, 0.6f, 1f);
        cb.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
        b.colors = cb;

        labelText = MakeText("Label", rt, label, fontSize, TextAnchor.MiddleCenter, TextColor);
        Stretch((RectTransform)labelText.transform, 8f, 2f, 8f, 2f);

        // 테두리
        RectTransform border = NewRect("Border", rt);
        Stretch(border, 0f, 0f, 0f, 0f);
        Image bi = AddImage(border, new Color(0.45f, 0.35f, 0.3f, 0.9f), false);
        bi.type = Image.Type.Sliced;
        border.SetAsFirstSibling();

        return b;
    }

    /// <summary>전체 화면을 덮는 패널을 만든다.</summary>
    public static RectTransform MakePanel(string name, Transform canvas, bool fullScreen)
    {
        Transform existing = canvas.Find(name);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        RectTransform rt = NewRect(name, canvas);
        if (fullScreen) Stretch(rt, 0f, 0f, 0f, 0f);
        AddImage(rt, PanelBg, true);
        return rt;
    }

    /// <summary>스크롤 뷰를 만들고 Content 를 돌려준다.</summary>
    public static RectTransform MakeScrollView(string name, Transform parent, Vector2 anchoredPos, Vector2 size,
                                               out ScrollRect scrollRect)
    {
        RectTransform root = NewRect(name, parent);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = anchoredPos;
        root.sizeDelta = size;
        AddImage(root, new Color(0f, 0f, 0f, 0.45f), true);

        scrollRect = root.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        RectTransform viewport = NewRect("Viewport", root);
        Stretch(viewport, 4f, 4f, 4f, 4f);
        Image vpImg = AddImage(viewport, new Color(1f, 1f, 1f, 0.01f), true);
        vpImg.type = Image.Type.Sliced;
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        RectTransform content = NewRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = content;

        return content;
    }

    // ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/TurnBasedGame/Build Missing Panels")]
    public static void BuildAll()
    {
        Transform canvas = GameObject.Find("Canvas").transform;

        GameObject shop = BuildShop(canvas);
        GameObject inventory = BuildInventory(canvas);
        GameObject pause = BuildPause(canvas);
        GameObject result = BuildResult(canvas);
        GameObject nickname = BuildNickname(canvas);

        UIManager uim = Object.FindFirstObjectByType<UIManager>();
        if (uim != null)
        {
            uim.shopPanel = shop;
            uim.inventoryPanel = inventory;
            uim.pausePanel = pause;
            uim.resultPanel = result;
            uim.nicknamePanel = nickname;
            EditorUtility.SetDirty(uim);
        }

        shop.SetActive(false);
        inventory.SetActive(false);
        pause.SetActive(false);
        result.SetActive(false);
        nickname.SetActive(false);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("[GameUIBuilder] 누락 패널 생성 완료");
    }

    // ── 상점 ──────────────────────────────────────────────────────
    public static GameObject BuildShop(Transform canvas)
    {
        RectTransform panel = MakePanel("Shop", canvas, true);

        Text title = MakeText("Title", panel, "상점 & 휴식", 40, TextAnchor.MiddleCenter, Accent);
        RectTransform trt = (RectTransform)title.transform;
        trt.anchorMin = new Vector2(0.5f, 1f); trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -30f); trt.sizeDelta = new Vector2(800f, 80f);

        Text gold = MakeText("GoldText", panel, "보유 골드 : 0 G", 27, TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.35f));
        RectTransform grt = (RectTransform)gold.transform;
        grt.anchorMin = new Vector2(1f, 1f); grt.anchorMax = new Vector2(1f, 1f);
        grt.pivot = new Vector2(1f, 1f);
        grt.anchoredPosition = new Vector2(-60f, -40f); grt.sizeDelta = new Vector2(500f, 60f);

        ScrollRect sr;
        RectTransform content = MakeScrollView("Scroll View", panel, new Vector2(0f, 20f), new Vector2(1200f, 620f), out sr);

        Text msg = MakeText("MessageText", panel, "", 24, TextAnchor.MiddleCenter, new Color(0.6f, 1f, 0.7f));
        RectTransform mrt = (RectTransform)msg.transform;
        mrt.anchorMin = new Vector2(0.5f, 0f); mrt.anchorMax = new Vector2(0.5f, 0f);
        mrt.pivot = new Vector2(0.5f, 0f);
        mrt.anchoredPosition = new Vector2(0f, 150f); mrt.sizeDelta = new Vector2(1200f, 50f);

        Text tmp;
        Button rest = MakeButton("RestButton", panel, "휴식 (HP 30% 회복)", 24, new Vector2(420f, 80f), out tmp);
        RectTransform rrt = (RectTransform)rest.transform;
        rrt.anchorMin = new Vector2(0.5f, 0f); rrt.anchorMax = new Vector2(0.5f, 0f);
        rrt.pivot = new Vector2(0.5f, 0f);
        rrt.anchoredPosition = new Vector2(-230f, 50f);

        Button leave = MakeButton("LeaveButton", panel, "나가기", 24, new Vector2(420f, 80f), out tmp);
        RectTransform lrt = (RectTransform)leave.transform;
        lrt.anchorMin = new Vector2(0.5f, 0f); lrt.anchorMax = new Vector2(0.5f, 0f);
        lrt.pivot = new Vector2(0.5f, 0f);
        lrt.anchoredPosition = new Vector2(230f, 50f);

        ShopUI ui = panel.gameObject.AddComponent<ShopUI>();
        ui.contentRoot = content;
        ui.itemButtonPrefab = MakeListEntryPrefab();
        ui.goldText = gold;
        ui.messageText = msg;
        ui.restButton = rest;
        ui.leaveButton = leave;

        return panel.gameObject;
    }

    /// <summary>목록 한 줄로 쓸 간단한 버튼 프리팹을 만들어 둔다.</summary>
    public static GameObject MakeListEntryPrefab()
    {
        const string path = "Assets/Prefabs/ListEntryButton.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject temp = new GameObject("ListEntryButton", typeof(RectTransform));
        RectTransform rt = (RectTransform)temp.transform;
        rt.sizeDelta = new Vector2(1100f, 62f);

        LayoutElement le = temp.AddComponent<LayoutElement>();
        le.minHeight = 62f;
        le.preferredHeight = 62f;

        Image img = temp.AddComponent<Image>();
        img.sprite = UiSprite;
        img.type = Image.Type.Sliced;
        img.color = BoxBg;

        Button b = temp.AddComponent<Button>();
        b.targetGraphic = img;
        ColorBlock cb = b.colors;
        cb.highlightedColor = new Color(1.35f, 1.1f, 1.05f, 1f);
        cb.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.55f);
        b.colors = cb;

        Text label = MakeText("Label", temp.transform, "항목", 22, TextAnchor.MiddleLeft, TextColor);
        Stretch((RectTransform)label.transform, 18f, 2f, 18f, 2f);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);
        return saved;
    }

    // ── 배낭 ──────────────────────────────────────────────────────
    public static GameObject BuildInventory(Transform canvas)
    {
        RectTransform panel = MakePanel("Inventory", canvas, false);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(1240f, 760f);

        Text title = MakeText("Title", panel, "배낭 & 장비", 34, TextAnchor.MiddleCenter, Accent);
        RectTransform trt = (RectTransform)title.transform;
        trt.anchorMin = new Vector2(0.5f, 1f); trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -20f); trt.sizeDelta = new Vector2(700f, 70f);

        // 배낭 6칸
        Button[] slots = new Button[GameData.InventorySize];
        Text[] labels = new Text[GameData.InventorySize];
        for (int i = 0; i < GameData.InventorySize; i++)
        {
            Text lbl;
            Button b = MakeButton("Slot " + i, panel, "빈 칸", 19, new Vector2(180f, 140f), out lbl);
            RectTransform brt = (RectTransform)b.transform;
            brt.anchorMin = new Vector2(0f, 1f); brt.anchorMax = new Vector2(0f, 1f);
            brt.pivot = new Vector2(0f, 1f);
            brt.anchoredPosition = new Vector2(40f + (i % 3) * 200f, -110f - (i / 3) * 160f);
            slots[i] = b;
            labels[i] = lbl;
        }

        Text weapon = MakeText("WeaponText", panel, "무기 : -", 24, TextAnchor.MiddleLeft, TextColor);
        RectTransform wrt = (RectTransform)weapon.transform;
        wrt.anchorMin = new Vector2(0f, 1f); wrt.anchorMax = new Vector2(0f, 1f);
        wrt.pivot = new Vector2(0f, 1f);
        wrt.anchoredPosition = new Vector2(40f, -450f); wrt.sizeDelta = new Vector2(560f, 50f);

        Text armor = MakeText("ArmorText", panel, "갑옷 : -", 24, TextAnchor.MiddleLeft, TextColor);
        RectTransform art = (RectTransform)armor.transform;
        art.anchorMin = new Vector2(0f, 1f); art.anchorMax = new Vector2(0f, 1f);
        art.pivot = new Vector2(0f, 1f);
        art.anchoredPosition = new Vector2(40f, -505f); art.sizeDelta = new Vector2(560f, 50f);

        Text stat = MakeText("StatText", panel, "", 22, TextAnchor.UpperLeft, new Color(0.75f, 0.9f, 1f));
        RectTransform strt = (RectTransform)stat.transform;
        strt.anchorMin = new Vector2(0f, 1f); strt.anchorMax = new Vector2(0f, 1f);
        strt.pivot = new Vector2(0f, 1f);
        strt.anchoredPosition = new Vector2(40f, -570f); strt.sizeDelta = new Vector2(560f, 100f);

        Text eqTitle = MakeText("EquipTitle", panel, "보유 장비 (클릭해서 착용)", 22, TextAnchor.MiddleCenter, TextColor);
        RectTransform ert = (RectTransform)eqTitle.transform;
        ert.anchorMin = new Vector2(1f, 1f); ert.anchorMax = new Vector2(1f, 1f);
        ert.pivot = new Vector2(1f, 1f);
        ert.anchoredPosition = new Vector2(-40f, -105f); ert.sizeDelta = new Vector2(560f, 46f);

        ScrollRect sr;
        RectTransform content = MakeScrollView("EquipScroll", panel, new Vector2(320f, -40f), new Vector2(560f, 500f), out sr);

        Text tmp;
        Button close = MakeButton("CloseButton", panel, "닫기", 24, new Vector2(300f, 74f), out tmp);
        RectTransform crt = (RectTransform)close.transform;
        crt.anchorMin = new Vector2(0.5f, 0f); crt.anchorMax = new Vector2(0.5f, 0f);
        crt.pivot = new Vector2(0.5f, 0f);
        crt.anchoredPosition = new Vector2(0f, 24f);

        InventoryUI ui = panel.gameObject.AddComponent<InventoryUI>();
        ui.slotButtons = slots;
        ui.slotLabels = labels;
        ui.weaponText = weapon;
        ui.armorText = armor;
        ui.statText = stat;
        ui.equipListRoot = content;
        ui.equipButtonPrefab = MakeListEntryPrefab();
        ui.closeButton = close;

        return panel.gameObject;
    }

    // ── 일시정지 ──────────────────────────────────────────────────
    public static GameObject BuildPause(Transform canvas)
    {
        RectTransform panel = MakePanel("Pause", canvas, true);
        AddImage(panel, new Color(0.03f, 0.025f, 0.04f, 1f), true);

        Text title = MakeText("Title", panel, "일시정지", 46, TextAnchor.MiddleCenter, Accent);
        RectTransform trt = (RectTransform)title.transform;
        trt.anchorMin = new Vector2(0.5f, 0.5f); trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.pivot = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = new Vector2(0f, 220f); trt.sizeDelta = new Vector2(700f, 100f);

        Text hint = MakeText("Hint", panel, "ESC 를 다시 누르면 계속합니다", 22, TextAnchor.MiddleCenter, TextColor);
        RectTransform hrt = (RectTransform)hint.transform;
        hrt.anchorMin = new Vector2(0.5f, 0.5f); hrt.anchorMax = new Vector2(0.5f, 0.5f);
        hrt.pivot = new Vector2(0.5f, 0.5f);
        hrt.anchoredPosition = new Vector2(0f, 150f); hrt.sizeDelta = new Vector2(900f, 50f);

        UIManager uim = Object.FindFirstObjectByType<UIManager>();
        Text tmp;

        Button resume = MakeButton("ResumeButton", panel, "계속하기", 26, new Vector2(420f, 88f), out tmp);
        ((RectTransform)resume.transform).anchoredPosition = new Vector2(0f, 40f);

        Button option = MakeButton("OptionButton", panel, "설정", 26, new Vector2(420f, 88f), out tmp);
        ((RectTransform)option.transform).anchoredPosition = new Vector2(0f, -60f);

        Button toMenu = MakeButton("MainMenuButton", panel, "메인 메뉴로", 26, new Vector2(420f, 88f), out tmp);
        ((RectTransform)toMenu.transform).anchoredPosition = new Vector2(0f, -160f);

        if (uim != null)
        {
            UnityEditor.Events.UnityEventTools.AddPersistentListener(resume.onClick, new UnityEngine.Events.UnityAction(uim.OnClickResume));
            UnityEditor.Events.UnityEventTools.AddPersistentListener(option.onClick, new UnityEngine.Events.UnityAction(uim.ShowOption));
            UnityEditor.Events.UnityEventTools.AddPersistentListener(toMenu.onClick, new UnityEngine.Events.UnityAction(uim.OnClickPauseToMainMenu));
        }

        return panel.gameObject;
    }

    // ── 결과 ──────────────────────────────────────────────────────
    public static GameObject BuildResult(Transform canvas)
    {
        RectTransform panel = MakePanel("Result", canvas, true);
        AddImage(panel, new Color(0.03f, 0.025f, 0.04f, 1f), true);

        Text msg = MakeText("MessageText", panel, "GAME OVER", 36, TextAnchor.MiddleCenter, Accent);
        RectTransform mrt = (RectTransform)msg.transform;
        mrt.anchorMin = new Vector2(0.5f, 0.5f); mrt.anchorMax = new Vector2(0.5f, 0.5f);
        mrt.pivot = new Vector2(0.5f, 0.5f);
        mrt.anchoredPosition = new Vector2(0f, 120f); mrt.sizeDelta = new Vector2(1200f, 300f);

        Text tmp;
        Button confirm = MakeButton("ConfirmButton", panel, "메인 메뉴로", 26, new Vector2(400f, 88f), out tmp);
        ((RectTransform)confirm.transform).anchoredPosition = new Vector2(-220f, -140f);

        Button ranking = MakeButton("RankingButton", panel, "랭킹 보기", 26, new Vector2(400f, 88f), out tmp);
        ((RectTransform)ranking.transform).anchoredPosition = new Vector2(220f, -140f);

        ResultUI ui = panel.gameObject.AddComponent<ResultUI>();
        ui.messageText = msg;
        ui.confirmButton = confirm;
        ui.rankingButton = ranking;

        return panel.gameObject;
    }

    // ── 닉네임 입력 ────────────────────────────────────────────────
    public static GameObject BuildNickname(Transform canvas)
    {
        RectTransform panel = MakePanel("NicknameInput", canvas, true);
        AddImage(panel, new Color(0.03f, 0.025f, 0.04f, 1f), true);

        Text title = MakeText("Title", panel, "STAGE CLEAR!", 42, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.35f));
        RectTransform trt = (RectTransform)title.transform;
        trt.anchorMin = new Vector2(0.5f, 1f); trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -40f); trt.sizeDelta = new Vector2(900f, 90f);

        Text clearTime = MakeText("ClearTimeText", panel, "CLEAR TIME  00:00", 28, TextAnchor.MiddleCenter, TextColor);
        RectTransform crt = (RectTransform)clearTime.transform;
        crt.anchorMin = new Vector2(0.5f, 1f); crt.anchorMax = new Vector2(0.5f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.anchoredPosition = new Vector2(0f, -135f); crt.sizeDelta = new Vector2(900f, 60f);

        Text guide = MakeText("GuideText", panel, "2글자 이상 입력하세요", 23, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.8f));
        RectTransform grt = (RectTransform)guide.transform;
        grt.anchorMin = new Vector2(0.5f, 1f); grt.anchorMax = new Vector2(0.5f, 1f);
        grt.pivot = new Vector2(0.5f, 1f);
        grt.anchoredPosition = new Vector2(0f, -200f); grt.sizeDelta = new Vector2(900f, 50f);

        // 입력 표시 상자
        RectTransform box = NewRect("DisplayBox", panel);
        box.anchorMin = new Vector2(0.5f, 1f); box.anchorMax = new Vector2(0.5f, 1f);
        box.pivot = new Vector2(0.5f, 1f);
        box.anchoredPosition = new Vector2(0f, -250f); box.sizeDelta = new Vector2(700f, 96f);
        AddImage(box, BoxBg, false);

        Text display = MakeText("DisplayText", box, "", 38, TextAnchor.MiddleCenter, Color.white);
        Stretch((RectTransform)display.transform, 10f, 4f, 10f, 4f);

        // A ~ Z 버튼 (13열 x 2행)
        string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        Button[] letterButtons = new Button[letters.Length];
        float bw = 108f, bh = 88f, gap = 10f;
        int perRow = 13;
        float rowWidth = perRow * bw + (perRow - 1) * gap;

        for (int i = 0; i < letters.Length; i++)
        {
            int row = i / perRow;
            int col = i % perRow;

            Text tmpLabel;
            Button b = MakeButton("Key_" + letters[i], panel, letters[i].ToString(), 27, new Vector2(bw, bh), out tmpLabel);
            RectTransform brt = (RectTransform)b.transform;
            brt.anchorMin = new Vector2(0.5f, 0.5f); brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(-rowWidth * 0.5f + bw * 0.5f + col * (bw + gap), -60f - row * (bh + gap));
            letterButtons[i] = b;
        }

        Text tmp2;
        Button back = MakeButton("BackspaceButton", panel, "지우기", 25, new Vector2(340f, 88f), out tmp2);
        RectTransform bkrt = (RectTransform)back.transform;
        bkrt.anchorMin = new Vector2(0.5f, 0.5f); bkrt.anchorMax = new Vector2(0.5f, 0.5f);
        bkrt.pivot = new Vector2(0.5f, 0.5f);
        bkrt.anchoredPosition = new Vector2(-190f, -290f);

        Button confirm = MakeButton("ConfirmButton", panel, "완료", 25, new Vector2(340f, 88f), out tmp2);
        RectTransform cfrt = (RectTransform)confirm.transform;
        cfrt.anchorMin = new Vector2(0.5f, 0.5f); cfrt.anchorMax = new Vector2(0.5f, 0.5f);
        cfrt.pivot = new Vector2(0.5f, 0.5f);
        cfrt.anchoredPosition = new Vector2(190f, -290f);
        confirm.interactable = false;

        NicknameInputUI ui = panel.gameObject.AddComponent<NicknameInputUI>();
        ui.displayText = display;
        ui.guideText = guide;
        ui.clearTimeText = clearTime;
        ui.letterButtons = letterButtons;
        ui.backspaceButton = back;
        ui.confirmButton = confirm;
        ui.minLength = 2;
        ui.maxLength = 8;

        return panel.gameObject;
    }
}
#endif
