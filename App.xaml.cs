using System;
using System.Windows;

namespace SkyrimAutoDiag
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Verhindert, dass WPF das Programm beendet, sobald sich der SplashScreen schließt!
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            try
            {
                var splash = new SplashScreenWindow();
                bool? result = splash.ShowDialog();

                if (result == true || splash.IsConfirmed)
                {
                    var main = new MainWindow();
                    
                    // Jetzt das echte Hauptfenster festlegen und normalen Shutdown-Modus aktivieren
                    MainWindow = main;
                    ShutdownMode = ShutdownMode.OnMainWindowClose;
                    
                    main.Show();
                }
                else
                {
                    Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kritischer Fehler: {ex.Message}\n{ex.StackTrace}");
                Shutdown();
            }
        }
    }
}