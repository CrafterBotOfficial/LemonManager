using AssetsTools.NET.Extra;
using LemonManager.ModManager.AndroidDebugBridge;
using LemonManager.ModManager.Models;
using MelonLoaderInstaller.Core;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
            Logger.SetStatus("Patching application");
            IsPatching = true;
            try
            {
                string cacheDirectory = ModManager.ApplicationLocator.GetLocalApplicationCache(info.Id);
                string unityDependencyDirectory = Path.Combine(cacheDirectory, "UnityDeps.zip");
                string melonLoaderDependencyDirectory = Path.Combine(cacheDirectory, "LemonInstallerDeps.zip");

                await DownloadInstallerDependencies(melonLoaderDependencyDirectory);

                instance = new Patcher(new PatchArguments()
                {
                    TempDirectory = cacheDirectory,
                    TargetApkPath = info.LocalAPKPath,
                    OutputApkDirectory = cacheDirectory,

                    UnityDependenciesPath = unityDependencyDirectory,

                    LemonDataPath = melonLoaderDependencyDirectory,
                    PackageName = info.Id,

                    UnityVersion = GetVersion(info.LocalAPKPath, info.Id) // for some reason the MelonInstaller's Unity version detector step dies, so Ima just do it here
                }, new PatcherhLoggerImplimentation()); ;

                Logger.Log("Starting patch process");
                if (instance.Run())
                {
                    IsPatching = false;
                    info.IsModded = true; // technically not modded yet, soooooo


                    Directory.Delete(cacheDirectory); // clean up
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                await ServerManager.PromptHandler.PromptUser("FUCK", "Failed to patch application, please open a Github issue and send the log file located in the LemonManager local storage directory.", PromptType.Confirmation);
            }
        }

        private static AssetRipper.Primitives.UnityVersion GetVersion(string localAPKPath, string appId)
        {
            string tempDir = Path.Combine(ApplicationLocator.GetLocalApplicationCache(appId), "supertempdir");
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);
            ZipFile.ExtractToDirectory(localAPKPath, tempDir);

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

        private static string GetIL2CppLibsPath(string apkPath)
        {
            using Stream stream = File.OpenRead(apkPath);
            using ZipArchive archive = new ZipArchive(stream);

            var libUnityEntry = archive.Entries.Single(entry => entry.Name == "libunity.so");
            var path = libUnityEntry.FullName.Split('\\', '/').ToList();
            path.Remove(libUnityEntry.Name);
            return string.Join('/', path);
        }

        private static async Task DownloadInstallerDependencies(string outputFile)
        {
            const string Url = "https://github.com/LemonLoader/MelonLoader/releases/download/0.2.0/installer_deps_0.2.0.zip";
            using WebClient webClient = new WebClient();

            await webClient.DownloadFileTaskAsync(Url, outputFile);
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
