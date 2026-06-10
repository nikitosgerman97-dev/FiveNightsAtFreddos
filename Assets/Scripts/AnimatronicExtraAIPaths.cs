using UnityEngine;

/// <summary>
/// Дополнительные пути для аниматроника.
/// Используется AnimatronicAI для выбора позиций патрулирования.
/// positions[0] — начальная позиция (сцена)
/// positions[последний] — позиция перед офисом
/// </summary>
public class AnimatronicExtraAIPaths : MonoBehaviour
{
    [Tooltip("Точки маршрута аниматроника (от сцены к офису)")]
    public Transform[] positions;

    [Tooltip("Дверь, которую этот аниматроник использует")]
    public DoorHolder doorHolder;

    /// <summary>
    /// Получить случайную промежуточную позицию маршрута.
    /// </summary>
    public Transform GetRandomPosition()
    {
        if (positions == null || positions.Length == 0) return null;
        return positions[Random.Range(0, positions.Length)];
    }

    /// <summary>
    /// Получить позицию по индексу безопасно.
    /// </summary>
    public Transform GetPosition(int index)
    {
        if (positions == null || positions.Length == 0) return null;
        return positions[Mathf.Clamp(index, 0, positions.Length - 1)];
    }
}
