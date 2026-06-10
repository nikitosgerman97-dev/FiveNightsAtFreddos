using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Динамически управляет post-processing эффектами в зависимости от состояния игры.
/// Требует Volume компонент на том же GameObject или ссылку через Inspector.
/// </summary>
public class PostProcessingController : MonoBehaviour
{
    public static PostProcessingController Instance { get; private set; }

    // ─── Ссылки ───────────────────────────────────────────────────────────────

    [Header("Volume")]
    [SerializeField] private Volume globalVolume;

    // ─── Настройки виньетки ───────────────────────────────────────────────────

    [Header("Vignette Settings")]
    [SerializeField] private float vignetteNormal        = 0.25f;
    [SerializeField] private float vignetteLowPower      = 0.55f;
    [SerializeField] private float vignettePulseMin      = 0.35f;
    [SerializeField] private float vignettePulseMax      = 0.65f;
    [SerializeField] private float vignettePulseSpeed    = 3.5f;
    [SerializeField] private float vignetteJumpscare     = 0.9f;

    // ─── Настройки Bloom ──────────────────────────────────────────────────────

    [Header("Bloom Settings")]
    [SerializeField] private float bloomNormal           = 0.3f;
    [SerializeField] private float bloomJumpscare        = 8.0f;
    [SerializeField] private float bloomRecoverDuration  = 1.2f;

    // ─── Настройки хроматической аберрации ───────────────────────────────────

    [Header("Chromatic Aberration Settings")]
    [SerializeField] private float chromaticNormal       = 0.0f;
    [SerializeField] private float chromaticLowPower     = 0.35f;
    [SerializeField] private float chromaticJumpscare    = 1.0f;

    // ─── Настройки зерна плёнки ───────────────────────────────────────────────

    [Header("Film Grain Settings")]
    [SerializeField] private float grainNormal           = 0.1f;
    [SerializeField] private float grainCameraActive     = 0.35f;
    [SerializeField] private float grainJumpscare        = 0.6f;

    // ─── Настройки Color Adjustments ─────────────────────────────────────────

    [Header("Color Adjustments Settings")]
    [SerializeField] private float saturationNormal      =  0.0f;
    [SerializeField] private float saturationLowPower    = -25.0f;

    // ─── Внутренние компоненты Volume ─────────────────────────────────────────

    private Vignette            _vignette;
    private Bloom               _bloom;
    private ChromaticAberration _chromatic;
    private FilmGrain           _filmGrain;
    private ColorAdjustments    _colorAdj;

    // ─── Состояние ────────────────────────────────────────────────────────────

    private float _currentPowerLevel = 1.0f; // 0..1
    private bool  _isPulsing         = false;
    private bool  _cameraIsActive    = false;
    private bool  _isJumpscareActive = false;

    private Coroutine _pulseCoroutine;
    private Coroutine _jumpscareCoroutine;

