using System.Text.Json;
using System.Text.Json.Serialization;

namespace WindowSeat.Settings;

/// <summary>
/// Reads and writes AppSettings to %AppData%\WindowSeat\settings.json.
/// Thread-safe for concurrent reads; callers should serialise writes.
/// </summary>
public class SettingsService
{
    private readonly string _settingsDirectory;
    private readonly string _settingsPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented               = true,
        Converters                  = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    private AppSettings _current = new();
    private readonly object _lock = new();

    /// <summary>Production constructor — uses %AppData%\WindowSeat.</summary>
    public SettingsService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WindowSeat"))
    { }

    /// <summary>Testable constructor — redirects to any directory.</summary>
    public SettingsService(string settingsDirectory)
    {
        _settingsDirectory = settingsDirectory;
        _settingsPath      = Path.Combine(_settingsDirectory, "settings.json");
    }

    public AppSettings Current
    {
        get { lock (_lock) { return _current; } }
    }

    /// <summary>Loads settings from disk. If no file exists, defaults are used and persisted.</summary>
    public void Load()
    {
        lock (_lock)
        {
            if (!File.Exists(_settingsPath))
            {
                _current = new AppSettings();
                Save();
                return;
            }

            try
            {
                var json = File.ReadAllText(_settingsPath);
                _current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
            catch
            {
                // Corrupted file — fall back to defaults and overwrite
                _current = new AppSettings();
                Save();
            }
        }
    }

    /// <summary>Persists the current settings to disk.</summary>
    public void Save()
    {
        lock (_lock)
        {
            Directory.CreateDirectory(_settingsDirectory);
            var json = JsonSerializer.Serialize(_current, JsonOptions);
            File.WriteAllText(_settingsPath, json);
        }
    }

    /// <summary>Applies a mutation to the current settings and saves immediately.</summary>
    public void Update(Action<AppSettings> mutate)
    {
        lock (_lock)
        {
            mutate(_current);
            Save();
        }
    }
}
