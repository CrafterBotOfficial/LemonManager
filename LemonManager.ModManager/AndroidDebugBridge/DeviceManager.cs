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
        return SendShellCommand($"ls -m {remotePath}").Split('\n', ',').Select(x => x.Trim().Insert(0, remotePath + "/").Trim()).ToArray();
    }

    public static bool CompareFileHashs(string remoteFile, string localFile)
    {
        string remoteHash = RemoteComputeHash(remoteFile);
        string localHash = LocalComputeHash(localFile);
        bool equals = remoteHash.Equals(localHash);
        return equals;
    }

    public static string RemoteComputeHash(string remoteFile) =>
       SendShellCommand($"md5sum {remoteFile}").Split(' ', '\n')[0].ToLowerInvariant().Trim();

    public static string LocalComputeHash(string path)
    {
        using Stream stream = File.OpenRead(path);
        return LocalComputeHash(stream);
    }
    public static string LocalComputeHash(Stream stream)
    {
        using MD5 md5 = MD5.Create();
        var hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant().Trim();
    }

    #endregion

    public static async Task DetermineDevice()
    {
        Logger.SetStatus("Finding device");
        var deviceDeterminationCompletionSource = new TaskCompletionSource<bool>();
        var devices = await GetDevices();

        if (devices.Length == 1) CurrentDevice = new DeviceInfo(devices.First());
        else
        {
            USBMonitor(deviceDeterminationCompletionSource.Task, (deviceArray) => ServerManager.PromptHandler.UpdateMultiSelectionPrompt("Select Device", deviceArray.Select(device => device.Model).ToArray()));

            int result = await ServerManager.PromptHandler.PromptUser("Select Device", devices.Select(device => device.Model).ToArray());
            CurrentDevice = new((await GetDevices())[result]);
            deviceDeterminationCompletionSource.SetResult(true);
        }
    }

    private static int deviceCount;
    private static async Task USBMonitor(Task task, Action<Device[]> onDevicesChanged)
    {
        deviceCount = (await GetDevices()).Length;
        while (!task.IsCompleted)
            try
            {
                var devices = await GetDevices();
                if (deviceCount != devices.Length) onDevicesChanged(devices);
                await Task.Delay(1500);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in USBMonitor: {ex.Message}");
            }
        Logger.Log("usb monitor completed");
    }


    private static async Task<Device[]> GetDevices()
    {
        List<Device> devices = new List<Device>();
        string[] lines = (await CommandExecuter.SendCommandAsync("devices -l")).Split('\n');
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