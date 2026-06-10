using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Пауза-меню с настройками графики, разрешения и звука.
/// Открывается по ESC во время игры.
/// 
/// ВАЖНО: Этот скрипт работает ТОЛЬКО в игровой сцене.
/// Для главного меню используй MenuButtons.cs
/// </summary>
public class GraphicsSettings : MonoBehaviour
{
    [Header("Панели")]
    [SerializeField] GameObject pausePanel;
    [SerializeField] GameObject gameHUD;

    [Header("Настройки качества")]
    [SerializeField] TMP_Dropdown qualityDropdown;
    // Варианты: Performant / Balanced / HighFidelity
    // (совпадают с QualitySettings в ProjectSettings)

    [Header("Настройки разрешения")]
    [SerializeField] TMP_Dropdown resolutionDropdown;
    [SerializeField] Toggle fullscreenToggle;

    [Header("Настройки звука")]
    [SerializeField] Slider masterVolumeSlider;
    [SerializeField] Slider sfxVolumeSlider;
    [SerializeField] Slider musicVolumeSlider;
    [SerializeField] Slider phoneGuyVolumeSlider;   // громкость Фон Гая

    [Header("Графика — дополнительно")]
    [SerializeField] Toggle shadowsToggle;
    [SerializeField] Toggle postProcessingToggle;
    [SerializeField] TMP_Dropdown shadowQualityDropdown;

    [Header("Экран — дополнительно")]
    [SerializeField] Toggle vsyncToggle;

    [Header("Управление")]
    [SerializeField] Slider mouseSensitivitySlider;  // 1..10
    [SerializeField] Toggle invertYToggle;

    [Header("Вкладки настроек")]
    [SerializeField] GameObject soundTab;
    [SerializeField] GameObject graphicsTab;
    [SerializeField] GameObject screenTab;
    [SerializeField] GameObject controlsTab;

    [Header("Текст — текущие настройки")]
    [SerializeField] TextMeshProUGUI fpsText;

    Resolution[] availableResolutions;
    bool isPaused = false;
    float fpsTimer = 0f;
    PlayerMovement _player;

    /// <summary>Открыто ли меню паузы (для PlayerMovement).</summary>
    public bool IsPaused => isPaused;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        _player = FindFirstObjectByType<PlayerMovement>();
        LoadAndApplySettings();
        BuildResolutionDropdown();
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    void Update()
    {
        // ESC открывает/закрывает паузу только если игрок жив
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool playerDead = (_player != null && !_player.alive);
            if (!playerDead)
                TogglePause();
        }