    // ─────────────────────────────────────────────────────────────────────────
    // Жизненный цикл
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeVolumeComponents();
    }

    /// <summary>
    /// Получаем компоненты из VolumeProfile.
    /// </summary>
    private void InitializeVolumeComponents()
    {
        if (globalVolume == null)
        {
            globalVolume = GetComponent<Volume>();
        }

        if (globalVolume == null)
        {
            Debug.LogError("[PostProcessingController] Не найден компонент Volume!");
            return;
        }

        VolumeProfile profile = globalVolume.profile;

        // Пробуем получить каждый компонент — если не нашли, логируем предупреждение
        if (!profile.TryGet(out _vignette))
            Debug.LogWarning("[PostProcessingController] Vignette не найдена в VolumeProfile.");

        if (!profile.TryGet(out _bloom))
            Debug.LogWarning("[PostProcessingController] Bloom не найден в VolumeProfile.");

        if (!profile.TryGet(out _chromatic))
            Debug.LogWarning("[PostProcessingController] ChromaticAberration не найдена в VolumeProfile.");

        if (!profile.TryGet(out _filmGrain))
            Debug.LogWarning("[PostProcessingController] FilmGrain не найдена в VolumeProfile.");

        if (!profile.TryGet(out _colorAdj))
            Debug.LogWarning("[PostProcessingController] ColorAdjustments не найдены в VolumeProfile.");

        // Применяем нормальные значения при старте
        ApplyNormalState();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Публичный API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Устанавливает уровень энергии (0 = пусто, 1 = полная).
    /// Меняет атмосферу: виньетка, хроматическая аберрация, насыщенность.
    /// </summary>
    public void SetPowerLevel(float normalizedPower)
    {
        _currentPowerLevel = Mathf.Clamp01(normalizedPower);

        if (_isJumpscareActive) return; // не перебиваем скример

        bool isLowPower = _currentPowerLevel < 0.2f;

        // Плавно интерполируем значения в зависимости от уровня энергии
        float t = Mathf.InverseLerp(0.2f, 0.0f, _currentPowerLevel); // 0 при 20%, 1 при 0%

        if (_vignette != null && !_isPulsing)
        {
            _vignette.intensity.Override(Mathf.Lerp(vignetteNormal, vignetteLowPower, t));
        }

        if (_chromatic != null)
        {
            _chromatic.intensity.Override(Mathf.Lerp(chromaticNormal, chromaticLowPower, t));
        }

        if (_colorAdj != null)
        {
            _colorAdj.saturation.Override(Mathf.Lerp(saturationNormal, saturationLowPower, t));
        }

        // Запускаем пульсацию виньетки при низкой энергии
        if (isLowPower && !_isPulsing)
        {
            StartVignettePulse();
        }
        else if (!isLowPower && _isPulsing)
        {
            StopVignettePulse();
        }
    }

    /// <summary>
    /// Вызывается при появлении аниматроника у двери — пульсирующая виньетка.
    /// </summary>
    public void TriggerAnimatronicAtDoor(bool active)
    {
        if (_isJumpscareActive) return;

        if (active && !_isPulsing)
        {
            StartVignettePulse();
        }
        else if (!active && _isPulsing && _currentPowerLevel >= 0.2f)
        {
            StopVignettePulse();
        }
    }

    /// <summary>
    /// Резкая вспышка bloom + хроматическая аберрация на максимум — скример.
    /// </summary>
    public void TriggerJumpscare()
    {
        if (_jumpscareCoroutine != null)
            StopCoroutine(_jumpscareCoroutine);

        _jumpscareCoroutine = StartCoroutine(JumpscareEffectCoroutine());
    }

    /// <summary>
    /// Переключает усиление зерна плёнки при просмотре камер.
    /// </summary>
    public void SetCameraActive(bool isActive)
    {
        _cameraIsActive = isActive;

        if (_isJumpscareActive) return;

        if (_filmGrain != null)
        {
            float targetGrain = isActive ? grainCameraActive : grainNormal;
            if (_pulseCoroutine == null) // не перебиваем пульс
                _filmGrain.intensity.Override(targetGrain);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Приватные методы
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Устанавливает нормальные (базовые) значения всех эффектов.
    /// </summary>
    private void ApplyNormalState()
    {
        _vignette?.intensity.Override(vignetteNormal);
        _bloom?.intensity.Override(bloomNormal);
        _chromatic?.intensity.Override(chromaticNormal);
        _filmGrain?.intensity.Override(grainNormal);
        _colorAdj?.saturation.Override(saturationNormal);
    }

    private void StartVignettePulse()
    {
        _isPulsing = true;
        if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
        _pulseCoroutine = StartCoroutine(VignettePulseCoroutine());
    }

    private void StopVignettePulse()
    {
        _isPulsing = false;
        if (_pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = null;
        }

        // Возвращаем виньетку к нормальному значению
        if (_vignette != null)
            _vignette.intensity.Override(vignetteNormal);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Coroutines
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Плавная синусоидальная пульсация виньетки — напряжение у двери / низкая энергия.
    /// </summary>
    private IEnumerator VignettePulseCoroutine()
    {
        float timer = 0f;

        while (_isPulsing)
        {
            timer += Time.deltaTime * vignettePulseSpeed;
            float t = (Mathf.Sin(timer) + 1f) * 0.5f; // 0..1
            float intensity = Mathf.Lerp(vignettePulseMin, vignettePulseMax, t);

            if (_vignette != null)
                _vignette.intensity.Override(intensity);

            yield return null;
        }
    }

    /// <summary>
    /// Эффект скримера: резкая вспышка, затем плавное восстановление.
    /// </summary>
    private IEnumerator JumpscareEffectCoroutine()
    {
        _isJumpscareActive = true;

        // Мгновенно выставляем максимальные значения
        _vignette?.intensity.Override(vignetteJumpscare);
        _bloom?.intensity.Override(bloomJumpscare);
        _chromatic?.intensity.Override(chromaticJumpscare);
        _filmGrain?.intensity.Override(grainJumpscare);

        // Держим эффект 0.15 сек
        yield return new WaitForSeconds(0.15f);

        // Плавно возвращаемся к нормальным значениям
        float elapsed = 0f;
        while (elapsed < bloomRecoverDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / bloomRecoverDuration;

            _bloom?.intensity.Override(Mathf.Lerp(bloomJumpscare, bloomNormal, t));
            _chromatic?.intensity.Override(Mathf.Lerp(chromaticJumpscare,
                _currentPowerLevel < 0.2f ? chromaticLowPower : chromaticNormal, t));

            yield return null;
        }

        _isJumpscareActive = false;

        // После скримера — восстанавливаем состояние в зависимости от энергии
        SetPowerLevel(_currentPowerLevel);
        SetCameraActive(_cameraIsActive);
    }
}
