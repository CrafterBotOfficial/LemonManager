using System.Diagnostics;
using System.Threading.Tasks;

namespace LemonManager.ModManager.AndroidDebugBridge;

internal static class CommandExecuter
{
    public static string ExecutablePath;

    public static string SendCommand(string command, int timeout = 6500) =>
        StartProcess(command, timeout);
     
    public static async Task<string> SendCommandAsync(string command) =>
        await StartProcessAsync(command);

    private static string StartProcess(string args, int timeout)
    {
        Process process = Process.Start(GetProcessStartInfo(args));
        process.EnableRaisingEvents = true;

        string output = string.Empty;

        process.OutputDataReceived += (sender, args) => output += args.Data + "\n";

        process.BeginOutputReadLine();
        
        if (!process.WaitForExit(timeout)) process.Kill();
        if (process.ExitCode != 0)
            Logger.Error($"Error running adb {process.ExitCode} {process.StandardError.ReadToEnd()}");

        return output.Trim();
    }
    private static async Task<string> StartProcessAsync(string args)
    {
        Process process = Process.Start(GetProcessStartInfo(args));
        process.EnableRaisingEvents = true;

        string output = string.Empty;
        process.OutputDataReceived += (sender, data) => output += data.Data + "\n";
        process.BeginOutputReadLine();

        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            Logger.Warning($"Process crashed {process.ExitCode} {args} {process.StandardError.ReadToEnd()}");
        }
        return output;
    }

    private static ProcessStartInfo GetProcessStartInfo(string args) =>
        new ProcessStartInfo(ExecutablePath, args)
        {
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Arguments = args,
            FileName = ExecutablePath
        };
}