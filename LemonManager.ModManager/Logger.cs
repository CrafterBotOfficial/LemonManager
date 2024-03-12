using System.Diagnostics;
using System.IO;

namespace LemonManager.ModManager;

public static class Logger
{
    public static string LogPath => Path.Combine(FilePaths.ApplicationDataPath, "Log.txt");
    private static StreamWriter streamWriter;

    static Logger()
    {
        string prevFilePath = Path.Combine(FilePaths.ApplicationDataPath, "Log-prev.txt");

        if (File.Exists(prevFilePath)) File.Delete(prevFilePath);
        if (File.Exists(LogPath)) File.Move(LogPath, prevFilePath);
        streamWriter = new(File.Create(LogPath));
    }

    public static void Log(object message)
    {
        WriteLine("[Info] " + message);
    }

    public static void Warning(object message)
    {
        WriteLine("[Warning] " + message);
    }

    public static void Error(object message)
    {
        WriteLine("[Error] " + message);
    }

    public static void SetStatus(string status)
    {
        Log("[Status] " + status);
    }


    private static void WriteLine(object line)
    {
        Debug.WriteLine(line);
        streamWriter.WriteLine(line);
        streamWriter.Flush();
    }
}