using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Управляет временем ночи.
/// 1 час = ~76 сек реального времени (как в оригинальном FNAF).
/// Ночь: 12 AM → 6 AM = 6 часов ≈ 7.5 минут.
/// </summary>
public class TimePass : MonoBehaviour
{
    [Header("Игрок")]
    [SerializeField] PlayerMovement player;

    [Header("Аудио ночи")]
    [SerializeField] AudioSource nightAmbience;
    [SerializeField] AudioSource nightEndJingle;
    [SerializeField] AudioSource ambiencePlayer;
    [SerializeField] AudioSource ambienceIntensePlayer;
    [SerializeField] AudioSource ambienceIntensePlayer2;

    [Header("Телефонный гай")]
    [SerializeField] AudioSource phoneRingSource;       // звук звонка телефона
    [SerializeField] AudioSource phoneCallPlayer;       // голос гая
    [SerializeField] AudioClip   phoneRingClip;         // клип звонка (можно NightBegin.wav)
    [SerializeField] AudioClip[] nightCalls;            // 7 клипов (ночи 1-7)
    [SerializeField] Subtitles[] subtitles;             // субтитры для каждой ночи
    [SerializeField] GameObject  nightCallMuteButton;
    [SerializeField] GameObject  helperText;

    [Header("Уровни ИИ")]
    [SerializeField] AiLevels[] aiLevels;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] TextMeshProUGUI nightText;
    [SerializeField] TextMeshProUGUI loaderText;
    [SerializeField] GameObject winUI;

    [Header("Диалоги Фон Гая (полный текст из лора)")]
    [Tooltip("Резервные тексты диалогов на случай отсутствия PhoneGuyDialogues.json. Индекс 0 = ночь 1.")]
    [TextArea(3, 12)]
    [SerializeField] string[] phoneGuyLines = new string[]
    {
        // Ночь 1
        "Алло! Алло, слышишь меня? Отлично, отлично. Артём говорит, можешь называть меня Фон Гай — это прозвище, долгая история, не важно. Слушай, поздравляю с первым рабочим днём, вернее, ночью! Добро пожаловать в «Весёлый Фреддо»!\n\nТак, давай сразу по делу, потому что смена уже идёт. Перед тобой — монитор с камерами. Их восемь штук, листаешь кнопками. Главное — следи за сценой, за кухней и за Пиратской пристанью, это такой закуток слева в конце коридора. По бокам у тебя две двери — левая и правая. У каждой двери есть кнопка света и кнопка закрытия. Закрытые двери жрут электричество, поэтому не держи их закрытыми просто так.\n\nТеперь про аниматроников. Их пятеро: Freddo, Bonita, ЧикаЛока и ФоксиРекс. Ну и... в общем, потом расскажу про пятого. Днём они нормальные, веселят детей, поют песни — всё такое. Ночью их переводят в режим свободного перемещения. Это, э-э, технически необходимо для... калибровки сервоприводов. Да. Так написано в инструкции.\n\nГлавное — не паникуй. Они тебя не трогают, если ты просто... сидишь и смотришь. Ладно?\n\nХорошей смены. Увидимся завтра!",
        // Ночь 2
        "Ты там? Хорошо, хорошо. Слушай, ты молодец, что вышел на вторую ночь. Некоторые, знаешь, после первой... в общем, не важно.\n\nЗначит, небольшое уточнение к вчерашнему. Я сказал, что аниматроники просто «ходят». Это верно, но... они ходят немного активнее, чем в первую ночь. Это нормально, просто их алгоритмы адаптируются. Ничего особенного.\n\nСледи особо за Bonita — она любит левый коридор. И за ЧикаЛокой, она, бывает, на кухне пропадает, потом раз — и уже в другом месте. Логику её перемещений я честно не очень понимаю.\n\nНо вот что важно — ФоксиРекс. Он стоит за занавеской на Пиратской пристани. Пока он за занавеской — всё нормально. Если занавеска открылась, а его там нет — закрывай правую дверь. Быстро. Просто поверь мне на слово, хорошо?\n\nКстати, ты заметил что-нибудь... странное на камерах? Если заметишь — просто переключи камеру. Не смотри долго.\n\nУдачи. Я серьёзно.",
        // Ночь 3
        "Привет. Это снова я. Слушай, я хочу тебе кое-что рассказать. Про это место. Думаю, ты имеешь право знать — хотя бы немного.\n\n«Весёлый Фреддо» открылся в восемьдесят седьмом. Нормальный такой ресторан был, дети любили, Freddo пел — всё хорошо. Основатель — Виктор Мозгов, инженер. Хороший, говорят, человек. Он вложил в аниматроников что-то особенное: систему, которая позволяла им... ну, почти чувствовать. Запоминать детей по именам.\n\nА потом был девяносто четвёртый год. Ноябрь. Официально — пожар. Все эвакуированы, ресторан закрывается на ремонт. Всё нормально, все живы.\n\nТолько вот Мозгов после этого пропал. Просто исчез. Ушёл за хлебом и не вернулся, как говорится.\n\nИ ещё одна деталь, которую я узнал уже потом: в ту ноябрьскую ночь в ресторане был кто-то ещё. Не Мозгов. Кто-то, кто пришёл посмотреть на его разработку. И что-то сделал с аниматрониками.\n\nС тех пор они... другие.\n\nЛадно. Следи за Freddo — он сегодня активнее обычного. И не открывай дверь, если слышишь стук. Они не стучат. Обычно.",
        // Ночь 4
        "Слушай, я буду говорить быстро. Значит, ты дожил до четвёртой ночи — это уже хорошо. Правда. Я хочу рассказать тебе про... про пятого.\n\nGLITCH. Его нет в официальных документах ресторана. Технически его не существует. Но если ты видел помехи на камерах — это он. Если свет в коридоре мигал без причины — это он. Если ты слышал детский смех, а никаких детей рядом нет — это определённо он.\n\nОн не аниматроник. У остальных есть тела, есть что-то, что можно остановить. GLITCH — это программа. Он в проводах. В камерах. В твоём мониторе прямо сейчас.\n\nЯ не знаю, что он хочет. Иногда он просто наблюдает. Иногда он... помогает другим добраться до тебя быстрее.\n\nЕсли экран начнёт глючить — не выключай монитор. Без него ты слепой, это хуже. Просто... не смотри ему в глаза.\n\nЯ нашёл старые бумаги Мозгова в подсобке. Там написано про «ДУША-87». Я всё ещё читаю. Когда дочитаю — расскажу.\n\nУдачи тебе. Честно.",
        // Ночь 5
        "Алло. Слушай... я дочитал бумаги Мозгова. И я... мне надо подумать, что тебе говорить. Просто следи за всеми сразу сегодня. Bonita ведёт себя странно. И ЧикаЛока тоже. Freddo пока на сцене, но... в общем. Ладно. Созвонимся.\n\n...\n\nЭто снова я. Слушай, мне очень жаль, что я не говорил тебе раньше. Я только сейчас понял, что... что нас обманули. Меня в том числе.\n\nОхранники здесь — мы не охранники. Мы подопытные. Вся эта система — это эксперимент. «ДУША-87» собирает данные со стресс-реакций живых людей. Чупрун получает эти данные и продаёт. Мы — расходный материал.\n\nЯ видел журнал. Сорок семь охранников до тебя. Сорок семь. Некоторые «уволились». Некоторые... в журнале просто стоит прочерк.\n\nЯ ухожу. Сегодня последняя моя ночь здесь. Я уже написал в полицию.\n\nТы... ты тоже уходи. После этой ночи — уходи и не возвращайся.\n\nИзвини.",
        // Ночь 6
        "Это автоматическое сообщение для сотрудника ночной охраны. Ваш инструктор временно недоступен. Приносим свои извинения за неудобство.\n\nНапоминаем: в соответствии с договором, вы обязаны оставаться на посту до шести ноль-ноль утра. Покидать помещение до окончания смены строго запрещено.\n\nАниматроники находятся в штатном режиме работы. Пожалуйста, следите за показаниями камер и используйте системы безопасности по инструкции.\n\nЕсли вы испытываете затруднения — обратитесь к старшему охраннику.\n\n...Артём не пришёл сегодня утром. Мы пытаемся с ним связаться. Пожалуйста, если вы с ним контактировали — сообщите в офис.\n\nУдачной смены.",
        // Ночь 7
        "...с-с-с-слы... слышишь...\n\nГенри. Меня зовут... меня звали Артём. Я здесь. Я не ушёл. Я не смог уйти.\n\nОни не убивают. Понимаешь? Они собирают. GLITCH... он собирает. Всё, что ты чувствуешь, всё, чего боишься — он забирает. Я теперь... я теперь часть этого.\n\n«Ты пришёл поиграть?»\n\nНе смотри на GLITCH. Не смотри ему... не... Генри, беги. Просто беги. Не жди шести. Беги—\n\n«Хорошей смены :)»",
    };

    // ─── Состояние ───────────────────────────────────────────────────────────
    public int nightNum;
    public bool phoneIsActive = false;   // TRUE пока Фон Гай говорит

    int gameTime = 0;
    int timeBonus = 0;
    const float HOUR_DURATION = 76f;

    static readonly string[] TIME_LABELS = { "12 AM", "1 AM", "2 AM", "3 AM", "4 AM", "5 AM", "6 AM" };

    void Awake()
    {
        nightNum = PlayerPrefs.GetInt("Night", 1);
        if (PlayerPrefs.GetString("CustomNight") == "active") nightNum = 7;
        nightNum = Mathf.Clamp(nightNum, 1, 7);

        if (nightText != null) nightText.SetText("Ночь " + nightNum);
        if (timeText  != null) timeText.SetText(TIME_LABELS[0]);

        if (nightNum == 1 && helperText != null) helperText.SetActive(true);

        if (PlayerPrefs.GetString("Drink") == "active")
        {
            PlayerPrefs.SetString("Drink", "not active");
            timeBonus = 6;
        }

        StartCoroutine(Count());
        StartCoroutine(PhoneCallSequence());
    }

    // ─── Уровень ИИ ──────────────────────────────────────────────────────────
    public int GetAILevel(int aiNum)
    {
        if (aiLevels == null || aiLevels.Length == 0) return 0;
        int idx = Mathf.Clamp(nightNum - 1, 0, aiLevels.Length - 1);
        if (aiLevels[idx] == null || aiLevels[idx].aiNum == null) return 0;
        if (aiNum >= aiLevels[idx].aiNum.Length) return 0;
        return aiLevels[idx].aiNum[aiNum];
    }

    // ─── Диалог ночи из лора ─────────────────────────────────────────────────
    /// <summary>
    /// Возвращает полный текст диалога Фон Гая для указанной ночи (1-7).
    /// Сначала пытается прочитать Resources/Lore/PhoneGuyDialogues.json,
    /// при неудаче — берёт текст из массива phoneGuyLines[] (вшитого в скрипт).
    /// </summary>
    public string GetNightDialogue(int night)
    {
        night = Mathf.Clamp(night, 1, 7);

        var data = LoadDialogueData();
        if (data != null && data.nights != null)
        {
            foreach (var entry in data.nights)
                if (entry != null && entry.night == night && !string.IsNullOrEmpty(entry.dialogue))
                    return entry.dialogue;
        }

        int idx = night - 1;
        if (phoneGuyLines != null && idx >= 0 && idx < phoneGuyLines.Length)
            return phoneGuyLines[idx];

        return string.Empty;
    }

    PhoneGuyDialogueData _dialogueCache;

    PhoneGuyDialogueData LoadDialogueData()
    {
        if (_dialogueCache != null) return _dialogueCache;

        var asset = Resources.Load<TextAsset>("Lore/PhoneGuyDialogues");
        if (asset == null || string.IsNullOrEmpty(asset.text)) return null;

        try
        {
            _dialogueCache = JsonUtility.FromJson<PhoneGuyDialogueData>(asset.text);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[TimePass] Не удалось разобрать PhoneGuyDialogues.json: " + e.Message);
            _dialogueCache = null;
        }
        return _dialogueCache;
    }

    // ─── Телефонный звонок (последовательность как в оригинале FNAF) ─────────
    IEnumerator PhoneCallSequence()
    {
        // Шаг 1: ждём 5 секунд
        yield return new WaitForSecondsRealtime(5f);

        bool isCustom = (PlayerPrefs.GetString("CustomNight") == "active");
        int callIdx   = nightNum - 1;
        bool hasCall  = !isCustom
                        && phoneCallPlayer != null
                        && nightCalls != null
                        && callIdx < nightCalls.Length
                        && nightCalls[callIdx] != null;

        if (!hasCall) yield break;

        // Шаг 2: звонит телефон (3 секунды)
        if (phoneRingSource != null && phoneRingClip != null)
        {
            phoneRingSource.clip   = phoneRingClip;
            phoneRingSource.volume = 0.7f;
            phoneRingSource.loop   = false;
            phoneRingSource.Play();
            yield return new WaitForSecondsRealtime(phoneRingClip.length > 0 ? Mathf.Min(phoneRingClip.length, 3f) : 3f);
            phoneRingSource.Stop();
        }
        else
        {
            // Нет клипа звонка — просто ждём 3 секунды
            yield return new WaitForSecondsRealtime(3f);
        }

        // Шаг 3: Фон Гай начинает говорить
        phoneCallPlayer.clip   = nightCalls[callIdx];
        phoneCallPlayer.volume = 0.85f;
        phoneCallPlayer.Play();
        phoneIsActive = true;

        // Уменьшаем фоновую музыку пока говорит гай
        if (ambiencePlayer != null) ambiencePlayer.volume = 0.1f;

        // Субтитры
        if (subtitles != null && callIdx < subtitles.Length && subtitles[callIdx] != null)
            subtitles[callIdx].StartSubtitles();

        if (nightCallMuteButton != null) nightCallMuteButton.SetActive(true);

        // Ждём окончания речи
        yield return new WaitForSecondsRealtime(phoneCallPlayer.clip.length);

        StopPhoneCall();
    }

    /// <summary>Остановить Фон Гая досрочно (вызывается кнопкой MUTE).</summary>
    public void StopPhoneCall()
    {
        phoneIsActive = false;

        if (phoneCallPlayer != null) phoneCallPlayer.Stop();
        if (phoneRingSource != null) phoneRingSource.Stop();

        // Возвращаем громкость амбиента
        if (ambiencePlayer != null) ambiencePlayer.volume = 0.5f;

        // Останавливаем субтитры
        if (subtitles != null)
            foreach (var s in subtitles)
                if (s != null) s.ResetSubtitles();

        if (nightCallMuteButton != null) nightCallMuteButton.SetActive(false);
    }

    // ─── Счётчик времени (правый верхний угол) ────────────────────────────────
    IEnumerator Count()
    {
        float hourDuration = HOUR_DURATION - timeBonus;
        yield return new WaitForSecondsRealtime(Mathf.Max(10f, hourDuration));

        gameTime = Mathf.Min(gameTime + 1, 6);

        if (timeText != null)
            timeText.SetText(TIME_LABELS[gameTime]);

        if (gameTime < 6)
            StartCoroutine(Count());
        else
            StartCoroutine(EndNight());
    }

    // ─── Конец ночи ───────────────────────────────────────────────────────────
    IEnumerator FadeAudio(AudioSource src, bool fadeIn, float targetVol = 0.5f)
    {
        if (src == null) yield break;
        float start = src.volume;
        float end   = fadeIn ? targetVol : 0f;
        if (fadeIn) src.volume = 0f;

        for (float t = 0; t < 1f; t += 0.02f)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            src.volume = Mathf.Lerp(start, end, t);
        }
        src.volume = end;
    }

    IEnumerator EndNight()
    {
        if (player == null || !player.alive) yield break;
        player.alive = false;

        StopPhoneCall();

        StartCoroutine(FadeAudio(ambiencePlayer,         false));
        StartCoroutine(FadeAudio(ambienceIntensePlayer,  false));
        StartCoroutine(FadeAudio(ambienceIntensePlayer2, false));

        if (nightEndJingle != null)
        {
            nightEndJingle.volume = 0f;
            nightEndJingle.Play();
            StartCoroutine(FadeAudio(nightEndJingle, true, 0.5f));
        }

        yield return new WaitForSecondsRealtime(4.6f);
        if (winUI != null) winUI.SetActive(true);

        switch (nightNum)
        {
            case 5: PlayerPrefs.SetString("beat5", "true"); break;
            case 6:
                PlayerPrefs.SetString("beat6", "true");
                PlayerPrefs.SetString("SeenPaycheck", "false");
                break;
            case 7:
                if (PlayerPrefs.GetInt("Freddo",0)==20 && PlayerPrefs.GetInt("Bonita",0)==20 &&
                    PlayerPrefs.GetInt("ChikaLoka",0)==20 && PlayerPrefs.GetInt("FoxyRex",0)==20 &&
                    PlayerPrefs.GetInt("GLITCH",0)==20)
                {
                    PlayerPrefs.SetString("beat7Ultimate", "true");
                    PlayerPrefs.SetString("SeenTrueEnding", "false");
                }
                break;
        }

        yield return new WaitForSecondsRealtime(6f);
        PlayerPrefs.SetString("CustomNight", "not active");

        var loader = FindFirstObjectByType<LoadingScreen>();
        if (loader == null) yield break;

        if (nightNum >= 5)
        {
            if (nightNum == 5) PlayerPrefs.SetInt("Night", 6);
            else if (nightNum == 6) PlayerPrefs.SetInt("Night", 7);
            loader.LoadScene("MainMenu");
        }
        else
        {
            PlayerPrefs.SetInt("Night", nightNum + 1);
            loader.LoadScene("ShopScene");
        }
    }
}

// ─── DTO для Resources/Lore/PhoneGuyDialogues.json ──────────────────────────
[System.Serializable]
public class PhoneGuyDialogueData
{
    public PhoneGuyNight[] nights;
    public PhoneGuyNewspaper[] newspapers;
}

[System.Serializable]
public class PhoneGuyNight
{
    public int night;
    public string dialogue;
    public float duration;
}

[System.Serializable]
public class PhoneGuyNewspaper
{
    public string transition;
    public string headline;
    public string article;
    public string date;
}
