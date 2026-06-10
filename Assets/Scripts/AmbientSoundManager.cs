using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton-менеджер звуков окружения.
/// Управляет фоновыми звуками: шаги, скрипы металла, далёкие жуткие звуки,
/// постоянный гул вентиляции. Поддерживает 3D позиционирование и реверберацию.
/// </summary>
public class AmbientSoundManager : MonoBehaviour
{
    public static AmbientSoundManager Instance { get; private set; }

    // ─── Категории звуков ─────────────────────────────────────────────────────

    [Header("Footstep Sounds (Corridors)")]
    [Tooltip("Случайные шаги в коридорах")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private float footstepIntervalMin = 20f;
    [SerializeField] private float footstepIntervalMax = 60f;
    [SerializeField] private float footstepVolume      = 0.4f;

    [Header("Metal Creak Sounds")]
    [Tooltip("Скрипы металлических конструкций")]
    [SerializeField] private AudioClip[] creakSounds;
    [SerializeField] private float creakIntervalMin = 30f;
    [SerializeField] private float creakIntervalMax = 90f;
    [SerializeField] private float creakVolume      = 0.35f;

    [Header("Distant Sounds")]
    [Tooltip("Далёкие неопознанные жуткие звуки")]
    [SerializeField] private AudioClip[] distantSounds;
    [SerializeField] private float distantIntervalMin = 45f;
    [SerializeField] private float distantIntervalMax = 120f;
    [SerializeField] private float distantVolume      = 0.2f;

    // ─── Вентиляция (loop) ────────────────────────────────────────────────────

    [Header("Ventilation Hum")]
    [Tooltip("Постоянный тихий гул вентиляции (должен быть loop AudioClip)")]
    [SerializeField] private AudioClip ventilationHum;
    [SerializeField] private float ventilationVolume = 0.12f;

    // ─── AudioSources ─────────────────────────────────────────────────────────

    [Header("Audio Sources")]
    [Tooltip("AudioSource для loop звуков (вентиляция)")]
    [SerializeField] private AudioSource loopSource;
    [Tooltip("AudioSource для 2D ambient (атмосферные)")]
    [SerializeField] private AudioSource ambientSource2D;

    // ─── 3D Sound Positions ───────────────────────────────────────────────────

    [Header("3D Sound Positions")]
    [Tooltip("Точки в пространстве, из которых идут 3D звуки (имитация ресторана)")]
    [SerializeField] private Transform[] soundEmitterPositions;
    [Tooltip("Дальность слышимости 3D звуков")]
    [SerializeField] private float sound3DMaxDistance = 25f;

    // ─── Реверберация при камерах ─────────────────────────────────────────────

    [Header("Camera Reverb")]
    [Tooltip("AudioMixerSnapshot для режима камер (усиленная реверберация)")]
    [SerializeField] private AudioReverbFilter reverbFilter;
    [SerializeField] private float reverbWetNormal = 0.1f;
    [SerializeField] private float reverbWetCamera = 0.65f;
    [SerializeField] private float reverbTransitionTime = 0.4f;

    // ─── Пул 3D AudioSources ──────────────────────────────────────────────────

    private List<AudioSource> _pool3D = new List<AudioSource>();
    private const int POOL_SIZE = 6;

    // ─── Coroutines ───────────────────────────────────────────────────────────

    private Coroutine _footstepRoutine;
    private Coroutine _creakRoutine;
    private Coroutine _distantRoutine;
    private Coroutine _reverbCoroutine;

    // ─────────────────────────────────────────────────────────────────────────
    // Жизненный цикл
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializePool();
    }

