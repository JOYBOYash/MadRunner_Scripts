using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform heartsContainer; // Parent of all hearts
    public GameObject fullHeartPrefab;    // Prefab for full heart
    public GameObject emptyHeartPrefab;   // Prefab for empty heart
    public int maxHealth = 3;             // Total hearts

    [Header("Layout Settings (no layout group needed)")]
    public float heartSpacing = 80f;      // Distance between each heart
    public float heartWidth = 64f;        // ❤️ Individual heart width
    public float heartHeight = 64f;       // ❤️ Individual heart height

    [Header("Animation Settings")]
    public float fadeOutDuration = 0.3f;
    public float popScale = 1.08f;        // ✅ Subtle pop animation

    private class HeartPair
    {
        public Image full;
        public Image empty;
        public RectTransform container;
    }

    private List<HeartPair> hearts = new List<HeartPair>();
    private int currentHealth;

    void Start()
    {
        SetupHearts();
    }

    void SetupHearts()
    {
        // 🧹 Clear old UI
        foreach (Transform child in heartsContainer)
            Destroy(child.gameObject);

        hearts.Clear();

        // 🧱 Spawn all hearts manually
        for (int i = 0; i < maxHealth; i++)
        {
            // Create slot
            GameObject slot = new GameObject($"Heart_{i}", typeof(RectTransform));
            slot.transform.SetParent(heartsContainer, false);
            RectTransform slotRect = slot.GetComponent<RectTransform>();

            // Slot layout
            slotRect.sizeDelta = new Vector2(heartWidth, heartHeight);
            slotRect.anchorMin = new Vector2(0, 0.5f);
            slotRect.anchorMax = new Vector2(0, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = new Vector2(i * heartSpacing, 0f);
            slotRect.localScale = Vector3.one;

            // Empty heart
            GameObject empty = Instantiate(emptyHeartPrefab, slot.transform);
            Image emptyImg = empty.GetComponent<Image>();
            RectTransform emptyRect = empty.GetComponent<RectTransform>();
            SetupHeartRect(emptyRect);

            // Full heart
            GameObject full = Instantiate(fullHeartPrefab, slot.transform);
            Image fullImg = full.GetComponent<Image>();
            RectTransform fullRect = full.GetComponent<RectTransform>();
            SetupHeartRect(fullRect);

            fullImg.gameObject.SetActive(true);
            emptyImg.gameObject.SetActive(false);

            hearts.Add(new HeartPair
            {
                full = fullImg,
                empty = emptyImg,
                container = slotRect
            });
        }

        currentHealth = maxHealth;
    }

    void SetupHeartRect(RectTransform rect)
    {
        rect.sizeDelta = new Vector2(heartWidth, heartHeight);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.anchoredPosition = Vector2.zero;
    }

    public void UpdateHealthUI(int current, int max)
    {
        if (maxHealth != max)
        {
            maxHealth = max;
            SetupHearts();
        }

        current = Mathf.Clamp(current, 0, maxHealth);

        for (int i = 0; i < hearts.Count; i++)
        {
            bool shouldShowFull = i < current;

            if (shouldShowFull && !hearts[i].full.gameObject.activeSelf)
                StartCoroutine(ShowFullHeart(hearts[i]));
            else if (!shouldShowFull && hearts[i].full.gameObject.activeSelf)
                StartCoroutine(RemoveFullHeart(hearts[i]));
        }

        currentHealth = current;
    }

    private IEnumerator RemoveFullHeart(HeartPair heart)
    {
        RectTransform rect = heart.full.rectTransform;
        Vector3 startScale = rect.localScale;
        Vector3 targetScale = Vector3.one * popScale;
        float t = 0f;

        // 💢 Slight "pop"
        while (t < 0.15f)
        {
            t += Time.deltaTime / 0.15f;
            rect.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        // 💔 Fade out and swap
        t = 0f;
        Color startColor = heart.full.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (t < fadeOutDuration)
        {
            t += Time.deltaTime / fadeOutDuration;
            rect.localScale = Vector3.Lerp(targetScale, Vector3.one * 0.7f, t);
            heart.full.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        heart.full.gameObject.SetActive(false);
        heart.empty.gameObject.SetActive(true);
        rect.localScale = Vector3.one;
        heart.full.color = Color.white;
    }

    private IEnumerator ShowFullHeart(HeartPair heart)
    {
        heart.empty.gameObject.SetActive(false);
        heart.full.gameObject.SetActive(true);
        RectTransform rect = heart.full.rectTransform;

        rect.localScale = Vector3.one * 0.5f;
        float t = 0f;

        while (t < 0.25f)
        {
            t += Time.deltaTime / 0.25f;
            rect.localScale = Vector3.Lerp(Vector3.one * 0.5f, Vector3.one, t);
            yield return null;
        }

        rect.localScale = Vector3.one;
    }

    public void ResetHealthUI()
    {
        if (hearts.Count == 0 || heartsContainer.childCount == 0)
            SetupHearts();

        foreach (var h in hearts)
        {
            h.full.gameObject.SetActive(true);
            h.empty.gameObject.SetActive(false);
            h.full.color = Color.white;
            h.full.rectTransform.localScale = Vector3.one;
        }

        currentHealth = maxHealth;
    }
}
