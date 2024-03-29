using Iteedee.ApkReader;
using LemonManager.ModManager.AndroidDebugBridge;
using LemonManager.ModManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LemonManager.ModManager
{

    public static class ApplicationLocator
    {
        private static ApkReader apkReader = new();

        public static Dictionary<string, ApplicationInfo> GetApplications()
        {
            string[] lines = DeviceManager.SendShellCommand("cmd package list packages -f -3").Split('\n');
            Dictionary<string, ApplicationInfo> infos = new Dictionary<string, ApplicationInfo>(lines.Length);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                var match = Regex.Match(line, @"package:(.+?)\.apk=(.+)");
                if (!match.Success)
                {
                    Logger.Warning("Couldn't parse " + line);
                    continue;
                }

                string id = match.Groups[2].Value;
                if (infos.ContainsKey(id))
                {
                    Logger.Warning("Duplicate package found: " + id);
                    continue;
                }
                infos.Add(id, new ApplicationInfo(match.Groups[1].Value + ".apk", id));
            }
            Logger.Log($"Found {infos.Count} packages");
            return infos;
        }

        /// <returns>Null if isn't Unity app</returns>
        public static async Task<UnityApplicationInfoModel> GetUnityApplicationInfo(ApplicationInfo info)
        {
            string localDirectory = GetLocalApplicationCache(info.Id);
            if (!Directory.Exists(localDirectory)) Directory.CreateDirectory(localDirectory);

            string localAPK = await DownloadRemoteApplication(info);

            Logger.SetStatus("Parsing APK");

            using ZipArchive zipArchive = ZipFile.OpenRead(localAPK);
            if (zipArchive.Entries.Any(entry => entry.Name == "libunity.so"))
            {
                byte[] manifestBytes = GetBytes(zipArchive.GetEntry("AndroidManifest.xml"));
                byte[] resourcesBytes = GetBytes(zipArchive.GetEntry("resources.arsc"));

                ApkInfo apkInfo = apkReader.extractInfo(manifestBytes, resourcesBytes); // really only extracting the apk version so it may not be worth it
                return new UnityApplicationInfoModel()
                {
                    Id = apkInfo.packageName,

                    Version = apkInfo.versionName,
                    UnityVersion = GetUnityVersion(zipArchive) ?? "UNKNOWN",
                    Il2CppVersion = GetIL2CppVersion(zipArchive.GetEntry("assets/bin/Data/Managed/Metadata/global-metadata.dat")) ?? "UNKNOWN", // unkown if mono

                    IsModded = zipArchive.Entries.Any(entry => entry.Name == "MelonLoader.dll"),
                    MelonLoaderInitialized = await MelonLoaderInitialized(info.Id),

                    RemoteAPKPath = info.RemoteAPKPath,
                    LocalAPKPath = localAPK,
                    RemoteDataPath = $"/sdcard/Android/data/{info.Id}/files/",

                    Icon = apkInfo.hasIcon ? GetBytes(zipArchive.GetEntry(apkInfo.iconFileName[0])) : null
                };
            }
            Logger.Log($"{info.Id} isn't a Unity Application");
            return null;
        }

        public static async Task<string> DownloadRemoteApplication(ApplicationInfo info)
        {
            Logger.SetStatus("Comparing hashes");
            string localAPK = Path.Combine(GetLocalApplicationCache(info.Id), "base.apk.zip");

            if (!File.Exists(localAPK) || !DeviceManager.CompareFileHashs(info.RemoteAPKPath, localAPK))
            {
                Logger.SetStatus("Downloading remote APK");
                if (File.Exists(localAPK)) File.Delete(localAPK);
                await DeviceManager.Pull(info.RemoteAPKPath, localAPK);
            }
            return localAPK;
        }

        public static string GetUnityVersion(ZipArchive archive)
        {
            try
            {
                string globalGameManagersPath = "assets/bin/Data/globalgamemanagers";
                ZipArchiveEntry globalGameManagersEntry = archive.GetEntry(globalGameManagersPath);
                var assetFileEntry = globalGameManagersEntry is object ? globalGameManagersEntry : archive.GetEntry("assets/bin/Data/data.unity3d");

                using Stream stream = assetFileEntry.Open();
                using MemoryStream memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);

                memoryStream.Position = globalGameManagersEntry is object ? 0x14 : 0x12;
                byte[] versionBuffer = new byte[11];
                memoryStream.Read(versionBuffer, 0, versionBuffer.Length);

                string unityVersion = Encoding.UTF8.GetString(versionBuffer);
                Logger.Log("Unity version from asset file: " + unityVersion);
                return unityVersion;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }

        // https://katyscode.wordpress.com/2020/12/27/il2cpp-part-2/
        private static string GetIL2CppVersion(ZipArchiveEntry il2cpp_metadata) // width 32
        {
            if (il2cpp_metadata is null)
                return null;

            using Stream stream = il2cpp_metadata.Open();
            byte[] buffer = new byte[8];
            stream.Read(buffer, 0, 8);

            var signature = BitConverter.ToUInt32(buffer, 0);
            if (signature != 0xFAB11BAF)
            {
                Logger.Warning("Metadata starts with a unknown signature " + signature);
                return null;
            }

            uint versionNumber = BitConverter.ToUInt32(buffer, 4);
            Logger.Log("Il2Cpp version " + versionNumber);
            return versionNumber.ToString();
        }

        private static async Task<bool> MelonLoaderInitialized(string appId)
        {
            string melonloaderPath = string.Format(FilePaths.RemoteApplicationDataPath, appId) + "/melonloader/";
            bool exists = await DeviceManager.RemoteDirectoryExists(melonloaderPath);
            Logger.Log($"Checked remote directory for melonloader: {melonloaderPath}\nresult:{exists}");
            return exists;
        }

        private static byte[] GetBytes(ZipArchiveEntry entry)
        {
            using Stream stream = entry.Open();
            byte[] buffer = new byte[entry.Length];
            stream.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        public static string GetLocalApplicationCache(string appId) => Path.Combine(FilePaths.LemonCacheDirectory, appId);

        public struct ApplicationInfo
        {
            public string RemoteAPKPath;
            public string Id;

            public ApplicationInfo(string remoteApkPath, string id)
            {
                RemoteAPKPath = remoteApkPath;
                Id = id;
            }
        }
    }
}