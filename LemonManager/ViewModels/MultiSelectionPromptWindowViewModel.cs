using Avalonia.Controls;
using DynamicData;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace LemonManager.ViewModels
{

    public class MultiSelectionPromptWindowViewModel : ViewModelBase
    {
        public string Title { get; set; }
        public ObservableCollection<string> Options { get; set; } = new ObservableCollection<string>();
        public ICommand SubmitCommand { get; set; }
        public int SelectedIndex { get; set; }

        public Window Window;

        public MultiSelectionPromptWindowViewModel(Window window, string title, params string[] options)
        {
            Title = title;
            Window = window;
            Options.AddRange(options);
            SubmitCommand = ReactiveCommand.Create(() =>
            {
                window.Close(SelectedIndex);
            });
        }
    }
}