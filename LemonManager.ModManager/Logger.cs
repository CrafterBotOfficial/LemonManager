using System.Diagnostics;

namespace LemonManager.ModManager;

internal static class Logger
{
    public static ILogger LoggerImplementation;

    public static void Log(object message)
    {
        Debug.WriteLine(message);
        LoggerImplementation?.Log(message);
    }

    public static void Warning(object message)
    {
        Debug.WriteLine(message);
        LoggerImplementation?.Warning(message);
    }

    public static void Error(object message)
    {
        Debug.WriteLine(message);
        LoggerImplementation?.Error(message);
    }

    public static void SetStatus(string status)
    {
        // TODO
        Log("[Status] " + status);
    }
}