using System.Collections;
using UnityEngine;

/// <summary>
/// Улучшенный AI аниматроника. Совместим с оригинальными сценами и GUID.
/// Добавлено: звуки движения/дыхания, ускорение при взгляде игрока через камеру,
/// иммунитет первой ночи при активном телефонном звонке.
/// </summary>
public class ImprovedAnimatronicAI : MonoBehaviour
{
    // ─── Идентификация ────────────────────────────────────────────────────────

    [Header("Identity")]
    [Tooltip("Уникальный ID аниматроника (0 = Freddo, 1 = Bonny, и т.д.)")]
    public int animatronicID = 0;

    // ─── Уровень AI ───────────────────────────────────────────────────────────

    [Header("AI Settings")]
    [Tooltip("Уровень агрессивности AI (1-20). Чем выше — тем чаще двигается.")]
    [Range(0, 20)]
    public int aiLevel = 5;

    [Tooltip("Базовое время между попытками переместиться (сек)")]
    [SerializeField] private float baseMoveInterval = 5f;

    [Tooltip("Множитель ускорения при взгляде игрока через камеру")]
    public float stareMultiplier = 1.5f;

    // ─── Waypoints ────────────────────────────────────────────────────────────

    [Header("Waypoints")]
    [SerializeField] private Transform[] waypoints;
    [Tooltip("Текущий индекс waypoint")]
    [SerializeField] private int currentWaypointIndex = 0;

    // ─── Дверь ────────────────────────────────────────────────────────────────

    [Header("Door Reference")]
    [Tooltip("Какая дверь блокирует аниматроника (0 = левая, 1 = правая)")]
    [SerializeField] private int doorSide = 0;

    // ─── Звуки ────────────────────────────────────────────────────────────────

    [Header("Sounds")]
    [Tooltip("Звуки шагов при перемещении между waypoints")]
    [SerializeField] private AudioClip[] moveSounds;

    [Tooltip("Тихое дыхание когда аниматроник стоит у двери")]
    [SerializeField] private AudioClip[] breathingSounds;

    [SerializeField] private AudioSource audioSource;

    [Tooltip("Громкость звука шагов")]
    [SerializeField] private float moveSoundVolume  = 0.6f;
    [Tooltip("Громкость дыхания у двери")]
    [SerializeField] private float breathSoundVolume = 0.25f;

    // ─── Ночь 1 — иммунитет телефона ─────────────────────────────────────────

    [Header("Night 1 Phone Immunity")]
    [Tooltip("Ссылка на флаг активного телефонного звонка (Ночь 1)")]
    [SerializeField] private bool phoneIsActive = false;

    // ─── Состояние ────────────────────────────────────────────────────────────

    /// <summary>Устанавливается CameraSystem когда игрок смотрит на аниматроника.</summary>
    [HideInInspector] public bool isBeingWatched = false;

    private bool _isAtDoor      = false;
    private bool _isActive      = true;
    private float _moveTimer    = 0f;
    private float _stareTimer   = 0f;
    private Coroutine _breathingCoroutine;

