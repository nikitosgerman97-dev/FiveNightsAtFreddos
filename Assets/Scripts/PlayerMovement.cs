using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Управление охранником Генри.
/// 
/// УПРАВЛЕНИЕ:
///   Мышь              — поворот головы влево/вправо
///   ЛКМ (удержать)    — фонарик
///   ПКМ               — открыть/закрыть планшет с камерами
///   ESC (удержать 3с) — выход в меню
///   W→I→N→D→Y         — пасхалка windy31
/// 
/// ПРИМЕЧАНИЕ: В оригинальном FNAF игрок не ходит,
/// только поворачивает камеру. Так реализовано здесь.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] TimePass timePass;
    [SerializeField] CameraSystem camSys;
    [SerializeField] EnergyLevel energyLevel;
    [SerializeField] Camera playerCam;

    [Header("Аудио")]
    [SerializeField] AudioSource jumpscareSound;
    [SerializeField] AudioSource squeakSound;
    [SerializeField] AudioSource flashOnSound;
    [SerializeField] AudioSource flashOffSound;

    [Header("UI — игровые объекты")]
    [SerializeField] TextMeshProUGUI loaderText;
    [SerializeField] GameObject jumpscareObjects;
    [SerializeField] GameObject playerUI;
    [SerializeField] GameObject goObject;         // экран Game Over
    [SerializeField] GameObject flashlight;
    [SerializeField] GameObject hallwayBlockers;  // туман/темнота в коридорах

    [Header("Скример — полноэкранная картинка")]
    [SerializeField] GameObject jumpscareImageObject;     // объект на Canvas (изначально выключен)
    [SerializeField] Image jumpscareImage;                // Image на весь экран
    [SerializeField] Sprite[] jumpscareSprites;           // [0]=Freddo [1]=Bonita [2]=ChikaLoka [3]=FoxyRex [4]=GLITCH

    [Header("Пасхалка windy31")]
    [SerializeField] GameObject easterEggPanel;   // панель с надписью "windy31"
    [SerializeField] AudioSource easterEggSound;

    [Header("Настройки поворота")]
    [SerializeField] float rotationSpeed = 13f;
    [SerializeField] float maxRotY = 90f;
    [SerializeField] float minRotY = -90f;

    // ─── Пасхалка windy31 ────────────────────────────────────────────────────
    readonly KeyCode[] windySequence = {
        KeyCode.W, KeyCode.I, KeyCode.N, KeyCode.D, KeyCode.Y
    };
    int windyProgress = 0;
    float windyResetTimer = 0f;
    const float WINDY_RESET_TIME = 3f; // сброс если не нажал за 3 сек

    // ─── Состояние ───────────────────────────────────────────────────────────
    public static PlayerMovement instance;
    bool holdingEsc = false;
    public bool alive = true;
    public bool flashIsOn = false;

    // Ссылка на паузу — ESC не срабатывает если открыто меню паузы
    GraphicsSettings _graphicsSettings;
    Coroutine _quitCoroutine;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        instance = this;
        _graphicsSettings = FindFirstObjectByType<GraphicsSettings>();
    }

    void FixedUpdate()
    {
        if (!alive) return;
        HandleRotation();
        HandleEscapeKey();
    }

    void Update()
    {
        if (!alive) return;
        HandleFlashlight();
        HandleMouseClick();
        HandleWindyEasterEgg();
    }

    // ─── Поворот по позиции мыши ─────────────────────────────────────────────
    void HandleRotation()
    {
        float quarterW = Screen.width / 4f;

        if (Input.mousePosition.x >= Screen.width - quarterW)
        {
            float currentY = transform.localEulerAngles.y;
            // Unity хранит углы 0-360, нормализуем
            if (currentY > 180f) currentY -= 360f;
            if (currentY < maxRotY)
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
        else if (Input.mousePosition.x <= quarterW)
        {
            float currentY = transform.localEulerAngles.y;
            if (currentY > 180f) currentY -= 360f;
            if (currentY > minRotY)
                transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime);
        }
    }

    // ─── ESC — выход в меню (зажать 3 секунды) ───────────────────────────────
    void HandleEscapeKey()
    {
        // Если открыто меню паузы — ESC обрабатывается там
        if (_graphicsSettings != null && _graphicsSettings.IsPaused) return;

        if (Input.GetKey(KeyCode.Escape))
        {
            if (!holdingEsc)
            {
                holdingEsc = true;
                _quitCoroutine = StartCoroutine(QuitToMenu());
            }
        }
        else
        {
            if (holdingEsc)
            {
                // ИСПРАВЛЕНО: StopCoroutine(QuitToMenu()) создавал новый итератор и не останавливал!
                if (_quitCoroutine != null) StopCoroutine(_quitCoroutine);
                _quitCoroutine = null;
                holdingEsc = false;
            }
        }
    }

    // ─── Фонарик (ЛКМ удержать) ──────────────────────────────────────────────
    void HandleFlashlight()
    {
        bool press   = Input.GetMouseButtonDown(0);
        bool release = Input.GetMouseButtonUp(0);

        if (press && energyLevel != null && energyLevel.energy > 0 && !flashIsOn)
        {
            flashIsOn = true;
            if (flashlight != null) flashlight.SetActive(true);
            if (hallwayBlockers != null) hallwayBlockers.SetActive(false);
            energyLevel.usage++;
            energyLevel.UpdateUsageSlider();
            if (flashOnSound != null) flashOnSound.Play();
        }

        if (release && flashIsOn)
        {
            TurnOffFlash();
        }
    }

    /// <summary>Принудительно выключить фонарик (вызывается CameraSystem).</summary>
    public void FlashOff()
    {
        if (flashIsOn)
            TurnOffFlash();
    }

    void TurnOffFlash()
    {
        flashIsOn = false;
        if (flashlight != null) flashlight.SetActive(false);
        if (energyLevel != null && energyLevel.energy > 0)
        {
            if (hallwayBlockers != null) hallwayBlockers.SetActive(true);
            energyLevel.usage--;
            energyLevel.UpdateUsageSlider();
            if (flashOffSound != null) flashOffSound.Play();
        }
    }

    // ─── ПКМ — камеры ─────────────────────────────────────────────────────────
    void HandleMouseClick()
    {
        if (Input.GetMouseButtonDown(1) && camSys != null)
            camSys.CamPlayAnim();

        // Клик на объекты сцены (двери, плюшевки и т.д.)
        // Фонарик обрабатывается отдельно в HandleFlashlight
    }

    // ─── Взаимодействие с объектами через Raycast ─────────────────────────────
    void HandleInteraction()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        RaycastHit hit;
        if (Physics.Raycast(playerCam.ScreenPointToRay(Input.mousePosition), out hit, 100f))
        {
            switch (hit.transform.gameObject.name)
            {
                case "Button":
                case "DoorButton":
                    hit.transform.GetComponent<DoorHolder>()?.ToggleDoor();
                    break;

                case "PeterPlushie":
                case "FreddoPlushie":
                    hit.transform.GetComponent<Animator>()?.Play("Squeak", 0, 0);
                    if (squeakSound != null) squeakSound.Play();
                    break;
            }
        }
    }

    // ─── Пасхалка W→I→N→D→Y ──────────────────────────────────────────────────
    void HandleWindyEasterEgg()
    {
        // Сброс по таймеру если долго не нажимали
        if (windyProgress > 0)
        {
            windyResetTimer += Time.deltaTime;
            if (windyResetTimer > WINDY_RESET_TIME)
            {
                windyProgress = 0;
                windyResetTimer = 0f;
            }
        }

        for (int i = 0; i < windySequence.Length; i++)
        {
            if (Input.GetKeyDown(windySequence[i]))
            {
                if (i == windyProgress)
                {
                    windyProgress++;
                    windyResetTimer = 0f;

                    if (windyProgress == windySequence.Length)
                    {
                        windyProgress = 0;
                        ActivateWindyEasterEgg();
                    }
                }
                else
                {
                    // Неверная клавиша — сброс (но если нажали W — начинаем заново)
                    windyProgress = (windySequence[i] == KeyCode.W) ? 1 : 0;
                    windyResetTimer = 0f;
                }
                break;
            }
        }
    }

    void ActivateWindyEasterEgg()
    {
        // Считаем сколько раз активировали (для звезды 4)
        int count = PlayerPrefs.GetInt("WindyEasterEggCount", 0) + 1;
        PlayerPrefs.SetInt("WindyEasterEggCount", count);

        StartCoroutine(WindyEasterEggRoutine());
    }

    IEnumerator WindyEasterEggRoutine()
    {
        if (easterEggSound != null) easterEggSound.Play();
        if (easterEggPanel != null) easterEggPanel.SetActive(true);
        yield return new WaitForSecondsRealtime(4f);
        if (easterEggPanel != null) easterEggPanel.SetActive(false);
    }

    // ─── Game Over (скример) ──────────────────────────────────────────────────
    public void GameOver(GameObject jsModel) => GameOver(jsModel, -1);

    public void GameOver(GameObject jsModel, int animatronicID)
    {
        if (!alive) return;
        alive = false;

        // Если пауза открыта — закрываем её (восстанавливаем timeScale и убираем меню)
        if (_graphicsSettings != null && _graphicsSettings.IsPaused)
            _graphicsSettings.Resume();

        if (timePass != null) timePass.enabled = false;
        // Останавливаем Фон Гая и субтитры
        TimePass tp = FindFirstObjectByType<TimePass>();
        if (tp != null) tp.StopPhoneCall();

        if (jsModel != null) jsModel.SetActive(true);
        if (jumpscareObjects != null) jumpscareObjects.SetActive(true);

        // Полноэкранная пугающая картинка аниматроника
        ShowJumpscareImage(animatronicID);

        if (playerUI != null) playerUI.SetActive(false);
        if (playerCam != null) playerCam.enabled = false;

        AudioListener al = GetComponent<AudioListener>();
        if (al != null) al.enabled = false;

        if (jumpscareSound != null) jumpscareSound.Play();

        StartCoroutine(GameOverScreen());
    }

    // ─── Полноэкранная картинка скримера ──────────────────────────────────────
    void ShowJumpscareImage(int animatronicID)
    {
        if (jumpscareImageObject == null || jumpscareImage == null) return;
        if (jumpscareSprites == null || jumpscareSprites.Length == 0) return;

        int idx = (animatronicID >= 0 && animatronicID < jumpscareSprites.Length)
            ? animatronicID : 0;
        if (jumpscareSprites[idx] == null) return;

        jumpscareImage.sprite = jumpscareSprites[idx];
        jumpscareImageObject.SetActive(true);
        StartCoroutine(JumpscareImagePunch());
    }

    // Резкое появление: scale от 1.2 до 1.0 за 0.1 сек
    IEnumerator JumpscareImagePunch()
    {
        Transform t = jumpscareImageObject.transform;
        float dur = 0.1f, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(1.2f, 1.0f, elapsed / dur);
            t.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    // ─── Корутины ─────────────────────────────────────────────────────────────
    IEnumerator QuitToMenu()
    {
        yield return new WaitForSecondsRealtime(3f);
        if (holdingEsc)
        {
            if (loaderText != null) loaderText.SetText("Главное меню");
            FindFirstObjectByType<LoadingScreen>()?.LoadScene("MainMenu");
        }
    }

    IEnumerator GameOverScreen()
    {
        yield return new WaitForSecondsRealtime(0.35f);
        if (goObject != null) goObject.SetActive(true);
        gameObject.SetActive(false);
    }
}