    private void Start()
    {
        StartVentilation();

        _footstepRoutine = StartCoroutine(AmbientLoop(footstepSounds, footstepIntervalMin,
                                                       footstepIntervalMax, footstepVolume, true));
        _creakRoutine    = StartCoroutine(AmbientLoop(creakSounds, creakIntervalMin,
                                                       creakIntervalMax, creakVolume, true));
        _distantRoutine  = StartCoroutine(AmbientLoop(distantSounds, distantIntervalMin,
                                                       distantIntervalMax, distantVolume, false));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Публичный API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Переключает реверберацию при открытии/закрытии камер видеонаблюдения.
    /// </summary>
    public void SetCameraMode(bool cameraOpen)
    {
        if (_reverbCoroutine != null)
            StopCoroutine(_reverbCoroutine);

        _reverbCoroutine = StartCoroutine(
            TransitionReverb(cameraOpen ? reverbWetCamera : reverbWetNormal));
    }

    /// <summary>
    /// Проигрывает случайный ambient звук — вызывается из AtmosphereManager.
    /// </summary>
    public void PlayRandomAmbient()
    {
        // Объединяем все ambient-категории и выбираем случайный
        List<AudioClip> allAmbient = new List<AudioClip>();

        if (footstepSounds != null) allAmbient.AddRange(footstepSounds);
        if (creakSounds    != null) allAmbient.AddRange(creakSounds);
        if (distantSounds  != null) allAmbient.AddRange(distantSounds);

        if (allAmbient.Count == 0) return;

        AudioClip clip = allAmbient[Random.Range(0, allAmbient.Count)];
        PlayAt2D(clip, distantVolume);
    }

    /// <summary>
    /// Воспроизводит звук шагов из случайной 3D позиции.
    /// </summary>
    public void PlayFootstep()
    {
        PlayRandom3D(footstepSounds, footstepVolume);
    }

    /// <summary>
    /// Воспроизводит скрип металла из случайной 3D позиции.
    /// </summary>
    public void PlayCreak()
    {
        PlayRandom3D(creakSounds, creakVolume);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Приватные методы
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Создаёт пул 3D AudioSource на отдельных GameObject.
    /// </summary>
    private void InitializePool()
    {
        for (int i = 0; i < POOL_SIZE; i++)
        {
            GameObject go = new GameObject($"AmbientSource3D_{i}");
            go.transform.SetParent(transform);

            AudioSource src = go.AddComponent<AudioSource>();
            src.spatialBlend  = 1.0f; // полностью 3D
            src.rolloffMode   = AudioRolloffMode.Logarithmic;
            src.maxDistance   = sound3DMaxDistance;
            src.minDistance   = 1f;
            src.playOnAwake   = false;
            src.loop          = false;

            _pool3D.Add(src);
        }
    }

    private void StartVentilation()
    {
        if (loopSource == null || ventilationHum == null) return;

        loopSource.clip   = ventilationHum;
        loopSource.volume = ventilationVolume;
        loopSource.loop   = true;
        loopSource.spatialBlend = 0f; // 2D фон
        loopSource.Play();
    }

    /// <summary>
    /// Универсальный цикл для периодического воспроизведения случайных звуков.
    /// use3D: true = из случайной 3D точки, false = 2D ambient.
    /// </summary>
    private IEnumerator AmbientLoop(AudioClip[] clips, float minInterval, float maxInterval,
                                     float volume, bool use3D)
    {
        while (true)
        {
            float delay = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(delay);

            if (clips == null || clips.Length == 0) continue;

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip == null) continue;

            if (use3D)
                PlayAt3D(clip, volume);
            else
                PlayAt2D(clip, volume);
        }
    }

    /// <summary>
    /// Воспроизводит звук из случайной позиции в soundEmitterPositions.
    /// Если точек нет — играет рядом с центром сцены в случайном радиусе.
    /// </summary>
    private void PlayAt3D(AudioClip clip, float volume)
    {
        AudioSource src = GetFreeSource3D();
        if (src == null) return;

        // Позиция эмиттера
        if (soundEmitterPositions != null && soundEmitterPositions.Length > 0)
        {
            int idx = Random.Range(0, soundEmitterPositions.Length);
            if (soundEmitterPositions[idx] != null)
                src.transform.position = soundEmitterPositions[idx].position;
        }
        else
        {
            // Случайная позиция в радиусе 10 м от центра
            src.transform.position = new Vector3(
                Random.Range(-10f, 10f),
                Random.Range(0f, 3f),
                Random.Range(-10f, 10f));
        }

        src.volume = volume * Random.Range(0.85f, 1.15f); // лёгкая вариация громкости
        src.pitch  = Random.Range(0.95f, 1.05f);
        src.PlayOneShot(clip);
    }

    private void PlayRandom3D(AudioClip[] clips, float volume)
    {
        if (clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null) PlayAt3D(clip, volume);
    }

    private void PlayAt2D(AudioClip clip, float volume)
    {
        if (ambientSource2D == null || clip == null) return;

        ambientSource2D.volume = volume * Random.Range(0.85f, 1.15f);
        ambientSource2D.pitch  = Random.Range(0.95f, 1.05f);
        ambientSource2D.PlayOneShot(clip);
    }

    /// <summary>
    /// Возвращает свободный (не играющий) 3D AudioSource из пула.
    /// </summary>
    private AudioSource GetFreeSource3D()
    {
        foreach (AudioSource src in _pool3D)
        {
            if (src != null && !src.isPlaying)
                return src;
        }

        // Все заняты — возвращаем первый (перебиваем самый старый)
        return _pool3D.Count > 0 ? _pool3D[0] : null;
    }

    /// <summary>
    /// Плавный переход wet-уровня AudioReverbFilter.
    /// </summary>
    private IEnumerator TransitionReverb(float targetWet)
    {
        if (reverbFilter == null) yield break;

        float startWet = reverbFilter.reverbLevel;
        float elapsed  = 0f;

        while (elapsed < reverbTransitionTime)
        {
            elapsed += Time.deltaTime;
            reverbFilter.reverbLevel = Mathf.Lerp(startWet, targetWet,
                                                   elapsed / reverbTransitionTime);
            yield return null;
        }

        reverbFilter.reverbLevel = targetWet;
    }
}
