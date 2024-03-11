using LemonManager.ModManager.AndroidDebugBridge;

namespace LemonManager.ModManager.Models;

public class DeviceInfo
{
    public string Id;
    public string Model;

    public DeviceInfo(DeviceManager.Device device)
    {
        Id = device.Id;
        Model = device.Model;
    }
}