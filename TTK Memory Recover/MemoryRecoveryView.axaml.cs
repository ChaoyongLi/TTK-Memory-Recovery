using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using TtkMemoryRecover.Core;

namespace TtkMemoryRecover;

public partial class MemoryRecoveryView : UserControl
{
    private static readonly IBrush RepeatBrush = new SolidColorBrush(Color.Parse("#E5E7EB"));
    private static readonly IBrush RepeatText = new SolidColorBrush(Color.Parse("#374151"));
    private static readonly IBrush FlagBrush = new SolidColorBrush(Color.Parse("#FEE2E2"));
    private static readonly IBrush FlagText = new SolidColorBrush(Color.Parse("#B91C1C"));
    private static readonly FontFamily MonoFont = new("Cascadia Mono, Consolas, monospace");

    private string _imgPath = "";
    private byte[] _data = [];
    private List<TimeEntry> _entries = [];
    private readonly List<CheckBox> _checks = [];
    private List<MemoryMatch> _matches = [];

    /// <summary>Flag offsets of matches whose Apply button is on (kept across re-scans).</summary>
    private readonly HashSet<int> _applied = [];

    public MemoryRecoveryView()
    {
        InitializeComponent();
    }

    public void Load(string imgPath, byte[] data, List<TimeEntry> entries)
    {
        _imgPath = imgPath;
        _data = data;
        _entries = entries;
        _applied.Clear();
        _checks.Clear();
        TimesHost.Children.Clear();

        foreach (TimeEntry entry in entries)
        {
            var check = new CheckBox { Content = $"#{entry.Index + 1}   {entry.DisplayText}" };
            check.IsCheckedChanged += (_, _) => Rescan();
            _checks.Add(check);
            TimesHost.Children.Add(check);
        }

        Rescan();
    }

    private void Rescan()
    {
        var selected = _checks.Select(c => c.IsChecked == true).ToList();
        var ranges = MemoryScanner.BuildRanges(_entries, selected, _data.Length);

        _matches = [];
        foreach (var (start, end) in ranges)
        {
            _matches.AddRange(MemoryScanner.Scan(_data, start, end));
        }

        MatchesHost.Children.Clear();
        foreach (MemoryMatch match in _matches)
        {
            MatchesHost.Children.Add(BuildRow(match));
        }

        MatchesHeader.Text = $"Matches ({_matches.Count})";
        if (ranges.Count == 0)
        {
            SetStatus("Select one or more times to scan.", false);
        }
        else
        {
            SetStatus(_matches.Count == 0 ? "No matches in the selected memory." : $"{_matches.Count} match(es) found.", false);
        }

        UpdateSaveEnabled();
    }

    private Control BuildRow(MemoryMatch m)
    {
        var text = new TextBlock
        {
            FontFamily = MonoFont,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var before = new Run(Hex(m.BeforeStart, m.LeftStart));
        var left = new Run(Hex(m.LeftStart, m.FlagStart)) { Background = RepeatBrush, Foreground = RepeatText };
        var flag = new Run(Hex(m.FlagStart, m.FlagEnd)) { Background = FlagBrush, Foreground = FlagText };
        var right = new Run(Hex(m.FlagEnd, m.RightEnd)) { Background = RepeatBrush, Foreground = RepeatText };
        var after = new Run(Hex(m.RightEnd, m.AfterEnd));

        // Empty segments (no repeat block on a side, no context at the file edge) are skipped.
        var inlines = new InlineCollection();
        foreach (Run run in new[] { before, left, flag, right, after })
        {
            if (string.IsNullOrEmpty(run.Text))
            {
                continue;
            }

            if (inlines.Count > 0)
            {
                inlines.Add(new Run(" "));
            }

            inlines.Add(run);
        }

        text.Inlines = inlines;

        var offset = new TextBlock
        {
            Text = $"@0x{m.FlagStart:X}",
            FontFamily = MonoFont,
            Width = 100,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var apply = new Button { Width = 80, HorizontalContentAlignment = HorizontalAlignment.Center };

        // Only the flag block and the repeat block after it are deleted.
        void Refresh()
        {
            bool on = _applied.Contains(m.FlagStart);
            apply.Content = on ? "Undo" : "Apply";
            apply.Classes.Set("primary", !on);
            apply.Classes.Set("secondary", on);
            TextDecorationCollection? deco = on ? TextDecorations.Strikethrough : null;
            flag.TextDecorations = deco;
            right.TextDecorations = deco;
        }

        apply.Click += (_, _) =>
        {
            if (!_applied.Add(m.FlagStart))
            {
                _applied.Remove(m.FlagStart);
            }

            Refresh();
            UpdateSaveEnabled();
        };
        Refresh();

        var row = new Border
        {
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#E5E7EB")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8),
        };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        Grid.SetColumn(offset, 0);
        Grid.SetColumn(text, 1);
        Grid.SetColumn(apply, 2);
        grid.Children.Add(offset);
        grid.Children.Add(text);
        grid.Children.Add(apply);
        row.Child = grid;
        return row;
    }

    /// <summary>Every byte of [start, end) as space-separated uppercase hex.</summary>
    private string Hex(int start, int end)
    {
        if (end <= start)
        {
            return "";
        }

        var sb = new StringBuilder((end - start) * 3);
        for (int i = start; i < end; i++)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            sb.Append(_data[i].ToString("X2"));
        }

        return sb.ToString();
    }

    private void UpdateSaveEnabled() =>
        SaveButton.IsEnabled = _matches.Any(m => _applied.Contains(m.FlagStart));

    private async void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        var ranges = _matches
            .Where(m => _applied.Contains(m.FlagStart))
            .Select(m => (m.FlagStart, m.RightEnd))
            .ToList();
        if (ranges.Count == 0)
        {
            SetStatus("No applied match. Nothing to save.", true);
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
            byte[] result = ImgWriter.DeleteRanges(_data, ranges);
            File.WriteAllBytes(target, result);
            SetStatus($"Removed {_data.Length - result.Length} byte(s) from {ranges.Count} match(es). Saved to {target}", false);
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
