using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace HollywoodHostelsPaymentSystem
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Show splash screen first
            var splash = new SplashScreen();
            splash.Show();

            // Use a timer to close splash and show login after delay
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(4);
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                splash.Close();

                // Now show login window
                var login = new LoginWindow();
                bool? result = login.ShowDialog();

                if (result != true)
                {
                    Current.Shutdown(); // exit app if login fails/cancelled
                    return;
                }

                // Show Main Window
                var main = new MainWindow();
                main.Show();
                Current.MainWindow = main;
            };
            timer.Start();
        }
    }
}

