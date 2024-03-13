using LemonManager.Models;
using LemonManager.ModManager;
using LemonManager.ModManager.MelonPreferences;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace LemonManager.ViewModels;

public class PreferenceEditorViewModel : ViewModelBase
{
    private MelonPreferenceFileManager manager;

    public ObservableCollection<PreferenceSectionModel> Entries { get; set; } = new ObservableCollection<PreferenceSectionModel>();
    public ICommand SaveCommand { get; }

    public PreferenceEditorViewModel()
    {
        SaveCommand = ReactiveCommand.Create(() =>
        {
            foreach (var section in Entries)
                foreach (var entry in section.Entries)
                {
                    Logger.Log($"Setting {section.Name}:{entry.Name} to {entry.Value}");
                    manager.SetValue(entry.Name, entry.Value);
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
                entries[i] = new(entry.Key, entry.Value.comment, entry.Value.value);
            }

            Entries.Add(new PreferenceSectionModel(section.Name, entries));
        }
    }
}