using System.Collections;
using UnityEngine;

/// <summary>
/// Управляет дверью безопасности.
/// Клик на кнопку → дверь открывается/закрывается.
/// Расходует энергию пока закрыта.
/// При отключении питания — принудительно открывается.
/// </summary>
public class DoorHolder : MonoBehaviour
{
    [Header("Аудио")]
    [SerializeField] AudioSource openSound;
    [SerializeField] AudioSource closeSound;

    [Header("Материалы кнопки")]
    [SerializeField] Material buttonActiveMat;    // когда дверь закрыта (зелёный)
    [SerializeField] Material buttonInactiveMat;  // когда открыта (серый)

    [Header("Ссылки")]
    [SerializeField] EnergyLevel energyLevel;

    [Header("Индекс материала на MeshRenderer (обычно 2)")]
    [SerializeField] int buttonMaterialIndex = 2;

    public Animator doorAnim;
    public bool closed = false;

    MeshRenderer buttonRenderer;

    void Awake()
    {
        buttonRenderer = GetComponent<MeshRenderer>();
    }

    // ─── Переключение двери ───────────────────────────────────────────────────
    public void ToggleDoor()
    {
        if (doorAnim == null) return;

        // Не переключаем во время анимации
        AnimatorStateInfo state = doorAnim.GetCurrentAnimatorStateInfo(0);
        if (!state.IsName("DoorIdle") && !state.IsName("Idle")) return;

        if (closed)
            OpenDoor();
        else
            CloseDoor();
    }

    void OpenDoor()
    {
        doorAnim.SetTrigger("OpenDoor");
        doorAnim.ResetTrigger("CloseDoor");
        closed = false;

        if (energyLevel != null)
        {
            energyLevel.usage--;
            energyLevel.UpdateUsageSlider();
        }

        SetButtonMaterial(false);
        if (openSound != null) openSound.Play();
    }

    void CloseDoor()
    {
        if (energyLevel == null) return;
        if (energyLevel.energy <= 0) return; // нет энергии — не закрываем

        doorAnim.SetTrigger("CloseDoor");
        doorAnim.ResetTrigger("OpenDoor");
        closed = true;

        energyLevel.usage++;
        energyLevel.UpdateUsageSlider();

        SetButtonMaterial(true);
        if (closeSound != null) closeSound.Play();
    }

    /// <summary>Вызывается EnergyLevel при отключении питания.</summary>
    public void PowerOff()
    {
        if (closed)
            OpenDoor();
    }

    void SetButtonMaterial(bool active)
    {
        if (buttonRenderer == null) return;
        Material[] mats = buttonRenderer.materials;
        int idx = Mathf.Clamp(buttonMaterialIndex, 0, mats.Length - 1);
        mats[idx] = active ? buttonActiveMat : buttonInactiveMat;
        buttonRenderer.materials = mats;
    }
}
