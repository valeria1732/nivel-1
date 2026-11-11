using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Gestor de audio para Madres Buscadoras.
/// Reproduce el sonido y consignas de los desaparecidos en México ("¿Dónde están?", "¡Vivos se los llevaron!").
/// Permite loop continuo, ajuste de volumen, silenciado y efectos sonoros de interfaz.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Clips")]
    public AudioClip desaparecidosBgmClip;
    public AudioClip dondeEstanClip;
    public AudioClip vivosSeLosLlevaronClip;

    [Header("Ajustes")]
    [Range(0f, 1f)] public float bgmVolume = 0.85f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f;
    public bool autoPlayOnStart = true;
    public bool loopBgm = true;

    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private bool isMuted = false;

    public bool IsMuted => isMuted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitializeAudio()
    {
        if (Instance == null)
        {
            GameObject audioGO = new GameObject("AudioManager");
            Instance = audioGO.AddComponent<AudioManager>();
            DontDestroyOnLoad(audioGO);
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupAudioSources();
        LoadAudioClips();
    }

    void Start()
    {
        if (autoPlayOnStart && !bgmSource.isPlaying)
        {
            PlayDesaparecidosBGM();
        }
    }

    void SetupAudioSources()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = loopBgm;
            bgmSource.playOnAwake = false;
            bgmSource.volume = bgmVolume;
            bgmSource.spatialBlend = 0f; // 2D Sound
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = sfxVolume;
            sfxSource.spatialBlend = 0f;
        }
    }

    void LoadAudioClips()
    {
        // 1. Intentar cargar desde Resources
        if (desaparecidosBgmClip == null)
            desaparecidosBgmClip = Resources.Load<AudioClip>("Audio/sonido_desaparecidos_mexico");

        if (dondeEstanClip == null)
            dondeEstanClip = Resources.Load<AudioClip>("Audio/canto_donde_estan");

        if (vivosSeLosLlevaronClip == null)
            vivosSeLosLlevaronClip = Resources.Load<AudioClip>("Audio/canto_vivos");

        // 2. Si aún no está en Resources (por refresco de importación pendiente), cargar directo de disco
        if (desaparecidosBgmClip == null)
        {
            string path = Path.Combine(Application.dataPath, "Audio/sonido_desaparecidos_mexico.wav");
            if (File.Exists(path))
            {
                StartCoroutine(LoadAudioFromDisk(path, clip =>
                {
                    desaparecidosBgmClip = clip;
                    if (autoPlayOnStart && !bgmSource.isPlaying)
                    {
                        PlayDesaparecidosBGM();
                    }
                }));
            }
        }
    }

    IEnumerator LoadAudioFromDisk(string fullPath, Action<AudioClip> onLoaded)
    {
        string uri = "file://" + fullPath.Replace('\\', '/');
        using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
                if (clip != null)
                {
                    clip.name = Path.GetFileNameWithoutExtension(fullPath);
                    onLoaded?.Invoke(clip);
                }
            }
            else
            {
                Debug.LogWarning("[AudioManager] No se pudo cargar audio desde archivo local: " + req.error);
            }
        }
    }

    public void PlayDesaparecidosBGM()
    {
        if (bgmSource == null) SetupAudioSources();

        if (desaparecidosBgmClip != null)
        {
            bgmSource.clip = desaparecidosBgmClip;
            bgmSource.volume = isMuted ? 0f : bgmVolume;
            bgmSource.loop = true;
            bgmSource.Play();
        }
        else
        {
            // Reintentar en 0.5s si aún está cargando desde disco
            StartCoroutine(RetryPlayBgm());
        }
    }

    IEnumerator RetryPlayBgm()
    {
        yield return new WaitForSeconds(0.4f);
        if (desaparecidosBgmClip != null && !bgmSource.isPlaying)
        {
            bgmSource.clip = desaparecidosBgmClip;
            bgmSource.volume = isMuted ? 0f : bgmVolume;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        if (bgmSource != null)
            bgmSource.volume = isMuted ? 0f : bgmVolume;
        if (sfxSource != null)
            sfxSource.volume = isMuted ? 0f : sfxVolume;
    }

    public void SetVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null && !isMuted)
            bgmSource.volume = bgmVolume;
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || isMuted || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    public void PlayClickSFX()
    {
        // Generar un click suave en procedural si no hay clip
        PlayProceduralTone(520f, 0.05f, 0.35f);
    }

    public void PlayLockedSFX()
    {
        // Tono doble de error / acceso denegado
        StartCoroutine(PlayLockedBeepRoutine());
    }

    IEnumerator PlayLockedBeepRoutine()
    {
        PlayProceduralTone(220f, 0.09f, 0.5f);
        yield return new WaitForSecondsRealtime(0.12f);
        PlayProceduralTone(180f, 0.12f, 0.6f);
    }

    public void PlayProceduralTone(float frequency, float duration, float volume = 0.5f)
    {
        if (isMuted || sfxSource == null) return;

        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1f - (float)i / sampleCount; // Decaimiento lineal
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;
        }

        AudioClip toneClip = AudioClip.Create("ProcTone", sampleCount, 1, sampleRate, false);
        toneClip.SetData(samples, 0);
        sfxSource.PlayOneShot(toneClip, volume * sfxVolume);
    }

    public void PlayDamage()
    {
        PlayProceduralTone(140f, 0.15f, 0.7f);
    }

    public void PlayDig()
    {
        PlayProceduralTone(280f, 0.08f, 0.5f);
    }

    public void PlayObjective()
    {
        PlayProceduralTone(660f, 0.35f, 0.8f);
    }

    public void PlayWaterPickup()
    {
        // Sonido cristalino de agua fresca (arpegio ascendente)
        StartCoroutine(PlayWaterRoutine());
    }

    IEnumerator PlayWaterRoutine()
    {
        PlayProceduralTone(523.25f, 0.08f, 0.5f); // Do
        yield return new WaitForSecondsRealtime(0.06f);
        PlayProceduralTone(659.25f, 0.08f, 0.55f); // Mi
        yield return new WaitForSecondsRealtime(0.06f);
        PlayProceduralTone(783.99f, 0.14f, 0.6f); // Sol
    }

    public void PlayClueFound()
    {
        // Acorde solemne y esperanzador al registrar una evidencia en el diario
        StartCoroutine(PlayClueRoutine());
    }

    IEnumerator PlayClueRoutine()
    {
        PlayProceduralTone(440f, 0.22f, 0.65f); // La
        yield return new WaitForSecondsRealtime(0.08f);
        PlayProceduralTone(554.37f, 0.25f, 0.7f); // Do#
        yield return new WaitForSecondsRealtime(0.08f);
        PlayProceduralTone(659.25f, 0.40f, 0.8f); // Mi
    }

    public void PlayCheckpointReached()
    {
        // Campanada de esperanza
        StartCoroutine(PlayCheckpointRoutine());
    }

    IEnumerator PlayCheckpointRoutine()
    {
        PlayProceduralTone(587.33f, 0.12f, 0.6f); // Re
        yield return new WaitForSecondsRealtime(0.09f);
        PlayProceduralTone(880f, 0.35f, 0.75f); // La
    }

    public void PlayEnemyAlert()
    {
        // Advertencia dramática del presidente
        PlayProceduralTone(190f, 0.18f, 0.85f);
    }

    public void PlayFootstep()
    {
        PlayProceduralTone(110f, 0.04f, 0.25f);
    }

    public void PlayDoorOpen()
    {
        PlayProceduralTone(330f, 0.15f, 0.5f);
    }
}
