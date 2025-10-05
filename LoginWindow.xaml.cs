using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Windows;

namespace HollywoodHostelsPaymentSystem
{
    public partial class LoginWindow : Window
    {
        private string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "payments.db");

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both username and password.",
                                "Missing details", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT PasswordHash, Role FROM Users WHERE Username=@u";
            cmd.Parameters.AddWithValue("@u", username);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string storedHash = reader.GetString(0);
                string role = reader.GetString(1);

                if (BCrypt.Net.BCrypt.Verify(password, storedHash))
                {
                    // Store session info
                    App.Current.Properties["UserRole"] = role;
                    App.Current.Properties["Username"] = username;

                    this.DialogResult = true; // ✅ Tell App.xaml.cs login succeeded
                    this.Close();
                    return;
                }
            }

            MessageBox.Show("Invalid username or password.",
                            "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // cancel login
            this.Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // also cancel
            this.Close();
        }
    }
}
