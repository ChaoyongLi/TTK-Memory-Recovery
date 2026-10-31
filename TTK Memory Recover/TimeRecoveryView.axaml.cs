using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using TtkMemoryRecover.Core;

namespace TtkMemoryRecover;

public partial class TimeRecoveryView : UserControl
{
    private const string KeepOriginalText = "(keep original)";

    private string _imgPath = "";
    private byte[] _data = [];
    private List<TimeEntry> _entries = [];
    private List<DateTime> _depthTimes = [];
    private readonly List<ComboBox> _combos = [];

    public TimeRecoveryView()
    {
        InitializeComponent();
    }

    public void Load(string imgPath, byte[] data, List<TimeEntry> entries)
    {
        _imgPath = imgPath;
        _data = data;
        _entries = entries;
        _depthTimes = [];
        _combos.Clear();
        RowsHost.Children.Clear();

        foreach (TimeEntry entry in entries)
        {
            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("50,220,*"),
            };

            var index = new TextBlock { Text = $"#{entry.Index + 1}", VerticalAlignment = VerticalAlignment.Center };
            var time = new TextBlock
            {
                Text = entry.DisplayText,
                FontFamily = new FontFamily("Cascadia Mono, Consolas, monospace"),
                VerticalAlignment = VerticalAlignment.Center,
            };
            var combo = new ComboBox
            {
                Width = 260,
                PlaceholderText = "Select time from depth file",
                IsEnabled = false,
            };

            Grid.SetColumn(index, 0);
            Grid.SetColumn(time, 1);
            Grid.SetColumn(combo, 2);
            row.Children.Add(index);
            row.Children.Add(time);
            row.Children.Add(combo);
            RowsHost.Children.Add(row);
            _combos.Add(combo);
        }

        SetStatus($"{entries.Count} time entries found. Load a depth file to choose replacement times.", false);
    }

    private async void DepthButton_Click(object? sender, RoutedEventArgs e)
    {
        string? path = await FileDialogs.PickOpenAsync(this, "Select depth file", FileDialogs.TextFiles);
        if (path is null)
        {
            return;
        }

        try
        {
            _depthTimes = DepthParser.Parse(path);
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to read depth file: {ex.Message}", true);
            return;
        }

        var items = new List<string> { KeepOriginalText };
        items.AddRange(_depthTimes.Select(t => t.ToString(TimeEntry.TimeFormat)));
        foreach (ComboBox combo in _combos)
        {
            combo.ItemsSource = items;
            combo.SelectedIndex = -1; // nothing selected by default
            combo.IsEnabled = true;
        }

        DepthInfo.Text = $"{Path.GetFileName(path)}: {_depthTimes.Count} date/time entries";
        SetStatus(_depthTimes.Count == 0 ? "No valid date/time found in the depth file." : "Depth file loaded.",
            _depthTimes.Count == 0);
    }

    private async void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        var replacements = new List<(int StampOffset, DateTime Time)>();
        for (int i = 0; i < _combos.Count; i++)
        {
            int selected = _combos[i].SelectedIndex;
            if (selected >= 1 && selected - 1 < _depthTimes.Count)
            {
                replacements.Add((_entries[i].StampOffset, _depthTimes[selected - 1]));
            }
        }

        if (replacements.Count == 0)
        {
            SetStatus("No replacement time selected. Nothing to save.", true);
            return;
        }

        string? target = await FileDialogs.PickSaveAsync(this, "Save IMG as", FileDialogs.SuggestSaveName(_imgPath));
        if (target is null)
        {
            return;
        }

        if (string.Equals(Path.GetFullPath(target), Path.GetFullPath(_imgPath), StringComparison.OrdinalIgnoreCase))
        {
            SetStatus("Choose a different file name; the original IMG is never overwritten.", true);
            return;
        }

        try
        {
            File.WriteAllBytes(target, ImgWriter.ReplaceTimestamps(_data, replacements));
            SetStatus($"Saved {replacements.Count} replaced time(s) to {target}", false);
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to save: {ex.Message}", true);
        }
    }

    private void SetStatus(string text, bool isError)
    {
        StatusText.Text = text;
        StatusText.Foreground = isError ? Brushes.OrangeRed : Brushes.SeaGreen;
    }
}
