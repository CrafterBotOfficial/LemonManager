using Iteedee.ApkReader;
using LemonManager.ModManager.AndroidDebugBridge;
using LemonManager.ModManager.Models;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LemonManager.ModManager;

public static class ApplicationLocator
{
    private static ApkReader apkReader = new();

    public static Dictionary<string, ApplicationInfo> GetApplications()
    {
        string[] lines = DeviceManager.SendShellCommand("pm list packages -f").Split('\n');
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
            if (id.StartsWith("com.oculus")) continue;

            infos.Add(id, new ApplicationInfo(match.Groups[1].Value + ".apk", id));
        }
        Logger.Log($"Found {infos.Count} packages");
        return infos;
    }

    /// <returns>Null if not modded</returns>
    public static async Task<ModdedApplicationModel> GetModdedApplicationInfo(ApplicationInfo info)
    {
        string localDirectory = Path.Combine(FilePaths.LemonCacheDirectory, info.Id);
        if (!Directory.Exists(localDirectory)) Directory.CreateDirectory(localDirectory);

        string localAPK = Path.Combine(localDirectory, "base.apk.zip");
        Stream localAPKReadStream = File.Exists(localAPK) ? File.OpenRead(localAPK) : null;

        if (localAPKReadStream is null || !DeviceManager.CompareFileHashs(info.RemoteAPKPath, localAPK))
        {
            Logger.SetStatus("Downloading remote APK");
            if (File.Exists(localAPK)) File.Delete(localAPK);
            await DeviceManager.Pull(info.RemoteAPKPath, localAPK);
            localAPKReadStream = File.OpenRead(localAPK);
        }

        Logger.SetStatus("Parsing APK");

        using ZipArchive zipArchive = ZipFile.OpenRead(localAPK);
        if (zipArchive.Entries.Any(entry => entry.Name == "MelonLoader.dll"))
        {
            byte[] manifestBytes = GetBytes(zipArchive.GetEntry("AndroidManifest.xml"));
            byte[] resourcesBytes = GetBytes(zipArchive.GetEntry("resources.arsc"));

            ApkInfo apkInfo = apkReader.extractInfo(manifestBytes, resourcesBytes); // really only extracting the apk version so it may not be worth it
            return new()
            {
                Id = apkInfo.packageName,
                Version = apkInfo.versionName,
                UnityVersion = GetUnityVersion(zipArchive),
                RemoteAPKPath = info.RemoteAPKPath,
                LocalAPKPath = localAPK,
            };
        }
        Logger.Log($"{info.Id} isn't modded");
        return null;
    }

    private static string GetUnityVersion(ZipArchive archive)
    {
        bool dataUnity3dExists = archive.Entries.Any(entry => entry.Name == "data.unity3d");
        var entry = archive.GetEntry("assets/bin/Data/" + (dataUnity3dExists ? "data.unity3d" : "globalgamemanagers"));

        Logger.Log("Extracting unity version");
        using Stream stream = entry.Open();
        using MemoryStream memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);

        memoryStream.Position = dataUnity3dExists ? 0x12 : 0x14;
        using (BinaryReader reader = new BinaryReader(memoryStream))
        {
            string version = Encoding.Default.GetString(reader.ReadBytes(11));
            Logger.Log("Detected version " + version);
            return version;
        }
    }

    private static byte[] GetBytes(ZipArchiveEntry entry)
    {
        using Stream stream = entry.Open();
        byte[] buffer = new byte[entry.Length];
        stream.Read(buffer, 0, buffer.Length);
        return buffer;
    }

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