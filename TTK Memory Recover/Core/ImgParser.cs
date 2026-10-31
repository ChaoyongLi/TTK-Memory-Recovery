namespace TtkMemoryRecover.Core;

/// <summary>
/// One "FORMATION PRESSURE TEST!" marker found in the image together with the
/// timestamp stored in the 6 bytes that follow it.
/// </summary>
/// <param name="Index">Zero-based order of the marker in the file.</param>
/// <param name="MarkerOffset">Offset of the first marker byte.</param>
/// <param name="StampOffset">Offset of the first timestamp byte (marker end).</param>
/// <param name="Time">Decoded time, or null when the 6 bytes are not a valid date/time.</param>
public sealed record TimeEntry(int Index, int MarkerOffset, int StampOffset, DateTime? Time)
{
    public const string TimeFormat = "yyyy-MM-dd HH:mm:ss";

    /// <summary>Placeholder shown when the stored bytes cannot be converted to a date/time.</summary>
    public const string InvalidText = "YYmmddHHmmss";

    public string DisplayText => Time?.ToString(TimeFormat) ?? InvalidText;
}

public static class ImgParser
{
    /// <summary>ASCII "FORMATION PRESSURE TEST!".</summary>
    public static readonly byte[] Marker =
        Convert.FromHexString("464F524D4154494F4E205052455353555245205445535421");

    /// <summary>Timestamp layout: YY MM DD hh mm ss, one binary byte each.</summary>
    public const int StampLength = 6;

    public static List<TimeEntry> Parse(byte[] data)
    {
        var entries = new List<TimeEntry>();
        int pos = 0;
        while (pos <= data.Length - Marker.Length)
        {
            int rel = data.AsSpan(pos).IndexOf(Marker);
            if (rel < 0)
            {
                break;
            }

            int markerOffset = pos + rel;
            int stampOffset = markerOffset + Marker.Length;
            if (stampOffset + StampLength > data.Length)
            {
                break; // truncated: no complete timestamp follows
            }

            DateTime? time = DecodeStamp(data.AsSpan(stampOffset, StampLength));
            entries.Add(new TimeEntry(entries.Count, markerOffset, stampOffset, time));
            pos = stampOffset;
        }

        return entries;
    }

    /// <summary>Returns null when the bytes do not form a valid date/time.</summary>
    public static DateTime? DecodeStamp(ReadOnlySpan<byte> s)
    {
        int year = 2000 + s[0];
        int month = s[1], day = s[2], hour = s[3], minute = s[4], second = s[5];

        if (month is < 1 or > 12) return null;
        if (day < 1 || day > DateTime.DaysInMonth(year, month)) return null;
        if (hour > 23 || minute > 59 || second > 59) return null;

        return new DateTime(year, month, day, hour, minute, second);
    }

    /// <summary>Encodes a time as YY MM DD hh mm ss (year keeps only the last two digits).</summary>
    public static byte[] EncodeStamp(DateTime t) =>
    [
        (byte)(t.Year % 100), (byte)t.Month, (byte)t.Day,
        (byte)t.Hour, (byte)t.Minute, (byte)t.Second,
    ];
}
