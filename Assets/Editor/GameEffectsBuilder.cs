#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 이펙트(파티클 / 피해 숫자 / 화면 플래시)를 만들어 씬에 연결하는 에디터 도구.
/// 메뉴 : Tools/TurnBasedGame/Build Battle Effects
/// </summary>
public static class GameEffectsBuilder
{
    private const string EffectFolder = "Assets/Prefabs/Effects";

    [MenuItem("Tools/TurnBasedGame/Build Battle Effects")]
    public static void BuildAll()
    {
        EnsureFolder();

        Material particleMat = GetParticleMaterial();

        GameObject hit = BuildBurst("FX_Hit", particleMat,
            new Color(1f, 0.75f, 0.25f), new Color(1f, 0.45f, 0.1f),
            20, 0.30f, 5.5f, 0.22f, 0.35f);

        GameObject crit = BuildBurst("FX_Critical", particleMat,
            new Color(1f, 0.45f, 0.2f), new Color(1f, 0.15f, 0.05f),
            38, 0.42f, 8.5f, 0.34f, 0.5f);

        GameObject heal = BuildBurst("FX_Heal", particleMat,
            new Color(0.45f, 1f, 0.55f), new Color(0.15f, 0.9f, 0.7f),
            22, 0.70f, 1.8f, 0.20f, 0.35f, true);

        GameObject death = BuildBurst("FX_Death", particleMat,
            new Color(0.75f, 0.15f, 0.15f), new Color(0.2f, 0.05f, 0.1f),
            46, 0.75f, 4.5f, 0.34f, 0.6f);

        GameObject floatingText = BuildFloatingText();

        WireScene(hit, crit, heal, death, floatingText);

        AssetDatabase.SaveAssets();
        Debug.Log("[GameEffectsBuilder] 전투 이펙트 생성 및 연결 완료");
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(EffectFolder))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Effects");
    }

    private static Material GetParticleMaterial()
    {
        const string path = EffectFolder + "/FX_Particle.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);
        mat.color = Color.white;

        // 가산 합성으로 어두운 던전에서도 잘 보이게
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);          // Transparent
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 1f);              // Additive
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    /// <summary>한 번 터지고 사라지는 파티클 프리팹을 만든다.</summary>
    private static GameObject BuildBurst(string name, Material mat, Color colorA, Color colorB,
                                         int count, float lifetime, float speed,
                                         float size, float duration, bool riseUp = false)
    {
        GameObject go = new GameObject(name);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.duration = duration;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
        main.gravityModifier = riseUp ? -0.25f : 0.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        main.stopAction = ParticleSystemStopAction.None;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = riseUp ? ParticleSystemShapeType.Circle : ParticleSystemShapeType.Sphere;
        shape.radius = riseUp ? 0.5f : 0.15f;
        if (riseUp) shape.rotation = new Vector3(-90f, 0f, 0f);

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.15f));

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 5;

        string path = EffectFolder + "/" + name + ".prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return saved;
    }

    /// <summary>피해 숫자용 월드 스페이스 텍스트 프리팹.</summary>
    private static GameObject BuildFloatingText()
    {
        const string path = EffectFolder + "/FX_DamageText.prefab";

        GameObject go = new GameObject("FX_DamageText", typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(260f, 90f);
        rt.localScale = Vector3.one * 0.011f;

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 20;

        RectTransform textRt = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
        textRt.SetParent(go.transform, false);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Text text = textRt.gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 56;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = textRt.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(2.5f, -2.5f);

        FloatingText ft = go.AddComponent<FloatingText>();
        ft.text = text;

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return saved;
    }

    /// <summary>씬에 BattleEffects / CameraShake / HitFlash / 화면 플래시를 붙이고 연결한다.</summary>
    private static void WireScene(GameObject hit, GameObject crit, GameObject heal, GameObject death, GameObject floatingText)
    {
        // 1) BattleEffects 오브젝트
        GameObject fxGo = GameObject.Find("BattleEffects");
        if (fxGo == null) fxGo = new GameObject("BattleEffects");
        BattleEffects fx = fxGo.GetComponent<BattleEffects>();
        if (fx == null) fx = fxGo.AddComponent<BattleEffects>();

        fx.hitEffect = hit.GetComponent<ParticleSystem>();
        fx.criticalEffect = crit.GetComponent<ParticleSystem>();
        fx.healEffect = heal.GetComponent<ParticleSystem>();
        fx.deathEffect = death.GetComponent<ParticleSystem>();
        fx.floatingTextPrefab = floatingText;

        // 2) 카메라 흔들림
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            CameraShake shake = cam.GetComponent<CameraShake>();
            if (shake == null) shake = cam.gameObject.AddComponent<CameraShake>();
            fx.cameraShake = shake;
        }

        // 3) 화면 플래시 (Canvas 최상단)
        Transform canvas = GameObject.Find("Canvas").transform;
        Transform flashT = canvas.Find("ScreenFlash");
        if (flashT == null)
        {
            GameObject flashGo = new GameObject("ScreenFlash", typeof(RectTransform));
            flashGo.transform.SetParent(canvas, false);
            flashGo.layer = LayerMask.NameToLayer("UI");
            flashT = flashGo.transform;
        }
        RectTransform frt = (RectTransform)flashT;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;
        frt.SetAsLastSibling();

        Image flashImg = flashT.GetComponent<Image>();
        if (flashImg == null) flashImg = flashT.gameObject.AddComponent<Image>();
        flashImg.color = new Color(0.8f, 0.05f, 0.05f, 0f);
        flashImg.raycastTarget = false;
        fx.screenFlash = flashImg;

        // 4) 플레이어 / 몬스터 프리팹에 HitFlash + 이펙트 위치
        Player player = Object.FindFirstObjectByType<Player>();
        if (player != null)
        {
            AddHitFlash(player.gameObject);
            player.effectPoint = MakeEffectPoint(player.transform, 1.4f);
            EditorUtility.SetDirty(player);
        }

        const string monsterPath = "Assets/Prefabs/Monster_Placeholder.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(monsterPath);
        if (root != null)
        {
            AddHitFlash(root);
            Monster m = root.GetComponent<Monster>();
            if (m != null) m.effectPoint = MakeEffectPoint(root.transform, 1.4f);
            PrefabUtility.SaveAsPrefabAsset(root, monsterPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        EditorUtility.SetDirty(fx);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
    }

    private static void AddHitFlash(GameObject go)
    {
        HitFlash flash = go.GetComponent<HitFlash>();
        if (flash == null) flash = go.AddComponent<HitFlash>();

        // UI 캔버스는 제외하고 몸통 Renderer 만 대상으로
        Renderer[] all = go.GetComponentsInChildren<Renderer>(true);
        System.Collections.Generic.List<Renderer> list = new System.Collections.Generic.List<Renderer>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] is MeshRenderer || all[i] is SkinnedMeshRenderer)
            {
                if (all[i].GetComponentInParent<Canvas>() != null) continue;
                list.Add(all[i]);
            }
        }
        flash.targetRenderers = list.ToArray();
        EditorUtility.SetDirty(flash);
    }

    private static Transform MakeEffectPoint(Transform parent, float height)
    {
        Transform p = parent.Find("EffectPoint");
        if (p == null)
        {
            GameObject go = new GameObject("EffectPoint");
            go.transform.SetParent(parent, false);
            p = go.transform;
        }
        p.localPosition = new Vector3(0f, height, 0f);
        return p;
    }
}
#endif
