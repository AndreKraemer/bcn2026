namespace DontLetMeExpire.Services;

public interface ISettingsService
{
  IEnumerable<KeyValuePair<string, string>> GetLanguages();

  IEnumerable<KeyValuePair<string, string>> GetThemes();

  string GetLanguage();

  string GetTheme();

  void SetLanguage(string language);

  void SetTheme(string theme);

  bool GetShowToolCalls();

  void SetShowToolCalls(bool show);

  bool GetUseLocalAi();

  void SetUseLocalAi(bool useLocal);

  AI.ReplayMode GetReplayMode();

  void SetReplayMode(AI.ReplayMode mode);
}