using System;
using System.IO;
using System.Windows;
using Microsoft.Data.Sqlite;

namespace HollywoodHostelsPaymentSystem
{
    public partial class MainWindow : Window
    {
        private string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "payments.db");

        public MainWindow()
        {
            InitializeComponent();

            // Ensure Users table exists and create default admin if none exists
            EnsureUsersTable();
            EnsureDefaultAdmin();

            // Apply role restrictions after successful login
            ApplyRoleRestrictions();
        }

        // Make sure Users table exists
        private void EnsureUsersTable()
        {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();

            var tableCmd = conn.CreateCommand();
            tableCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT UNIQUE,
                    PasswordHash TEXT,
                    Role TEXT
                );
            ";
            tableCmd.ExecuteNonQuery();
        }

        // Create a default admin if none exists
        private void EnsureDefaultAdmin()
        {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();

            var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM Users";
            long userCount = (long)checkCmd.ExecuteScalar();

            if (userCount == 0)
            {
                string defaultHash = BCrypt.Net.BCrypt.HashPassword("admin123");
                var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = "INSERT INTO Users (Username, PasswordHash, Role) VALUES (@u, @p, @r)";
                insertCmd.Parameters.AddWithValue("@u", "admin");
                insertCmd.Parameters.AddWithValue("@p", defaultHash);
                insertCmd.Parameters.AddWithValue("@r", "Admin");
                insertCmd.ExecuteNonQuery();
            }
        }

        // Apply role restrictions for non-admins
        private void ApplyRoleRestrictions()
        {
            var role = App.Current.Properties["UserRole"]?.ToString();
            if (role != "Admin")
            {
                // Example: restrict tenant management for non-admins
                if (btnManageTenants != null)
                    btnManageTenants.IsEnabled = false;
            }
        }

        // 🏠 Open tenants window
        private void OpenTenants_Click(object sender, RoutedEventArgs e)
        {
            var tenantWindow = new TenantWindow();
            tenantWindow.Owner = this; // Set main window as owner
            tenantWindow.Closed += (s, args) => this.Show(); // Show main window when tenant window closes
            tenantWindow.Show();
            this.Hide(); // Hide main window
        }

        // 💰 Open payments window
        private void OpenPayments_Click(object sender, RoutedEventArgs e)
        {
            var paymentWindow = new PaymentWindow();
            paymentWindow.Owner = this; // Set main window as owner
            paymentWindow.Closed += (s, args) => this.Show(); // Show main window when payment window closes
            paymentWindow.Show();
            this.Hide(); // Hide main window
        }

        // 📊 Open dashboard window
        private void OpenDashboard_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = new DashboardWindow();
            dashboardWindow.Owner = this; // Set main window as owner
            dashboardWindow.Closed += (s, args) => this.Show(); // Show main window when dashboard window closes
            dashboardWindow.Show();
            this.Hide(); // Hide main window
        }

        // Show main window method for child windows to call
        public void ShowMainWindow()
        {
            this.Show();
        }

        // Handle window closing to properly exit application
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }
    }
}