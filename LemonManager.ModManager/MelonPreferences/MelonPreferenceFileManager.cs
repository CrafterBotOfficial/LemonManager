using LemonManager.ModManager.AndroidDebugBridge;
using LemonManager.ModManager.MelonPreferences.Models;
using System.Collections.Generic;
using System.Linq;

namespace LemonManager.ModManager.MelonPreferences
{

    public class MelonPreferenceFileManager
    {
        public string RawText;
        public string RemotePath;

        public MelonPreferenceSection[] Sections;

        public MelonPreferenceFileManager(string remotePath)
        {
            try
            {
                RemotePath = remotePath;
                RawText = DeviceManager.SendShellCommand($"cat {remotePath}");
                ParseFile();
            }
            catch { } // config path most likely doesn't exists
        }

        private string lastComment = string.Empty;
        private void ParseFile()
        {
            Logger.Log("Parsing config ini");
            List<MelonPreferenceSection> sections = new();
            foreach (string line in RawText.Split('\n'))
            {
                try
                {
                    string formattedLine = line.Trim();

                    if (formattedLine.Length < 0) continue;

                    // New section
                    if (formattedLine.StartsWith('[') && formattedLine.EndsWith(']'))
                    {
                        sections.Add(new(formattedLine.Replace("[", "").Replace("]", "")));
                        continue;
                    }

                    // Comment
                    if (formattedLine.StartsWith("#"))
                    {
                        lastComment = formattedLine.Replace("#", "");
                        continue;
                    }

                    // Entry
                    if (formattedLine.Contains(" = "))
                    {
                        string name = formattedLine.Split(' ')[0];
                        string value = formattedLine.Split(" = ")[1].Trim() ?? "UNKNOWN";
                        sections.Last().Values.Add(name, (value, lastComment));
                    }
                }
                catch (System.Exception ex)
                {
                    Logger.Warning($"Couldn't parse line \"{line}\" {ex.Message}");
                }
            }

            Sections = sections.ToArray();
        }

        public async void SetValue(string key, string newValue)
        {
            string[] lines = RawText.Split('\n');

            int lineIndex = 0;
            for (lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                if (lines[lineIndex].StartsWith(key)) break;

            string oldValue = lines[lineIndex].Split('=')[1].Trim();
            lines[lineIndex] = lines[lineIndex].Replace(oldValue, newValue);

            await DeviceManager.SendShellCommandAsync($"echo \"\" > {RemotePath}");
            foreach (string line in lines)
            {
                Logger.Log(line); // .Replace("\"", "\\\"")
                await DeviceManager.SendShellCommandAsync($"echo \'{line.Replace("\"", "\\\"")}\' >> \'{RemotePath}\'");
            }
        }
    }
}