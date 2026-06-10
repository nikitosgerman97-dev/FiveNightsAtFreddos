using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Селектор уровня ИИ для кастомной ночи (Ночь 7).
/// Имена аниматроников: Freddo, Bonita, ChikaLoka, FoxyRex, GLITCH
/// </summary>
public class AiSelector : MonoBehaviour
{
    [Header("Аниматроник")]
    [SerializeField] string animatronicName;
    [SerializeField] string animatronicDescription;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI aiLevelText;
    [SerializeField] TextMeshProUGUI aiDescr;

    int _aiLevel;
    bool _holdingButton;
    Coroutine _holdCoroutine;

    void Awake()
    {
        _aiLevel = PlayerPrefs.GetInt(animatronicName, 0);
        UpdateAiLevel();
    }

    public void OnHover()
    {
        if (aiDescr != null) aiDescr.SetText(animatronicDescription);
    }

    public void UnHover()
    {
        if (aiDescr != null) aiDescr.SetText("");
    }

    public void IncreaseValue()
    {
        if (_aiLevel < 20)
        {
            _aiLevel++;
            UpdateAiLevel();
        }
        if (_holdCoroutine != null) StopCoroutine(_holdCoroutine);
        _holdCoroutine = StartCoroutine(HolderTimer("increase", true));
    }

    public void DecreaseValue()
    {
        if (_aiLevel > 0)
        {
            _aiLevel--;
            UpdateAiLevel();
        }
        if (_holdCoroutine != null) StopCoroutine(_holdCoroutine);
        _holdCoroutine = StartCoroutine(HolderTimer("decrease", true));
    }

    IEnumerator HolderTimer(string dir, bool firstPress)
    {
        // Задержка перед началом быстрого изменения
        yield return new WaitForSecondsRealtime(firstPress ? 0.4f : 0.07f);

        _holdingButton = true;

        if (dir == "increase" && _aiLevel < 20)
        {
            _aiLevel++;
            UpdateAiLevel();
        }
        else if (dir == "decrease" && _aiLevel > 0)
        {
            _aiLevel--;
            UpdateAiLevel();
        }

        // Продолжаем пока держим кнопку
        _holdCoroutine = StartCoroutine(HolderTimer(dir, false));
    }

    public void UnHoldButton()
    {
        if (_holdCoroutine != null)
        {
            StopCoroutine(_holdCoroutine);
            _holdCoroutine = null;
        }
        _holdingButton = false;
    }

    void UpdateAiLevel()
    {
        if (aiLevelText != null) aiLevelText.SetText(_aiLevel.ToString());
        PlayerPrefs.SetInt(animatronicName, _aiLevel);
    }
}
