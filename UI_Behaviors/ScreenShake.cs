using UnityEngine;
using System.Collections;

public class ScreenShake : MonoBehaviour
{
    private Coroutine shakeRoutine;

    /// <summary>
    /// Call this to shake the camera relative to its current position.
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        // 🟢 Store position *right when shaking starts*, not at Awake()
        Vector3 originalPos = transform.localPosition;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Random offset each frame
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Smoothly return to current local position instead of teleporting
        float returnTime = 0.1f;
        float t = 0;
        while (t < returnTime)
        {
            t += Time.deltaTime / returnTime;
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPos, t);
            yield return null;
        }

        transform.localPosition = originalPos;
        shakeRoutine = null;
    }
}
