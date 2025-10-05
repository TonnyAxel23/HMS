using System;
using System.Threading.Tasks;
using System.Windows;

namespace HollywoodHostelsPaymentSystem
{
    public partial class SplashScreen : Window
    {
        public SplashScreen()
        {
            InitializeComponent();
            Loaded += SplashScreen_Loaded;
        }

        private async void SplashScreen_Loaded(object sender, RoutedEventArgs e)
        {
            // Wait 2 seconds (simulate loading)
            await Task.Delay(2000);

            // After splash, open login window
            LoginWindow login = new LoginWindow();
            login.Show();

            // Close splash
            this.Close();
        }
    }
}
