using System.Text.Json;

namespace CopyDecrypt;

internal sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _path;

    internal SettingsStore()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CopyDecrypt");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "settings.json");
    }

    internal AppSettings Load()
    {
        if (!File.Exists(_path))
        {
            var fresh = new AppSettings();
            fresh.Sanitize();
            return fresh;
        }

        try
        {
            var json = File.ReadAllText(_path);
            var s = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            s.Sanitize();
            return s;
        }
        catch
        {
            var fallback = new AppSettings();
            fallback.Sanitize();
            return fallback;
        }
    }

    internal void Save(AppSettings settings)
    {
        settings.Sanitize();
        File.WriteAllText(_path, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
