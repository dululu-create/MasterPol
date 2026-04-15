using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MasterPol4.Views
{
    public partial class WarehouseWindow : Page
    {
        private string connectionString = "data source=DESKTOP-L8QU7O1\\SQLEXPRESS;initial catalog=MasterPol;integrated security=True;";
        private User currentUser;
        private List<dynamic> allItems;

        public WarehouseWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            LoadWarehouse();
        }

        private void LoadWarehouse()
        {
            try
            {
                var items = new List<dynamic>();
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT ItemID, ItemType, ItemName, Quantity, Unit, Location FROM Warehouse ORDER BY ItemType, ItemName";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        items.Add(new
                        {
                            ItemID = reader["ItemID"],
                            ItemType = reader["ItemType"],
                            ItemName = reader["ItemName"],
                            Quantity = reader["Quantity"],
                            Unit = reader["Unit"],
                            Location = reader["Location"]
                        });
                    }
                }
                allItems = items.Cast<dynamic>().ToList();
                dgWarehouse.ItemsSource = items;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterWarehouse()
        {
            string search = txtSearch.Text?.ToLower() ?? "";
            if (string.IsNullOrWhiteSpace(search))
            {
                dgWarehouse.ItemsSource = allItems;
                return;
            }
            var filtered = allItems.Where(i =>
                i.ItemName?.ToLower().Contains(search) == true ||
                i.ItemType?.ToLower().Contains(search) == true ||
                i.Location?.ToLower().Contains(search) == true).ToList();
            dgWarehouse.ItemsSource = filtered;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => FilterWarehouse();
        private void BtnSearch_Click(object sender, RoutedEventArgs e) => FilterWarehouse();
        private void BtnReset_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; FilterWarehouse(); }
    }
}