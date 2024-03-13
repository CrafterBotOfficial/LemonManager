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
using System.IO.Compression;
using System.Net;

namespace LemonManager.ViewModels;

public class GameControlsViewModel : ViewModelBase, INotifyPropertyChanged
{
    public ObservableCollection<GameControlButtonModel> Options { get; } = new ObservableCollection<GameControlButtonModel>();

    private ModdedApplicationModel moddedApplication => MainWindowViewModel.Instance.ApplicationManager.Info;
    public string Info => string.Format(ApplicationInfoTemplate, moddedApplication.Id, moddedApplication.Version, moddedApplication.UnityVersion);

    public GameControlsViewModel()
    {
        Options.AddRange(new GameControlButtonModel[]
        {
            new GameControlButtonModel("MelonLog", MelonLog),
            new GameControlButtonModel("Clear LemonCache", ClearLemonCache),
            new GameControlButtonModel("Stop Game", "Start Game", StartStopGame),
            new GameControlButtonModel("Download Game Data", DownloadGameData),
            new GameControlButtonModel("Change Application", ChangeApplication)
        });
    }

    private async void MelonLog()
    {
        string adbDirectory = Path.Combine(FilePaths.ApplicationDataPath, "platform-tools");
        string melonLogExecutable = Path.Combine(adbDirectory, "MelonLog.exe");
        if (!File.Exists(melonLogExecutable))
        {
            if (!await PromptHandler.Instance.PromptUser("Download MelonLog?", "Are you sure you want to download MelonLog, it is by a third party and may contain malware.", PromptType.Confirmation))
                return;
            MainWindowViewModel.IsLoading = true;
            MainWindowViewModel.LoadingStatus = "Downloading MelonLog";

            string tempPath = Path.Combine(FilePaths.ApplicationDataPath, "MelonLog.zip");
            using WebClient webClient = new WebClient();
            await webClient.DownloadFileTaskAsync("https://github.com/Lewko6702/MelonLog/releases/download/1.0.1/MelonLog.zip", tempPath);

            MainWindowViewModel.LoadingStatus = "Extracting MelonLog";
            ZipFile.ExtractToDirectory(tempPath, adbDirectory);
            File.Delete(tempPath);

            MainWindowViewModel.IsLoading = false;
        }

        Logger.Log("Starting MelonLog");
        Process.Start(new ProcessStartInfo(melonLogExecutable)
        {
            WorkingDirectory = adbDirectory,
        });
    }

    private void ClearLemonCache()
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

    private bool StartStopGame()
    {
        string packageName = MainWindowViewModel.Instance.ApplicationManager.Info.Id;
        bool isRunning = ModManager.AndroidDebugBridge.DeviceManager.SendShellCommand("pidof " + packageName) != string.Empty;
        Logger.Log($"{packageName} is current running? {isRunning}");

        string command = isRunning ? $"am force-stop {packageName}" : $"monkey -p {packageName} 1";
        ModManager.AndroidDebugBridge.DeviceManager.SendShellCommand(command);

        return !isRunning;
    }

    private async void DownloadGameData()
    {
        var dialog = new OpenFolderDialog();
        var result = await dialog.ShowAsync(MainWindow.Instance);

        MainWindowViewModel.LoadingStatus = "Downloading data";
        await DeviceManager.Pull(string.Format(FilePaths.RemoteApplicationDataPath, moddedApplication.Id), result);
        await PromptHandler.Instance.PromptUser("Success", $"Successfully downloaded {moddedApplication.Id}'s data.", PromptType.Notification);
        MainWindowViewModel.IsLoading = false;
    }

    private async void ChangeApplication()
    {
        MainWindowViewModel.LoadingStatus = "Changing application";
        MainWindowViewModel.IsLoading = true;
        await MainWindowViewModel.Instance.SelectApplication(true);
        await MainWindowViewModel.Instance.PopulateLemons();
        this.RaisePropertyChanged(nameof(Info));
        MainWindowViewModel.IsLoading = false;
    }

    private const string ApplicationInfoTemplate =
        "PackageName: {0}\n" +
        "Version: {1}\n" +
        "Unity Version: {2}\n";
}