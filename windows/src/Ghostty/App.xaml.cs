using System.Windows;
using Wpf.Ui.Appearance;

namespace Ghostty;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Apply the system theme
        ApplicationThemeManager.Apply(ApplicationTheme.Dark);
    }
}
