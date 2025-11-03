using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFlash : MonoBehaviour
{
    public Image flashImage;

    public void Flash(Color color, float duration)
    {
        if (flashImage == null) return;
        StopAllCoroutines();
        StartCoroutine(FlashRoutine(color, duration));
    }

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        flashImage.color = new Color(color.r, color.g, color.b, 0);
        flashImage.enabled = true;

        float halfDuration = duration / 2f;
        float t = 0;

        // Fade in
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            flashImage.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0, 0.6f, t / halfDuration));
            yield return null;
        }

        // Fade out
        t = 0;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            flashImage.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.6f, 0, t / halfDuration));
            yield return null;
        }

        flashImage.enabled = false;
    }
}
