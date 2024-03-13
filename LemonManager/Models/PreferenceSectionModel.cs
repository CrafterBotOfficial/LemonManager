using DynamicData;
using System.Collections.ObjectModel;

namespace LemonManager.Models;

public class PreferenceSectionModel
{
    public string Name { get; set; }
    public ObservableCollection<PreferenceEntryModel> Entries { get; set; } = new ObservableCollection<PreferenceEntryModel>();

    public PreferenceSectionModel(string name, PreferenceEntryModel[] entries)
    {
        Name = name;
        Entries.AddRange(entries);
    }
}