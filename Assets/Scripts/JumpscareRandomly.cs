using System.Collections;
using UnityEngine;

/// <summary>
/// Случайный джампскейр — активируется при появлении аниматроника в коридоре.
/// ИСПРАВЛЕНО: проверяем player.alive перед вызовом GameOver.
/// </summary>
public class JumpscareRandomly : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] PlayerMovement player;
    [SerializeField] GameObject jsModel;

    Coroutine _jsCoroutine;

    void OnEnable()
    {
        // Запускаем таймер при активации
        if (_jsCoroutine != null) StopCoroutine(_jsCoroutine);
        _jsCoroutine = StartCoroutine(JSRandom());
    }

    void OnDisable()
    {
        if (_jsCoroutine != null)
        {
            StopCoroutine(_jsCoroutine);
            _jsCoroutine = null;
        }

        // Останавливаем звук при деактивации
        var audio = GetComponent<AudioSource>();
        if (audio != null) audio.Stop();
    }

    IEnumerator JSRandom()
    {
        // Случайная задержка перед джампскейром
        float delay = Random.Range(8f, 15f);
        yield return new WaitForSecondsRealtime(delay);

        // Останавливаем фоновый звук
        var audio = GetComponent<AudioSource>();
        if (audio != null) audio.Stop();

        // Вызываем GameOver только если игрок жив
        if (player != null && player.alive)
        {
            player.GameOver(jsModel);
        }
    }
}
