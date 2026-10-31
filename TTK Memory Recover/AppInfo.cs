using System.Reflection;

namespace TtkMemoryRecover;

public static class AppInfo
{
    /// <summary>Version from the csproj &lt;Version&gt; property (build metadata after '+' removed).</summary>
    public static string Version { get; } = ReadVersion();

    private static string ReadVersion()
    {
        string? v = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrEmpty(v))
        {
            return "0.0.0";
        }

        int plus = v.IndexOf('+');
        return plus >= 0 ? v[..plus] : v;
    }
}
