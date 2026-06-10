using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ИИ аниматроника — продвижение по точкам пути, скример, сброс.
///
/// КАК РАБОТАЕТ:
/// 1. Каждые ~timer секунд бросает кубик (1-20).
/// 2. Если результат ≤ aiLevel → аниматроник делает шаг вперёд.
/// 3. Когда достигает последней позиции — проверяет дверь.
///    - Дверь закрыта: стучит и возвращается на старт.
///    - Дверь открыта: СКРИМЕР.
///
/// АНИМАТРОНИКИ (animatronicName / aiNum):
///   Freddo    / 0
///   Bonita    / 1
///   ChikaLoka / 2
///   FoxyRex   / 3
///   GLITCH    / 4
/// </summary>
public class AnimatronicAI : MonoBehaviour
{
    [Header("Основные настройки")]
    [SerializeField] string animatronicName = "Freddo";
    [SerializeField] int aiNum = 0;
    [SerializeField] float moveTimerBase = 20f;   // базовое время между ходами (сек)

    [Header("Путь")]
    [SerializeField] Transform[] positions;       // точки пути (stage 1 = positions[0])
    [SerializeField] bool randomizePath = false;
    [SerializeField] AnimatronicExtraAIPaths[] extraPaths;

    [Header("Дверь и игрок")]
    [SerializeField] DoorHolder doorHolder;
    [SerializeField] PlayerMovement player;
    [SerializeField] TimePass timePass;

    [Header("Скример")]
    [SerializeField] GameObject jsModel;          // 3D-модель скримера

    [Header("Визуальные эффекты")]
    [SerializeField] GameObject globalStatic;     // помехи при движении
    [SerializeField] GameObject camUI;            // UI камер (скрывается перед скримером)

    [Header("Аудио")]
    [SerializeField] AudioSource moveSound;
    [SerializeField] AudioClip[] moveSounds;
    [SerializeField] AudioSource[] doorHitSounds;
    [SerializeField] AudioSource[] ambienceIntenseSounds;

    // ─── Состояние ───────────────────────────────────────────────────────────
    public int stage = 0;
    public bool alive = true;
    Animator animator;
    Coroutine _aiCoroutine;
    Vector3 defaultPos;
    Quaternion defaultRot;
    int aiLevel = 0;
    const int MAX_AI_LEVEL = 20;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        animator = GetComponent<Animator>();
        defaultPos = transform.position;
        defaultRot = transform.rotation;

        // Кастомная ночь — уровень из PlayerPrefs
        if (PlayerPrefs.GetString("CustomNight", "not active") == "active")
        {
            aiLevel = PlayerPrefs.GetInt(animatronicName, 0);
        }
        else
        {
            aiLevel = (timePass != null) ? timePass.GetAILevel(aiNum) : 0;

            // Предмет «Лёд» — снижает агрессию
            if (PlayerPrefs.GetString("Ice") == "active")
            {
                PlayerPrefs.SetString("Ice", "not active");
                aiLevel = Mathf.Max(0, aiLevel - 3);
            }
        }

