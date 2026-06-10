using UnityEngine;
using TMPro;

/// <summary>
/// Скрипт магазина — обработка кликов по предметам.
/// Товары: ПиццаКусок (30 монет), Генератор (15), Лёд (45).
/// Покупки сохраняются в PlayerPrefs.
/// </summary>
public class ShopInteractScript : MonoBehaviour
{
    [Header("UI монет")]
    [SerializeField] TextMeshProUGUI coinText;

    void Start()
    {
        UpdateCoinText();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Camera.main == null) return; // null check — камера ещё не инициализирована
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit rayHit, 1000f))
            {
                Interact(rayHit);
            }
        }
    }

    /// <summary>
    /// Обработка взаимодействия с предметом в магазине.
    /// </summary>
    void Interact(RaycastHit hit)
    {
        string itemName = hit.transform.gameObject.name;

        switch (itemName)
        {
            // Кусок пиццы — восстанавливает немного энергии ночью
            case "DrinkHolder":
            case "PizzaHolder":
                TryBuy(hit, "Drink", 30);  // ИСПРАВЛЕНО: было "PizzaSlice", TimePass проверяет "Drink"
                break;

            // Генератор — замедляет расход энергии
            case "GenHolder":
            case "GeneratorHolder":
                TryBuy(hit, "Generator", 15);
                break;

            // Холодильник — замораживает аниматроника на время
            case "IceHolder":
            case "FreezerHolder":
                TryBuy(hit, "Ice", 45);
                break;
        }
    }

    void TryBuy(RaycastHit hit, string itemKey, int cost)
    {
        int coins = PlayerPrefs.GetInt("CogCoins", 0);
        if (coins < cost) return;

        // Убираем hover эффект
        var hover = hit.transform.GetComponent<ShopHoverEffect>();
        if (hover != null) hover.OnMouseExit();

        PlayerPrefs.SetInt("CogCoins", coins - cost);
        UpdateCoinText();

        Destroy(hit.transform.gameObject);
        PlayerPrefs.SetString(itemKey, "active");
    }

    void UpdateCoinText()
    {
        if (coinText != null)
            coinText.SetText(PlayerPrefs.GetInt("CogCoins", 0).ToString());
    }
}
