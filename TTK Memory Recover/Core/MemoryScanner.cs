namespace TtkMemoryRecover.Core;

/// <summary>
/// A flag block (3+ consecutive 0xAA bytes) with an optional repeat block on each side
/// (either side may be empty).
/// Layout: [BeforeStart..LeftStart) context, [LeftStart..FlagStart) left repeat,
/// [FlagStart..FlagEnd) flag, [FlagEnd..RightEnd) right repeat, [RightEnd..AfterEnd) context.
/// </summary>
public sealed record MemoryMatch(
    int BeforeStart, int LeftStart, int FlagStart, int FlagEnd, int RightEnd, int AfterEnd);

public static class MemoryScanner
{
    public const byte FlagByte = 0xAA;
    public const int MinFlagLength = 3;
    public const int ContextLength = 10;

    /// <summary>A repeat block is a unit of 1..MaxUnitLength bytes repeated at least this many times.</summary>
    public const int MinRepeatCount = 2;
    public const int MaxUnitLength = 64;

    /// <summary>
    /// Builds the memory ranges to scan. Every selected time covers the data from just
    /// after its timestamp up to the start of the next time's marker (end of file for the
    /// last one); consecutive selected times therefore form one range that ends at the
    /// next unselected time.
    /// </summary>
    public static List<(int Start, int End)> BuildRanges(
        IReadOnlyList<TimeEntry> entries, IReadOnlyList<bool> selected, int dataLength)
    {
        var ranges = new List<(int Start, int End)>();
        for (int i = 0; i < entries.Count; i++)
        {
            if (!selected[i])
            {
                continue;
            }

            int start = entries[i].StampOffset + ImgParser.StampLength;
            int end = i + 1 < entries.Count ? entries[i + 1].MarkerOffset : dataLength;

            if (i > 0 && selected[i - 1])
            {
                ranges[^1] = (ranges[^1].Start, end);
            }
            else
            {
                ranges.Add((start, end));
            }
        }

        return ranges;
    }

    /// <summary>
    /// Finds every flag block inside [start, end). Directly before and after each flag block
    /// the longest repeat block (a 1..<see cref="MaxUnitLength"/> byte unit repeated at least
    /// <see cref="MinRepeatCount"/> times) is attached; a side without one is empty.
    /// </summary>
    public static List<MemoryMatch> Scan(byte[] data, int start, int end)
    {
        var matches = new List<MemoryMatch>();
        end = Math.Min(end, data.Length);

        int i = start;
        while (i < end)
        {
            if (data[i] != FlagByte)
            {
                i++;
                continue;
            }

            int flagStart = i;
            while (i < end && data[i] == FlagByte)
            {
                i++;
            }

            int flagEnd = i;
            if (flagEnd - flagStart < MinFlagLength)
            {
                continue;
            }

            int leftStart = flagStart - LeftRepeatLength(data, start, flagStart);
            int rightEnd = flagEnd + RightRepeatLength(data, flagEnd, end);

            matches.Add(new MemoryMatch(
                Math.Max(0, leftStart - ContextLength), leftStart, flagStart, flagEnd, rightEnd,
                Math.Min(data.Length, rightEnd + ContextLength)));
        }

        return matches;
    }

    /// <summary>
    /// Length of the longest repeat block that starts at <paramref name="pos"/> and ends at or
    /// before <paramref name="end"/>; 0 when nothing repeats. Ties favour the shorter unit.
    /// </summary>
    private static int RightRepeatLength(byte[] data, int pos, int end)
    {
        int best = 0;
        for (int k = 1; k <= MaxUnitLength && pos + k * MinRepeatCount <= end; k++)
        {
            var unit = data.AsSpan(pos, k);
            int copies = 1;
            while (pos + (copies + 1) * k <= end && data.AsSpan(pos + copies * k, k).SequenceEqual(unit))
            {
                copies++;
            }

            if (copies >= MinRepeatCount && copies * k > best)
            {
                best = copies * k;
            }
        }

        return best;
    }

    /// <summary>
    /// Length of the longest repeat block that ends at <paramref name="pos"/> and starts at or
    /// after <paramref name="start"/>; 0 when nothing repeats. Ties favour the shorter unit.
    /// </summary>
    private static int LeftRepeatLength(byte[] data, int start, int pos)
    {
        int best = 0;
        for (int k = 1; k <= MaxUnitLength && pos - k * MinRepeatCount >= start; k++)
        {
            var unit = data.AsSpan(pos - k, k);
            int copies = 1;
            while (pos - (copies + 1) * k >= start && data.AsSpan(pos - (copies + 1) * k, k).SequenceEqual(unit))
            {
                copies++;
            }

            if (copies >= MinRepeatCount && copies * k > best)
            {
                best = copies * k;
            }
        }

        return best;
    }
}
