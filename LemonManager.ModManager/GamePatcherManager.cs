using AssetsTools.NET.Extra;
using LemonManager.ModManager.AndroidDebugBridge;
using LemonManager.ModManager.Models;
using MelonLoaderInstaller.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
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
            if (IsPatching) return;
            IsPatching = true;

            Logger.SetStatus("Patching " + info.Id);
            try
            {
                string cacheDirectory = ModManager.ApplicationLocator.GetLocalApplicationCache(info.Id);
                string unityDependencyDirectory = Path.Combine(cacheDirectory, "UnityDeps.zip");
                string melonLoaderDependencyDirectory = Path.Combine(cacheDirectory, "LemonInstallerDeps.zip");
                string il2cpp_etc_path = await GetIL2CppEtc();
                string outputApk = Path.Combine(cacheDirectory, "base.apk");

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

                    UnityVersion = GetVersion(info.LocalAPKPath, info.Id) // for some reason the MelonInstaller's Unity version detector step dies, so Ima just do it here
                }, new PatcherhLoggerImplimentation());

                Logger.Log("Starting patch process");
                if (instance.Run())
                {
                    await InstallApk(info.Id, outputApk);

                    IsPatching = false;
                    info.IsModded = true; // technically not modded yet, soooooo
                    Directory.Delete(cacheDirectory, true); // clean up
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
                Environment.Exit(0);
            }
        }

        private static async Task InstallApk(string appId, string apkPath)
        {
            DeviceManager.SendShellCommand($"am force-stop {appId}"); // just incase its already running
            if (!await AndroidDebugBridge.ServerManager.PromptHandler.PromptUser("Install Modded APK?", "Are you sure you want to proceed, your game may become unplayable if the process fails.", PromptType.Confirmation))
                return;

            Logger.SetStatus("Uninstalling " + appId);
            await DeviceManager.SendShellCommandAsync("pm uninstall -k " + appId);
            Logger.SetStatus("Installing modded apk");
            await DeviceManager.SendCommandAsync("install " + apkPath);

            await AndroidDebugBridge.ServerManager.PromptHandler.PromptUser("Modded APK Installed", "You must run your game once before being able to install any lemons. The first time you run the game it may take sevral minutes to start.", PromptType.Notification);
        }

        private static AssetRipper.Primitives.UnityVersion GetVersion(string localAPKPath, string appId)
        {
            string tempDir = Path.Combine(ApplicationLocator.GetLocalApplicationCache(appId), "supertempdir");
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);
            ZipFile.ExtractToDirectory(localAPKPath, tempDir, true);

            string globalGameManagersPath = Path.GetFullPath(tempDir + "/assets/bin/Data/globalgamemanagers");
            string assetFilePath = File.Exists(globalGameManagersPath) ? globalGameManagersPath : Path.GetFullPath(tempDir + "/assets/bin/Data/data.unity3d");

            AssetRipper.Primitives.UnityVersion result = AssetRipper.Primitives.UnityVersion.MinVersion;
            using (FileStream assetFileStream = File.OpenRead(assetFilePath))
            {
                AssetsManager assetsManager = new AssetsManager();
                var assetFile = assetsManager.LoadAssetsFile(assetFileStream, true);
                result = AssetRipper.Primitives.UnityVersion.Parse(assetFile.file.Metadata.UnityVersion);
                Logger.Log(result);
            }
            return result;
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

        public class PatcherhLoggerImplimentation : IPatchLogger
        {
            void IPatchLogger.Log(string message)
            {
                Logger.SetStatus(message);
            }
        }
    }
}
