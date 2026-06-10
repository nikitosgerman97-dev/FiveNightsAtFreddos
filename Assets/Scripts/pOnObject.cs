using System.Collections;
using UnityEngine;

/// <summary>
/// Аниматроник "на объекте" — таймер перед джампскейром.
/// Случайная задержка 8-12 секунд, потом выключает свет и вызывает GameOver.
/// ИСПРАВЛЕНО: проверяем alive у игрока перед вызовом GameOver.
/// </summary>
public class pOnObject : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] PlayerMovement player;
    [SerializeField] GameObject jsModel;
    [SerializeField] GameObject lightObj;

    Coroutine _animCoroutine;

    void OnEnable()
    {
        // Запускаем таймер при активации объекта
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(BeginAnim());
    }

    void OnDisable()
    {
        if (_animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
            _animCoroutine = null;
        }
    }

    IEnumerator BeginAnim()
    {
        // Случайная задержка перед выключением света
        yield return new WaitForSecondsRealtime(Random.Range(8f, 12f));

        // Выключаем свет в комнате
        if (lightObj != null) lightObj.SetActive(false);

        // Небольшая пауза в темноте
        yield return new WaitForSecondsRealtime(Random.Range(4f, 6f));

        // Проверяем что игрок жив
        if (player != null && player.alive)
        {
            player.GameOver(jsModel);
        }
    }
}
