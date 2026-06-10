using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Полная система управления энергией.
/// Отслеживает расход от разных источников (камеры, двери, свет),
/// обновляет UI и инициирует Game Over при отключении питания.
/// </summary>
public class PowerSystem : MonoBehaviour
{
    public static PowerSystem Instance { get; private set; }

    // ─── Энергия ──────────────────────────────────────────────────────────────

    [Header("Power Settings")]
    [SerializeField] private float maxPower     = 100f;
    [SerializeField] private float currentPower = 100f;

    // Базовые значения расхода (в единицах в секунду)
    [Header("Drain Rates (per second)")]
    [SerializeField] private float cameraDrain    = 0.5f;
    [SerializeField] private float leftDoorDrain  = 1.0f;
    [SerializeField] private float rightDoorDrain = 1.0f;
    [SerializeField] private float lightDrain     = 0.3f;

    // ─── UI ───────────────────────────────────────────────────────────────────

    [Header("UI - Power Bar")]
    [Tooltip("Image с fillAmount для полосы энергии")]
    [SerializeField] private Image powerBarFill;
    [Tooltip("Текст с процентом энергии")]
    [SerializeField] private TextMeshProUGUI powerPercentText;

    [Header("UI - Colors")]
    [SerializeField] private Color colorFull    = new Color(0.2f, 0.9f, 0.2f); // зелёный
    [SerializeField] private Color colorMid     = new Color(0.9f, 0.85f, 0.1f); // жёлтый
    [SerializeField] private Color colorLow     = new Color(0.9f, 0.3f, 0.1f); // красный
    [SerializeField] private float pulseSpeed   = 4f;

    // Пороги изменения цвета
    private const float YELLOW_THRESHOLD = 0.5f;
    private const float RED_THRESHOLD    = 0.25f;
    private const float PULSE_THRESHOLD  = 0.2f;

    // ─── Состояние ────────────────────────────────────────────────────────────

    /// <summary>Активные источники расхода: ключ → значение расхода в сек.</summary>
    private Dictionary<string, float> _activeDrains = new Dictionary<string, float>();

    private bool _powerOut     = false;
    private bool _isPulsing    = false;

    // ─── Нормализованный уровень (0..1) ──────────────────────────────────────

    /// <summary>Уровень энергии от 0 до 1.</summary>
    public float NormalizedPower => currentPower / maxPower;

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

        currentPower = maxPower;
    }

    private void Update()
    {
        if (_powerOut) return;

        DrainPower();
        UpdateUI();

        // Уведомляем PostProcessingController об уровне энергии
        if (PostProcessingController.Instance != null)
            PostProcessingController.Instance.SetPowerLevel(NormalizedPower);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Публичный API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Добавляет источник расхода с именованным ключом.
    /// Предустановленные ключи: "camera", "leftDoor", "rightDoor", "light".
    /// </summary>
    public void AddDrain(string source)
    {
        if (_activeDrains.ContainsKey(source)) return;

        float drainValue = GetDrainValue(source);
        _activeDrains[source] = drainValue;
    }

    /// <summary>
    /// Убирает источник расхода.
    /// </summary>
    public void RemoveDrain(string source)
    {
        _activeDrains.Remove(source);
    }

    /// <summary>Добавляет произвольный расход с указанным значением.</summary>
    public void AddCustomDrain(string key, float drainPerSecond)
    {
        _activeDrains[key] = drainPerSecond;
    }

    /// <summary>Текущий суммарный расход в секунду.</summary>
    public float GetTotalDrainPerSecond()
    {
        float total = 0f;
        foreach (var kvp in _activeDrains)
            total += kvp.Value;
        return total;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Приватные методы
    // ─────────────────────────────────────────────────────────────────────────

    private float GetDrainValue(string source)
    {
        return source switch
        {
            "camera"     => cameraDrain,
            "leftDoor"   => leftDoorDrain,
            "rightDoor"  => rightDoorDrain,
            "light"      => lightDrain,
            _            => 0f
        };
    }

    /// <summary>
    /// Списывает энергию каждый кадр исходя из активных источников расхода.
    /// </summary>
    private void DrainPower()
    {
        if (_activeDrains.Count == 0) return;

        float totalDrain = GetTotalDrainPerSecond();
        currentPower -= totalDrain * Time.deltaTime;
        currentPower = Mathf.Clamp(currentPower, 0f, maxPower);

        if (currentPower <= 0f)
        {
            PowerOutage();
        }
    }

    /// <summary>
    /// Обновляет полосу энергии и текст в UI.
    /// При < 20% — пульсирующий alpha.
    /// </summary>
    private void UpdateUI()
    {
        float normalized = NormalizedPower;

        // --- Fill amount ---
        if (powerBarFill != null)
        {
            powerBarFill.fillAmount = normalized;

            // Цвет: зелёный → жёлтый → красный
            Color barColor;
            if (normalized > YELLOW_THRESHOLD)
                barColor = Color.Lerp(colorMid, colorFull, (normalized - YELLOW_THRESHOLD) / (1f - YELLOW_THRESHOLD));
            else if (normalized > RED_THRESHOLD)
                barColor = Color.Lerp(colorLow, colorMid, (normalized - RED_THRESHOLD) / (YELLOW_THRESHOLD - RED_THRESHOLD));
            else
                barColor = colorLow;

            // Пульсация alpha при критическом уровне
            if (normalized < PULSE_THRESHOLD)
            {
                float alpha = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0..1
                barColor.a = Mathf.Lerp(0.5f, 1.0f, alpha);
            }
            else
            {
                barColor.a = 1f;
            }

            powerBarFill.color = barColor;
        }

        // --- Процент ---
        if (powerPercentText != null)
        {
            int percent = Mathf.RoundToInt(normalized * 100f);
            powerPercentText.text = $"{percent}%";
        }
    }

    /// <summary>
    /// Отключение питания: снимаем все расходы, ждём 10 сек — Game Over.
    /// </summary>
    private void PowerOutage()
    {
        if (_powerOut) return;
        _powerOut = true;

        currentPower = 0f;
        _activeDrains.Clear();

        // Гасим UI
        if (powerBarFill != null)
        {
            powerBarFill.fillAmount = 0f;
            powerBarFill.color = colorLow;
        }

        if (powerPercentText != null)
            powerPercentText.text = "0%";

        // Уведомляем PostProcessingController
        if (PostProcessingController.Instance != null)
            PostProcessingController.Instance.SetPowerLevel(0f);

        Debug.Log("[PowerSystem] Питание отключено! Freddo придёт через 10 секунд...");

        StartCoroutine(FreddoArrivalCoroutine());
    }

    /// <summary>
    /// Через 10 секунд после отключения питания — приходит Freddo (Game Over).
    /// </summary>
    private IEnumerator FreddoArrivalCoroutine()
    {
        yield return new WaitForSeconds(10f);

        Debug.Log("[PowerSystem] Freddo пришёл! GAME OVER");

        // Запускаем скример Freddo (animatronicID = 0)
        if (JumpscareSystem.Instance != null)
            JumpscareSystem.Instance.TriggerJumpscare(0);
    }
}
