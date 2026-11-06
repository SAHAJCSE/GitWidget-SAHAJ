using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;

namespace GitHubDesktopWidget
{
    public partial class App : Application
    {
        private static Mutex? _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
            {
                try { File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), ev.ExceptionObject?.ToString() ?? "Unknown crash"); } catch { }
            };
            DispatcherUnhandledException += (s, ev) =>
            {
                try { File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), ev.Exception?.ToString() ?? "Unknown dispatcher crash"); } catch { }
            };

            const string appName = "Local\\GitHubDesktopWidget_SingleInstance_Mutex";
            try
            {
                _mutex = new Mutex(true, appName, out bool createdNew);

                if (!createdNew)
                {
                    // Check if another instance is actually alive
                    var current = Process.GetCurrentProcess();
                    var running = Process.GetProcessesByName(current.ProcessName);
                    if (running.Length > 1)
                    {
                        Shutdown();
                        return;
                    }
                }
            }
            catch
            {
                // In case of AbandonedMutexException or access issues, continue safely
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
            catch { }
            base.OnExit(e);
        }
    }
}
