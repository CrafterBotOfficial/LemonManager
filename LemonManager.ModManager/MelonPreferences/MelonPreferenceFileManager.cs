using LemonManager.ModManager.AndroidDebugBridge;
using System.Threading.Tasks;

namespace LemonManager.ModManager.MelonPreferences
{

    public class MelonPreferenceFileManager
    {
        public string RawText;
        public string RemotePath;

        public MelonPreferenceFileManager(string remotePath)
        {
            try
            {
                RemotePath = remotePath;
                RawText = DeviceManager.SendShellCommand($"cat {remotePath}");
            }
            catch { } // config path most likely doesn't exists
        }

        public async Task SaveText(string text)
        {
            DeviceManager.SendShellCommand("echo \"\" > " + RemotePath);
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                Logger.SetStatus($"{i + 1}/{lines.Length} lines");
                await DeviceManager.SendShellCommandAsync($"echo \'{lines[i].Replace("\"", "\\\"")}\' >> \'{RemotePath}\'");
            }
        }
    }
}