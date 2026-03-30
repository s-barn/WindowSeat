using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using Microsoft.Win32;
using WindowSeat.Settings;

namespace WindowSeat.App.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;

    public SettingsWindow(SettingsService settingsService)
    {
        _settingsService = settingsService;
        InitializeComponent();
        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        var s = _settingsService.Current;

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"v{version?.Major}.{version?.Minor}.{version?.Build}";

        EnabledCheckBox.IsChecked  = s.IsEnabled;
        StartupCheckBox.IsChecked  = s.LaunchAtStartup;
        UpdatesCheckBox.IsChecked  = s.CheckForUpdates;

        CursorRadio.IsChecked  = s.DetectionMethod == DetectionMethod.CursorPosition;
        TaskbarRadio.IsChecked = s.DetectionMethod == DetectionMethod.LastActiveTaskbar;

        ExclusionsList.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<string>(
            s.ExcludedProcessNames);
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)   { /* live — saved on Save */ }
    private void StartupCheckBox_Changed(object sender, RoutedEventArgs e)   { /* live — saved on Save */ }
    private void UpdatesCheckBox_Changed(object sender, RoutedEventArgs e)   { /* live — saved on Save */ }
    private void DetectionMethod_Changed(object sender, RoutedEventArgs e)   { /* live — saved on Save */ }

    private void AddExclusion_Click(object sender, RoutedEventArgs e)
    {
        var name = ExclusionTextBox.Text.Trim();
        if (string.IsNullOrEmpty(name)) return;
        if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            name += ".exe";
        AddToList(name);
        ExclusionTextBox.Clear();
    }

    private void BrowseExclusion_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title  = "Select an application",
            Filter = "Executables (*.exe)|*.exe",
        };
        if (dialog.ShowDialog() == true)
            AddToList(System.IO.Path.GetFileName(dialog.FileName));
    }

    private void ExclusionTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) AddExclusion_Click(sender, e);
    }

    private void RemoveExclusion_Click(object sender, RoutedEventArgs e)
    {
        if (ExclusionsList.SelectedItem is string selected)
        {
            var list = (System.Collections.ObjectModel.ObservableCollection<string>)ExclusionsList.ItemsSource;
            list.Remove(selected);
        }
    }

    private void AddToList(string name)
    {
        var list = (System.Collections.ObjectModel.ObservableCollection<string>)ExclusionsList.ItemsSource;
        if (!list.Contains(name, StringComparer.OrdinalIgnoreCase))
            list.Add(name);
    }

    private void GitHubLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _settingsService.Update(s =>
        {
            s.IsEnabled       = EnabledCheckBox.IsChecked == true;
            s.LaunchAtStartup = StartupCheckBox.IsChecked == true;
            s.CheckForUpdates = UpdatesCheckBox.IsChecked == true;
            s.DetectionMethod = CursorRadio.IsChecked == true
                ? DetectionMethod.CursorPosition
                : DetectionMethod.LastActiveTaskbar;
            s.ExcludedProcessNames = ((System.Collections.ObjectModel.ObservableCollection<string>)
                ExclusionsList.ItemsSource).ToList();
        });

        StartupService.Sync(_settingsService.Current.LaunchAtStartup);
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
