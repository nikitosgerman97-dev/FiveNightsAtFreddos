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

    // ─── Загрузка/применение сохранённых настроек ────────────────────────────
    void LoadAndApplySettings()
    {
        float master  = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float sfx     = PlayerPrefs.GetFloat("SFXVolume", 1f);
        float music   = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        int quality   = PlayerPrefs.GetInt("QualityLevel", 1);   // Balanced по умолчанию
        bool fullscr  = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        AudioListener.volume = master;
        QualitySettings.SetQualityLevel(quality, true);
        Screen.fullScreen = fullscr;

        if (masterVolumeSlider != null) masterVolumeSlider.value = master;
        if (sfxVolumeSlider != null)    sfxVolumeSlider.value    = sfx;
        if (musicVolumeSlider != null)  musicVolumeSlider.value  = music;
        if (qualityDropdown != null)    qualityDropdown.value    = quality;
        if (fullscreenToggle != null)   fullscreenToggle.isOn    = fullscr;
    }

    public void SaveAndClose()
    {
        PlayerPrefs.Save();
        Resume();
    }
}
