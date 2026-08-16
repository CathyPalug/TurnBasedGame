using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 연출을 한 곳에서 담당한다.
/// 타격 파티클 / 피해 숫자 / 피격 번쩍임 / 카메라 흔들림 / 화면 붉은 플래시.
///
/// 어디서든 정적 함수로 부를 수 있고, 인스턴스가 없으면 아무 일도 하지 않는다.
///   BattleEffects.Hit(target, damage, isCritical);
///   BattleEffects.Miss(target);
///   BattleEffects.Heal(target, amount);
///   BattleEffects.Death(target);
/// </summary>
public class BattleEffects : MonoBehaviour
{
    private static BattleEffects cached;

    public static BattleEffects instance
    {
        get
        {
            if (cached == null) cached = FindFirstObjectByType<BattleEffects>(FindObjectsInactive.Include);
            return cached;
        }
        private set { cached = value; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cached = null;
    }

    [Header("파티클 프리팹")]
    public ParticleSystem hitEffect;
    public ParticleSystem criticalEffect;
    public ParticleSystem healEffect;
    public ParticleSystem deathEffect;

    [Header("피해 숫자")]
    [Tooltip("FloatingText 가 붙은 월드 스페이스 프리팹")]
    public GameObject floatingTextPrefab;

    public Color damageColor = new Color(1f, 0.85f, 0.35f);
    public Color criticalColor = new Color(1f, 0.35f, 0.2f);
    public Color playerDamageColor = new Color(1f, 0.35f, 0.35f);
    public Color healColor = new Color(0.4f, 1f, 0.5f);
    public Color missColor = new Color(0.8f, 0.85f, 0.95f);

    [Header("카메라 흔들림")]
    public CameraShake cameraShake;
    public float hitShake = 0.06f;
    public float criticalShake = 0.16f;
    public float shakeDuration = 0.18f;

    [Header("화면 플래시 (플레이어 피격)")]
    public Image screenFlash;
    public Color screenFlashColor = new Color(0.8f, 0.05f, 0.05f, 0.35f);
    public float screenFlashTime = 0.22f;

    private Coroutine flashRoutine;

    private void Awake()
    {
        instance = this;
        if (screenFlash != null)
        {
            Color c = screenFlash.color;
            c.a = 0f;
            screenFlash.color = c;
            screenFlash.raycastTarget = false;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 정적 진입점
    // ─────────────────────────────────────────────────────────────
    public static void Hit(Entry target, int damage, bool critical)
    {
        if (instance == null || target == null) return;
        instance.PlayHit(target, damage, critical);
    }

    public static void Miss(Entry target)
    {
        if (instance == null || target == null) return;
        instance.SpawnText(target.EffectPosition, "MISS", instance.missColor, 0.85f);
    }

    public static void Heal(Entry target, int amount)
    {
        if (instance == null || target == null || amount <= 0) return;
        instance.PlayHeal(target, amount);
    }

    public static void Death(Entry target)
    {
        if (instance == null || target == null) return;
        instance.Spawn(instance.deathEffect, target.EffectPosition);
    }

    public static void Shake(float amount, float duration)
    {
        if (instance == null || instance.cameraShake == null) return;
        instance.cameraShake.Shake(amount, duration);
    }

    // ─────────────────────────────────────────────────────────────
    // 실제 연출
    // ─────────────────────────────────────────────────────────────
    private void PlayHit(Entry target, int damage, bool critical)
    {
        Vector3 pos = target.EffectPosition;

        Spawn(critical && criticalEffect != null ? criticalEffect : hitEffect, pos);

        bool targetIsPlayer = target is Player;
        Color color = critical ? criticalColor : (targetIsPlayer ? playerDamageColor : damageColor);
        string label = critical ? damage.ToString() + "!" : damage.ToString();

        SpawnText(pos, label, color, critical ? 1.5f : 1f);

        HitFlash flash = target.GetComponentInChildren<HitFlash>(true);
        if (flash != null) flash.Play();

        if (cameraShake != null)
        {
            cameraShake.Shake(critical ? criticalShake : hitShake, shakeDuration);
        }

        if (targetIsPlayer) PlayScreenFlash();
    }

    private void PlayHeal(Entry target, int amount)
    {
        Vector3 pos = target.EffectPosition;
        Spawn(healEffect, pos);
        SpawnText(pos, "+" + amount, healColor, 1f);
    }

    private void Spawn(ParticleSystem prefab, Vector3 position)
    {
        if (prefab == null) return;

        ParticleSystem ps = Instantiate(prefab, position, Quaternion.identity);
        ps.Play();

        float life = ps.main.duration + ps.main.startLifetime.constantMax;
        Destroy(ps.gameObject, life + 0.2f);
    }

    private void SpawnText(Vector3 position, string content, Color color, float scale)
    {
        if (floatingTextPrefab == null) return;

        // 같은 자리에 겹치지 않도록 살짝 흩뿌린다
        Vector3 offset = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.1f, 0.2f), 0f);

        GameObject go = Instantiate(floatingTextPrefab, position + offset, Quaternion.identity);
        FloatingText ft = go.GetComponent<FloatingText>();
        if (ft != null) ft.Show(content, color, scale);
        else Destroy(go, 1f);
    }

    private void PlayScreenFlash()
    {
        if (screenFlash == null) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(ScreenFlashRoutine());
    }

    private IEnumerator ScreenFlashRoutine()
    {
        float t = 0f;
        while (t < screenFlashTime)
        {
            t += Time.deltaTime;
            Color c = screenFlashColor;
            c.a = screenFlashColor.a * (1f - Mathf.Clamp01(t / screenFlashTime));
            screenFlash.color = c;
            yield return null;
        }

        Color end = screenFlashColor;
        end.a = 0f;
        screenFlash.color = end;
        flashRoutine = null;
    }
}
