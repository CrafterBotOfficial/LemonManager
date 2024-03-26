using System.Collections.Generic;

namespace LemonManager.ModManager.MelonPreferences.Models
{

    public class MelonPreferenceSection
    {
        public string Name { get; }
        public Dictionary<string, (string value, string comment)> Values { get; set; }

        internal MelonPreferenceSection(string name)
        {
            Name = name;
            Values = new();
        }
    }
}