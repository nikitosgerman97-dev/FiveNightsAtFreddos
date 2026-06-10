using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Управляет временем ночи.
/// 1 час = ~76 сек реального времени (как в оригинальном FNAF).
/// Ночь: 12 AM → 6 AM = 6 часов ≈ 7.5 минут.
/// </summary>
public class TimePass : MonoBehaviour
{
    [Header("Игрок")]
    [SerializeField] PlayerMovement player;

    [Header("Аудио ночи")]
    [SerializeField] AudioSource nightAmbience;
    [SerializeField] AudioSource nightEndJingle;
    [SerializeField] AudioSource ambiencePlayer;
    [SerializeField] AudioSource ambienceIntensePlayer;
    [SerializeField] AudioSource ambienceIntensePlayer2;

    [Header("Телефонный гай")]
    [SerializeField] AudioSource phoneRingSource;       // звук звонка телефона
    [SerializeField] AudioSource phoneCallPlayer;       // голос гая
    [SerializeField] AudioClip   phoneRingClip;         // клип звонка (можно NightBegin.wav)
    [SerializeField] AudioClip[] nightCalls;            // 7 клипов (ночи 1-7)
    [SerializeField] Subtitles[] subtitles;             // субтитры для каждой ночи
    [SerializeField] GameObject  nightCallMuteButton;
    [SerializeField] GameObject  helperText;

