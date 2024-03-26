using Avalonia.Controls;
using DynamicData;
using LemonManager.Models;
using LemonManager.ModManager;
using LemonManager.ModManager.AndroidDebugBridge;
using LemonManager.ModManager.Models;
using LemonManager.Views;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace LemonManager.ViewModels;

public class GameControlsViewModel : ViewModelBase, INotifyPropertyChanged
{
    public ObservableCollection<GameControlButtonModel> Options { get; } = new ObservableCollection<GameControlButtonModel>();

    private UnityApplicationInfoModel moddedApplication => MainWindowViewModel.Instance.ApplicationManager.Info;
    public string Info => string.Format(ApplicationInfoTemplate, moddedApplication.Id, moddedApplication.Version, moddedApplication.UnityVersion, moddedApplication.Il2CppVersion);

    public GameControlsViewModel()
    {
        Options.AddRange(new GameControlButtonModel[]
        {
            new GameControlButtonModel("Logcat", Logcat),
            new GameControlButtonModel("Clear LemonCache", ClearLemonCache),
            new GameControlButtonModel("Clear MelonLoader Logs", ClearRemoteMelonLogs),
            new GameControlButtonModel("Stop Game", "Start Game", StartStopGame),
            new GameControlButtonModel("Download Game Data", DownloadGameData),
            new GameControlButtonModel("Download Header File", DownloadHeaderFiles),
            new GameControlButtonModel("Change Application", ChangeApplication),
        });
    }

    public void Logcat()
    {
        string adbPath = Path.Combine(FilePaths.ApplicationDataPath, "platform-tools", "adb.exe");

        Logger.Log("Starting logcat");
        Process.Start(new ProcessStartInfo(adbPath)
        {
            WorkingDirectory = Path.GetDirectoryName(adbPath),
            Arguments = "logcat -v time MelonLoader:D CRASH:D Mono:D mono:D mono-rt:D Zygote:D A64_HOOK:V DEBUG:D funchook:D Unity:D Binder:D AndroidRuntime:D *:S" // https://github.com/LemonLoader/MelonLoader/wiki/Logging#realtime-logging
        });
    }

    public void ClearLemonCache()
    {
        try
        {
            Directory.Delete(FilePaths.LemonCacheDirectory, true);
        }
        catch (Exception ex)
        {
            Logger.Error("Couldn't clear LemonCache " + ex);
        }
    }

    public async void ClearRemoteMelonLogs()
    {
        string remoteLogDirectory = moddedApplication.RemoteDataPath + "/melonloader/etc/logs/";
        if (!await PromptHandler.Instance.PromptUser("Are You Sure?", $"If you continue all MelonLoader log files located in the game's MelonLoader log directory will be deleted.", PromptType.Confirmation))
            return;

        string[] files = ModManager.AndroidDebugBridge.DeviceManager.GetFiles(remoteLogDirectory);
        foreach (string file in files)
        {
            if (!file.EndsWith(".log")) continue;
            Logger.Log($"Deleting {file}");

            await DeviceManager.SendShellCommandAsync("rm -f " + file);
        }

        await PromptHandler.Instance.PromptUser("Success", "Purged all files ending with .log in melon log directory.", PromptType.Notification);
    }

    public bool StartStopGame()
    {
        string packageName = MainWindowViewModel.Instance.ApplicationManager.Info.Id;
        bool isRunning = DeviceManager.SendShellCommand("pidof " + packageName) != string.Empty;
        bool isMetaQuest = DeviceManager.CurrentDevice.Model.StartsWith("Quest");

        Logger.Log($"{packageName} is current running? {isRunning}");

        if (isMetaQuest)
        {
            DeviceManager.SendShellCommand($"am broadcast -a com.oculus.vrpowermanager.{(isRunning ? "automation_disable" : "prox_close")}"); // I found this command in Sidequest's source code
        }
        DeviceManager.SendShellCommand(isRunning ? $"am force-stop {packageName}" : $"monkey -p {packageName} 1");

        return !isRunning;
    }

    public async void DownloadGameData()
    {
        var dialog = new OpenFolderDialog();
        var result = Path.GetFullPath((await dialog.ShowAsync(MainWindow.Instance)) ?? Directory.GetCurrentDirectory());

        await DeviceManager.Pull(string.Format(FilePaths.RemoteApplicationDataPath, moddedApplication.Id), Path.GetFullPath(result));
        Logger.Log("Pulled files to " + result);
        await PromptHandler.Instance.PromptUser("Success", $"Successfully downloaded {moddedApplication.Id}'s data.", PromptType.Notification);
    }

    public async void DownloadHeaderFiles()
    {
        try
        {
            var dialog = new OpenFolderDialog();
            string outputPath = Path.GetFullPath(await dialog.ShowAsync(MainWindow.Instance)) ?? null;
            if (outputPath is null) return;

            string url = $"https://raw.githubusercontent.com/nneonneo/Il2CppVersions/master/headers/{moddedApplication.UnityVersion}.h";
            Logger.Log("Header file url " + url);
            using WebClient webClient = new WebClient();
            await webClient.DownloadFileTaskAsync(url, Path.Combine(outputPath, "il2cpp.h"));
        }
        catch
        {
            await PromptHandler.Instance.PromptUser("Failed Download", "Couldn't download il2cpp header file for application " + moddedApplication.UnityVersion, PromptType.Notification);
        }
    }

    public async void ChangeApplication()
    {
        MainWindowViewModel.LoadingStatus = "Changing application";
        MainWindowViewModel.IsLoading = true;
        await MainWindowViewModel.Instance.SelectApplication(true);
        await MainWindowViewModel.Instance.PopulateLemons();
        this.RaisePropertyChanged(nameof(Info));
        MainWindowViewModel.IsLoading = false;
    }

    // TODO: add melonloader version
    public const string ApplicationInfoTemplate =
        "PackageName: {0}\n" +
        "Version: {1}\n" +
        "Unity Version: {2}\n" +
        "IL2Cpp Version: v{3}";
}