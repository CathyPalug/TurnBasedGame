using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// ═════════════════════════════════════════════════════════════════════════════
//  DamagePopup.cs  —  피해 숫자를 유닛 머리 위에 띄우고, 타격 이펙트를 터뜨린다
//
//  ── 어떻게 동작하나 ──
//    1) 유닛의 3D 위치를 카메라로 "화면 좌표" 로 바꾼다  (WorldToScreenPoint)
//    2) 그 자리에 글자 프리팹을 만든다
//    3) 코루틴으로 위로 떠오르게 하다가 서서히 사라지고 지운다
//
//  ★ 타격 이펙트는 다운받은 에셋 "Matthew Guz - Hits Effects FREE" 의 프리팹을 쓴다.
//    (Inspector 의 hitEffect / criticalEffect 칸에 끌어다 놓는다. 비워둬도 에러 안 남)
// ═════════════════════════════════════════════════════════════════════════════

public class DamagePopup : MonoBehaviour
{
    public static DamagePopup instance;

    [Header("연결")]
    [Tooltip("숫자가 나타날 Canvas 안의 빈 오브젝트")]
    public RectTransform popupParent;

    [Tooltip("Text 가 붙어있는 숫자 프리팹")]
    public GameObject popupPrefab;

    [Tooltip("3D 를 비추는 카메라. 비우면 Camera.main 을 쓴다")]
    public Camera worldCamera;

    [Header("타격 이펙트 (에셋 : Matthew Guz - Hits Effects FREE)")]
    public GameObject hitEffect;        // 평타
    public GameObject criticalEffect;   // 크리티컬

    [Header("글자 색")]
    public Color damageColor = new Color(1f, 0.9f, 0.4f);       // 적이 맞음 (노랑)
    public Color criticalColor = new Color(1f, 0.4f, 0.2f);     // 크리티컬 (주황)
    public Color playerHurtColor = new Color(1f, 0.35f, 0.35f); // 내가 맞음 (빨강)
    public Color healColor = new Color(0.4f, 1f, 0.5f);         // 회복 (초록)
    public Color missColor = new Color(0.85f, 0.9f, 1f);        // 빗나감 (흰색)

    [Header("연출")]
    public float liveTime = 0.9f;   // 몇 초 동안 보일지
    public float riseSpeed = 60f;   // 위로 올라가는 속도 (화면 픽셀 기준)

    private void Awake()
    {
        instance = this;
    }


    // ═════════════════════════════════════════════════════════════
    //  밖에서 부르는 함수들
    // ═════════════════════════════════════════════════════════════

    /// <summary>피해 숫자를 띄운다.</summary>
    public void ShowDamage(Unit target, int damage, bool critical)
    {
        if (target == null) return;

        // 색과 글자를 정한다.
        Color 색;
        if (critical) 색 = criticalColor;
        else if (target is Player) 색 = playerHurtColor;   // 내가 맞으면 빨강
        else 색 = damageColor;

        string 글자 = damage.ToString();
        if (critical) 글자 = damage + "!";

        float 크기 = 1f;
        if (critical) 크기 = 1.5f;

        MakePopup(target, 글자, 색, 크기);

        // 타격 이펙트를 터뜨린다.
        if (critical && criticalEffect != null) MakeEffect(criticalEffect, target);
        else if (hitEffect != null) MakeEffect(hitEffect, target);
    }

    /// <summary>회복 숫자를 띄운다.</summary>
    public void ShowHeal(Unit target, int amount)
    {
        if (target == null) return;
        if (amount <= 0) return;   // 0 회복이면 보여줄 필요 없다

        MakePopup(target, "+" + amount, healColor, 1f);
    }

    /// <summary>빗나갔을 때 MISS 를 띄운다.</summary>
    public void ShowMiss(Unit target)
    {
        if (target == null) return;
        MakePopup(target, "MISS", missColor, 0.9f);
    }


    // ═════════════════════════════════════════════════════════════
    //  실제로 만드는 부분
    // ═════════════════════════════════════════════════════════════

    /// <summary>글자 하나를 만들어서 띄운다.</summary>
    private void MakePopup(Unit target, string text, Color color, float scale)
    {
        if (popupPrefab == null) return;
        if (popupParent == null) return;

        // 1) 3D 위치를 화면 위치로 바꾼다.
        Camera 카메라 = worldCamera;
        if (카메라 == null) 카메라 = Camera.main;
        if (카메라 == null) return;

        Vector3 화면위치 = 카메라.WorldToScreenPoint(target.GetHitPos());

        // 카메라 뒤에 있으면 화면에 안 보이므로 만들지 않는다.
        if (화면위치.z < 0f) return;

        // 2) 글자를 만든다.
        GameObject 글자오브젝트 = Instantiate(popupPrefab, popupParent);

        RectTransform 위치 = 글자오브젝트.GetComponent<RectTransform>();
        위치.position = 화면위치;
        위치.localScale = new Vector3(scale, scale, 1f);

        Text 글자 = 글자오브젝트.GetComponentInChildren<Text>();
        if (글자 != null)
        {
            글자.text = text;
            글자.color = color;
        }

        // 3) 위로 떠오르다 사라지게 한다.
        StartCoroutine(MoveAndFade(위치, 글자));
    }

    /// <summary>글자를 위로 올리면서 서서히 투명하게 만들고, 끝나면 지운다.</summary>
    private IEnumerator MoveAndFade(RectTransform rect, Text text)
    {
        float 지난시간 = 0f;

        // 처음 색을 기억해 둔다. (투명도만 바꿀 것이기 때문)
        Color 처음색 = Color.white;
        if (text != null) 처음색 = text.color;

        while (지난시간 < liveTime)
        {
            // 도중에 지워졌으면 코루틴을 끝낸다.
            if (rect == null) yield break;

            지난시간 = 지난시간 + Time.deltaTime;   // Time.deltaTime = 지난 한 프레임의 시간

            // 위로 조금씩 올린다.
            rect.position = rect.position + new Vector3(0f, riseSpeed * Time.deltaTime, 0f);

            // 뒤쪽 절반 구간에서 서서히 투명해진다.
            if (text != null)
            {
                float 사라지기시작 = liveTime * 0.5f;

                if (지난시간 > 사라지기시작)
                {
                    float 남은비율 = 1f - ((지난시간 - 사라지기시작) / (liveTime - 사라지기시작));

                    Color 새색 = 처음색;
                    새색.a = Mathf.Clamp01(남은비율);   // a = 투명도
                    text.color = 새색;
                }
            }

            // yield return null = 다음 프레임까지 기다렸다가 여기서 다시 시작
            yield return null;
        }

        if (rect != null) Destroy(rect.gameObject);
    }

    /// <summary>타격 파티클을 유닛 위치에 만들고, 잠시 뒤 지운다.</summary>
    private void MakeEffect(GameObject prefab, Unit target)
    {
        GameObject 이펙트 = Instantiate(prefab, target.GetHitPos(), Quaternion.identity);

        // 2초 뒤에 자동으로 지운다. (Destroy 에 시간을 같이 넣으면 그 시간 뒤에 지워진다)
        Destroy(이펙트, 2f);
    }
}
