using System;
using System.Threading;
using System.Windows;

namespace GitHubDesktopWidget
{
    public partial class App : Application
    {
        private static Mutex? _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "GitHubDesktopWidget_SingleInstance_Mutex";
            _mutex = new Mutex(true, appName, out bool createdNew);

            if (!createdNew)
            {
                // App already running
                Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}
