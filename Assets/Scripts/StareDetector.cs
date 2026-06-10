using System.Collections;
using UnityEngine;

/// <summary>
/// Определяет, на что смотрит игрок через камеру видеонаблюдения.
/// Использует Raycast из центра экрана каждые 0.5 сек.
/// При взгляде на аниматроника > 3 сек — вызывает OnPlayerStare().
/// Работает только когда система камер активна.
/// </summary>
public class StareDetector : MonoBehaviour
{
    // ─── Настройки ────────────────────────────────────────────────────────────

    [Header("Raycast Settings")]
    [Tooltip("Максимальная дистанция луча")]
    [SerializeField] private float raycastDistance = 100f;
    [Tooltip("Layers для обнаружения аниматроников")]
    [SerializeField] private LayerMask animatronicLayerMask = ~0;
    [Tooltip("Тег объектов-аниматроников")]
    [SerializeField] private string animatronicTag = "Animatronic";

    [Header("Stare Timer")]
    [Tooltip("Время взгляда (сек) до вызова OnPlayerStare()")]
    [SerializeField] private float stareThreshold = 3.0f;
    [Tooltip("Интервал между raycast-проверками (сек)")]
    [SerializeField] private float checkInterval = 0.5f;

    // ─── Состояние ────────────────────────────────────────────────────────────

    /// <summary>Накопленное время взгляда на текущий объект.</summary>
    private float _stareTimer = 0f;

    private ImprovedAnimatronicAI _currentTarget = null;
    private Coroutine _checkCoroutine;

    // ─── Камера ───────────────────────────────────────────────────────────────

    private Camera _mainCamera;

    // ─────────────────────────────────────────────────────────────────────────
    // Жизненный цикл
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _mainCamera = Camera.main;

        if (_mainCamera == null)
            Debug.LogWarning("[StareDetector] Main Camera не найдена!");
    }

    private void Start()
    {
        // Начинаем проверку только при активных камерах (см. SetCameraActive)
        // Coroutine не запускается до включения камер
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Публичный API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Включает/выключает детектор взгляда.
    /// Должно вызываться из CameraSystem когда игрок открывает/закрывает камеры.
    /// </summary>
    public void SetCameraActive(bool active)
    {
        if (active)
        {
            if (_checkCoroutine == null)
                _checkCoroutine = StartCoroutine(StareCheckCoroutine());
        }
        else
        {
            if (_checkCoroutine != null)
            {
                StopCoroutine(_checkCoroutine);
                _checkCoroutine = null;
            }

            // Сбрасываем состояние при закрытии камер
            ResetStare();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Приватные методы
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Периодически пускает луч из центра экрана и проверяет попадание в аниматроника.
    /// </summary>
    private IEnumerator StareCheckCoroutine()
    {
        WaitForSeconds waitInterval = new WaitForSeconds(checkInterval);

        while (true)
        {
            PerformStareCheck();
            yield return waitInterval;
        }
    }

    private void PerformStareCheck()
    {
        if (_mainCamera == null) return;

        // Луч из центра экрана вперёд
        Ray ray = _mainCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, animatronicLayerMask))
        {
            // Проверяем тег
            if (hit.collider.CompareTag(animatronicTag))
            {
                ImprovedAnimatronicAI animatronic = hit.collider.GetComponentInParent<ImprovedAnimatronicAI>();

                if (animatronic == null)
                    animatronic = hit.collider.GetComponent<ImprovedAnimatronicAI>();

                if (animatronic != null)
                {
                    // Тот же аниматроник — накапливаем таймер
                    if (animatronic == _currentTarget)
                    {
                        _stareTimer += checkInterval;

                        // Каждый кадр проверки уведомляем аниматроника о взгляде
                        animatronic.OnPlayerStare();

                        // Если накопили достаточно — триггер и сброс
                        if (_stareTimer >= stareThreshold)
                        {
                            OnStareThresholdReached(animatronic);
                        }
                    }
                    else
                    {
                        // Новый аниматроник — сбрасываем таймер
                        _currentTarget = animatronic;
                        _stareTimer = 0f;
                    }

                    return;
                }
            }
        }

        // Луч не попал в аниматроника — сбрасываем
        ResetStare();
    }

    /// <summary>
    /// Вызывается когда игрок смотрел на аниматроника дольше stareThreshold.
    /// </summary>
    private void OnStareThresholdReached(ImprovedAnimatronicAI animatronic)
    {
        Debug.Log($"[StareDetector] Игрок долго смотрит на аниматроника ID={animatronic.animatronicID}");

        // Вызываем метод аниматроника
        animatronic.OnPlayerStare();

        // Сбрасываем таймер (будет снова накапливаться)
        _stareTimer = 0f;
    }

    private void ResetStare()
    {
        _currentTarget = null;
        _stareTimer    = 0f;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Визуализация луча в редакторе.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (_mainCamera == null) return;

        Gizmos.color = Color.cyan;
        Vector3 origin = _mainCamera.transform.position;
        Vector3 direction = _mainCamera.transform.forward * raycastDistance;
        Gizmos.DrawRay(origin, direction);
    }
#endif
}
