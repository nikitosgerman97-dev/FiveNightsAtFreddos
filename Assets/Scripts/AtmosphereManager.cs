using System.Collections;
using UnityEngine;

/// <summary>
/// Управляет атмосферой сцены: мигание светильников, частицы пыли,
/// ambient-звуки, туман. Усиливает напряжение при высоких номерах ночей.
/// </summary>
public class AtmosphereManager : MonoBehaviour
{
    public static AtmosphereManager Instance { get; private set; }

    // ─── Светильники ──────────────────────────────────────────────────────────

    [Header("Flicker Lights")]
    [SerializeField] private Light[] flickerLights;
    [Tooltip("Минимальный интервал между миганиями (сек)")]
    [SerializeField] private float flickerIntervalMin = 30f;
    [Tooltip("Максимальный интервал между миганиями (сек)")]
    [SerializeField] private float flickerIntervalMax = 90f;
    [Tooltip("Минимальное количество миганий за один раз")]
    [SerializeField] private int flickerCountMin = 2;
    [Tooltip("Максимальное количество миганий за один раз")]
    [SerializeField] private int flickerCountMax = 5;
    [Tooltip("Длительность одного мигания (сек)")]
    [SerializeField] private float flickerOnDuration  = 0.05f;
    [SerializeField] private float flickerOffDuration = 0.08f;

    // ─── Частицы пыли ─────────────────────────────────────────────────────────

    [Header("Dust Particles")]
    [SerializeField] private ParticleSystem dustParticleSystem;

    // ─── Ambient звуки ────────────────────────────────────────────────────────

    [Header("Ambient Sounds")]
    [SerializeField] private AudioClip[] ambientSounds;
    [SerializeField] private AudioSource ambientAudioSource;
    [Tooltip("Минимальный интервал между ambient-звуками (сек)")]
    [SerializeField] private float ambientIntervalMin = 45f;
    [Tooltip("Максимальный интервал между ambient-звуками (сек)")]
    [SerializeField] private float ambientIntervalMax = 120f;
    [SerializeField] private float ambientVolumeMin   = 0.15f;
    [SerializeField] private float ambientVolumeMax   = 0.30f;

    // ─── Туман ────────────────────────────────────────────────────────────────

    [Header("Fog Settings")]
    [SerializeField] private float fogDensityNormal  = 0.02f;
    [SerializeField] private float fogDensityTense   = 0.08f;
    [SerializeField] private float fogTransitionTime = 3.5f;

    // ─── Освещение ────────────────────────────────────────────────────────────

    [Header("Ambient Light Settings")]
    [SerializeField] private float ambientIntensityNormal = 0.15f;
    [SerializeField] private float ambientIntensityTense  = 0.05f;

    // ─── Состояние ────────────────────────────────────────────────────────────

    private bool   _isTense       = false;
    private int    _currentNight  = 1;
    private Coroutine _fogCoroutine;
    private Coroutine _flickerCoroutine;
    private Coroutine _ambientCoroutine;

    // Модификаторы для поздних ночей
    private float FlickerIntervalMultiplier => _currentNight > 3 ? 0.5f : 1.0f;
    private float FogTenseDensity          => _currentNight > 3 ? fogDensityTense * 1.4f : fogDensityTense;

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
    }

    private void Start()
    {
        // Инициализируем туман из настроек сцены
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = fogDensityNormal;
        RenderSettings.ambientIntensity = ambientIntensityNormal;

        // Запускаем пыль
        if (dustParticleSystem != null)
            dustParticleSystem.Play();

        // Запускаем Coroutines
        _flickerCoroutine = StartCoroutine(FlickerRoutine());
        _ambientCoroutine = StartCoroutine(AmbientSoundRoutine());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Публичный API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Устанавливает номер текущей ночи для масштабирования интенсивности.
    /// </summary>
    public void SetNight(int nightNumber)
    {
        _currentNight = Mathf.Max(1, nightNumber);
    }

    /// <summary>
    /// Включает/выключает напряжённую атмосферу:
    /// плавно меняет плотность тумана и яркость окружающего освещения.
    /// </summary>
    public void SetTenseAtmosphere(bool tense)
    {
        if (_isTense == tense) return;
        _isTense = tense;

        if (_fogCoroutine != null)
            StopCoroutine(_fogCoroutine);

        _fogCoroutine = StartCoroutine(FogTransitionCoroutine(tense));
    }

    /// <summary>
    /// Проигрывает случайный ambient-звук. Может вызываться извне.
    /// </summary>
    public void PlayRandomAmbient()
    {
        if (ambientAudioSource == null || ambientSounds == null || ambientSounds.Length == 0)
            return;

        AudioClip clip = ambientSounds[Random.Range(0, ambientSounds.Length)];
        if (clip == null) return;

        ambientAudioSource.volume = Random.Range(ambientVolumeMin, ambientVolumeMax);
        ambientAudioSource.PlayOneShot(clip);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Coroutines
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Периодически заставляет один случайный светильник мигнуть несколько раз.
    /// Интервалы уменьшаются при ночи > 3.
    /// </summary>
    private IEnumerator FlickerRoutine()
    {
        while (true)
        {
            float interval = Random.Range(
                flickerIntervalMin * FlickerIntervalMultiplier,
                flickerIntervalMax * FlickerIntervalMultiplier);

            yield return new WaitForSeconds(interval);

            if (flickerLights == null || flickerLights.Length == 0) continue;

            // Выбираем случайный светильник
            int idx = Random.Range(0, flickerLights.Length);
            Light light = flickerLights[idx];

            if (light == null) continue;

            int count = Random.Range(flickerCountMin, flickerCountMax + 1);

            // Мигаем нужное число раз
            for (int i = 0; i < count; i++)
            {
                light.enabled = false;
                yield return new WaitForSeconds(flickerOffDuration);
                light.enabled = true;
                yield return new WaitForSeconds(flickerOnDuration);
            }
        }
    }

    /// <summary>
    /// Периодически проигрывает случайный ambient-звук с рандомной тихой громкостью.
    /// </summary>
    private IEnumerator AmbientSoundRoutine()
    {
        while (true)
        {
            float interval = Random.Range(ambientIntervalMin, ambientIntervalMax);
            yield return new WaitForSeconds(interval);
            PlayRandomAmbient();
        }
    }

    /// <summary>
    /// Плавно интерполирует плотность тумана и интенсивность окружающего света.
    /// </summary>
    private IEnumerator FogTransitionCoroutine(bool goTense)
    {
        float startDensity   = RenderSettings.fogDensity;
        float targetDensity  = goTense ? FogTenseDensity : fogDensityNormal;

        float startAmbient   = RenderSettings.ambientIntensity;
        float targetAmbient  = goTense ? ambientIntensityTense : ambientIntensityNormal;

        float elapsed = 0f;

        while (elapsed < fogTransitionTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fogTransitionTime;

            RenderSettings.fogDensity       = Mathf.Lerp(startDensity,  targetDensity,  t);
            RenderSettings.ambientIntensity = Mathf.Lerp(startAmbient, targetAmbient, t);

            yield return null;
        }

        // Гарантируем точные конечные значения
        RenderSettings.fogDensity       = targetDensity;
        RenderSettings.ambientIntensity = targetAmbient;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        flickerIntervalMin = Mathf.Max(1f, flickerIntervalMin);
        flickerIntervalMax = Mathf.Max(flickerIntervalMin + 1f, flickerIntervalMax);
        ambientIntervalMin = Mathf.Max(1f, ambientIntervalMin);
        ambientIntervalMax = Mathf.Max(ambientIntervalMin + 1f, ambientIntervalMax);
    }
#endif
}
