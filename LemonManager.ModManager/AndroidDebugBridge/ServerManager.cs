using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;

namespace LemonManager.ModManager.AndroidDebugBridge;

public static class ServerManager
{
    public static IPromptHandler PromptHandler;

    public static async Task Initialize(IPromptHandler promptHandler)
    {
        PromptHandler = promptHandler;
        Logger.Log("Initializing server manager");

        CommandExecuter.ExecutablePath = await GetADBExecutable();
        Logger.SetStatus("Starting ADB server");
        await CommandExecuter.SendCommandAsync("start-server");

        await DeviceManager.DetermineDevice();
    }

    public static Task<string> GetADBExecutable()
    {
        TaskCompletionSource<string> taskCompletionSource = new TaskCompletionSource<string>();
        string adbDirectory = Path.Combine(FilePaths.ApplicationDataPath, "platform-tools");
        string adbPath = Path.Combine(adbDirectory, "adb.exe");

        if (!Directory.Exists(adbDirectory) || !File.Exists(adbPath))
        {
            Logger.SetStatus("Downloading Android Debug Bridge");
            using (WebClient webClient = new WebClient())
            {
                string tempFile = Path.Combine(FilePaths.ApplicationDataPath, "platform-tools.zip");
                webClient.DownloadFileCompleted += (sender, args) =>
                {
                    Logger.SetStatus("Extracting ADB");
                    ZipFile.ExtractToDirectory(tempFile, Path.GetDirectoryName(tempFile));
                    File.Delete(tempFile);
                    taskCompletionSource.SetResult(adbPath);
                };
                webClient.DownloadFileAsync(new("https://dl.google.com/android/repository/platform-tools-latest-windows.zip"), tempFile);
            }
        }
        else taskCompletionSource.SetResult(adbPath);

        return taskCompletionSource.Task;
    }
}