using System.Globalization;

namespace TtkMemoryRecover.Core;

public static class DepthParser
{
    private const int HeaderLines = 2; // labels + units

    /// <summary>
    /// Reads the date/time columns of a depth txt file. The first two lines are
    /// headers; columns are separated by any run of tabs/spaces.
    /// Lines that cannot be parsed are skipped.
    /// </summary>
    public static List<DateTime> Parse(string path)
    {
        var result = new List<DateTime>();
        foreach (string line in File.ReadLines(path).Skip(HeaderLines))
        {
            string[] parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            if (DateTime.TryParseExact($"{parts[0]} {parts[1]}", "dd.MM.yyyy HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
            {
                result.Add(dt);
            }
        }

        return result;
    }
}
