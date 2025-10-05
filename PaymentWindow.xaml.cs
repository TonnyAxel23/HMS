using Microsoft.Data.Sqlite;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace HollywoodHostelsPaymentSystem
{
    public partial class PaymentWindow : Window
    {
        private string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "payments.db");
        private ObservableCollection<PaymentRecord> payments = new ObservableCollection<PaymentRecord>();
        private ObservableCollection<PaymentRecord> allPayments = new ObservableCollection<PaymentRecord>();

        public PaymentWindow()
        {
            InitializeComponent();
            dgPayments.ItemsSource = payments;
            LoadTenants();
        }

        // Load tenants into dropdown
        private void LoadTenants()
        {
            cbTenant.Items.Clear();
            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT RoomNo, Name FROM Rooms ORDER BY RoomNo";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cbTenant.Items.Add(new { RoomNo = reader.GetString(0), Name = reader.GetString(1) });
                    }
                }
            }
        }

        // Auto-fill amount based on month selection and tenant's fee settings
        private void CbMonth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbTenant.SelectedItem != null && cbMonth.SelectedItem is ComboBoxItem selected)
            {
                string month = selected.Content.ToString();
                var tenant = cbTenant.SelectedItem;
                string roomNo = (string)tenant.GetType().GetProperty("RoomNo").GetValue(tenant, null);
                txtAmount.Text = GetFeeForTenant(roomNo, month).ToString();
            }
        }

        // Get fee from DB depending on month type
        private double GetFeeForTenant(string roomNo, string month)
        {
            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT MonthlyFee, HolidayFee FROM Rooms WHERE RoomNo=@room";
                cmd.Parameters.AddWithValue("@room", roomNo);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        double monthlyFee = reader.IsDBNull(0) ? 0 : reader.GetDouble(0);
                        double holidayFee = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);

                        if (month == "September" || month == "October" || month == "November" || month == "December" ||
                            month == "January" || month == "February" || month == "March" || month == "April")
                        {
                            return monthlyFee;
                        }
                        else
                        {
                            return holidayFee;
                        }
                    }
                }
            }
            return 0;
        }

        // Record a payment
        private void BtnRecord_Click(object sender, RoutedEventArgs e)
        {
            if (cbTenant.SelectedItem == null || cbMonth.SelectedItem == null || string.IsNullOrWhiteSpace(txtYear.Text) || string.IsNullOrWhiteSpace(txtAmount.Text))
            {
                MessageBox.Show("Please fill all fields.");
                return;
            }

            var tenant = cbTenant.SelectedItem;
            string roomNo = (string)tenant.GetType().GetProperty("RoomNo").GetValue(tenant, null);
            string month = ((ComboBoxItem)cbMonth.SelectedItem).Content.ToString();

            if (!int.TryParse(txtYear.Text, out int year) || !double.TryParse(txtAmount.Text, out double amount))
            {
                MessageBox.Show("Year and Amount must be valid numbers.");
                return;
            }

            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();

                // Prevent duplicate entry
                var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(*) FROM Payments WHERE RoomNo=@room AND Month=@month AND Year=@year";
                checkCmd.Parameters.AddWithValue("@room", roomNo);
                checkCmd.Parameters.AddWithValue("@month", month);
                checkCmd.Parameters.AddWithValue("@year", year);

                long count = (long)checkCmd.ExecuteScalar();
                if (count > 0)
                {
                    MessageBox.Show("Payment for this month and year already exists for this tenant.");
                    return;
                }

                // Insert payment
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO Payments (RoomNo, Month, Year, AmountPaid) VALUES (@room, @month, @year, @amount)";
                cmd.Parameters.AddWithValue("@room", roomNo);
                cmd.Parameters.AddWithValue("@month", month);
                cmd.Parameters.AddWithValue("@year", year);
                cmd.Parameters.AddWithValue("@amount", amount);

                try
                {
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Payment recorded successfully.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }

            LoadPaymentsForTenant(roomNo);
        }

        // Load all payments for the selected tenant
        private void LoadPaymentsForTenant(string roomNo)
        {
            payments.Clear();
            allPayments.Clear();

            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT p.Month, p.Year, p.AmountPaid, r.Name
                    FROM Payments p
                    JOIN Rooms r ON p.RoomNo = r.RoomNo
                    WHERE p.RoomNo=@room
                    ORDER BY p.Year, p.Month";
                cmd.Parameters.AddWithValue("@room", roomNo);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var record = new PaymentRecord
                        {
                            TenantName = reader.GetString(3),
                            Month = reader.GetString(0),
                            Year = reader.GetInt32(1),
                            AmountPaid = reader.GetDouble(2)
                        };
                        payments.Add(record);
                        allPayments.Add(record);
                    }
                }
            }
        }

        // Filter payments by search box
        private void TxtPaymentSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = txtPaymentSearch.Text.ToLower();
            payments.Clear();

            foreach (var item in allPayments.Where(p =>
                p.Month.ToLower().Contains(searchText) ||
                p.Year.ToString().Contains(searchText)))
            {
                payments.Add(item);
            }
        }

        // Export payments to CSV
        private void BtnExportCsv_Payments_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = "PaymentsExport.csv"
            };

            if (dlg.ShowDialog() == true)
            {
                StringBuilder csv = new StringBuilder();
                csv.AppendLine("Tenant,Month,Year,AmountPaid");

                foreach (var p in payments)
                {
                    csv.AppendLine($"{p.TenantName},{p.Month},{p.Year},{p.AmountPaid}");
                }

                File.WriteAllText(dlg.FileName, csv.ToString());
                MessageBox.Show("Payments exported successfully.");
            }
        }

        // Back to Main Menu
        private void BackToMainMenu_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // The Closed event handler in MainWindow will handle showing the main window
        }
    }

    public class PaymentRecord
    {
        public string TenantName { get; set; }
        public string Month { get; set; }
        public int Year { get; set; }
        public double AmountPaid { get; set; }
    }
}