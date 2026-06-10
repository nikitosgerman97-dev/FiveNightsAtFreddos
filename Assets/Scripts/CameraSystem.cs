using System.Collections;
using UnityEngine;

/// <summary>
/// Система камер наблюдения.
/// Открывает/закрывает планшет, переключает камеры, расходует энергию.
/// </summary>
public class CameraSystem : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] PlayerMovement player;
    [SerializeField] Animator camObjAnim;
    [SerializeField] AudioSource camSwitchSound;
    [SerializeField] EnergyLevel energyLevel;

    [Header("Объекты камер")]
    [SerializeField] GameObject[] cameras;

    [Header("UI-объекты")]
    [SerializeField] GameObject camUI;
    [SerializeField] GameObject playerCamObj;   // GameObject игрока (не Camera)
    [SerializeField] GameObject whiteStatic;
    [SerializeField] GameObject PFXObj;
    [SerializeField] GameObject globalStatic;
    [SerializeField] GameObject blockerObj;

    int prevCam = 0;
    public bool camUp = false;

    // ─── Открытие планшета (через анимацию) ─────────────────────────────────
    public void CamPlayAnim()
    {
        if (!camUp && camObjAnim != null)
            camObjAnim.Play("CamOpenAnim", 0, 0);

        StartCoroutine(WaitForAnim());
    }

    // ─── Переключение планшета ───────────────────────────────────────────────
    public void CameraSwitch()
    {
        if (energyLevel == null) return;
        if (energyLevel.energy <= 0) return; // нельзя открыть при нулевой батарее

        if (whiteStatic != null) whiteStatic.SetActive(true);
        if (PFXObj != null)      PFXObj.SetActive(true);
        if (blockerObj != null)  blockerObj.SetActive(true);

        if (!camUp)
        {
            // Открываем планшет
            if (camSwitchSound != null) camSwitchSound.Play();
            camUp = true;
            if (camUI != null) camUI.SetActive(true);

            // Выключаем фонарик
            if (player != null) player.FlashOff();
            if (playerCamObj != null) playerCamObj.SetActive(false);

            if (cameras != null && cameras.Length > 0 && cameras[prevCam] != null)
                cameras[prevCam].SetActive(true);

            energyLevel.usage++;
            energyLevel.UpdateUsageSlider();
            if (globalStatic != null) globalStatic.SetActive(true);
            if (player != null) player.flashIsOn = false;
        }
        else
        {
            // Закрываем планшет
            if (PFXObj != null)      PFXObj.SetActive(false);
            camUp = false;
            if (camUI != null) camUI.SetActive(false);
            if (playerCamObj != null) playerCamObj.SetActive(true);

            if (cameras != null && cameras.Length > 0 && cameras[prevCam] != null)
                cameras[prevCam].SetActive(false);

            energyLevel.usage--;
            energyLevel.UpdateUsageSlider();

            if (camObjAnim != null) camObjAnim.Play("CamCloseAnim", 0, 0);
            if (globalStatic != null) globalStatic.SetActive(false);
            if (blockerObj != null)  blockerObj.SetActive(false); // БАГ ИСПРАВЛЕН: было SetActive(true)
        }

        StartCoroutine(ResetStaticAnim());
    }

    // ─── Смена камеры ────────────────────────────────────────────────────────
    public void CameraChange(int camNum)
    {
        if (cameras == null || camNum >= cameras.Length) return;

        if (camSwitchSound != null) camSwitchSound.Play();
        if (whiteStatic != null) whiteStatic.SetActive(true);

        if (cameras[prevCam] != null) cameras[prevCam].SetActive(false);
        if (cameras[camNum] != null)  cameras[camNum].SetActive(true);
        prevCam = camNum;

        StartCoroutine(ResetStaticAnim());
    }

    // ─── Корутины ────────────────────────────────────────────────────────────
    IEnumerator WaitForAnim()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        CameraSwitch();
    }

    IEnumerator ResetStaticAnim()
    {
        yield return new WaitForSecondsRealtime(0.3f);
        if (whiteStatic != null) whiteStatic.SetActive(false);
    }
}
