using System.Collections;
using UnityEngine;

/// <summary>
/// Спавнер монет — появляются случайно на экране для подбора.
/// Спавнит монеты на канвасах UI с задержкой.
/// </summary>
public class CoinSpawner : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] GameObject[] canvases;
    [SerializeField] GameObject coinPref;

    [Tooltip("Максимум монет одновременно на экране")]
    [SerializeField] int maxCoins = 5;

    [Tooltip("Задержка между спавнами (сек)")]
    [SerializeField] float spawnDelay = 7f;

    void Start()
    {
        StartCoroutine(SpawnCoins());
    }

    /// <summary>
    /// Считаем только живые монеты (объекты с CoinScript) на всех канвасах.
    /// Использование GetComponentsInChildren надёжнее, чем childCount,
    /// который мог бы считать другие UI-элементы на том же канвасе.
    /// </summary>
    int ActiveCoinCount()
    {
        if (canvases == null) return 0;
        int count = 0;
        foreach (var canvas in canvases)
        {
            if (canvas == null) continue;
            // false = не включать сам объект канваса, только дочерние
            count += canvas.GetComponentsInChildren<CoinScript>(false).Length;
        }
        return count;
    }

    IEnumerator SpawnCoins()
    {
        yield return new WaitForSecondsRealtime(Random.Range(spawnDelay * 0.5f, spawnDelay * 1.5f));

        if (ActiveCoinCount() < maxCoins && canvases != null && canvases.Length > 0 && coinPref != null)
        {
            // Случайная позиция в пределах UI
            Vector3 randomPos = new Vector3(
                Random.Range(-600f, 600f),
                Random.Range(-300f, 300f),
                0f
            );

            // Спавним на случайном канвасе
            Transform coinTransform = Instantiate(coinPref, transform.position, Quaternion.identity).transform;
            int canvasIdx = Random.Range(0, canvases.Length);
            coinTransform.SetParent(canvases[canvasIdx].transform, false);
            coinTransform.localPosition = randomPos;
        }

        StartCoroutine(SpawnCoins());
    }
}
