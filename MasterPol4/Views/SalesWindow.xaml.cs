using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MasterPol4.Views
{
    public partial class SalesWindow : Page
    {
        private string connectionString = "data source=DESKTOP-L8QU7O1\\SQLEXPRESS;initial catalog=MasterPol;integrated security=True;";
        private User currentUser;
        private List<dynamic> allSales;

        public SalesWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            LoadSales();
        }

        private void LoadSales()
        {
            try
            {
                var sales = new List<dynamic>();
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT p.PartnerName, pr.ProductName, ps.Quantity, ps.SaleDate, (ps.Quantity * pr.MinPartnerPrice) as TotalAmount
                        FROM PartnerSales ps
                        JOIN Partners p ON ps.PartnerID = p.PartnerID
                        JOIN Products pr ON ps.ProductID = pr.ProductID
                        ORDER BY ps.SaleDate DESC";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        sales.Add(new
                        {
                            PartnerName = reader["PartnerName"],
                            ProductName = reader["ProductName"],
                            Quantity = reader["Quantity"],
                            SaleDate = Convert.ToDateTime(reader["SaleDate"]),
                            TotalAmount = reader["TotalAmount"]
                        });
                    }
                }
                allSales = sales.Cast<dynamic>().ToList();
                dgSales.ItemsSource = sales;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterSales()
        {
            string search = txtSearch.Text?.ToLower() ?? "";
            if (string.IsNullOrWhiteSpace(search))
            {
                dgSales.ItemsSource = allSales;
                return;
            }
            var filtered = allSales.Where(s =>
                s.PartnerName?.ToLower().Contains(search) == true ||
                s.ProductName?.ToLower().Contains(search) == true).ToList();
            dgSales.ItemsSource = filtered;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => FilterSales();
        private void BtnSearch_Click(object sender, RoutedEventArgs e) => FilterSales();
        private void BtnReset_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; FilterSales(); }
    }
}