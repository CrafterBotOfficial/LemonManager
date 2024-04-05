using LemonManager.ModManager;
using LemonManager.ModManager.AndroidDebugBridge;
using LemonManager.ModManager.MelonPreferences;
using ReactiveUI;
using System;
using System.ComponentModel;
using System.Windows.Input;

namespace LemonManager.ViewModels
{
    public class PreferenceEditorViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private MelonPreferenceFileManager manager;

        public bool FileExists => Text != string.Empty;
        public string Text { get; set; }
        public ICommand SaveCommand { get; }
        public ICommand RefreshEntriesCommand { get; }
        public ICommand DeleteFileCommand { get; }

        public PreferenceEditorViewModel()
        {
            SaveCommand = ReactiveCommand.Create(async () =>
            {
                await manager.SaveText(Text);
                MainWindowViewModel.IsLoading = false;
            });
            RefreshEntriesCommand = ReactiveCommand.Create(() =>
            {
                Init(MainWindowViewModel.Instance.ApplicationManager.Info.Id);
            });
            DeleteFileCommand = ReactiveCommand.Create(() =>
            {
                try
                {
                    DeviceManager.SendShellCommand($"rm -f \"{manager.RemotePath}\"");
                    RefreshEntriesCommand.Execute(null);
                }
                catch (Exception ex)
                {
                    PromptHandler.Instance.PromptUser("Failed Deleting File", "Couldn't delete remote config file: " + ex.Message);
                }
            });
        }

        public void Init(string appId)
        {
            string remotePath = string.Format(FilePaths.RemoteApplicationDataPath, appId) + "/UserData/MelonPreferences.cfg";

            manager = new MelonPreferenceFileManager(remotePath);
            Text = manager.RawText;
            this.RaisePropertyChanged(nameof(Text));

            this.RaisePropertyChanged(nameof(FileExists));
        }
    }
}
