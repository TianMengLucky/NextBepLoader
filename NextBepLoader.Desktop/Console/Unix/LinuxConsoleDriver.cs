using HarmonyLib;
using NextBepLoader.Core.Configuration;
using NextBepLoader.Core.LoaderInterface;
using NextBepLoader.Core.Logging;

namespace NextBepLoader.Deskstop.Console.Unix;

internal class LinuxConsoleDriver : IConsoleDivider
{
    /*private static readonly ConfigEntry<bool> ForceCustomTtyDriverConfig =
        ConfigFile.CoreConfig.Bind(
                                   "Logging.Console",
                                   "ForceBepInExTTYDriver",
                                   false,
                                   "If enabled, forces to use custom BepInEx TTY driver for handling terminal output on unix.");
                                   */

    static LinuxConsoleDriver()
    {
        UseMonoTtyDriver = false;

        /*if (ForceCustomTtyDriverConfig.Value)
            return;

        var consoleDriverType = typeof(System.Console).Assembly.GetType("System.ConsoleDriver");

        if (consoleDriverType != null)
            UseMonoTtyDriver = typeof(System.Console).Assembly.GetType("System.ParameterizedStrings") != null;*/
    }

    public static bool UseMonoTtyDriver { get; }

    public bool StdoutRedirected { get; private set; }

    public TtyInfo TtyInfo { get; private set; }

    public TextWriter? StandardOut { get; private set; }
    public TextWriter? ConsoleOut { get; private set; }

    public bool ConsoleActive { get; private set; }
    public bool ConsoleIsExternal => false;

    public void PreventClose()
    {
        // Not supported by all distros
    }

    public void Initialize(bool alreadyActive)
    {
        // Console is always considered active on Unix
        ConsoleActive = true;

        StdoutRedirected = UnixStreamHelper.isatty(1) != 1;

        var duplicateStream = UnixStreamHelper.CreateDuplicateStream(1);

        if (UseMonoTtyDriver && !StdoutRedirected)
        {
            // Mono implementation handles xterm for us

            var writer = ConsoleWriter.CreateConsoleStreamWriter(duplicateStream, System.Console.Out.Encoding, true);

            StandardOut = TextWriter.Synchronized(writer);

            var driver = AccessTools.Field(AccessTools.TypeByName("System.ConsoleDriver"), "driver").GetValue(null);
            AccessTools.Field(AccessTools.TypeByName("System.TermInfoDriver"), "stdout").SetValue(driver, writer);
        }
        else
        {
            // Handle TTY ourselves

            var writer = new StreamWriter(duplicateStream, System.Console.Out.Encoding);

            writer.AutoFlush = true;

            StandardOut = TextWriter.Synchronized(writer);

            TtyInfo = TtyHandler.GetTtyInfo();
        }

        ConsoleOut = StandardOut;
    }

    public void CreateConsole(uint codepage) =>
        Logger.Log(LogLevel.Warning, "An external console currently cannot be spawned on a Unix platform.");

    public void DetachConsole() =>
        throw new PlatformNotSupportedException("Cannot detach console on a Unix platform");

    public void SetConsoleColor(ConsoleColor color)
    {
        if (StdoutRedirected)
            return;

        if (UseMonoTtyDriver)
            // Use mono's inbuilt terminfo driver to set the foreground color for us
            SafeConsole.ForegroundColor = color;
        else
            ConsoleOut?.Write(TtyInfo.GetAnsiCode(color));
    }

    public void WriteConsoleLine(string message, ConsoleColor color)
    {
        throw new NotImplementedException();
    }

    public void SetConsoleTitle(string title)
    {
        if (StdoutRedirected)
            return;

        if (UseMonoTtyDriver && SafeConsole.TitleExists)
            SafeConsole.Title = title;
        else
            ConsoleOut?.Write($"\u001B]2;{title.Replace("\\", "\\\\")}\u0007");
    }
}
