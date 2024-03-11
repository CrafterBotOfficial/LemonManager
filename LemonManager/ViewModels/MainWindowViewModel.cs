using Avalonia;
using LemonManager.ModManager;

namespace LemonManager.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public string Greeting => "Welcome to Avalonia!";

        public static MainWindowViewModel Instance;

        public ApplicationManager ApplicationManager;

        public MainWindowViewModel()
        {
            Instance = this;
            
        }
    }
}
