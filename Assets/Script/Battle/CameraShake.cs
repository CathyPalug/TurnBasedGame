using System.Collections;
using UnityEngine;

/// <summary>
/// 카메라를 잠깐 흔든다. 큰 피해나 크리티컬 때 호출한다.
/// </summary>
public class CameraShake : MonoBehaviour
{
    private Vector3 basePosition;
    private Coroutine running;

    private void Awake()
    {
        basePosition = transform.localPosition;
    }

    private void OnDisable()
    {
        transform.localPosition = basePosition;
        running = null;
    }

    public void Shake(float amount, float duration)
    {
        if (!isActiveAndEnabled) return;

        if (running != null)
        {
            StopCoroutine(running);
            transform.localPosition = basePosition;
        }
        running = StartCoroutine(ShakeRoutine(amount, duration));
    }

    private IEnumerator ShakeRoutine(float amount, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float falloff = 1f - Mathf.Clamp01(t / duration);
            Vector3 offset = Random.insideUnitSphere * amount * falloff;
            offset.z *= 0.3f;
            transform.localPosition = basePosition + offset;
            yield return null;
        }

        transform.localPosition = basePosition;
        running = null;
    }
}
