using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Управляет батареей охранника.
/// Расход зависит от количества активных устройств (usage).
/// При нуле энергии — отключение питания и скример.
/// </summary>
public class EnergyLevel : MonoBehaviour
{
    [Header("Аудио")]
    [SerializeField] AudioSource powerOut;

    [Header("Объект при отключении питания")]
    [SerializeField] GameObject pOnObject;

    [Header("Двери — открыть при отключении")]
    [SerializeField] DoorHolder[] doorHolder;

    [Header("Объекты — выкл/вкл при отключении")]
    [SerializeField] GameObject[] turnOffObjects;
    [SerializeField] GameObject[] turnOnObjects;

    [Header("UI")]
    [SerializeField] Slider usageSlider;
    [SerializeField] Image energyFill;
    [SerializeField] UnityEngine.UI.Text energyPercentText; // опционально

    public float energy = 100f;  // float для плавного убывания (ShockSystem: -= 4f)
    public int usage = 1;

    // Бонус от предмета «Генератор» из магазина (замедляет расход)
    float extraItem = 1f;

    // Расход в секундах на 1% батареи при каждом уровне usage
    static readonly float[] drainRates = {
        13.8f,  // usage 0 — ничего не включено
        8.94f,  // usage 1 — только базовый (просто сидишь)
        7.6f,   // usage 2 — камеры или фонарик
        4.6f,   // usage 3
        3.6f,   // usage 4
        2.1f,   // usage 5
        0.65f,  // usage 6 — всё включено
    };

    public bool powerIsOut = false;

    void Start()
    {
        UpdateUsageSlider();
        StartCoroutine(DecreaseEnergy());

        if (PlayerPrefs.GetString("Generator") == "active")
        {
            PlayerPrefs.SetString("Generator", "not active");
            extraItem = 1.15f; // немного более заметный бонус
        }
    }

    void Update()
    {
        // Обновляем текст процента если он есть
        if (energyPercentText != null)
            energyPercentText.text = Mathf.CeilToInt(energy) + "%";
    }

    public void UpdateUsageSlider()
    {
        if (usageSlider != null)
            usageSlider.value = usage;
    }

    IEnumerator DecreaseEnergy()
    {
        if (powerIsOut) yield break;

        int clampedUsage = Mathf.Clamp(usage, 0, drainRates.Length - 1);
        float waitTime = drainRates[clampedUsage] * extraItem;

        yield return new WaitForSecondsRealtime(waitTime);

        energy--;
        energy = Mathf.Max(0, energy);

        if (energyFill != null)
            energyFill.fillAmount = energy / 100f;

        if (energy > 0)
            StartCoroutine(DecreaseEnergy());
        else
            StartCoroutine(PowerOut());
    }

    IEnumerator PowerOut()
    {
        powerIsOut = true;

        if (powerOut != null) powerOut.Play();

        // Гасим свет и интерфейс
        foreach (var obj in turnOffObjects)
            if (obj != null) obj.SetActive(false);

        // Открываем все двери (питание пропало)
        foreach (var door in doorHolder)
            if (door != null) door.PowerOff();

        // Включаем аварийные объекты
        foreach (var obj in turnOnObjects)
            if (obj != null) obj.SetActive(true);

        // Небольшая пауза перед скримером
        yield return new WaitForSecondsRealtime(Random.Range(8f, 12f));

        if (pOnObject != null)
            pOnObject.SetActive(true);
    }
}
