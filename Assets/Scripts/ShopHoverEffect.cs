using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Эффект наведения на предмет в магазине — показывает имя, описание, цену.
/// </summary>
public class ShopHoverEffect : MonoBehaviour
{
    [Header("Данные предмета")]
    [SerializeField] string itemNameStr;
    [SerializeField] string itemDescStr;
    [SerializeField] string itemCostStr;

    [Header("UI элементы")]
    [SerializeField] TextMeshProUGUI itemName;
    [SerializeField] TextMeshProUGUI itemDescription;
    [SerializeField] TextMeshProUGUI itemCost;
    [SerializeField] GameObject costBG;

    [Header("Анимация")]
    [SerializeField] Animator animator;

    bool _hovered = false;

    void OnMouseOver()
    {
        if (!_hovered)
        {
            if (animator != null) animator.Play("Hover", 0, 0f);
            _hovered = true;
        }

        if (costBG != null) costBG.SetActive(true);
        if (itemCost != null) itemCost.SetText(itemCostStr);
        if (itemName != null) itemName.SetText(itemNameStr);
        if (itemDescription != null) itemDescription.SetText(itemDescStr);
    }

    public void OnMouseExit()
    {
        _hovered = false;
        if (animator != null) animator.Play("UnHover", 0, 0f);

        if (costBG != null) costBG.SetActive(false);
        if (itemCost != null) itemCost.SetText("");
        if (itemName != null) itemName.SetText("");
        if (itemDescription != null) itemDescription.SetText("");
    }
}
