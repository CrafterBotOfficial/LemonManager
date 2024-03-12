using LemonManager.ViewModels;
using ReactiveUI;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace LemonManager.Models;

public class GameControlButtonModel : INotifyPropertyChanged
{
    public delegate void OnTogglePress(bool enabled);
    public delegate bool ToggleHandler();

    public ICommand OnPressCommand { get; set; }

    public string Name => Enabled ? enabledName : disabledName;
    private string enabledName;
    private string disabledName;

    public bool Enabled;

    public GameControlButtonModel(string name, Action onPress)
    {
        OnPressCommand = ReactiveCommand.Create(onPress);
        disabledName = name;
    }

    /// <summary> Toggle button, on press invokes toggle handler button enabled/disabled = result of toggle handler</summary>
    public GameControlButtonModel(string enabledName, string disabledName, ToggleHandler toggleHandler)
    {
        this.enabledName = enabledName;
        this.disabledName = disabledName;
        OnPressCommand = ReactiveCommand.Create(() =>
        {
            Enabled = toggleHandler();
            OnPropertyChanged("Name");
        });
    }

    /// <summary> Toggle button</summary>
    public GameControlButtonModel(string enabledName, string disabledName, OnTogglePress onToggle)
    {
        this.enabledName = enabledName;
        this.disabledName = disabledName;
        OnPressCommand = ReactiveCommand.Create(() =>
        {
            Enabled = !Enabled;
            OnPropertyChanged("Name");
            onToggle(Enabled);
        });
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}