namespace LemonManager.ModManager.Models;

public class UnityApplicationInfoModel
{
    public string Id;
    
    public string Version;
    public string UnityVersion;
    public string Il2CppVersion;

    public bool IsModded;
    public bool MelonLoaderInitialized; // If the game has been ran once and the dummy DLLs are generated

    public string RemoteAPKPath;
    public string LocalAPKPath;
    public string RemoteDataPath;

    public byte[]? Icon;
}