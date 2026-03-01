using Newtonsoft.Json;

namespace DevARIManager.Core.Services;

public class AppSettings
{
    public bool IsDarkTheme { get; set; } = true;
    public string Language { get; set; } = "Turkce";
    public bool AutoStartServices { get; set; }
    public bool CheckUpdatesOnStartup { get; set; } = true;
    public string PreferredInstaller { get; set; } = "winget";
}

public interface ISettingsService
{
    AppSettings Current { get; }
    void Save();
    void Load();
}

public class SettingsService : ISettingsService
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DevARIManager");
    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

    public AppSettings Current { get; private set; } = new();

    public SettingsService()
    {
        Load();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonConvert.SerializeObject(Current, Formatting.Indented);
            File.WriteAllText(SettingsFile, json);
        }
        catch { /* silently fail on save error */ }
    }

    public void Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                Current = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            Current = new AppSettings();
        }
    }
}
