namespace LemonManager.ModManager.Models;

public class UnityApplicationInfoModel
{
    public string Id;
    
    public string Version;
    public string UnityVersion;
    public string Il2CppVersion;

    public bool IsModded;

    public string RemoteAPKPath;
    public string LocalAPKPath;
    public string RemoteDataPath;

    public byte[]? Icon;
}