using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Анимированный переход между ночами в стиле старой газеты.
/// Fade in чёрного экрана → slide-in газетного экрана → зарплата → кнопка продолжить.
/// </summary>
public class NightTransitionUI : MonoBehaviour
{
    public static NightTransitionUI Instance { get; private set; }

    // ─── Чёрный экран fade ────────────────────────────────────────────────────

    [Header("Fade Overlay")]
    [Tooltip("Image на весь экран для fade in/out (черный цвет, alpha 0 в старте)")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeDuration = 0.8f;

    // ─── Газетный экран ───────────────────────────────────────────────────────

    [Header("Newspaper Panel")]
    [Tooltip("GameObject с фоном газеты, заголовками и текстом")]
    [SerializeField] private GameObject newspaperPanel;
    [Tooltip("RectTransform газетного экрана (для slide-in анимации)")]
    [SerializeField] private RectTransform newspaperRect;

    // ─── Текстовые поля ───────────────────────────────────────────────────────

    [Header("Newspaper Text Fields")]
    [SerializeField] private TextMeshProUGUI headlineText;
    [SerializeField] private TextMeshProUGUI subheadlineText;
    [SerializeField] private TextMeshProUGUI articleText;
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private TextMeshProUGUI paycheckText;

    // ─── Sepia эффект ─────────────────────────────────────────────────────────

    [Header("Sepia / Tint")]
    [Tooltip("Image-фон газеты — устанавливается желтоватый tint для sepia")]
    [SerializeField] private Image newspaperBackground;
    [SerializeField] private Color sepiaColor = new Color(0.92f, 0.84f, 0.62f, 1f);

    // ─── Кнопка ───────────────────────────────────────────────────────────────

    [Header("Next Night Button")]
    [SerializeField] private Button nextNightButton;
    [SerializeField] private GameObject nextNightButtonObj;

    // ─── Звуки ────────────────────────────────────────────────────────────────

    [Header("Audio")]
    [SerializeField] private AudioClip nightCompleteSound;
    [SerializeField] private AudioSource audioSource;

    // ─── Анимация slide-in ────────────────────────────────────────────────────

    [Header("Slide Animation")]
    [Tooltip("Высота экрана сверху, с которой начинается slide-in (пиксели)")]
    [SerializeField] private float slideStartOffsetY = 1200f;
    [SerializeField] private float slideDuration     = 0.65f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // ─── Зарплата ─────────────────────────────────────────────────────────────

    [Header("Paycheck")]
    [SerializeField] private float paycheckPerNight = 120f;
    [SerializeField] private string paycheckPrefix  = "Зарплата: $";

    // ─── Callback ─────────────────────────────────────────────────────────────

    private System.Action _onNextNightCallback;

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

        // Скрываем всё при старте
        if (newspaperPanel != null) newspaperPanel.SetActive(false);
        if (nextNightButtonObj != null) nextNightButtonObj.SetActive(false);

        // Чёрный экран — начинаем прозрачным
        if (fadeOverlay != null)
        {
            Color c = fadeOverlay.color;
            c.a = 0f;
            fadeOverlay.color = c;
            fadeOverlay.gameObject.SetActive(false);
        }

        // Применяем sepia к фону газеты
        if (newspaperBackground != null)
            newspaperBackground.color = sepiaColor;

        // Подключаем кнопку
        if (nextNightButton != null)
            nextNightButton.onClick.AddListener(OnNextNightClicked);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Публичный API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Запускает полную анимацию перехода между ночами.
    /// </summary>
    /// <param name="nightNumber">Номер завершённой ночи</param>
    /// <param name="headline">Главный заголовок газеты</param>
    /// <param name="article">Текст статьи</param>
    /// <param name="date">Дата на газете</param>
    /// <param name="onNextNight">Callback при нажатии "Следующая ночь"</param>
    public void ShowNightComplete(int nightNumber, string headline, string article,
                                  string date, System.Action onNextNight = null)
    {
        _onNextNightCallback = onNextNight;
        StartCoroutine(NightCompleteSequence(nightNumber, headline, article, date));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Coroutines
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Полная последовательность:
    /// 1. Fade in чёрного экрана
    /// 2. Заполняем текст газеты
    /// 3. Slide-in газетного экрана
    /// 4. Показываем зарплату и кнопку
    /// </summary>
    private IEnumerator NightCompleteSequence(int nightNumber, string headline,
                                               string article, string date)
    {
        // --- 1. Fade in чёрного экрана ---
        yield return StartCoroutine(FadeOverlay(0f, 1f, fadeDuration));

        // --- 2. Заполняем текст ---
        FillNewspaperText(nightNumber, headline, article, date);

        // Скрываем кнопку, показываем газетный экран (вне экрана сверху)
        if (nextNightButtonObj != null) nextNightButtonObj.SetActive(false);
        PrepareNewspaperForSlide();

        // Небольшая пауза на чёрном экране
        yield return new WaitForSeconds(0.3f);

        // Проигрываем звук завершения ночи
        PlayNightCompleteSound();

        // --- 3. Slide-in газеты ---
        yield return StartCoroutine(SlideNewspaper());

        // Показываем зарплату с небольшой задержкой
        yield return new WaitForSeconds(0.4f);
        ShowPaycheck(nightNumber);

        // --- 4. Появление кнопки ---
        yield return new WaitForSeconds(0.6f);
        if (nextNightButtonObj != null) nextNightButtonObj.SetActive(true);
    }

    /// <summary>
    /// Плавный fade чёрного overlay.
    /// </summary>
    private IEnumerator FadeOverlay(float from, float to, float duration)
    {
        if (fadeOverlay == null) yield break;

        fadeOverlay.gameObject.SetActive(true);

        float elapsed = 0f;
        Color c = fadeOverlay.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / duration);
            fadeOverlay.color = c;
            yield return null;
        }

        c.a = to;
        fadeOverlay.color = c;

        // Если fade out — можно скрыть overlay
        if (to <= 0f) fadeOverlay.gameObject.SetActive(false);
    }

