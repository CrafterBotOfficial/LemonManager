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

    private ModdedApplicationModel moddedApplication => MainWindowViewModel.Instance.ApplicationManager.Info;
    public string Info => string.Format(ApplicationInfoTemplate, moddedApplication.Id, moddedApplication.Version, moddedApplication.UnityVersion);

    public GameControlsViewModel()
    {
        Options.AddRange(new GameControlButtonModel[]
        {
            new GameControlButtonModel("Logcat", Logcat),
            new GameControlButtonModel("Clear LemonCache", ClearLemonCache),
            new GameControlButtonModel("Stop Game", "Start Game", StartStopGame),
            new GameControlButtonModel("Download Game Data", DownloadGameData),
            new GameControlButtonModel("Change Application", ChangeApplication)
        });
    }

    private void Logcat()
    {
        string adbPath = Path.Combine(FilePaths.ApplicationDataPath, "platform-tools", "adb.exe");

        Logger.Log("Starting logcat");
        Process.Start(new ProcessStartInfo(adbPath)
        {
            WorkingDirectory = Path.GetDirectoryName(adbPath),
            Arguments = "logcat -v time MelonLoader:D CRASH:D Mono:D mono:D mono-rt:D Zygote:D A64_HOOK:V DEBUG:D funchook:D Unity:D Binder:D AndroidRuntime:D *:S" // https://github.com/LemonLoader/MelonLoader/wiki/Logging#realtime-logging
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