namespace TtkMemoryRecover.Core;

/// <summary>Produces modified copies of the image data; the input array is never changed.</summary>
public static class ImgWriter
{
    /// <summary>Overwrites only the 6 timestamp bytes at each given offset.</summary>
    public static byte[] ReplaceTimestamps(
        byte[] data, IEnumerable<(int StampOffset, DateTime Time)> replacements)
    {
        var copy = (byte[])data.Clone();
        foreach (var (offset, time) in replacements)
        {
            ImgParser.EncodeStamp(time).CopyTo(copy, offset);
        }

        return copy;
    }

    /// <summary>Removes the given [Start, End) ranges (overlapping ranges are merged).</summary>
    public static byte[] DeleteRanges(byte[] data, IEnumerable<(int Start, int End)> ranges)
    {
        var sorted = ranges.OrderBy(r => r.Start).ToList();
        var merged = new List<(int Start, int End)>();
        foreach (var r in sorted)
        {
            if (merged.Count > 0 && r.Start <= merged[^1].End)
            {
                merged[^1] = (merged[^1].Start, Math.Max(merged[^1].End, r.End));
            }
            else
            {
                merged.Add(r);
            }
        }

        using var output = new MemoryStream(data.Length);
        int pos = 0;
        foreach (var (start, end) in merged)
        {
            output.Write(data, pos, start - pos);
            pos = end;
        }

        output.Write(data, pos, data.Length - pos);
        return output.ToArray();
    }
}
