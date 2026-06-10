using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Загрузчик дополнительных элементов главного меню:
/// - кнопка «Продолжить»
/// - кнопка «Кастомная ночь»
/// - звёзды прогресса (3 + 1 секретная)
/// - чёрный экран с секретной концовкой
/// - сброс данных (зажать Delete 3 сек)
/// </summary>
public class ExtraMenuItemsLoader : MonoBehaviour
{
    [Header("Кнопки")]
    [SerializeField] GameObject continueButton;
    [SerializeField] GameObject customNightButton;

    [Header("Специальные экраны")]
    [SerializeField] GameObject paycheckObj;      // экран после ночи 6
    [SerializeField] GameObject trueEndingObj;    // секретная концовка
    [SerializeField] GameObject menuObj;          // основное меню (скрывается во время концовки)

    [Header("Звёзды (0=ночь5, 1=ночь6, 2=ультимейт, 3=секрет)")]
    [SerializeField] GameObject[] stars;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI loaderText;

    bool holdingDel = false;
    Coroutine _deleteCoroutine;

    void Awake()
    {
        // Кнопка «Продолжить» — только если уже начали игру
        if (continueButton != null)
            continueButton.SetActive(PlayerPrefs.GetInt("Night", 1) > 1);

        // Звезда 1 — пройдена ночь 5
        if (stars != null && stars.Length > 0 && stars[0] != null)
            stars[0].SetActive(PlayerPrefs.GetString("beat5") == "true");

        // Звезда 2 — пройдена ночь 6 → открывает кастомную ночь
        if (PlayerPrefs.GetString("beat6") == "true")
        {
            if (customNightButton != null)
                customNightButton.SetActive(true);

            if (stars != null && stars.Length > 1 && stars[1] != null)
                stars[1].SetActive(true);

            // Показываем «зарплату» один раз
            if (PlayerPrefs.GetString("SeenPaycheck") == "false")
            {
                PlayerPrefs.SetString("SeenPaycheck", "true");
                if (paycheckObj != null) paycheckObj.SetActive(true);
            }
        }

        // Звезда 3 — секретная концовка (все аниматроники на 20 в ночи 7)
        if (PlayerPrefs.GetString("beat7Ultimate") == "true")
        {
            if (stars != null && stars.Length > 2 && stars[2] != null)
                stars[2].SetActive(true);

            if (PlayerPrefs.GetString("SeenTrueEnding") == "false")
            {
                PlayerPrefs.SetString("SeenTrueEnding", "true");
                if (menuObj != null) menuObj.SetActive(false);
                if (trueEndingObj != null) trueEndingObj.SetActive(true);
                StartCoroutine(ReenableMenu());
            }
        }

        // Звезда 4 — намеренно недостижима (пасхалка для внимательных)
        // Разблокируется только если пасхалка windy31 использована 5+ раз
        int windyCount = PlayerPrefs.GetInt("WindyEasterEggCount", 0);
        if (stars != null && stars.Length > 3 && stars[3] != null)
            stars[3].SetActive(windyCount >= 5);
    }

    void FixedUpdate()
    {
        // Зажать Delete 3 секунды — сброс прогресса
        if (Input.GetKey(KeyCode.Delete))
        {
            if (!holdingDel)
            {
                holdingDel = true;
                if (loaderText != null) loaderText.SetText("Сброс данных...");
                _deleteCoroutine = StartCoroutine(DeleteData());
            }
        }
        else
        {
            if (holdingDel)
            {
                // БАГ ИСПРАВЛЕН: StopCoroutine(DeleteData()) не работал (создавал новый итератор)
                if (_deleteCoroutine != null) StopCoroutine(_deleteCoroutine);
                _deleteCoroutine = null;
                holdingDel = false;
                if (loaderText != null) loaderText.SetText("");
            }
        }
    }

    IEnumerator DeleteData()
    {
        yield return new WaitForSecondsRealtime(3f);
        if (holdingDel)
        {
            PlayerPrefs.DeleteAll();
            if (loaderText != null) loaderText.SetText("Данные сброшены!");
            yield return new WaitForSecondsRealtime(1f);
            FindFirstObjectByType<LoadingScreen>()?.LoadScene("MainMenu");
        }
    }

    IEnumerator ReenableMenu()
    {
        yield return new WaitForSecondsRealtime(27f);
        if (menuObj != null) menuObj.SetActive(true);
        if (trueEndingObj != null) trueEndingObj.SetActive(false);
    }
}
