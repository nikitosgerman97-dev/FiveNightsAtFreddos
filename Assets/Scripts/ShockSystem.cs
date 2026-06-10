using System.Collections;
using UnityEngine;

/// <summary>
/// Система шока — поражает аниматроника электрошоком.
/// Тратит энергию, сбрасывает аниматроника если он на стадии <= 3.
/// </summary>
public class ShockSystem : MonoBehaviour
{
    [Header("Аудио")]
    [SerializeField] AudioSource shockSound;

    [Header("UI")]
    [SerializeField] GameObject shockButton;
    [SerializeField] GameObject shockUI;

    [Header("Ссылки")]
    [SerializeField] EnergyLevel energyLevel;
    [SerializeField] AnimatronicAI affectedAnimatronic;

    bool _shockCooldown = false;

    /// <summary>
    /// Вызывается при нажатии кнопки шока игроком.
    /// </summary>
    public void Shocked()
    {
        if (_shockCooldown) return;
        if (energyLevel != null && energyLevel.powerIsOut) return;

        if (shockButton != null) shockButton.SetActive(false);
        StartCoroutine(ShockAnim());
    }

    IEnumerator ShockAnim()
    {
        _shockCooldown = true;

        // Сброс аниматроника если он ещё не в офисе
        if (affectedAnimatronic != null && affectedAnimatronic.alive && affectedAnimatronic.stage <= 3)
        {
            affectedAnimatronic.ResetAnimatronic();
        }

        // Звук шока
        if (shockSound != null) shockSound.Play();

        // Показываем UI шока
        if (shockUI != null) shockUI.SetActive(true);

        // Повышаем потребление энергии
        if (energyLevel != null)
        {
            energyLevel.usage++;
            energyLevel.energy -= 4f;
            energyLevel.UpdateUsageSlider();
        }

        yield return new WaitForSecondsRealtime(0.2f);
        if (shockUI != null) shockUI.SetActive(false);
        yield return new WaitForSecondsRealtime(1.8f);

        // Возвращаем энергопотребление
        if (energyLevel != null)
        {
            energyLevel.usage--;
            energyLevel.UpdateUsageSlider();
        }

        _shockCooldown = false;
        if (shockButton != null) shockButton.SetActive(true);
    }
}
