using LemonManager.Models;
using LemonManager.ModManager;
using LemonManager.ModManager.AndroidDebugBridge;
using LemonManager.ModManager.MelonPreferences;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace LemonManager.ViewModels
{

    public class PreferenceEditorViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private MelonPreferenceFileManager manager;

        public bool FileExists => Entries.Count > 0;
        public ObservableCollection<PreferenceSectionModel> Entries { get; set; } = new ObservableCollection<PreferenceSectionModel>();
        public ICommand SaveCommand { get; }
        public ICommand RefreshEntriesCommand { get; }
        public ICommand DeleteFileCommand { get; }

        public PreferenceEditorViewModel()
        {
            SaveCommand = ReactiveCommand.Create(() =>
            {
                for (int sectionIndex = 0; sectionIndex < Entries.Count; sectionIndex++)
                {
                    var section = Entries[sectionIndex];
                    foreach (var entry in section.Entries)
                    {
                        string oldValue = manager.Sections[sectionIndex].Values[entry.Name].value;
                        if (entry.Value == oldValue) continue;
                        Logger.Log($"Setting {section.Name}:{entry.Name} from {oldValue} to {entry.Value}");
                        manager.SetValue(entry.Name, entry.Value);
                    }
                }
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
            Entries.Clear();

            foreach (var section in manager.Sections)
            {
                PreferenceEntryModel[] entries = new PreferenceEntryModel[section.Values.Count];
                for (int i = 0; i < section.Values.Count; i++)
                {
                    var entry = section.Values.ElementAt(i);
                    entries[i] = new(entry.Key.Trim(), entry.Value.comment.Trim(), entry.Value.value.Trim());
                }

                Entries.Add(new PreferenceSectionModel(section.Name, entries));
            }
            this.RaisePropertyChanged(nameof(FileExists));
        }
    }
}