    [Header("Уровни ИИ")]
    [SerializeField] AiLevels[] aiLevels;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] TextMeshProUGUI nightText;
    [SerializeField] TextMeshProUGUI loaderText;
    [SerializeField] GameObject winUI;

    // ─── Состояние ───────────────────────────────────────────────────────────
    public int nightNum;
    public bool phoneIsActive = false;   // TRUE пока Фон Гай говорит

    int gameTime = 0;
    int timeBonus = 0;
    const float HOUR_DURATION = 76f;

    static readonly string[] TIME_LABELS = { "12 AM", "1 AM", "2 AM", "3 AM", "4 AM", "5 AM", "6 AM" };

    void Awake()
    {
        nightNum = PlayerPrefs.GetInt("Night", 1);
        if (PlayerPrefs.GetString("CustomNight") == "active") nightNum = 7;
        nightNum = Mathf.Clamp(nightNum, 1, 7);

        if (nightText != null) nightText.SetText("Ночь " + nightNum);
        if (timeText  != null) timeText.SetText(TIME_LABELS[0]);

        if (nightNum == 1 && helperText != null) helperText.SetActive(true);

        if (PlayerPrefs.GetString("Drink") == "active")
        {
            PlayerPrefs.SetString("Drink", "not active");
            timeBonus = 6;
        }

        StartCoroutine(Count());
        StartCoroutine(PhoneCallSequence());
    }

    // ─── Уровень ИИ ──────────────────────────────────────────────────────────
    public int GetAILevel(int aiNum)
    {
        if (aiLevels == null || aiLevels.Length == 0) return 0;
        int idx = Mathf.Clamp(nightNum - 1, 0, aiLevels.Length - 1);
        if (aiLevels[idx] == null || aiLevels[idx].aiNum == null) return 0;
        if (aiNum >= aiLevels[idx].aiNum.Length) return 0;
        return aiLevels[idx].aiNum[aiNum];
    }

    // ─── Телефонный звонок (последовательность как в оригинале FNAF) ─────────
    IEnumerator PhoneCallSequence()
    {
        // Шаг 1: ждём 5 секунд
        yield return new WaitForSecondsRealtime(5f);

        bool isCustom = (PlayerPrefs.GetString("CustomNight") == "active");
        int callIdx   = nightNum - 1;
        bool hasCall  = !isCustom
                        && phoneCallPlayer != null
                        && nightCalls != null
                        && callIdx < nightCalls.Length
                        && nightCalls[callIdx] != null;

        if (!hasCall) yield break;

        // Шаг 2: звонит телефон (3 секунды)
        if (phoneRingSource != null && phoneRingClip != null)
        {
            phoneRingSource.clip   = phoneRingClip;
            phoneRingSource.volume = 0.7f;
            phoneRingSource.loop   = false;
            phoneRingSource.Play();
            yield return new WaitForSecondsRealtime(phoneRingClip.length > 0 ? Mathf.Min(phoneRingClip.length, 3f) : 3f);
            phoneRingSource.Stop();
        }
        else
        {
            // Нет клипа звонка — просто ждём 3 секунды
            yield return new WaitForSecondsRealtime(3f);
        }

        // Шаг 3: Фон Гай начинает говорить
        phoneCallPlayer.clip   = nightCalls[callIdx];
        phoneCallPlayer.volume = 0.85f;
        phoneCallPlayer.Play();
        phoneIsActive = true;

        // Уменьшаем фоновую музыку пока говорит гай
        if (ambiencePlayer != null) ambiencePlayer.volume = 0.1f;

        // Субтитры
        if (subtitles != null && callIdx < subtitles.Length && subtitles[callIdx] != null)
            subtitles[callIdx].StartSubtitles();

        if (nightCallMuteButton != null) nightCallMuteButton.SetActive(true);

        // Ждём окончания речи
        yield return new WaitForSecondsRealtime(phoneCallPlayer.clip.length);

        StopPhoneCall();
    }

    /// <summary>Остановить Фон Гая досрочно (вызывается кнопкой MUTE).</summary>
    public void StopPhoneCall()
    {
        phoneIsActive = false;

        if (phoneCallPlayer != null) phoneCallPlayer.Stop();
        if (phoneRingSource != null) phoneRingSource.Stop();

        // Возвращаем громкость амбиента
        if (ambiencePlayer != null) ambiencePlayer.volume = 0.5f;

        // Останавливаем субтитры
        if (subtitles != null)
            foreach (var s in subtitles)
                if (s != null) s.ResetSubtitles();

        if (nightCallMuteButton != null) nightCallMuteButton.SetActive(false);
    }

    // ─── Счётчик времени (правый верхний угол) ────────────────────────────────
    IEnumerator Count()
    {
        float hourDuration = HOUR_DURATION - timeBonus;
        yield return new WaitForSecondsRealtime(Mathf.Max(10f, hourDuration));

        gameTime = Mathf.Min(gameTime + 1, 6);

        if (timeText != null)
            timeText.SetText(TIME_LABELS[gameTime]);

        if (gameTime < 6)
            StartCoroutine(Count());
        else
            StartCoroutine(EndNight());
    }

    // ─── Конец ночи ───────────────────────────────────────────────────────────
    IEnumerator FadeAudio(AudioSource src, bool fadeIn, float targetVol = 0.5f)
    {
        if (src == null) yield break;
        float start = src.volume;
        float end   = fadeIn ? targetVol : 0f;
        if (fadeIn) src.volume = 0f;

        for (float t = 0; t < 1f; t += 0.02f)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            src.volume = Mathf.Lerp(start, end, t);
        }
        src.volume = end;
    }

    IEnumerator EndNight()
    {
        if (player == null || !player.alive) yield break;
        player.alive = false;

        StopPhoneCall();

        StartCoroutine(FadeAudio(ambiencePlayer,         false));
        StartCoroutine(FadeAudio(ambienceIntensePlayer,  false));
        StartCoroutine(FadeAudio(ambienceIntensePlayer2, false));

        if (nightEndJingle != null)
        {
            nightEndJingle.volume = 0f;
            nightEndJingle.Play();
            StartCoroutine(FadeAudio(nightEndJingle, true, 0.5f));
        }

        yield return new WaitForSecondsRealtime(4.6f);
        if (winUI != null) winUI.SetActive(true);

        switch (nightNum)
        {
            case 5: PlayerPrefs.SetString("beat5", "true"); break;
            case 6:
                PlayerPrefs.SetString("beat6", "true");
                PlayerPrefs.SetString("SeenPaycheck", "false");
                break;
            case 7:
                if (PlayerPrefs.GetInt("Freddo",0)==20 && PlayerPrefs.GetInt("Bonita",0)==20 &&
                    PlayerPrefs.GetInt("ChikaLoka",0)==20 && PlayerPrefs.GetInt("FoxyRex",0)==20 &&
                    PlayerPrefs.GetInt("GLITCH",0)==20)
                {
                    PlayerPrefs.SetString("beat7Ultimate", "true");
                    PlayerPrefs.SetString("SeenTrueEnding", "false");
                }
                break;
        }

        yield return new WaitForSecondsRealtime(6f);
        PlayerPrefs.SetString("CustomNight", "not active");

        var loader = FindFirstObjectByType<LoadingScreen>();
        if (loader == null) yield break;

        if (nightNum >= 5)
        {
            if (nightNum == 5) PlayerPrefs.SetInt("Night", 6);
            else if (nightNum == 6) PlayerPrefs.SetInt("Night", 7);
            loader.LoadScene("MainMenu");
        }
        else
        {
            PlayerPrefs.SetInt("Night", nightNum + 1);
            loader.LoadScene("ShopScene");
        }
    }
}
