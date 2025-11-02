using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class MusicManager : MonoBehaviour
{
    [System.Serializable]
    public class SceneMusic
    {
        public string sceneName;
        public AudioClip musicClip;
    }

    [Header("🎶 Scene Music Settings")]
    public List<SceneMusic> sceneTracks = new List<SceneMusic>();

    [Header("🔊 Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 0.8f;
    public float fadeDuration = 1.5f;

    private static MusicManager instance;
    private AudioSource activeSource;
    private AudioSource inactiveSource;

    private Coroutine fadeRoutine;
    private string currentScene = "";
    private bool initialized = false;

    void Awake()
    {
        // ✅ Singleton Protection (stronger)
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // ✅ Create two sources only once
        activeSource = gameObject.AddComponent<AudioSource>();
        inactiveSource = gameObject.AddComponent<AudioSource>();

        activeSource.loop = true;
        inactiveSource.loop = true;

        activeSource.playOnAwake = false;
        inactiveSource.playOnAwake = false;

        activeSource.volume = 0f;
        inactiveSource.volume = 0f;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        if (!initialized)
        {
            currentScene = SceneManager.GetActiveScene().name;
            PlayMusicForScene(currentScene, true);
            initialized = true;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == currentScene) return; // ⚡ Prevent duplicate reloads

        currentScene = scene.name;
        PlayMusicForScene(scene.name, false);
    }

    void PlayMusicForScene(string sceneName, bool instant)
    {
        AudioClip newClip = GetMusicForScene(sceneName);

        if (newClip == null)
        {
            Debug.LogWarning($"⚠ No music assigned for scene: {sceneName}");
            return;
        }

        if (activeSource.clip == newClip)
            return; // Already playing correct track

        // Swap active/inactive sources
        AudioSource temp = activeSource;
        activeSource = inactiveSource;
        inactiveSource = temp;

        activeSource.clip = newClip;
        activeSource.volume = 0f;
        activeSource.Play();

        // ✅ Cancel old fade before starting a new one
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (instant)
        {
            activeSource.volume = masterVolume;
            inactiveSource.Stop();
        }
        else
        {
            fadeRoutine = StartCoroutine(FadeTracks());
        }
    }

    IEnumerator FadeTracks()
    {
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime; // ✅ Use unscaled for smoother transitions even during pause
            float t = timer / fadeDuration;

            activeSource.volume = Mathf.Lerp(0f, masterVolume, t);
            inactiveSource.volume = Mathf.Lerp(masterVolume, 0f, t);

            yield return null;
        }

        inactiveSource.Stop();
        inactiveSource.volume = 0f;
        fadeRoutine = null;
    }

    AudioClip GetMusicForScene(string sceneName)
    {
        foreach (SceneMusic sm in sceneTracks)
            if (sm.sceneName == sceneName)
                return sm.musicClip;

        return null;
    }
}
