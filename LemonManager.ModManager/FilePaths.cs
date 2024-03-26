using System;
using System.IO;

namespace LemonManager.ModManager;

public static class FilePaths
{
    public static string ApplicationDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LemonManager");
    public static string LemonCacheDirectory => Path.Combine(ApplicationDataPath, "LemonCache");

    public const string RemoteLemonManagerDataDirectory = "/sdcard/LemonManager";

    public const string RemoteApplicationDataPath = "/sdcard/Android/data/{0}/files/";
    public const string RemoteApplicationMelonLoaderDLLPath = RemoteApplicationDataPath + "/melonloader/etc/MelonLoader.dll";

    public const string DisabledPrefix = ".disabled";

    static FilePaths()
    {
        if (!Directory.Exists(ApplicationDataPath))
            Directory.CreateDirectory(ApplicationDataPath);
    }
}