    // ─────────────────────────────────────────────────────────────────────────
    // Жизненный цикл
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Телепортируемся на начальный waypoint
        SnapToWaypoint(currentWaypointIndex);
    }

    private void Update()
    {
        if (!_isActive) return;

        // Ночь 1: пока звонит телефон — не двигаемся
        if (phoneIsActive) return;

        HandleStareAcceleration();
        HandleMoveTimer();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Публичный API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Вызывается CameraSystem когда игрок смотрит на аниматроника через камеру.
    /// Ускоряет таймер перемещения при долгом взгляде (> 3 сек).
    /// </summary>
    public void OnPlayerStare()
    {
        isBeingWatched = true;
        // Фактическое применение происходит в HandleStareAcceleration()
    }

    /// <summary>Активировать / деактивировать AI.</summary>
    public void SetActive(bool active) => _isActive = active;

    /// <summary>Устанавливает флаг активности телефонного звонка (Ночь 1).</summary>
    public void SetPhoneActive(bool active) => phoneIsActive = active;

    // ─────────────────────────────────────────────────────────────────────────
    // Приватные методы
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Если игрок смотрит на аниматроника > 3 сек — ускоряем moveTimer.
    /// </summary>
    private void HandleStareAcceleration()
    {
        if (isBeingWatched)
        {
            _stareTimer += Time.deltaTime;

            if (_stareTimer > 3f)
            {
                // Ускоряем таймер движения
                _moveTimer += Time.deltaTime * (stareMultiplier - 1f);
                _stareTimer = 0f; // сбрасываем, чтобы эффект применялся каждые 3 сек
            }

            isBeingWatched = false; // флаг сбрасывается каждый кадр, CameraSystem обновляет его
        }
        else
        {
            _stareTimer = 0f;
        }
    }

    /// <summary>
    /// Основная логика таймера перемещения.
    /// Шанс движения 1/(21-aiLevel) при каждой попытке.
    /// </summary>
    private void HandleMoveTimer()
    {
        float interval = baseMoveInterval / Mathf.Max(1, aiLevel);
        _moveTimer += Time.deltaTime;

        if (_moveTimer >= interval)
        {
            _moveTimer = 0f;
            TryMove();
        }
    }

    /// <summary>
    /// Пытается переместиться на следующий waypoint с учётом уровня AI.
    /// Шанс успеха: aiLevel / 20.
    /// </summary>
    private void TryMove()
    {
        float chance = aiLevel / 20f;
        if (Random.value > chance) return;

        int nextIndex = currentWaypointIndex + 1;

        if (waypoints == null || nextIndex >= waypoints.Length)
        {
            // Достигли конца — аниматроник у двери
            ArrivedAtDoor();
            return;
        }

        // Перемещаемся
        currentWaypointIndex = nextIndex;
        SnapToWaypoint(currentWaypointIndex);
        PlayMoveSound();

        // Уведомляем PostProcessingController о близости аниматроника
        bool nearDoor = currentWaypointIndex >= waypoints.Length - 1;
        if (PostProcessingController.Instance != null)
            PostProcessingController.Instance.TriggerAnimatronicAtDoor(nearDoor);
    }

    /// <summary>
    /// Телепортирует аниматроника на указанный waypoint.
    /// </summary>
    private void SnapToWaypoint(int index)
    {
        if (waypoints == null || index < 0 || index >= waypoints.Length) return;
        if (waypoints[index] == null) return;

        transform.position = waypoints[index].position;
        transform.rotation = waypoints[index].rotation;
    }

    /// <summary>
    /// Вызывается когда аниматроник достигает последнего waypoint (дверь).
    /// </summary>
    private void ArrivedAtDoor()
    {
        if (_isAtDoor) return;
        _isAtDoor = true;

        // Проверяем закрыта ли дверь
        bool doorClosed = IsDoorClosed();

        if (doorClosed)
        {
            // Дверь закрыта — дыхание у двери, стоим и ждём
            StartBreathingAtDoor();

            // Через некоторое время уходим
            StartCoroutine(LeaveAfterDelay(Random.Range(15f, 30f)));
        }
        else
        {
            // Дверь открыта — СКРИМЕР
            TriggerJumpscare();
        }
    }

    /// <summary>
    /// Проверяет закрыта ли соответствующая дверь (через GameManager или прямую ссылку).
    /// Замените реализацию под вашу систему дверей.
    /// </summary>
    private bool IsDoorClosed()
    {
        // TODO: интегрировать с вашей системой дверей
        // Пример: return GameManager.Instance.IsDoorClosed(doorSide);
        return false;
    }

    private void TriggerJumpscare()
    {
        if (JumpscareSystem.Instance != null)
            JumpscareSystem.Instance.TriggerJumpscare(animatronicID);
    }

    // ─── Звуки ────────────────────────────────────────────────────────────────

    private void PlayMoveSound()
    {
        if (audioSource == null || moveSounds == null || moveSounds.Length == 0) return;

        AudioClip clip = moveSounds[Random.Range(0, moveSounds.Length)];
        if (clip != null)
            audioSource.PlayOneShot(clip, moveSoundVolume);
    }

    private void StartBreathingAtDoor()
    {
        if (_breathingCoroutine != null)
            StopCoroutine(_breathingCoroutine);

        _breathingCoroutine = StartCoroutine(BreathingCoroutine());
    }

    /// <summary>
    /// Случайное дыхание пока аниматроник стоит у двери.
    /// </summary>
    private IEnumerator BreathingCoroutine()
    {
        while (_isAtDoor)
        {
            if (audioSource != null && breathingSounds != null && breathingSounds.Length > 0)
            {
                AudioClip clip = breathingSounds[Random.Range(0, breathingSounds.Length)];
                if (clip != null)
                    audioSource.PlayOneShot(clip, breathSoundVolume);
            }

            yield return new WaitForSeconds(Random.Range(3f, 7f));
        }
    }

    /// <summary>
    /// Аниматроник уходит от двери после задержки.
    /// </summary>
    private IEnumerator LeaveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_breathingCoroutine != null)
        {
            StopCoroutine(_breathingCoroutine);
            _breathingCoroutine = null;
        }

        _isAtDoor = false;
        currentWaypointIndex = 0;
        SnapToWaypoint(0);

        // Убираем эффект у двери
        if (PostProcessingController.Instance != null)
            PostProcessingController.Instance.TriggerAnimatronicAtDoor(false);
    }
}
