using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Core;
using BepInEx.Core.Console;
using BepInEx.Core.Logging;
using MonoMod.Utils;

namespace BepInEx.Preloader.Core.Logging;

public static class ChainloaderLogHelper
{
    private static Dictionary<string, string> MacOSVersions { get; } = new()
    {
        // https://en.wikipedia.org/wiki/Darwin_%28operating_system%29#Release_history
        ["16.0.0"] = "10.12",
        ["16.5.0"] = "10.12.4",
        ["16.6.0"] = "10.12.6",
        ["17.5.0"] = "10.13.4",
        ["17.6.0"] = "10.13.5",
        ["17.7.0"] = "10.13.6",
        ["18.2.0"] = "10.14.1",
        ["19.2.0"] = "10.15.2",
        ["19.3.0"] = "10.15.3",
        ["19.5.0"] = "10.15.5.1",
        ["20.1.0"] = "11.0",
        ["20.2.0"] = "11.1",
        ["20.3.0"] = "11.2",
        ["20.4.0"] = "11.3",
        ["20.5.0"] = "11.4",
        ["21.0.1"] = "12.0",
        ["21.1.0"] = "12.0.1",
        ["21.2.0"] = "12.1",
    };

    public static void PrintLogInfo(ManualLogSource log)
    {
        var bepinVersion = Paths.BepInExVersion;
        var versionMini = new SemanticVersioning.Version(bepinVersion.Major, bepinVersion.Minor, bepinVersion.Patch,
                                                         bepinVersion.PreRelease);
        var consoleTitle = $"BepInEx {versionMini} - {Paths.ProcessName}";
        log.Log(LogLevel.Message, consoleTitle);

        if (ConsoleManager.ConsoleActive)
            ConsoleManager.SetConsoleTitle(consoleTitle);

        if (!string.IsNullOrEmpty(bepinVersion.Build))
            log.Log(LogLevel.Message, $"Built from commit {bepinVersion.Build}");

        Logger.Log(LogLevel.Info, $"System platform: {GetPlatformString()}");
    }

    private static string GetPlatformString()
    {
        var builder = new StringBuilder();

        var osVersion = Environment.OSVersion.Version;

        // NOTE: this logic needs to be different for .NET 5.
        // We don't use it and I don't think we will for a long time (possibly ever), but upgrading will break Environment.OSVersion
        // https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/5.0/environment-osversion-returns-correct-version#change-description

        // Some additional notes
        // On .NET Framework and .NET Core platforms before 5, Environment.OSVersion does not work as you would expect.

        // On Windows, it returns at maximum 6.3 (Windows 8) if you don't specify that your application is specifically compatible with Windows 10, due to compatibility layer stuff.
        // So we have to call RtlGetVersion which bypasses that and gets the values for us. This is done in PlatformUtils

        // On macOS it returns the Darwin kernel version. I've included a mapping of most versions, but there's definitely some missing versions

        // Not sure what it does on Linux. I think it returns the kernel version there too, but we already get the utsname structure from SetPlatform() regardless

        if (PlatformDetection.OS.Is(OSKind.Windows))
        {
            osVersion = PlatformUtils.WindowsVersion;

            builder.Append("Windows ");

            // https://stackoverflow.com/a/2819962

            switch (osVersion.Major)
            {
                case >= 10 when osVersion.Build >= 22000:
                    builder.Append("11");
                    break;
                case >= 10:
                    builder.Append("10");
                    break;
                case 6 when osVersion.Minor == 3:
                    builder.Append("8.1");
                    break;
                case 6 when osVersion.Minor == 2:
                    builder.Append("8");
                    break;
                case 6 when osVersion.Minor == 1:
                    builder.Append("7");
                    break;
                case 6 when osVersion.Minor == 0:
                    builder.Append("Vista");
                    break;
                case <= 5:
                    builder.Append("XP");
                    break;
            }

            if (PlatformDetection.OS.Is(OSKind.Wine))
                builder.AppendFormat(" (Wine {0})", PlatformUtils.WineVersion);
        }
        else if (PlatformDetection.OS.Is(OSKind.OSX))
        {
            builder.Append("macOS ");


            var osxVersion = osVersion.ToString(3);

            if (MacOSVersions.TryGetValue(osxVersion, out var macOsVersion))
            {
                builder.Append(macOsVersion);
            }
            else
            {
                builder.AppendFormat("Unknown (kernel {0})", osVersion);
            }
        }
        else if (PlatformDetection.OS.Is(OSKind.Linux))
        {
            builder.Append("Linux");

            if (PlatformUtils.LinuxKernelVersion != null)
            {
                builder.AppendFormat(" (kernel {0})", PlatformUtils.LinuxKernelVersion);
            }
        }
        
        if (PlatformDetection.OS.Is(OSKind.Android))
            builder.Append(" Android");
        
        builder.Append(PlatformDetection.Architecture);

        return builder.ToString();
    }

    public static void RewritePreloaderLogs()
    {
        if (PreloaderConsoleListener.LogEvents == null || PreloaderConsoleListener.LogEvents.Count == 0)
            return;

        // Temporarily disable the console log listener (if there is one from preloader) as we replay the preloader logs
        var logListener = Logger.Listeners.FirstOrDefault(logger => logger is ConsoleLogListener);

        if (logListener != null)
            Logger.Listeners.Remove(logListener);

        foreach (var preloaderLogEvent in PreloaderConsoleListener.LogEvents)
            Logger.InternalLogEvent(PreloaderLogger.Log, preloaderLogEvent);

        if (logListener != null)
            Logger.Listeners.Add(logListener);
    }
}
