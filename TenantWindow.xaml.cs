using Microsoft.Data.Sqlite;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace HollywoodHostelsPaymentSystem
{
    public partial class TenantWindow : Window
    {
        private string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "payments.db");
        private ObservableCollection<Tenant> tenants = new ObservableCollection<Tenant>();
        private ICollectionView tenantView;

        public TenantWindow()
        {
            InitializeComponent();
            dgTenants.ItemsSource = tenants;

            // Setup CollectionView for filtering
            tenantView = CollectionViewSource.GetDefaultView(dgTenants.ItemsSource);
            tenantView.Filter = TenantFilter;

            LoadTenants();
        }

        private void LoadTenants()
        {
            tenants.Clear();
            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();

                using (var pragma = conn.CreateCommand())
                {
                    pragma.CommandText = "PRAGMA foreign_keys = ON;";
                    pragma.ExecuteNonQuery();
                }

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT RoomNo, Name, Phone, MonthlyFee, HolidayFee 
                    FROM Rooms 
                    ORDER BY RoomNo";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tenants.Add(new Tenant
                        {
                            RoomNo = reader.GetString(0),
                            Name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            Phone = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            MonthlyFee = reader.IsDBNull(3) ? 0 : reader.GetDouble(3),
                            HolidayFee = reader.IsDBNull(4) ? 0 : reader.GetDouble(4)
                        });
                    }
                }
            }
            tenantView.Refresh();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRoomNo.Text) ||
                string.IsNullOrWhiteSpace(txtName.Text) ||
                string.IsNullOrWhiteSpace(txtPhone.Text) ||
                string.IsNullOrWhiteSpace(txtMonthlyFee.Text) ||
                string.IsNullOrWhiteSpace(txtHolidayFee.Text))
            {
                MessageBox.Show("Please fill all fields before adding a tenant.");
                return;
            }

            if (!double.TryParse(txtMonthlyFee.Text, out double monthlyFee) ||
                !double.TryParse(txtHolidayFee.Text, out double holidayFee))
            {
                MessageBox.Show("Monthly Fee and Holiday Fee must be numbers.");
                return;
            }

            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();

                using (var pragma = conn.CreateCommand())
                {
                    pragma.CommandText = "PRAGMA foreign_keys = ON;";
                    pragma.ExecuteNonQuery();
                }

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Rooms (RoomNo, Name, Phone, MonthlyFee, HolidayFee) 
                    VALUES (@room, @name, @phone, @monthly, @holiday)";
                cmd.Parameters.AddWithValue("@room", txtRoomNo.Text);
                cmd.Parameters.AddWithValue("@name", txtName.Text);
                cmd.Parameters.AddWithValue("@phone", txtPhone.Text);
                cmd.Parameters.AddWithValue("@monthly", monthlyFee);
                cmd.Parameters.AddWithValue("@holiday", holidayFee);

                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error adding tenant: " + ex.Message);
                }
            }
            LoadTenants();
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (dgTenants.SelectedItem is Tenant selected)
            {
                if (!double.TryParse(txtMonthlyFee.Text, out double monthlyFee) ||
                    !double.TryParse(txtHolidayFee.Text, out double holidayFee))
                {
                    MessageBox.Show("Monthly Fee and Holiday Fee must are numbers.");
                    return;
                }

                using (var conn = new SqliteConnection($"Data Source={dbPath}"))
                {
                    conn.Open();

                    using (var pragma = conn.CreateCommand())
                    {
                        pragma.CommandText = "PRAGMA foreign_keys = ON;";
                        pragma.ExecuteNonQuery();
                    }

                    var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        UPDATE Rooms 
                        SET Name=@name, Phone=@phone, MonthlyFee=@monthly, HolidayFee=@holiday 
                        WHERE RoomNo=@room";
                    cmd.Parameters.AddWithValue("@name", txtName.Text);
                    cmd.Parameters.AddWithValue("@phone", txtPhone.Text);
                    cmd.Parameters.AddWithValue("@monthly", monthlyFee);
                    cmd.Parameters.AddWithValue("@holiday", holidayFee);
                    cmd.Parameters.AddWithValue("@room", selected.RoomNo);

                    cmd.ExecuteNonQuery();
                }
                LoadTenants();
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgTenants.SelectedItem is Tenant selected)
            {
                using (var conn = new SqliteConnection($"Data Source={dbPath}"))
                {
                    conn.Open();

                    using (var pragma = conn.CreateCommand())
                    {
                        pragma.CommandText = "PRAGMA foreign_keys = ON;";
                        pragma.ExecuteNonQuery();
                    }

                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "DELETE FROM Rooms WHERE RoomNo=@room";
                    cmd.Parameters.AddWithValue("@room", selected.RoomNo);
                    cmd.ExecuteNonQuery();
                }
                LoadTenants();
            }
        }

        private void DgTenants_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (dgTenants.SelectedItem is Tenant selected)
            {
                txtRoomNo.Text = selected.RoomNo;
                txtName.Text = selected.Name;
                txtPhone.Text = selected.Phone;
                txtMonthlyFee.Text = selected.MonthlyFee.ToString();
                txtHolidayFee.Text = selected.HolidayFee.ToString();
            }
        }

        // --- Search Filter Logic ---
        private bool TenantFilter(object item)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text)) return true;
            var t = item as Tenant;
            var q = txtSearch.Text.ToLower();
            return (t.RoomNo?.ToLower().Contains(q) ?? false) || (t.Name?.ToLower().Contains(q) ?? false);
        }

        private void TxtSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            tenantView.Refresh();
        }

        // --- Export CSV ---
        private void BtnExportCsv_Tenants_Click(object sender, RoutedEventArgs e)
        {
            var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "tenants.csv");
            ExportToCsv(tenants, file);
            MessageBox.Show($"Exported to {file}");
        }

        public static void ExportToCsv<T>(IEnumerable<T> list, string filepath)
        {
            using (var writer = new StreamWriter(filepath))
            {
                var props = typeof(T).GetProperties();
                // header
                writer.WriteLine(string.Join(",", props.Select(p => p.Name)));
                foreach (var item in list)
                {
                    var vals = props.Select(p => p.GetValue(item)?.ToString()?.Replace(",", " ") ?? "");
                    writer.WriteLine(string.Join(",", vals));
                }
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

    public class Tenant
    {
        public string RoomNo { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public double MonthlyFee { get; set; }
        public double HolidayFee { get; set; }
    }
}