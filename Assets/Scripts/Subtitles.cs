using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Субтитры телефонного гая.
/// Тексты и тайминги назначаются в инспекторе на каждую ночь.
/// Вызов: subtitles.StartSubtitles()
/// 
/// ИСПРАВЛЕНО: рекурсивная корутина ShowNext() заменена на цикл while —
/// это надёжнее и не накапливает вложенные корутины при длинных субтитрах.
/// </summary>
public class Subtitles : MonoBehaviour
{
    [Header("Субтитры")]
    [SerializeField] string[] text;
    [SerializeField] float[] textTime;
    [SerializeField] TextMeshProUGUI subtitleText;

    Coroutine _currentCoroutine;

    void OnDisable()
    {
        ResetSubtitles();
    }

    /// <summary>
    /// Сброс субтитров в начальное состояние.
    /// </summary>
    public void ResetSubtitles()
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
            _currentCoroutine = null;
        }
        if (subtitleText != null) subtitleText.SetText("");
    }

    /// <summary>
    /// Начать воспроизведение субтитров.
    /// </summary>
    public void StartSubtitles()
    {
        ResetSubtitles();
        _currentCoroutine = StartCoroutine(RunSubtitles());
    }

    /// <summary>
    /// IEnumerator версия для совместимости (вызывается через StartCoroutine снаружи).
    /// </summary>
    public IEnumerator BeginText()
    {
        StartSubtitles();
        // Ждём пока корутина завершится
        while (_currentCoroutine != null)
            yield return null;
    }

    IEnumerator RunSubtitles()
    {
        if (subtitleText == null) { _currentCoroutine = null; yield break; }
        if (text == null || text.Length == 0) { _currentCoroutine = null; yield break; }
        if (textTime == null || textTime.Length < text.Length) { _currentCoroutine = null; yield break; }

        // Обычный цикл — никакой рекурсии
        for (int i = 0; i < text.Length; i++)
        {
            subtitleText.SetText(text[i]);
            yield return new WaitForSecondsRealtime(textTime[i]);
        }

        subtitleText.SetText("");
        _currentCoroutine = null;
    }
}
