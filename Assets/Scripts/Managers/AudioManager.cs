using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip coinClip;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField] private AudioClip unlockClip;
    [SerializeField] private AudioClip newBestClip;
    [SerializeField] private AudioClip springClip;
    [SerializeField] private AudioClip crumbleClip;
    [SerializeField] private AudioClip bgmClip;

    [Header("Volume Settings")]
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.55f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.85f;

    private bool isMusicMuted = false;
    private bool isSfxMuted = false;

    public bool IsMusicMuted => isMusicMuted;
    public bool IsSfxMuted => isSfxMuted;

    public event Action<bool> OnMusicMuteToggled;
    public event Action<bool> OnSfxMuteToggled;

    private void Awake()
    {
        instance = this;
        EnsureAudioSources();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Start()
    {
        LoadPreferences();
        ApplyMuteSettings();
        PlayBGM();
    }

    private void EnsureAudioSources()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.spatialBlend = 0f; // 2D
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D
        }
    }

    private void LoadPreferences()
    {
        if (SaveManager.instance != null && SaveManager.instance.CurrentSave != null)
        {
            isMusicMuted = SaveManager.instance.CurrentSave.isMusicMuted;
            isSfxMuted = SaveManager.instance.CurrentSave.isSfxMuted;
        }
    }

    private void SavePreferences()
    {
        if (SaveManager.instance != null && SaveManager.instance.CurrentSave != null)
        {
            SaveManager.instance.CurrentSave.isMusicMuted = isMusicMuted;
            SaveManager.instance.CurrentSave.isSfxMuted = isSfxMuted;
            SaveManager.instance.Save(syncCloudIfLoggedIn: false);
        }
    }

    public void ApplyMuteSettings()
    {
        EnsureAudioSources();

        if (bgmSource != null)
        {
            bgmSource.volume = isMusicMuted ? 0f : musicVolume;
            bgmSource.mute = isMusicMuted;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = isSfxMuted ? 0f : sfxVolume;
            sfxSource.mute = isSfxMuted;
        }

        OnMusicMuteToggled?.Invoke(isMusicMuted);
        OnSfxMuteToggled?.Invoke(isSfxMuted);
    }

    public void ToggleMusic()
    {
        isMusicMuted = !isMusicMuted;
        ApplyMuteSettings();
        SavePreferences();
    }

    public void ToggleSFX()
    {
        isSfxMuted = !isSfxMuted;
        ApplyMuteSettings();
        SavePreferences();
    }

    public void PlayBGM()
    {
        if (bgmSource == null || bgmClip == null) return;

        if (!bgmSource.isPlaying)
        {
            bgmSource.clip = bgmClip;
            bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }
    }

    public void PlaySFX(AudioClip clip, float volume = 1f, bool pitchJitter = false)
    {
        if (clip == null || sfxSource == null || isSfxMuted) return;

        if (pitchJitter)
        {
            // Subtle pitch variation (+- 4%) to keep jumping and coin audio pleasant
            sfxSource.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
        }
        else
        {
            sfxSource.pitch = 1.0f;
        }

        sfxSource.PlayOneShot(clip, volume * sfxVolume);
    }

    public void PlayJump()
    {
        PlaySFX(jumpClip, 0.85f, pitchJitter: true);
    }

    public void PlayCoin()
    {
        PlaySFX(coinClip, 0.9f, pitchJitter: true);
    }

    public void PlayClick()
    {
        PlaySFX(clickClip, 0.75f, pitchJitter: false);
    }

    public void PlayGameOver()
    {
        PlaySFX(gameOverClip, 1.0f, pitchJitter: false);
    }

    public void PlayUnlock()
    {
        PlaySFX(unlockClip, 1.0f, pitchJitter: false);
    }

    public void PlayNewBest()
    {
        PlaySFX(newBestClip, 1.0f, pitchJitter: false);
    }

    public void PlaySpring()
    {
        PlaySFX(springClip, 0.95f, pitchJitter: true);
    }

    public void PlayCrumble()
    {
        PlaySFX(crumbleClip, 0.9f, pitchJitter: false);
    }
}

