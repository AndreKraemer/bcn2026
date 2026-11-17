using DontLetMeExpire.Resources.Strings;

namespace DontLetMeExpire.Services;

public class SettingsService : ISettingsService
{
  private readonly IPreferences _preferences;

  public SettingsService(IPreferences preferences)
  {
    _preferences = preferences;
  }

  // Liefert eine lokalisierte Liste der verfügbaren Sprachen
  public IEnumerable<KeyValuePair<string, string>> GetLanguages()
  {
    // lokalisierte Namen der Sprachen zu einer Liste hinzufügen.
    var languages = new List<KeyValuePair<string, string>>(4)
        {
            new("en", AppResources.English),
            new("de", AppResources.German)
        };

    // lokalisierte Liste alphabetisch sortieren
    languages.Sort((a, b) => string.Compare(a.Value, b.Value, StringComparison.Ordinal));

    // Standard-Eintrag an der ersten Position hinzufügen.
    languages.Insert(0, new KeyValuePair<string, string>(string.Empty, AppResources.Default));

    return languages;
  }

  // Liefert eine lokalisierte Liste der verfügbaren Themes
  public IEnumerable<KeyValuePair<string, string>> GetThemes()
  {

    // lokalisierte Theme-Namen zu einer Liste hinzufügen.
    var themes = new List<KeyValuePair<string, string>>(4)
        {
            new(nameof(AppTheme.Dark), AppResources.Dark),
            new(nameof(AppTheme.Light), AppResources.Light)
        };

    // lokalisierte Liste alphabetisch sortieren
    themes.Sort((a, b) => string.Compare(a.Value, b.Value, StringComparison.Ordinal));
    
    // Standard-Eintrag an der ersten Position hinzufügen.
    themes.Insert(0, new KeyValuePair<string, string>(string.Empty, AppResources.Default));

    return themes;
  }

  // Ausgewählte Sprache aus den Benutzereinstellungen abrufen
  public string GetLanguage()
  {
    return _preferences.Get(nameof(AppResources.Language), string.Empty);
  }

  // Ausgewähltes Theme aus den Benutzereinstellungen abrufen
  public string GetTheme()
  {
    return _preferences.Get(nameof(AppResources.Theme), string.Empty);
  }

  // Leiste mit den KI-Tool-Aufrufen auf der ItemPage anzeigen (Standard: an, für die Demo)
  public bool GetShowToolCalls()
  {
    return _preferences.Get(nameof(AppResources.ShowToolCalls), true);
  }

  public void SetShowToolCalls(bool show)
  {
    _preferences.Set(nameof(AppResources.ShowToolCalls), show);
  }

  // Lokale KI (Apple Intelligence) statt Cloud verwenden, wo das Gerät es kann
  public bool GetUseLocalAi()
  {
    return _preferences.Get(nameof(AppResources.UseLocalAi), false);
  }

  public void SetUseLocalAi(bool useLocal)
  {
    _preferences.Set(nameof(AppResources.UseLocalAi), useLocal);
  }

  // Live-Modell und/oder aufgenommene Antworten (Standard: live mit Aufnahme als Rückfall)
  public AI.ReplayMode GetReplayMode()
  {
    return (AI.ReplayMode)_preferences.Get(nameof(AppResources.ReplayMode), (int)AI.ReplayMode.Auto);
  }

  public void SetReplayMode(AI.ReplayMode mode)
  {
    _preferences.Set(nameof(AppResources.ReplayMode), (int)mode);
  }

  // Ausgewählte Sprache in den Benutzereinstellungen speichern
  public void SetLanguage(string language)
  {
    _preferences.Set(nameof(AppResources.Language), language);
  }

  // Ausgewähltes Theme in den Benutzereinstellungen speichern
  public void SetTheme(string theme)
  {
    _preferences.Set(nameof(AppResources.Theme), theme);

    // Theme nach dem Speichern direkt umstellen
    Application.Current!.UserAppTheme = theme switch
    {
      nameof(AppTheme.Dark) => AppTheme.Dark,
      nameof(AppTheme.Light) => AppTheme.Light,
      _ => Application.Current.UserAppTheme = AppTheme.Unspecified
    };
  }
}