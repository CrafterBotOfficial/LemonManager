using LemonManager.ModManager.AndroidDebugBridge;
using LemonManager.ModManager.MelonPreferences.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LemonManager.ModManager.MelonPreferences;

public class MelonPreferenceFileManager
{
    public string RawText;
    public string RemotePath;

    public MelonPreferenceSection[] Sections;

    public MelonPreferenceFileManager(string remotePath)
    {
        RemotePath = remotePath;
        RawText = DeviceManager.SendShellCommand($"cat {remotePath}");

        Logger.Log(RawText);
        ParseFile();
    }

    private string lastComment = string.Empty;
    private void ParseFile()
    {
        Logger.Log("Parsing config ini");
        List<MelonPreferenceSection> sections = new();
        foreach (string line in RawText.Split('\n'))
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

        Sections = sections.ToArray();
    }

    public async void SetValue(string key, string value)
    {
        string[] lines = RawText.Split('\n');
        Logger.Log(lines.Length);
        int lineIndex = 0;
        for (lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            if (lines[lineIndex].StartsWith(key)) break;

        string oldValue = lines[lineIndex].Split('=')[1].Trim();
        lines[lineIndex] = lines[lineIndex].Replace(oldValue, value);
  
        string tempPath = Path.GetTempFileName();
        File.WriteAllLines(tempPath, lines);
        DeviceManager.SendShellCommand("rm -f " + RemotePath);
        await DeviceManager.Push(tempPath, RemotePath);
        File.Delete(tempPath);
    }
}