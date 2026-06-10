using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Анимация заполнения кнопок в главном меню при наведении.
/// </summary>
public class MenuButtonFiller : MonoBehaviour
{
    [SerializeField] Image rSlider;
    [SerializeField] Image lSlider;

    const float STEP = 0.05f;
    const float DELAY = 0.001f;

    void OnDisable()
    {
        StopAllCoroutines();
        SetFill(0f);
    }

    public void Hover()
    {
        StopAllCoroutines();
        StartCoroutine(FillTo(1f));
    }

    public void UnHover()
    {
        StopAllCoroutines();
        StartCoroutine(FillTo(0f));
    }

    IEnumerator FillTo(float target)
    {
        float current = rSlider != null ? rSlider.fillAmount : 0f;
        float dir = target > current ? STEP : -STEP;

        while (Mathf.Abs(target - current) > 0.001f)
        {
            current = Mathf.Clamp01(current + dir);
            SetFill(current);
            yield return new WaitForSecondsRealtime(DELAY);
        }

        SetFill(target);
    }

    void SetFill(float v)
    {
        if (rSlider != null) rSlider.fillAmount = v;
        if (lSlider != null) lSlider.fillAmount = v;
    }
}
