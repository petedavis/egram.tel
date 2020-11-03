using System;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Tel.Egram.Model.Application;
using Tel.Egram.Views.Application;

namespace Tel.Egram.Application
{
    public class MainApplication : Avalonia.Application
    {
        public event EventHandler Initializing;

        public event EventHandler Disposing;
        
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            
            Initializing?.Invoke(this, null);
        }
        
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowModel(),
                };
                
                desktop.Exit += (s, a) => Disposing?.Invoke(this, EventArgs.Empty);
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
