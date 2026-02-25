using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ghostty.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private string _title = "Ghostty";

    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
