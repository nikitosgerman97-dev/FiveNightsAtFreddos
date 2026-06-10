using UnityEngine;

/// <summary>
/// Монета — подбирается при наведении мыши.
/// Добавляет 1 монету в PlayerPrefs "CogCoins".
/// </summary>
public class CoinScript : MonoBehaviour
{
    bool _collected = false;

    /// <summary>
    /// Вызывается из CoinSpawner при наведении курсора.
    /// </summary>
    public void MouseOver()
    {
        if (_collected) return;
        _collected = true;

        // Начисляем монету
        int coins = PlayerPrefs.GetInt("CogCoins", 0);
        coins++;
        PlayerPrefs.SetInt("CogCoins", coins);

        // Звук подбора
        var audio = GetComponent<AudioSource>();
        if (audio != null) audio.Play();

        // Скрываем объект
        transform.localScale = Vector3.zero;

        // Уничтожаем через небольшую задержку (чтобы звук доиграл)
        Destroy(gameObject, 0.3f);
    }
}