        // FPS счётчик
        if (fpsText != null)
        {
            fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer > 0.5f)
            {
                fpsText.text = "FPS: " + Mathf.RoundToInt(1f / Time.unscaledDeltaTime);
                fpsTimer = 0f;
            }
        }
    }

    // ─── Пауза ───────────────────────────────────────────────────────────────
    public void TogglePause()
    {
        isPaused = !isPaused;
        ApplyPauseState(isPaused);
    }

    public void Resume()
    {
        isPaused = false;
        ApplyPauseState(false);
    }

    void ApplyPauseState(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;
        if (pausePanel != null) pausePanel.SetActive(paused);
        if (gameHUD != null)    gameHUD.SetActive(!paused);
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = paused;
    }

    // ─── Разрешения ──────────────────────────────────────────────────────────
    void BuildResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        availableResolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        var options = new List<string>();
        int savedIndex = 0;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            options.Add($"{availableResolutions[i].width}×{availableResolutions[i].height}");
            if (availableResolutions[i].width  == Screen.currentResolution.width &&
                availableResolutions[i].height == Screen.currentResolution.height)
                savedIndex = i;
        }

        resolutionDropdown.AddOptions(options);
        int savedRes = PlayerPrefs.GetInt("Resolution", savedIndex);
        resolutionDropdown.value = Mathf.Clamp(savedRes, 0, options.Count - 1);
        resolutionDropdown.RefreshShownValue();
    }

    public void OnResolutionChanged(int index)
    {
        if (availableResolutions == null || index >= availableResolutions.Length) return;
        var r = availableResolutions[index];
        Screen.SetResolution(r.width, r.height, Screen.fullScreen);
        PlayerPrefs.SetInt("Resolution", index);
    }

    public void OnFullscreenToggled(bool value)
    {
        Screen.fullScreen = value;
        PlayerPrefs.SetInt("Fullscreen", value ? 1 : 0);
    }

    // ─── Качество ────────────────────────────────────────────────────────────
    public void OnQualityChanged(int value)
    {
        QualitySettings.SetQualityLevel(value, true);
        PlayerPrefs.SetInt("QualityLevel", value);
    }

    // ─── Звук ─────────────────────────────────────────────────────────────────
    public void OnMasterVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        // SFX громкость применяется через AudioMixer — если используется
    }

    public void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public void OnPhoneGuyVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("PhoneGuyVolume", value);
    }

    // ─── Графика — тени и пост-обработка ──────────────────────────────────────
    public void OnShadowsToggled(bool value)
    {
        QualitySettings.shadows = value ? ShadowQuality.All : ShadowQuality.Disable;
        PlayerPrefs.SetInt("Shadows", value ? 1 : 0);
    }

    public void OnPostProcessingToggled(bool value)
    {
        PlayerPrefs.SetInt("PostProcessing", value ? 1 : 0);
        if (PostProcessingController.Instance != null)
            PostProcessingController.Instance.SetEnabled(value);
    }

    public void OnShadowQualityChanged(int index)
    {
        // 0 = Низкое, 1 = Среднее, 2 = Высокое
        switch (index)
        {
            case 0: QualitySettings.shadowResolution = ShadowResolution.Low;    break;
            case 1: QualitySettings.shadowResolution = ShadowResolution.Medium; break;
            default: QualitySettings.shadowResolution = ShadowResolution.High;  break;
        }
        PlayerPrefs.SetInt("ShadowQuality", index);
    }

    // ─── Экран — VSync ────────────────────────────────────────────────────────
    public void OnVSyncToggled(bool value)
    {
        QualitySettings.vSyncCount = value ? 1 : 0;
        PlayerPrefs.SetInt("VSync", value ? 1 : 0);
    }

    // ─── Управление ───────────────────────────────────────────────────────────
    public void OnMouseSensitivityChanged(float value)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", value);
    }

    public void OnInvertYToggled(bool value)
    {
        PlayerPrefs.SetInt("InvertY", value ? 1 : 0);
    }

    // ─── Именованные методы применения (по ТЗ) ────────────────────────────────
    public void ApplyResolution(int index) => OnResolutionChanged(index);
    public void ApplyQuality(int index)    => OnQualityChanged(index);
    public void ApplyVolume(float value)   => OnMasterVolumeChanged(value);

    // ─── Переключение вкладок ─────────────────────────────────────────────────
    public void ShowSoundTab()    => SwitchTab(0);
    public void ShowGraphicsTab() => SwitchTab(1);
    public void ShowScreenTab()   => SwitchTab(2);
    public void ShowControlsTab() => SwitchTab(3);

    void SwitchTab(int tab)
    {
        if (soundTab    != null) soundTab.SetActive(tab == 0);
        if (graphicsTab != null) graphicsTab.SetActive(tab == 1);
        if (screenTab   != null) screenTab.SetActive(tab == 2);
        if (controlsTab != null) controlsTab.SetActive(tab == 3);
    }

    // ─── Загрузка/применение сохранённых настроек ────────────────────────────
    void LoadAndApplySettings()
    {
        float master  = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float sfx     = PlayerPrefs.GetFloat("SFXVolume", 1f);
        float music   = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        float phone   = PlayerPrefs.GetFloat("PhoneGuyVolume", 0.85f);
        int quality   = PlayerPrefs.GetInt("QualityLevel", 1);   // Balanced по умолчанию
        bool fullscr  = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        bool shadows  = PlayerPrefs.GetInt("Shadows", 1) == 1;
        bool postFx   = PlayerPrefs.GetInt("PostProcessing", 1) == 1;
        int shadowQ   = PlayerPrefs.GetInt("ShadowQuality", 2);
        bool vsync    = PlayerPrefs.GetInt("VSync", 1) == 1;
        float mouseSn = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
        bool invertY  = PlayerPrefs.GetInt("InvertY", 0) == 1;

        AudioListener.volume = master;
        QualitySettings.SetQualityLevel(quality, true);
        Screen.fullScreen = fullscr;
        QualitySettings.shadows = shadows ? ShadowQuality.All : ShadowQuality.Disable;
        QualitySettings.vSyncCount = vsync ? 1 : 0;
        OnShadowQualityChanged(shadowQ);
        if (PostProcessingController.Instance != null)
            PostProcessingController.Instance.SetEnabled(postFx);

        if (masterVolumeSlider != null) masterVolumeSlider.value = master;
        if (sfxVolumeSlider != null)    sfxVolumeSlider.value    = sfx;
        if (musicVolumeSlider != null)  musicVolumeSlider.value  = music;
        if (phoneGuyVolumeSlider != null) phoneGuyVolumeSlider.value = phone;
        if (qualityDropdown != null)    qualityDropdown.value    = quality;
        if (fullscreenToggle != null)   fullscreenToggle.isOn    = fullscr;
        if (shadowsToggle != null)      shadowsToggle.isOn       = shadows;
        if (postProcessingToggle != null) postProcessingToggle.isOn = postFx;
        if (shadowQualityDropdown != null) shadowQualityDropdown.value = shadowQ;
        if (vsyncToggle != null)        vsyncToggle.isOn         = vsync;
        if (mouseSensitivitySlider != null) mouseSensitivitySlider.value = mouseSn;
        if (invertYToggle != null)      invertYToggle.isOn       = invertY;

        // Открываем вкладку «Звук» по умолчанию
        SwitchTab(0);
    }

    public void SaveAndClose()
    {
        PlayerPrefs.Save();
        Resume();
    }
}
