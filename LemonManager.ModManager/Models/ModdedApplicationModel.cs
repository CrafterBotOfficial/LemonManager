namespace LemonManager.ModManager.Models;

public class ModdedApplicationModel
{
    public string Id;
    
    public string Version;
    public string UnityVersion;
    public string Il2CppVersion;

    public string RemoteAPKPath;
    public string LocalAPKPath;
    public byte[]? Icon;
}