    /// <summary>
    /// Устанавливает газету за экраном (сверху) для slide-in.
    /// </summary>
    private void PrepareNewspaperForSlide()
    {
        if (newspaperPanel == null) return;

        newspaperPanel.SetActive(true);

        if (newspaperRect != null)
        {
            Vector2 pos = newspaperRect.anchoredPosition;
            pos.y = slideStartOffsetY;
            newspaperRect.anchoredPosition = pos;
        }
    }

    /// <summary>
    /// Плавно опускает газету на экран с кривой EaseInOut.
    /// </summary>
    private IEnumerator SlideNewspaper()
    {
        if (newspaperRect == null) yield break;

        float elapsed = 0f;
        float startY  = slideStartOffsetY;
        float targetY = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t      = Mathf.Clamp01(elapsed / slideDuration);
            float curveT = slideCurve.Evaluate(t);

            Vector2 pos = newspaperRect.anchoredPosition;
            pos.y = Mathf.Lerp(startY, targetY, curveT);
            newspaperRect.anchoredPosition = pos;

            yield return null;
        }

        // Гарантируем финальную позицию
        Vector2 finalPos = newspaperRect.anchoredPosition;
        finalPos.y = targetY;
        newspaperRect.anchoredPosition = finalPos;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Вспомогательные методы
    // ─────────────────────────────────────────────────────────────────────────

    private void FillNewspaperText(int nightNumber, string headline, string article, string date)
    {
        if (headlineText != null)
            headlineText.text = headline;

        if (subheadlineText != null)
            subheadlineText.text = $"Ночь {nightNumber} — Выжил!";

        if (articleText != null)
            articleText.text = article;

        if (dateText != null)
            dateText.text = date;

        // Скрываем зарплату до её появления
        if (paycheckText != null)
        {
            paycheckText.text = "";
            paycheckText.gameObject.SetActive(false);
        }
    }

    private void ShowPaycheck(int nightNumber)
    {
        if (paycheckText == null) return;

        float amount = paycheckPerNight * nightNumber;
        paycheckText.gameObject.SetActive(true);
        paycheckText.text = $"{paycheckPrefix}{amount:F2}";
    }

    private void PlayNightCompleteSound()
    {
        if (audioSource == null || nightCompleteSound == null) return;
        audioSource.PlayOneShot(nightCompleteSound);
    }

    private void OnNextNightClicked()
    {
        _onNextNightCallback?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Утилиты (вызов из GameManager)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Скрывает газетный экран и fade overlay немедленно (для перезагрузки сцены).
    /// </summary>
    public void HideImmediate()
    {
        if (newspaperPanel != null) newspaperPanel.SetActive(false);
        if (nextNightButtonObj != null) nextNightButtonObj.SetActive(false);

        if (fadeOverlay != null)
        {
            Color c = fadeOverlay.color;
            c.a = 0f;
            fadeOverlay.color = c;
            fadeOverlay.gameObject.SetActive(false);
        }
    }
}
