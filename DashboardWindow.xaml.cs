using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents; // for FlowDocument
using System.Windows.Input;
using System.Windows.Media;

// PDF library
using iTextSharp.text; // still keep for PDF
using iTextSharp.text.pdf;

namespace HollywoodHostelsPaymentSystem
{
    public partial class DashboardWindow : Window
    {
        private string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "payments.db");
        private ObservableCollection<UnpaidRecord> unpaidList = new ObservableCollection<UnpaidRecord>();

        private string[] months = { "September", "October", "November", "December",
                                    "January", "February", "March", "April",
                                    "May", "June", "July", "August" };

        public DashboardWindow()
        {
            InitializeComponent();
            dgUnpaid.ItemsSource = unpaidList;
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            unpaidList.Clear();
            int currentYear = DateTime.Now.Year;

            int totalTenants = 0;
            int totalCollected = 0;
            int totalOutstanding = 0;

            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();

                // Get all tenants
                var tenantCmd = conn.CreateCommand();
                tenantCmd.CommandText = "SELECT RoomNo, Name FROM Rooms";
                using (var reader = tenantCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        totalTenants++;
                        string roomNo = reader.GetString(0);
                        string name = reader.GetString(1);

                        // Expected payments
                        var expected = new Dictionary<string, int>();
                        foreach (var m in months)
                            expected[m] = GetFeeForMonth(m);

                        // Get actual payments
                        var paymentCmd = conn.CreateCommand();
                        paymentCmd.CommandText = "SELECT Month, AmountPaid FROM Payments WHERE RoomNo=@room AND Year=@year";
                        paymentCmd.Parameters.AddWithValue("@room", roomNo);
                        paymentCmd.Parameters.AddWithValue("@year", currentYear);

                        using (var payReader = paymentCmd.ExecuteReader())
                        {
                            while (payReader.Read())
                            {
                                string paidMonth = payReader.GetString(0);
                                int amountPaid = payReader.GetInt32(1);

                                expected[paidMonth] -= amountPaid;
                                if (expected[paidMonth] < 0) expected[paidMonth] = 0;
                                totalCollected += amountPaid;
                            }
                        }

                        // Find unpaid months
                        var unpaidMonths = expected.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
                        int balanceDue = expected.Values.Sum();
                        totalOutstanding += balanceDue;

                        if (unpaidMonths.Count > 0)
                        {
                            unpaidList.Add(new UnpaidRecord
                            {
                                RoomNo = roomNo,
                                Name = name,
                                UnpaidMonths = string.Join(", ", unpaidMonths),
                                BalanceDue = balanceDue
                            });
                        }
                    }
                }
            }

            // Update summary
            txtTotalTenants.Text = $"Total Tenants: {totalTenants}";
            txtTotalCollected.Text = $"Collected: Ksh {totalCollected}";
            txtOutstanding.Text = $"Outstanding: Ksh {totalOutstanding}";
        }

        private int GetFeeForMonth(string month)
        {
            if (month == "September" || month == "October" || month == "November" || month == "December")
                return 3000;
            if (month == "January" || month == "February" || month == "March" || month == "April")
                return 3000;
            return 1000; // May–Aug
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                dgUnpaid.SelectAllCells();
                dgUnpaid.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
                ApplicationCommands.Copy.Execute(null, dgUnpaid);
                dgUnpaid.UnselectAllCells();

                string result = (string)Clipboard.GetData(DataFormats.Text);

                // ✅ Explicitly reference WPF Paragraph
                FlowDocument doc = new FlowDocument(new System.Windows.Documents.Paragraph(new Run(result)))
                {
                    PagePadding = new Thickness(50),
                    FontSize = 12
                };
                doc.PageWidth = printDialog.PrintableAreaWidth;

                printDialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Unpaid Tenants Report");
            }
        }

        private void BtnExportPDF_Click(object sender, RoutedEventArgs e)
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Unpaid_Tenants_Report.pdf");

            Document pdfDoc = new Document(PageSize.A4, 50, 50, 25, 25);
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();

                // ✅ Explicitly reference iTextSharp Paragraph
                pdfDoc.Add(new iTextSharp.text.Paragraph("Unpaid Tenants Report")
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 10
                });

                // Table
                PdfPTable table = new PdfPTable(4);
                table.AddCell("Room No");
                table.AddCell("Name");
                table.AddCell("Unpaid Months");
                table.AddCell("Balance Due");

                foreach (var record in unpaidList)
                {
                    table.AddCell(record.RoomNo);
                    table.AddCell(record.Name);
                    table.AddCell(record.UnpaidMonths);
                    table.AddCell("Ksh " + record.BalanceDue);
                }

                pdfDoc.Add(table);
                pdfDoc.Close();
            }

            MessageBox.Show("PDF Exported to Desktop Successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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

    public class UnpaidRecord
    {
        public string RoomNo { get; set; }
        public string Name { get; set; }
        public string UnpaidMonths { get; set; }
        public int BalanceDue { get; set; }
    }
}