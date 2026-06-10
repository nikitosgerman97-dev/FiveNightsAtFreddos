using System.Collections;
using UnityEngine;

/// <summary>
/// Хитбокс фонарика: если светить на аниматроника достаточно долго,
/// он сбрасывается на стартовую позицию.
/// Тег фонарика: "LightTag"
/// </summary>
public class LightHitbox : MonoBehaviour
{
    [SerializeField] PlayerMovement player;
    [SerializeField] AnimatronicAI affectedAnimatronic;

    // Сколько раз подряд нужно «осветить» чтобы сбросить аниматроника
    [SerializeField] int flashesRequired = 3;
    [SerializeField] float flashCheckInterval = 0.85f;

    bool inLight = false;
    int fillMeter = 0;
    Coroutine lightRoutine;

    void OnTriggerEnter(Collider col)
    {
        if (col.CompareTag("LightTag"))
        {
            inLight = true;
            fillMeter = 0;
            if (lightRoutine != null) StopCoroutine(lightRoutine);
            lightRoutine = StartCoroutine(InLight());
        }
    }

    void OnTriggerExit(Collider col)
    {
        if (col.CompareTag("LightTag"))
        {
            inLight = false;
            fillMeter = 0;
            if (lightRoutine != null)
            {
                StopCoroutine(lightRoutine);
                lightRoutine = null;
            }
        }
    }

    IEnumerator InLight()
    {
        while (inLight)
        {
            if (player != null && player.flashIsOn)
            {
                fillMeter++;
                if (fillMeter >= flashesRequired)
                {
                    if (affectedAnimatronic != null)
                        affectedAnimatronic.ResetAnimatronic();
                    inLight = false;
                    fillMeter = 0;
                    yield break;
                }
            }
            else
            {
                // Фонарик выключили — сбрасываем счётчик
                fillMeter = 0;
            }

            yield return new WaitForSecondsRealtime(flashCheckInterval);
        }
    }
}
