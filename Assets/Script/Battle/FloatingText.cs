using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 피해량 / 회복량 같은 숫자를 유닛 위에 띄우고 위로 떠오르며 사라지게 한다.
/// 월드 스페이스 캔버스 프리팹에 붙여서 쓴다.
/// </summary>
public class FloatingText : MonoBehaviour
{
    public Text text;

    [Header("연출")]
    public float lifetime = 0.9f;
    public float riseSpeed = 1.4f;
    [Tooltip("처음에 살짝 커졌다가 원래 크기로 돌아오는 정도")]
    public float popScale = 1.35f;
    public float popTime = 0.12f;

    private float elapsed;
    private Vector3 baseScale;
    private Camera cam;

    public void Show(string content, Color color, float scaleMultiplier)
    {
        if (text != null)
        {
            text.text = content;
            text.color = color;
        }

        baseScale = transform.localScale * scaleMultiplier;
        transform.localScale = baseScale;
        elapsed = 0f;
    }

    private void Awake()
    {
        if (text == null) text = GetComponentInChildren<Text>(true);
        baseScale = transform.localScale;
    }

    private void LateUpdate()
    {
        elapsed += Time.deltaTime;

        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        // 카메라 바라보기
        if (cam == null) cam = ResolveCamera();
        if (cam != null) transform.rotation = cam.transform.rotation;

        // 팝 -> 원래 크기
        float scale = (elapsed < popTime)
            ? Mathf.Lerp(popScale, 1f, elapsed / popTime)
            : 1f;
        transform.localScale = baseScale * scale;

        // 뒤쪽 30% 구간에서 서서히 사라진다
        if (text != null)
        {
            float fadeStart = lifetime * 0.7f;
            if (elapsed > fadeStart)
            {
                Color c = text.color;
                c.a = Mathf.Clamp01(1f - (elapsed - fadeStart) / (lifetime - fadeStart));
                text.color = c;
            }
        }

        if (elapsed >= lifetime) Destroy(gameObject);
    }

    private Camera ResolveCamera()
    {
        if (BattleManager.instance != null && BattleManager.instance.targetingCamera != null)
            return BattleManager.instance.targetingCamera;
        if (Camera.main != null) return Camera.main;
        return FindFirstObjectByType<Camera>();
    }
}
