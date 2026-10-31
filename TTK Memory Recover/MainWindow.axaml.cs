using Avalonia.Controls;
using Avalonia.Interactivity;
using TtkMemoryRecover.Core;

namespace TtkMemoryRecover;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        using (Stream? icon = typeof(MainWindow).Assembly.GetManifestResourceStream("app.ico"))
        {
            if (icon is not null)
            {
                Icon = new WindowIcon(icon);
            }
        }

        Title = $"TTK Memory Recover v{AppInfo.Version}";
        StartVersion.Text = $"Version {AppInfo.Version}";
        HeaderVersion.Text = $"v{AppInfo.Version}";
    }

    private async void OpenButton_Click(object? sender, RoutedEventArgs e)
    {
        string? path = await FileDialogs.PickOpenAsync(this, "Select IMG file", FileDialogs.ImgFiles);
        if (path is null)
        {
            return;
        }

        try
        {
            byte[] data = await File.ReadAllBytesAsync(path);
            List<TimeEntry> entries = ImgParser.Parse(data);
            if (entries.Count == 0)
            {
                StartStatus.Text = "No \"FORMATION PRESSURE TEST!\" marker found in this file.";
                return;
            }

            FileInfoText.Text = $"{path}  ({data.Length:N0} bytes, {entries.Count} time entries)";
            TimeView.Load(path, data, entries);
            MemoryView.Load(path, data, entries);
            StartPanel.IsVisible = false;
            MainPanel.IsVisible = true;
        }
        catch (Exception ex)
        {
            StartStatus.Text = $"Failed to open file: {ex.Message}";
        }
    }
}
