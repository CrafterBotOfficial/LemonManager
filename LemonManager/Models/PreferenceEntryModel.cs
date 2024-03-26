namespace LemonManager.Models
{

    public class PreferenceEntryModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Value { get; set; }

        public PreferenceEntryModel(string name, string description, string value)
        {
            Name = name;
            Description = description;
            Value = value;
        }
    }
}