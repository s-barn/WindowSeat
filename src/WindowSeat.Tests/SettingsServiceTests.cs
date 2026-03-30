using FluentAssertions;
using WindowSeat.Settings;
using Xunit;

namespace WindowSeat.Tests;

public sealed class SettingsServiceTests : IDisposable
{
    // Use a temp path so tests never touch the real %AppData%
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"WindowSeatTests_{Guid.NewGuid()}");

    public SettingsServiceTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Load_WhenNoFileExists_CreatesDefaultSettings()
    {
        var svc = BuildService();
        svc.Load();

        svc.Current.IsEnabled.Should().BeTrue();
        svc.Current.DetectionMethod.Should().Be(DetectionMethod.CursorPosition);
        svc.Current.LaunchAtStartup.Should().BeTrue();
        svc.Current.CheckForUpdates.Should().BeTrue();
        svc.Current.ExcludedProcessNames.Should().BeEmpty();
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsAllFields()
    {
        var svc = BuildService();
        svc.Load();

        svc.Update(s =>
        {
            s.IsEnabled       = false;
            s.DetectionMethod = DetectionMethod.LastActiveTaskbar;
            s.LaunchAtStartup = false;
            s.CheckForUpdates = false;
            s.ExcludedProcessNames.Add("Spotify.exe");
            s.ExcludedProcessNames.Add("Discord.exe");
        });

        // Fresh instance — simulates next app launch
        var svc2 = BuildService();
        svc2.Load();

        svc2.Current.IsEnabled.Should().BeFalse();
        svc2.Current.DetectionMethod.Should().Be(DetectionMethod.LastActiveTaskbar);
        svc2.Current.LaunchAtStartup.Should().BeFalse();
        svc2.Current.CheckForUpdates.Should().BeFalse();
        svc2.Current.ExcludedProcessNames.Should().BeEquivalentTo(new[] { "Spotify.exe", "Discord.exe" });
    }

    [Fact]
    public void Load_WithCorruptedJson_FallsBackToDefaults()
    {
        var settingsPath = Path.Combine(_tempDir, "settings.json");
        File.WriteAllText(settingsPath, "{ this is not valid json }}}");

        var svc = BuildService();
        svc.Load();

        svc.Current.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Update_PersistsChanges()
    {
        var svc = BuildService();
        svc.Load();
        svc.Update(s => s.IsEnabled = false);

        svc.Current.IsEnabled.Should().BeFalse();
        File.Exists(Path.Combine(_tempDir, "settings.json")).Should().BeTrue();
    }

    // Builds a SettingsService that writes to the temp dir instead of %AppData%
    private TestableSettingsService BuildService() => new(_tempDir);
}

/// <summary>
/// Subclass that redirects the settings path to a temp directory for testing.
/// </summary>
internal sealed class TestableSettingsService : SettingsService
{
    public TestableSettingsService(string directory) : base(directory) { }
}
