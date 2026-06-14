using UnityEngine;

/// <summary>
/// Система локализации интерфейса и диалогов Фон Гая.
/// Поддерживаемые языки: RU (по умолчанию), EN, DE, FR.
///
/// Язык хранится в PlayerPrefs("Language"): 0=RU, 1=EN, 2=DE, 3=FR.
/// Для русского GetPhoneGuyLine() возвращает null — TimePass берёт
/// текст из своего вшитого русского массива phoneGuyLines[].
/// </summary>
public class LanguageManager : MonoBehaviour
{
    public enum Language { RU = 0, EN = 1, DE = 2, FR = 3 }

    public static LanguageManager Instance { get; private set; }

    [SerializeField] Language currentLanguage = Language.RU;

    // Локализованные строки Фон Гая. Индекс 0 = ночь 1.
    // RU намеренно пуст: для русского используется вшитый текст в TimePass.
    static readonly string[] EN =
    {
        "Hello? Hello, can you hear me? Great. This is your first night at \"Freddo's\". Keep an eye on the cameras, watch the doors, and don't waste power. The animatronics roam at night — just sit still and watch. Good luck.",
        "You're back, good. They move a bit more tonight, that's normal. Watch Bonita on the left and FoxyRex behind the curtain — if the curtain's open and he's gone, shut the right door. Fast.",
        "It's me again. This place opened in '87. There was a fire in '94 — officially. After that the founder, Mozgov, just vanished. Something happened to the animatronics that night. They're different now. Watch Freddo tonight.",
        "Listen fast. There's a fifth one. GLITCH. He isn't in any document. The static on the cameras, the flickering lights, the child's laughter — that's him. He lives in the wires. Don't look him in the eyes.",
        "I read Mozgov's papers. We're not guards, we're test subjects. \"SOUL-87\" harvests stress data from living people. Forty-seven guards before you. I'm leaving tonight. You should too — after this night, leave and don't come back.",
        "This is an automated message for the night security employee. Your instructor is temporarily unavailable. Per your contract you must remain on post until 6 AM. The animatronics are operating normally. Have a good shift.",
        "...c-can you... hear me. My name was Artyom. I'm still here. They don't kill. They collect. GLITCH collects everything you fear. Don't look at GLITCH. Run. Don't wait for six. Run— \"Have a good shift :)\"",
    };

    static readonly string[] DE =
    {
        "Hallo? Hallo, hörst du mich? Gut. Das ist deine erste Nacht bei \"Freddo's\". Behalte die Kameras im Auge, achte auf die Türen und verschwende keinen Strom. Nachts laufen die Animatronics frei herum — sitz einfach still und beobachte. Viel Glück.",
        "Du bist zurück, gut. Heute bewegen sie sich etwas mehr, das ist normal. Achte auf Bonita links und auf FoxyRex hinter dem Vorhang — ist der Vorhang offen und er weg, schließ sofort die rechte Tür.",
        "Ich bin's wieder. Dieser Ort öffnete '87. '94 gab es ein Feuer — offiziell. Danach verschwand der Gründer Mozgow einfach. In jener Nacht geschah etwas mit den Animatronics. Sie sind jetzt anders. Pass heute auf Freddo auf.",
        "Hör schnell zu. Es gibt einen Fünften. GLITCH. Er steht in keinem Dokument. Das Rauschen auf den Kameras, das flackernde Licht, das Kinderlachen — das ist er. Er lebt in den Kabeln. Sieh ihm nicht in die Augen.",
        "Ich habe Mozgows Papiere gelesen. Wir sind keine Wächter, wir sind Versuchskaninchen. \"SEELE-87\" sammelt Stressdaten von lebenden Menschen. Siebenundvierzig Wächter vor dir. Ich gehe heute. Geh auch — nach dieser Nacht, geh und komm nicht zurück.",
        "Dies ist eine automatische Nachricht für den Nachtwächter. Ihr Ausbilder ist vorübergehend nicht erreichbar. Laut Vertrag müssen Sie bis 6 Uhr auf Posten bleiben. Die Animatronics arbeiten normal. Eine gute Schicht.",
        "...h-hörst du... mich. Mein Name war Artjom. Ich bin noch hier. Sie töten nicht. Sie sammeln. GLITCH sammelt alles, wovor du dich fürchtest. Sieh GLITCH nicht an. Lauf. Warte nicht auf sechs. Lauf— \"Eine gute Schicht :)\"",
    };

    static readonly string[] FR =
    {
        "Allô ? Allô, tu m'entends ? Parfait. C'est ta première nuit chez \"Freddo's\". Surveille les caméras, garde un œil sur les portes et ne gaspille pas l'énergie. La nuit, les animatroniques se déplacent — reste assis et observe. Bonne chance.",
        "Te revoilà, bien. Ce soir ils bougent un peu plus, c'est normal. Surveille Bonita à gauche et FoxyRex derrière le rideau — si le rideau est ouvert et qu'il a disparu, ferme la porte droite. Vite.",
        "C'est encore moi. Cet endroit a ouvert en 87. Il y a eu un incendie en 94 — officiellement. Ensuite le fondateur, Mozgov, a disparu. Quelque chose est arrivé aux animatroniques cette nuit-là. Ils sont différents maintenant. Surveille Freddo ce soir.",
        "Écoute vite. Il y en a un cinquième. GLITCH. Il n'est dans aucun document. Les parasites sur les caméras, les lumières qui clignotent, le rire d'enfant — c'est lui. Il vit dans les câbles. Ne le regarde pas dans les yeux.",
        "J'ai lu les papiers de Mozgov. Nous ne sommes pas des gardiens, nous sommes des cobayes. \"ÂME-87\" récolte les données de stress des vivants. Quarante-sept gardiens avant toi. Je pars ce soir. Pars aussi — après cette nuit, va-t'en et ne reviens pas.",
        "Ceci est un message automatique pour l'agent de sécurité de nuit. Votre instructeur est temporairement indisponible. Selon votre contrat, vous devez rester à votre poste jusqu'à 6 h. Les animatroniques fonctionnent normalement. Bonne garde.",
        "...t-tu... m'entends. Je m'appelais Artyom. Je suis toujours là. Ils ne tuent pas. Ils collectent. GLITCH collecte tout ce que tu crains. Ne regarde pas GLITCH. Cours. N'attends pas six heures. Cours— \"Bonne garde :)\"",
    };

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        currentLanguage = (Language)PlayerPrefs.GetInt("Language", (int)currentLanguage);
    }

    /// <summary>Текущий язык.</summary>
    public Language CurrentLanguage => currentLanguage;

    /// <summary>Сменить язык (0=RU,1=EN,2=DE,3=FR) и сохранить выбор.</summary>
    public void SetLanguage(int languageIndex)
    {
        languageIndex = Mathf.Clamp(languageIndex, 0, 3);
        currentLanguage = (Language)languageIndex;
        PlayerPrefs.SetInt("Language", languageIndex);
    }

    /// <summary>
    /// Возвращает локализованную строку диалога Фон Гая для ночи (nightIndex 0-based).
    /// Для русского языка возвращает null — вызывающий код использует вшитый русский текст.
    /// </summary>
    public string GetPhoneGuyLine(int nightIndex)
    {
        string[] table = currentLanguage switch
        {
            Language.EN => EN,
            Language.DE => DE,
            Language.FR => FR,
            _ => null,
        };

        if (table == null) return null;
        if (nightIndex < 0 || nightIndex >= table.Length) return null;
        return table[nightIndex];
    }
}