        _aiCoroutine = StartCoroutine(AILoop());
    }

    // ─── Сброс (вызывается ShockSystem или LightHitbox) ──────────────────────
    public void ResetAnimatronic()
    {
        if (_aiCoroutine != null)
        {
            StopCoroutine(_aiCoroutine);
            _aiCoroutine = null;
        }
        stage = 0;
        alive = true;
        transform.position = defaultPos;
        transform.rotation = defaultRot;

        if (animator != null && HasState(animator, "0"))
            animator.Play("0", 0, 0);

        StartCoroutine(GlobalStaticFader());
        _aiCoroutine = StartCoroutine(AILoop());
    }

    // ─── Главный цикл ИИ (while — без накопления корутин) ───────────────────
    IEnumerator AILoop()
    {
        // Случайный путь при старте
        if (randomizePath && stage == 0 && extraPaths != null && extraPaths.Length > 0)
        {
            int rnd = Random.Range(0, extraPaths.Length);
            positions  = extraPaths[rnd].positions;
            doorHolder = extraPaths[rnd].doorHolder;
        }

        while (alive)
        {
            // Если игрок мёртв — останавливаем цикл
            if (player != null && !player.alive) yield break;

            // Ожидание перед следующим ходом
            float wait = (positions != null && stage == positions.Length)
                ? moveTimerBase / 2f + 1.5f
                : moveTimerBase + Random.Range(-1.5f, 1.5f);

            yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, wait));

            // Снова проверяем живого игрока после паузы
            if (player != null && !player.alive) yield break;

            // Бросок кубика
            bool willMove = (aiLevel > 0) && (Random.Range(1, MAX_AI_LEVEL + 1) <= aiLevel);

            if (positions == null || positions.Length == 0)
                continue; // нет пути — ждём следующего тика

            if (willMove)
            {
                if (stage < positions.Length)
                {
                    // Шаг вперёд
                    stage++;
                    MoveToCurrentStage();
                    PlayMoveSound();
                    StartCoroutine(GlobalStaticFader());
                }
                else
                {
                    // Достигли двери
                    // Иммунитет на Ночи 1 пока говорит Фон Гай
                    TimePass tp = FindFirstObjectByType<TimePass>();
                    if (tp != null && tp.phoneIsActive && timePass != null && timePass.nightNum == 1)
                    {
                        ResetToStart();
                        continue;
                    }
                    AttemptJumpscare();
                    // После скримера цикл тоже остановится (player.alive = false)
                }
            }
            else
            {
                // Не двигается, но если уже у двери и дверь закрыта — отступаем
                if (stage == positions.Length && doorHolder != null && doorHolder.closed)
                {
                    PlayDoorHit();
                    ResetToStart();
                }
            }

            // Усиленный эмбиент — аниматроник близко (stage > 3)
            if (stage > 3)
                PlayIntenseAmbience();
        }
    }

    // ─── Попытка скримера ────────────────────────────────────────────────────
    void AttemptJumpscare()
    {
        if (doorHolder != null && doorHolder.closed)
        {
            // Дверь заблокирована — стучим и возвращаемся
            PlayDoorHit();
            ResetToStart();
        }
        else
        {
            // Дверь открыта — СКРИМЕР
            if (player != null)
            {
                // Если в режиме камер — переключаем обратно на игрока
                if (!player.gameObject.activeSelf)
                {
                    if (camUI != null) camUI.SetActive(false);
                    player.gameObject.SetActive(true);
                }
                player.GameOver(jsModel, aiNum);
            }
        }
    }

    // ─── Перемещение ─────────────────────────────────────────────────────────
    void MoveToCurrentStage()
    {
        if (stage > 0 && stage <= positions.Length)
        {
            var t = positions[stage - 1];
            transform.position = t.position;
            transform.rotation = t.rotation;
        }

        // Анимация по номеру этапа
        if (animator != null)
        {
            string stateName = stage.ToString();
            if (HasState(animator, stateName))
                animator.Play(stateName, 0, 0);
        }
    }

    // Проверяем есть ли такое состояние в аниматоре
    bool HasState(Animator anim, string stateName)
    {
        int hash = Animator.StringToHash(stateName);
        for (int i = 0; i < anim.layerCount; i++)
        {
            if (anim.HasState(i, hash))
                return true;
        }
        return false;
    }

    void ResetToStart()
    {
        stage = 0;
        transform.position = defaultPos;
        transform.rotation = defaultRot;
        if (animator != null && HasState(animator, "0"))
            animator.Play("0", 0, 0);
        StartCoroutine(GlobalStaticFader());
    }

    // ─── Аудио ───────────────────────────────────────────────────────────────
    void PlayMoveSound()
    {
        if (moveSound != null && moveSounds != null && moveSounds.Length > 0)
        {
            moveSound.clip = moveSounds[Random.Range(0, moveSounds.Length)];
            moveSound.Play();
        }
    }

    void PlayDoorHit()
    {
        if (doorHitSounds != null && doorHitSounds.Length > 0)
            doorHitSounds[Random.Range(0, doorHitSounds.Length)].Play();
    }

    void PlayIntenseAmbience()
    {
        if (ambienceIntenseSounds == null || ambienceIntenseSounds.Length == 0) return;
        foreach (var src in ambienceIntenseSounds)
            if (src != null && src.isPlaying) return;
        ambienceIntenseSounds[Random.Range(0, ambienceIntenseSounds.Length)].Play();
    }

    // ─── Визуальные эффекты ───────────────────────────────────────────────────
    IEnumerator GlobalStaticFader()
    {
        if (globalStatic == null) yield break;
        RawImage img = globalStatic.GetComponent<RawImage>();
        if (img == null) yield break;

        img.color = new Color32(123, 123, 123, 255);
        yield return new WaitForSecondsRealtime(0.2f);

        for (byte alpha = 255; alpha > 46; alpha -= 3)
        {
            img.color = new Color32(123, 123, 123, alpha);
            yield return new WaitForSecondsRealtime(0.01f);
        }

        img.color = new Color32(123, 123, 123, 0);
    }
}
