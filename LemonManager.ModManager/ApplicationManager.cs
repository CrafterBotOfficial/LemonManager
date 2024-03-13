using LemonManager.ModManager.AndroidDebugBridge;
using LemonManager.ModManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LemonManager.ModManager;

public class ApplicationManager
{
    public ModdedApplicationModel Info;

    public string ApplicationData => string.Format(FilePaths.RemoteApplicationDataPath, Info.Id);

    private static Assembly melonLoaderAssembly;


    public ApplicationManager(ModdedApplicationModel info)
    {
        Info = info;
    }

    public async Task<LemonInfo[]> GetLemons(bool plugins)
    {
        Logger.Log("Getting Lemons");
        await LoadMelonLoaderAssembly();

        string prefix = (plugins ? "Plugins" : "Mods");
        string localCache = Path.Combine(FilePaths.LemonCacheDirectory, Info.Id, prefix);
        string remoteLemonDirectory = string.Format(FilePaths.RemoteApplicationDataPath, Info.Id) + prefix;

        if (Directory.Exists(localCache)) Directory.Delete(localCache, true);
        await DeviceManager.Pull(remoteLemonDirectory, localCache);

        List<LemonInfo> infos = new List<LemonInfo>();
        foreach (string file in Directory.GetFiles(localCache))
        {
            if (!file.EndsWith(".dll") && !file.EndsWith(FilePaths.DisabledPrefix)) continue;
            LemonInfo? info = LoadLemon(file);
            if (info.HasValue) infos.Add(info.Value);
        }

        Logger.Log($"Found {infos.Count} lemons, yum");
        return infos.ToArray();
    }

    public LemonInfo? LoadLemon(string file)
    {
        try
        {
            Type melonPluginType = melonLoaderAssembly.GetType("MelonLoader.MelonPlugin");

            using Stream stream = File.OpenRead(file);
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);

            Assembly assembly = Assembly.Load(buffer);

            var attributes = assembly.GetCustomAttributesData();
            if (!attributes.Any(attribute => attribute.AttributeType.FullName == "MelonLoader.MelonInfoAttribute")) return null;

            var entryPointArgs = attributes.Single(x => x.AttributeType.FullName == "MelonLoader.MelonInfoAttribute").ConstructorArguments;
            bool isPlugin = (entryPointArgs[0].Value as Type).IsSubclassOf(melonPluginType);

            return new() // TODO: Add other contructor variants
            {
                Name = (string)entryPointArgs[1].Value,
                Version = (string)entryPointArgs[2].Value,
                Author = (string)entryPointArgs[3].Value,
                IsPlugin = isPlugin,
                LocalDLLCachePath = file,
                RemotePath = ApplicationData + (isPlugin ? "/Plugins/" : "/Mods/") + Path.GetFileName(file),
            };
        }
        catch (Exception ex) { Logger.Error($"Couldn't load Lemon {Path.GetFileNameWithoutExtension(file)} due to {ex}"); }
        return null;
    }

    public async Task InstallLemon(string localDLL)
    {
        Logger.SetStatus("Installing " + Path.GetFileName(localDLL));
        LemonInfo? info = LoadLemon(localDLL);
        if (!info.HasValue) return; // Not a DLL file / Melon

        string remoteDestination = string.Format(FilePaths.RemoteApplicationDataPath, Info.Id) + (info.Value.IsPlugin ? "Plugins/" : "Mods/") + Path.GetFileName(info.Value.RemotePath);
        Logger.Log("Pushing to " + remoteDestination);
        await DeviceManager.Push($"\"{localDLL}\"", $"\"{remoteDestination}\"");
    }

    public void UninstallLemon(string remotePath)
    {
        Logger.Log("Uninstalling lemon " + remotePath);
        DeviceManager.SendShellCommand("rm -f " + remotePath);
    }

    public void SetLemonEnabled(string remotePath, bool enabled)
    {
        string newPath = $"{Path.GetDirectoryName(remotePath)}/{Path.GetFileNameWithoutExtension(remotePath)}{(enabled ? ".dll" : FilePaths.DisabledPrefix)}";
        Logger.Log($"Moving {remotePath} to {newPath}");
        DeviceManager.SendShellCommand($"mv {remotePath.Replace('\\', '/')} {newPath.Replace('\\', '/')}");
    }

    public async Task LoadMelonLoaderAssembly()
    {
        if (melonLoaderAssembly is object) return;
        string melonLoader = Path.Combine(FilePaths.LemonCacheDirectory, Info.Id, "MelonLoader.dll");
        string remoteMelonLoader = string.Format(FilePaths.RemoteApplicationMelonLoaderDLLPath, Info.Id);
        if (!File.Exists(melonLoader) || DeviceManager.LocalComputeHash(melonLoader) != DeviceManager.RemoteComputeHash(remoteMelonLoader))
            await DeviceManager.Pull(remoteMelonLoader, melonLoader);
        melonLoaderAssembly = Assembly.LoadFrom(melonLoader);
    }

    public struct LemonInfo
    {
        public string Name;
        public string Author;
        public string Version;

        public bool IsPlugin;

        public string LocalDLLCachePath;
        public string RemotePath;
    }
}