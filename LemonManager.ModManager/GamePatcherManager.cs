using LemonManager.ModManager.AndroidDebugBridge;
using LemonManager.ModManager.Models;
using MelonLoaderInstaller.Core;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LemonManager.ModManager
{
    /// <summary> Manages the LemonLoader.MelonInstallerCore project</summary>
    public static class GamePatcherManager
    {
        private static Patcher instance;
        public static bool IsPatching;

        public static async void PatchApp(UnityApplicationInfoModel info)
        {
            Logger.SetStatus("Patching " + info.Id);
            Logger.Log("Thread: " + Thread.CurrentThread.ManagedThreadId);
            try
            {
                string cacheDirectory = ModManager.ApplicationLocator.GetLocalApplicationCache(info.Id);
                string unityDependencyDirectory = Path.Combine(cacheDirectory, "UnityDeps.zip");
                string melonLoaderDependencyDirectory = Path.Combine(cacheDirectory, "LemonInstallerDeps.zip");
                string il2cpp_etc_path = await GetIL2CppEtc();
                string outputApk = Path.Combine(cacheDirectory, "base.apk");

                var unityVersion = GetVersion(info.LocalAPKPath); // for some reason the MelonInstaller's Unity version detector step dies, so Ima just do it here
                if (unityVersion == AssetRipper.Primitives.UnityVersion.MinVersion)
                {
                    Logger.Error("Failed to parse Unity version, aborting.");
                    await ServerManager.PromptHandler.PromptUser("Failed Patching", "Failed to parse Unity version, please try to patch the game with the Android MelonInstaller application instead.", PromptType.Notification);
                    Environment.Exit(1);
                }

                CleanFiles(unityDependencyDirectory, outputApk, melonLoaderDependencyDirectory); // just incase the process failed mid way through and this is a retry

                DownloadInstallerDependencies(melonLoaderDependencyDirectory);

                instance = new Patcher(new PatchArguments()
                {
                    TempDirectory = cacheDirectory,
                    TargetApkPath = info.LocalAPKPath,
                    OutputApkDirectory = cacheDirectory,

                    UnityDependenciesPath = unityDependencyDirectory,
                    Il2CppEtcPath = il2cpp_etc_path,

                    LemonDataPath = melonLoaderDependencyDirectory,
                    PackageName = info.Id,

                    UnityVersion = unityVersion
                }, new PatcherLoggerImplimentation());

                Logger.Log("Starting patch process");
                if (instance.Run())
                {
                    Logger.Log("Patching completed");
                    if (await InstallApk(info, outputApk))
                    {
                        IsPatching = false;
                        info.IsModded = true;
                        Directory.Delete(cacheDirectory, true); // clean up
                    }
                    else Logger.SetStatus("Finished!\nModded APK:\n" + outputApk);
                }
                else
                {
                    Logger.Error("Something unexpected and unhandled happened while patching the game... uh oh");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                await ServerManager.PromptHandler.PromptUser("FUCK", "Failed to patch application, please open a Github issue and send the log file located in the LemonManager local storage directory.", PromptType.Notification);
                Environment.Exit(1);
            }
        }

        private static async Task<bool> InstallApk(UnityApplicationInfoModel appInfo, string moddedAPKPath)
        {
            DeviceManager.SendShellCommand($"am force-stop {appInfo.Id}"); // just incase its already running
            if (!await AndroidDebugBridge.ServerManager.PromptHandler.PromptUser("Install Modded APK?", "Are you sure you want to proceed, your game may become unplayable if the process fails. All game data WILL be lost!", PromptType.Confirmation))
                return false;

            // const string RemoteTempAPKPath = FilePaths.RemoteLemonManagerDataDirectory + "/temp.apk";
            // if (!await DeviceManager.RemoteDirectoryExists(FilePaths.RemoteLemonManagerDataDirectory))
            // {
            // Logger.SetStatus("Setting up directories");
            //     await DeviceManager.SendShellCommandAsync("mkdir " + FilePaths.RemoteLemonManagerDataDirectory);
            // }

            Logger.SetStatus("Uninstalling " + appInfo.Id);
            await DeviceManager.SendShellCommandAsync("pm uninstall " + appInfo.Id);

            // Logger.SetStatus("Uploading Modded APK");
            // await DeviceManager.Push(moddedAPKPath, RemoteTempAPKPath);

            Logger.SetStatus("Installing Modded APK");
            await DeviceManager.SendCommandAsync("install " + moddedAPKPath);

            // Logger.SetStatus("Cleaning up");
            // await DeviceManager.SendShellCommandAsync("rm " + RemoteTempAPKPath); // just incase


            await AndroidDebugBridge.ServerManager.PromptHandler.PromptUser("Modded APK Installed", "You must run your game once before being able to install any lemons. The first time you run the game it may take sevral minutes to start.", PromptType.Notification);
            return true;
        }

        private static AssetRipper.Primitives.UnityVersion GetVersion(string localApk)
        {
            try
            {
                using ZipArchive archive = ZipFile.OpenRead(localApk);
                return AssetRipper.Primitives.UnityVersion.Parse(ApplicationLocator.GetUnityVersion(archive));
            }
            catch
            {
                return AssetRipper.Primitives.UnityVersion.MinVersion;
            }
        }

        private static async Task<string> GetIL2CppEtc()
        {
            string outputFile = Path.Combine(FilePaths.LemonCacheDirectory, "il2cpp_etc.zip");
            if (!File.Exists(outputFile))
            {
                using WebClient webClient = new WebClient();
                await webClient.DownloadFileTaskAsync("https://raw.githubusercontent.com/CrafterBotOfficial/LemonManager/RemoteGamePatching/il2cpp_etc.zip", outputFile);
            }
            return outputFile;
        }

        private static void DownloadInstallerDependencies(string outputFile)
        {
            Logger.SetStatus("Downloading LemonLoader & dependencies");
            const string Url = "https://github.com/LemonLoader/MelonLoader/releases/download/0.2.0/installer_deps_0.2.0.zip";
            using WebClient webClient = new WebClient();

            webClient.DownloadFile(Url, outputFile);
            Logger.Log("Finished downloading!");
        }

        private static void CleanFiles(params string[] files)
        {
            foreach (var file in files)
            {
                try
                {
                    if (File.Exists(file)) File.Delete(file);
                    else if (Directory.Exists(file)) Directory.Delete(file);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }

        public class PatcherLoggerImplimentation : IPatchLogger
        {
            void IPatchLogger.Log(string message)
            {
                Logger.SetStatus(message);
            }
        }
    }
}
