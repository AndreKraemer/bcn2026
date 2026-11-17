using System.Globalization;

namespace DontLetMeExpire.AI;

/// <summary>
/// Alle Prompts sind englisch (eine Quelle, Modelle befolgen englische Anweisungen am zuverlässigsten).
/// Die Antwortsprache richtet sich nach der Sprache der App und wird hier an einer Stelle festgelegt.
/// </summary>
public static class AiLanguage
{
    /// <summary>Sprache der App (Einstellungen oder System).</summary>
    public static CultureInfo Culture => CultureInfo.CurrentUICulture;

    /// <summary>Englischer Name der Sprache für den Prompt, z. B. "German" oder "English".</summary>
    public static string Name => CultureInfo.GetCultureInfo(Culture.TwoLetterISOLanguageName).EnglishName;

    /// <summary>Zweibuchstabiger Sprachcode, z. B. "de" oder "en".</summary>
    public static string Code => Culture.TwoLetterISOLanguageName;

    /// <summary>Anweisung, die an jeden Systemprompt angehängt wird.</summary>
    public static string ResponseInstruction =>
        $"Always respond in {Name}. Every text meant for the user (names, answers, hints, checks, recipes) must be in {Name}."
        + Code switch
        {
            "de" => " Address the user informally (\"du\").",
            _ => string.Empty,
        };

    /// <summary>"Today is Sunday, 2026-11-15." Wochentag auf Englisch, Datum eindeutig im ISO-Format.</summary>
    public static string Today => $"Today is {DateTime.Today.ToString("dddd, yyyy-MM-dd", CultureInfo.InvariantCulture)}.";
}
