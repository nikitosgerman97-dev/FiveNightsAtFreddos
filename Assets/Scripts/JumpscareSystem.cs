using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Централизованная система скримеров.
/// Показывает спрайт аниматроника, проигрывает звук, трясёт камеру,
/// вызывает PostProcessingController и загружает Game Over.
/// </summary>
public class JumpscareSystem : MonoBehaviour
{
    public static JumpscareSystem Instance { get; private set; }

    // ─── UI ───────────────────────────────────────────────────────────────────

    [Header("Jumpscare UI")]
    [Tooltip("Canvas, отображаемый поверх всего при скримере")]
    [SerializeField] private Canvas jumpscareCanvas;
    [Tooltip("Image для отображения спрайта аниматроника")]
    [SerializeField] private Image jumpscareImage;

    // ─── Спрайты аниматроников ────────────────────────────────────────────────

    [Header("Jumpscare Sprites")]
    [Tooltip("Спрайт по индексу = animatronicID (0 = Freddo, 1 = Bonny, ...)")]
    [SerializeField] private Sprite[] jumpscareImages;

    // ─── Звук ────────────────────────────────────────────────────────────────

    [Header("Audio")]
    [SerializeField] private AudioClip jumpscareSound;
    [SerializeField] private AudioSource audioSource;

    // ─── Тряска камеры ────────────────────────────────────────────────────────

    [Header("Camera Shake")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float cameraShakeIntensity = 0.3f;
    [SerializeField] private float cameraShakeDuration  = 0.5f;

    // ─── Game Over ────────────────────────────────────────────────────────────

    [Header("Game Over")]
    [Tooltip("Имя сцены Game Over. Если пусто — показывается gameOverPanel.")]
    [SerializeField] private string gameOverSceneName = "GameOver";
    [Tooltip("Альтернатива сцене: UI панель Game Over на Canvas")]
    [SerializeField] private GameObject gameOverPanel;
    [Tooltip("Время показа скримера до Game Over (сек)")]
    [SerializeField] private float jumpscareDisplayTime = 2.0f;

    // ─── Состояние ────────────────────────────────────────────────────────────

    private bool _jumpscareActive = false;
    private Vector3 _cameraOriginalPos;

    // ─────────────────────────────────────────────────────────────────────────
    // Жизненный цикл
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Скрываем Canvas при старте
        if (jumpscareCanvas != null)
            jumpscareCanvas.gameObject.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Запоминаем исходную позицию камеры
        if (cameraTransform != null)
            _cameraOriginalPos = cameraTransform.localPosition;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Публичный API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Запускает полную последовательность скримера для указанного аниматроника.
    /// animatronicID соответствует индексу в jumpscareImages.
    /// </summary>
    public void TriggerJumpscare(int animatronicID)
    {
        if (_jumpscareActive) return;

        _jumpscareActive = true;
        StartCoroutine(JumpscareSequence(animatronicID));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Приватные методы
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Полная последовательность скримера:
    /// 1. Показывает спрайт
    /// 2. Проигрывает звук
    /// 3. Трясёт камеру
    /// 4. Вызывает PostProcessingController
    /// 5. Загружает Game Over
    /// </summary>
    private IEnumerator JumpscareSequence(int animatronicID)
    {
        // 1. Показываем правильный спрайт
        ShowJumpscareSprite(animatronicID);

        // 2. Проигрываем звук на максимальной громкости
        PlayJumpscareSound();

        // 3. Тряска камеры
        if (cameraTransform != null)
            StartCoroutine(CameraShakeCoroutine());

        // 4. Эффект в PostProcessingController
        if (PostProcessingController.Instance != null)
            PostProcessingController.Instance.TriggerJumpscare();

        // 5. Ждём и показываем Game Over
        yield return new WaitForSeconds(jumpscareDisplayTime);

        GoToGameOver();
    }

    private void ShowJumpscareSprite(int animatronicID)
    {
        if (jumpscareCanvas == null) return;

        jumpscareCanvas.gameObject.SetActive(true);

        if (jumpscareImage == null) return;

        // Выбираем нужный спрайт или первый по умолчанию
        if (jumpscareImages != null && animatronicID >= 0 && animatronicID < jumpscareImages.Length)
        {
            jumpscareImage.sprite = jumpscareImages[animatronicID];
        }
        else if (jumpscareImages != null && jumpscareImages.Length > 0)
        {
            jumpscareImage.sprite = jumpscareImages[0];
            Debug.LogWarning($"[JumpscareSystem] Нет спрайта для animatronicID={animatronicID}, использован первый.");
        }
    }

    private void PlayJumpscareSound()
    {
        if (audioSource == null || jumpscareSound == null) return;

        audioSource.volume = 1.0f;
        audioSource.pitch  = 1.0f;
        audioSource.PlayOneShot(jumpscareSound, 1.0f);
    }

    private void GoToGameOver()
    {
        // Приоритет — загрузка сцены
        if (!string.IsNullOrEmpty(gameOverSceneName))
        {
            SceneManager.LoadScene(gameOverSceneName);
            return;
        }

        // Альтернатива — UI панель
        if (gameOverPanel != null)
        {
            if (jumpscareCanvas != null)
                jumpscareCanvas.gameObject.SetActive(false);

            gameOverPanel.SetActive(true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Coroutines
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Простая тряска камеры без DOTween — случайное смещение localPosition.
    /// </summary>
    private IEnumerator CameraShakeCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < cameraShakeDuration)
        {
            elapsed += Time.deltaTime;

            float progress  = elapsed / cameraShakeDuration;
            float damping   = 1f - progress; // затухание к концу
            float magnitude = cameraShakeIntensity * damping;

            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;

            cameraTransform.localPosition = _cameraOriginalPos + new Vector3(offsetX, offsetY, 0f);

            yield return null;
        }

        // Возвращаем камеру на место
        cameraTransform.localPosition = _cameraOriginalPos;
    }
}
