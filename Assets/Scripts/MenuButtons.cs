using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Кнопки главного меню.
/// Также управляет настройками звука и графики из меню.
/// </summary>
public class MenuButtons : MonoBehaviour
{
    [Header("Аудио")]
    [SerializeField] AudioSource menuMusic;
    [SerializeField] AudioSource nightCallAudio;        // телефонный гай в меню (если есть)

    [Header("Слайдеры громкости")]
    [SerializeField] Slider masterVolumeSlider;
    [SerializeField] Slider musicVolumeSlider;

    [Header("UI объекты")]
    [SerializeField] TextMeshProUGUI nightHoverText;    // "Ночь X" при наведении
    [SerializeField] TextMeshProUGUI loaderText;
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] GameObject settingsPanel;
    [SerializeField] GameObject extrasPanel;
    [SerializeField] GameObject howToPlayPanel;
    [SerializeField] GameObject newsPaperObj;           // газета при новой игре
    [SerializeField] GameObject nightCallMuteButton;
    [SerializeField] GameObject nightCallSubtitleObj;

    [Header("Настройки графики")]
    [SerializeField] TMP_Dropdown qualityDropdown;
    [SerializeField] TMP_Dropdown resolutionDropdown;
    [SerializeField] Toggle fullscreenToggle;

    // ─────────────────────────────────────────────────────────────────────────
    Resolution[] _resolutions;

    void Start()
    {
        // Гарантируем что timeScale сброшен (на случай если вышли из игры во время паузы)
        Time.timeScale = 1f;
        Application.targetFrameRate = 60;
        LoadSettings();
        BuildResolutionDropdown();
    }

    void BuildResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        _resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        var options = new List<string>();
        int currentIdx = 0;

        for (int i = 0; i < _resolutions.Length; i++)
        {
            options.Add($"{_resolutions[i].width}×{_resolutions[i].height}");
            if (_resolutions[i].width  == Screen.currentResolution.width &&
                _resolutions[i].height == Screen.currentResolution.height)
                currentIdx = i;
        }

        resolutionDropdown.AddOptions(options);
        int saved = PlayerPrefs.GetInt("Resolution", currentIdx);
        resolutionDropdown.value = Mathf.Clamp(saved, 0, options.Count - 1);
        resolutionDropdown.RefreshShownValue();
    }

    public void OnResolutionChanged(int index)
    {
        if (_resolutions == null || index >= _resolutions.Length) return;
        var r = _resolutions[index];
        Screen.SetResolution(r.width, r.height, Screen.fullScreen);
        PlayerPrefs.SetInt("Resolution", index);
    }

    // ─── Загрузка настроек ───────────────────────────────────────────────────
    void LoadSettings()
    {
        float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float music  = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        int quality  = PlayerPrefs.GetInt("QualityLevel", 1);
        bool fs      = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        AudioListener.volume = master;
        QualitySettings.SetQualityLevel(quality, true);
        Screen.fullScreen = fs;

        if (masterVolumeSlider != null) masterVolumeSlider.value = master;
        if (musicVolumeSlider  != null)
        {
            musicVolumeSlider.value = music;
            if (menuMusic != null) menuMusic.volume = music;
        }
        if (qualityDropdown  != null) qualityDropdown.value  = quality;
        if (fullscreenToggle != null) fullscreenToggle.isOn  = fs;
    }

    // ─── Слайдеры ─────────────────────────────────────────────────────────────
    public void OnMasterVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    public void OnMusicVolumeChanged(float value)
    {
        if (menuMusic != null) menuMusic.volume = value;
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public void OnQualityChanged(int value)
    {
        QualitySettings.SetQualityLevel(value, true);
        PlayerPrefs.SetInt("QualityLevel", value);
    }

    public void OnFullscreenToggled(bool value)
    {
        Screen.fullScreen = value;
        PlayerPrefs.SetInt("Fullscreen", value ? 1 : 0);
    }

    // ─── Навигация ────────────────────────────────────────────────────────────
    public void NewGameButton()
    {
        PlayerPrefs.SetInt("Night", 1);
        PlayerPrefs.SetString("CustomNight", "not active");

        if (newsPaperObj != null) newsPaperObj.SetActive(true);
        if (loaderText != null)   loaderText.SetText("Ночь 1");

        StartCoroutine(NewspaperDelay());
    }

    IEnumerator NewspaperDelay()
    {
        yield return new WaitForSecondsRealtime(11f);
        DisableMusic();
        LoadScene("SampleScene");
    }

    public void ContinueButton()
    {
        int night = PlayerPrefs.GetInt("Night", 1);
        PlayerPrefs.SetString("CustomNight", "not active");
        if (loaderText != null) loaderText.SetText("Ночь " + night);
        DisableMusic();
        LoadScene("SampleScene");
    }

    public void RetryButton()
    {
        int night = PlayerPrefs.GetInt("Night", 1);
        if (loaderText != null) loaderText.SetText("Ночь " + night);
        LoadScene("SampleScene");
    }

    public void BeginNextNightButton()
    {
        int night = PlayerPrefs.GetInt("Night", 1) + 1;
        PlayerPrefs.SetInt("Night", night);
        PlayerPrefs.SetString("CustomNight", "not active");
        if (loaderText != null) loaderText.SetText("Ночь " + night);
        DisableMusic();
        LoadScene("SampleScene");
    }

    public void CustomNightButton()
    {
        PlayerPrefs.SetString("CustomNight", "active");
        if (loaderText != null) loaderText.SetText("Кастомная ночь");
        DisableMusic();
        LoadScene("SampleScene");
    }

    public void BackToMenuButton()
    {
        // Сбрасываем timeScale на случай если игрок вышел в меню во время паузы
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // ─── Hover на кнопке «Продолжить» ────────────────────────────────────────
    public void ContinueHover()
    {
        if (nightHoverText != null)
        {
            nightHoverText.SetText("Ночь " + PlayerPrefs.GetInt("Night", 1));
            nightHoverText.gameObject.SetActive(true);
        }
    }

    public void ContinueUnhover()
    {
        if (nightHoverText != null)
            nightHoverText.gameObject.SetActive(false);
    }

    // ─── Телефонный гай ───────────────────────────────────────────────────────
    public void MutePhoneCall()
    {
        if (nightCallAudio != null) nightCallAudio.Stop();
        if (nightCallMuteButton != null)    nightCallMuteButton.SetActive(false);
        if (nightCallSubtitleObj != null)   nightCallSubtitleObj.SetActive(false);
        // Также останавливаем через TimePass (если мы в игровой сцене)
        TimePass tp = FindFirstObjectByType<TimePass>();
        if (tp != null) tp.StopPhoneCall();
    }

    public void MuteNightCall() => MutePhoneCall();

    // ─── Переключение панелей ─────────────────────────────────────────────────
    public void OpenSettings()
    {
        SwitchPanel(settingsPanel);
    }

    public void OpenExtras()
    {
        SwitchPanel(extrasPanel);
    }

    public void OpenHowToPlay()
    {
        SwitchPanel(howToPlayPanel);
    }

    public void BackToMain()
    {
        SwitchPanel(mainMenuPanel);
    }

    void SwitchPanel(GameObject target)
    {
        if (mainMenuPanel  != null) mainMenuPanel.SetActive(false);
        if (settingsPanel  != null) settingsPanel.SetActive(false);
        if (extrasPanel    != null) extrasPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        if (target         != null) target.SetActive(true);
    }

    // ─── Служебное ────────────────────────────────────────────────────────────
    void DisableMusic()
    {
        if (menuMusic != null) menuMusic.enabled = false;
    }

    void LoadScene(string sceneName)
    {
        var loader = FindFirstObjectByType<LoadingScreen>();
        if (loader != null)
            loader.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    public void QuitButton()
    {
        PlayerPrefs.Save();
        Application.Quit();
    }

    // ─── Алиасы методов (назначены в сценах, добавлены для совместимости) ─────────────────

    /// <summary>Начать следующую ночь без добавления +1 (ночь уже выставлена TimePass в магазине).</summary>
    public void BeginNightButton()
    {
        // TimePass уже установил Night = nightNum+1 перед переходом в магазин,
        // поэтому просто загружаем следующую ночь.
        int night = PlayerPrefs.GetInt("Night", 1);
        PlayerPrefs.SetString("CustomNight", "not active");
        if (loaderText != null) loaderText.SetText("Ночь " + night);
        DisableMusic();
        LoadScene("SampleScene");
    }

    /// <summary>Возврат в главное меню (используется в MainMenu как BackButton).</summary>
    public void BackButton() => BackToMain();

    /// <summary>Открыть экстра (используется в MainMenu как ExtraButton).</summary>
    public void ExtraButton() => OpenExtras();

    /// <summary>Hover на кнопке Продолжить (MainMenu).</summary>
    public void ContinueButtonHover() => ContinueHover();

    /// <summary>UnHover на кнопке Продолжить (MainMenu).</summary>
    public void ContinueButtonUnHover() => ContinueUnhover();

    /// <summary>Заглушить телефонный звонок в игровой сцене (SampleScene использует MuteNightCall).</summary>
    // MuteNightCall defined above as part of MutePhoneCall block
}
