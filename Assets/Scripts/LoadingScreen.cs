using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Экран загрузки между сценами.
/// Показывает название следующей локации.
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nightText;
    [SerializeField] GameObject loadingScreen;

    public void LoadScene(string sceneName)
    {
        // Показываем название следующей сцены на экране загрузки
        if (nightText != null)
        {
            if (sceneName == "ShopScene")
                nightText.SetText("Магазин");
            else if (sceneName == "MainMenu")
                nightText.SetText("Главное меню");
            else if (PlayerPrefs.GetString("CustomNight") == "active")
                nightText.SetText("Ночь 7 — Кастомная");
            else
                nightText.SetText("Ночь " + PlayerPrefs.GetInt("Night", 1));
        }

        StartCoroutine(LoadSceneAsync(sceneName));
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        // Небольшая пауза для показа экрана загрузки
        yield return new WaitForSecondsRealtime(0.8f);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
            yield return null;

        yield return new WaitForSecondsRealtime(0.2f);
        operation.allowSceneActivation = true;
    }
}
