using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MasterPol4.Views
{
    public partial class ProductionWindow : Page
    {
        private string connectionString = "data source=DESKTOP-L8QU7O1\\SQLEXPRESS;initial catalog=MasterPol;integrated security=True;";
        private User currentUser;
        private List<dynamic> allOrders;
        private List<dynamic> products;
        public bool IsAdmin => currentUser?.Role == "Admin";

        public ProductionWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            btnAdd.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            LoadProducts();
            LoadOrders();
        }

        private void LoadProducts()
        {
            try
            {
                products = new List<dynamic>();
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT ProductID, ProductName FROM Products ORDER BY ProductName";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        products.Add(new
                        {
                            ProductID = reader["ProductID"],
                            ProductName = reader["ProductName"]
                        });
                    }
                }
                cmbProduct.ItemsSource = products;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки продуктов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadOrders()
        {
            try
            {
                var orders = new List<dynamic>();
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT po.OrderID, p.ProductName, po.Quantity, po.OrderDate, po.CompletionDate, po.Status 
                                    FROM ProductionOrders po
                                    JOIN Products p ON po.ProductID = p.ProductID
                                    ORDER BY po.OrderDate DESC";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        orders.Add(new
                        {
                            OrderID = reader["OrderID"],
                            ProductName = reader["ProductName"],
                            Quantity = reader["Quantity"],
                            OrderDate = reader["OrderDate"],
                            CompletionDate = reader["CompletionDate"] == DBNull.Value ? null : (DateTime?)reader["CompletionDate"],
                            Status = reader["Status"]
                        });
                    }
                }
                allOrders = orders.Cast<dynamic>().ToList();
                dgOrders.ItemsSource = orders;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterOrders()
        {
            string search = txtSearch.Text?.ToLower() ?? "";
            if (string.IsNullOrWhiteSpace(search))
            {
                dgOrders.ItemsSource = allOrders;
                return;
            }
            var filtered = allOrders.Where(o => o.ProductName?.ToLower().Contains(search) == true).ToList();
            dgOrders.ItemsSource = filtered;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => FilterOrders();
        private void BtnSearch_Click(object sender, RoutedEventArgs e) => FilterOrders();
        private void BtnReset_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; FilterOrders(); }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin) { ShowAccessDenied(); return; }
            txtQuantity.Text = "";
            cmbProduct.SelectedIndex = -1;
            EditPanel.Visibility = Visibility.Visible;
        }

        private void BtnChangeStatus_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin) { ShowAccessDenied(); return; }
            Button btn = sender as Button;
            dynamic order = btn?.Tag;
            if (order == null) return;

            string[] statuses = { "Ожидание", "В работе", "Завершено" };
            string newStatus = statuses[(Array.IndexOf(statuses, order.Status) + 1) % statuses.Length];

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE ProductionOrders SET Status=@status, CompletionDate=@completion WHERE OrderID=@id";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", order.OrderID);
                    cmd.Parameters.AddWithValue("@status", newStatus);
                    cmd.Parameters.AddWithValue("@completion", newStatus == "Завершено" ? DateTime.Now : (object)DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
                LoadOrders();
                MessageBox.Show($"Статус заказа изменен на '{newStatus}'", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProduct.SelectedItem == null)
            {
                MessageBox.Show("Выберите продукт", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int quantity;
            if (!int.TryParse(txtQuantity.Text, out quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            dynamic selectedProduct = cmbProduct.SelectedItem;
            int productId = selectedProduct.ProductID;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "INSERT INTO ProductionOrders (ProductID, Quantity, OrderDate, Status) VALUES (@productId, @quantity, @date, 'Ожидание')";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@productId", productId);
                    cmd.Parameters.AddWithValue("@quantity", quantity);
                    cmd.Parameters.AddWithValue("@date", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }
                EditPanel.Visibility = Visibility.Collapsed;
                LoadOrders();
                MessageBox.Show("Заказ создан", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            EditPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowAccessDenied()
        {
            MessageBox.Show("У вас нет прав на это действие", "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}