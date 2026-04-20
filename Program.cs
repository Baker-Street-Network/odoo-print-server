using Microsoft.Win32;
using Velopack;

namespace OdooPrintServer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Velopack: Handle installation/uninstallation/update events
            VelopackApp.Build().Run();

            // Register in Windows startup so the app launches on every boot
            AddToStartup();

            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }

        public static void AddToStartup()
        {
            string appName = "OdooPrintServer";
            string appPath = Environment.ProcessPath ?? Application.ExecutablePath;

            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (key is null) return;

            string? existing = key.GetValue(appName) as string;
            if (existing != $"\"{appPath}\"")
            {
                key.SetValue(appName, $"\"{appPath}\"");
            }
        }
    }
}
