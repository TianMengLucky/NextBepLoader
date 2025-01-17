using System;
using NextBepLoader.Core.Logging;
using NextBepLoader.Core.Logging.Interface;
using UnityEngine;

namespace NextBepLoader.Core.IL2CPP.Logging;

public class IL2CPPUnityLogSource : ILogSource
{
    public IL2CPPUnityLogSource()
    {
        Application.s_LogCallbackHandler = new Action<string, string, LogType>(UnityLogCallback);

        Il2CppInterop.Runtime.IL2CPP
                     .ResolveICall<
                         SetLogCallbackDefinedDelegate>("UnityEngine.Application::SetLogCallbackDefined")(true);
    }

    public string SourceName { get; } = "Unity";

    public event EventHandler<LogEventArgs> LogEvent;

    public void Dispose() { }

    public void UnityLogCallback(string logLine, string exception, LogType type)
    {
        var level = type switch
        {
            LogType.Error     => LogLevel.Error,
            LogType.Assert    => LogLevel.Debug,
            LogType.Warning   => LogLevel.Warning,
            LogType.Log       => LogLevel.Message,
            LogType.Exception => LogLevel.Error,
            _                 => LogLevel.Message
        };
        LogEvent(this, new LogEventArgs(logLine, level, this));
    }

    private delegate IntPtr SetLogCallbackDefinedDelegate(bool defined);
}
