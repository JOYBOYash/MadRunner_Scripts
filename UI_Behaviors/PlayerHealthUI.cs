using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform heartsContainer; // Parent of all heart icons
    public GameObject heartPrefab;    // Prefab for heart icon
    public int maxHealth = 3;         // Total hearts

    [Header("Animation Settings")]
    public float fadeOutDuration = 0.5f;
    public float scaleOutDuration = 0.4f;
    public float scaleUpOnHit = 1.4f;

    private List<Image> heartImages = new List<Image>();
    private int currentHealth;

    void Start()
    {
        SetupHearts();
    }

    void SetupHearts()
    {
        // Clear existing
        foreach (Transform child in heartsContainer)
            Destroy(child.gameObject);

        heartImages.Clear();

        // Spawn all hearts
        for (int i = 0; i < maxHealth; i++)
        {
            GameObject heart = Instantiate(heartPrefab, heartsContainer);
            Image img = heart.GetComponent<Image>();

            if (img != null)
                heartImages.Add(img);
        }

        currentHealth = maxHealth;
    }

    /// <summary>
    /// Call this whenever the player takes damage.
    /// </summary>
    public void OnPlayerHit()
    {
        if (currentHealth <= 0) return;

        currentHealth--;

        if (currentHealth >= 0 && currentHealth < heartImages.Count)
        {
            Image heartToRemove = heartImages[currentHealth];
            StartCoroutine(AnimateHeartRemoval(heartToRemove));
        }
    }

    private IEnumerator AnimateHeartRemoval(Image heart)
    {
        if (heart == null) yield break;

        RectTransform rect = heart.rectTransform;
        Color startColor = heart.color;
        float t = 0f;

        Vector3 startScale = rect.localScale;
        Vector3 targetScale = Vector3.one * scaleUpOnHit;

        // 🔥 Pop effect before fading
        while (t < 0.2f)
        {
            t += Time.deltaTime / 0.2f;
            rect.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        // ⚰️ Dramatic fade + scale out
        t = 0f;
        Vector3 fadeTargetScale = Vector3.zero;
        Color fadeTargetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (t < fadeOutDuration)
        {
            t += Time.deltaTime / fadeOutDuration;
            rect.localScale = Vector3.Lerp(targetScale, fadeTargetScale, t);
            heart.color = Color.Lerp(startColor, fadeTargetColor, t);
            yield return null;
        }

        heart.gameObject.SetActive(false);
    }

    /// <summary>
    /// Call this when player revives or resets health.
    /// </summary>
    public void ResetHealthUI()
    {
        foreach (var heart in heartImages)
        {
            heart.gameObject.SetActive(true);
            heart.color = new Color(1, 1, 1, 1);
            heart.rectTransform.localScale = Vector3.one;
        }

        currentHealth = maxHealth;
    }
}
