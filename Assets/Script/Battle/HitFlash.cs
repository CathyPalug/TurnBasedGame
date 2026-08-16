using System.Collections;
using UnityEngine;

/// <summary>
/// 피격 시 유닛을 잠깐 하얗게 번쩍이게 한다.
/// MaterialPropertyBlock 을 쓰기 때문에 같은 재질을 공유해도 이 오브젝트만 바뀐다.
/// </summary>
public class HitFlash : MonoBehaviour
{
    [Tooltip("비워두면 자식의 Renderer 를 모두 찾아 쓴다.")]
    public Renderer[] targetRenderers;

    public Color flashColor = new Color(1f, 0.95f, 0.9f);
    public float duration = 0.14f;

    private MaterialPropertyBlock block;
    private Color[] originalColors;
    private Coroutine running;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>(true);

        block = new MaterialPropertyBlock();
        originalColors = new Color[targetRenderers.Length];

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null) continue;
            Material m = targetRenderers[i].sharedMaterial;
            if (m == null) { originalColors[i] = Color.white; continue; }

            if (m.HasProperty(BaseColorId)) originalColors[i] = m.GetColor(BaseColorId);
            else if (m.HasProperty(ColorId)) originalColors[i] = m.GetColor(ColorId);
            else originalColors[i] = Color.white;
        }
    }

    public void Play()
    {
        if (!isActiveAndEnabled) return;
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / duration);
            ApplyColors(k);
            yield return null;
        }

        ApplyColors(0f);
        running = null;
    }

    /// <summary>k = 1 이면 완전히 flashColor, 0 이면 원래 색.</summary>
    private void ApplyColors(float k)
    {
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer r = targetRenderers[i];
            if (r == null) continue;

            Color c = Color.Lerp(originalColors[i], flashColor, k);

            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            block.SetColor(ColorId, c);
            r.SetPropertyBlock(block);
        }
    }
}
