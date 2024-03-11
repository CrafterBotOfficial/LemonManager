using LemonManager.ModManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LemonManager.ModManager.AndroidDebugBridge;

public static class DeviceManager
{
    public static DeviceInfo CurrentDevice;

    #region Command Sending

    public static string SendCommand(string command) =>
        CommandExecuter.SendCommand(GetCommand(command));
    public static async Task<string> SendCommandAsync(string command) =>
       await CommandExecuter.SendCommandAsync(GetCommand(command));
    public static string SendShellCommand(string command) =>
        CommandExecuter.SendCommand(GetCommand(command.Insert(0, "shell ")));
    public static async Task<string> SendShellCommandAsync(string command) =>
       await CommandExecuter.SendCommandAsync(GetCommand(command.Insert(0, "shell ")));

    private static string GetCommand(string original) =>
        original.Insert(0, "-s " + CurrentDevice.Id + " ");

    #endregion

    #region File managment

    public static async Task Pull(string remote, string localOutput) =>
        await SendCommandAsync($"pull -a {remote} {localOutput}");

    public static async Task Push(string localFile, string remoteFile) =>
        await SendCommandAsync($"push {localFile} {remoteFile}");

    public static string[] GetFiles(string remotePath)
    {
        return SendShellCommand($"ls -m {remotePath}").Split('\n').Select(x => x.Insert(0, remotePath + "/")).ToArray();
    }

    public static string RemoteComputeHash(string remoteFile) =>
        SendShellCommand($"sha256sum {remoteFile}");

    public static string LocalComputeHash(string path)
    {
        using Stream stream = File.OpenRead(path);
        return LocalComputeHash(stream);
    }
    public static string LocalComputeHash(Stream stream)
    {
        using SHA256 sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    #endregion

    public static async Task DetermineDevice()
    {
        Logger.Log("Finding device");
        var devices = GetDevices();

        if (devices.Length == 1) CurrentDevice = new DeviceInfo(devices.First());
        else
        {
            int result = await ServerManager.PromptHandler.PromptUser("Select Device", devices.Select(device => device.Model).ToArray());
            CurrentDevice = new(devices[result]);
        }
    }

    private static Device[] GetDevices()
    {
        List<Device> devices = new List<Device>();
        string[] lines = CommandExecuter.SendCommand("devices -l").Split('\n');
        foreach (string line in lines)
        {
            var match = Regex.Match(line, @"^([^\s]+)\s+device\s+product:([^\s]+)\s+model:([^\s]+)");
            if (!match.Success) continue;

            Logger.Log("Found device " + match.Groups[3].Value);
            devices.Add(new Device(match.Groups[3].Value, match.Groups[1].Value));
        }
        return devices.ToArray();
    }

    public struct Device
    {
        public string Model;
        public string Id;

        public Device(string model, string id)
        {
            Model = model;
            Id = id;
        }
    }
}