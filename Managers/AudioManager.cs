using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudioManager : MonoBehaviour
{
    [Header("Footstep Settings")]
    public AudioClip runningLoop;     // continuous running loop
    public float runVolume = 1f;

    [Header("Action Sounds")]
    public AudioClip dashSound;
    public AudioClip slideSound;
    public float sfxVolume = 2f;

    private AudioSource runSource;
    private AudioSource sfxSource;

    private bool isRunning = false;

    void Awake()
    {
        // Create two separate AudioSources:
        // one for looping footsteps, one for one-shot SFX.
        runSource = gameObject.AddComponent<AudioSource>();
        runSource.loop = true;
        runSource.playOnAwake = false;
        runSource.spatialBlend = 1f;
        runSource.volume = runVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 1f;
        sfxSource.volume = sfxVolume;
    }

    // 🦶 Control continuous run loop
    public void SetRunningState(bool moving)
    {
        if (moving && !isRunning)
        {
            if (runningLoop != null)
            {
                runSource.clip = runningLoop;
                runSource.Play();
            }
            isRunning = true;
        }
        else if (!moving && isRunning)
        {
            runSource.Stop();
            isRunning = false;
        }
    }

    // 🚀 Dash / Slide SFX
    public void PlayDash()
    {
        if (dashSound != null)
            sfxSource.PlayOneShot(dashSound);
    }

    public void PlaySlide()
    {
        if (slideSound != null)
            sfxSource.PlayOneShot(slideSound);
    }
}
