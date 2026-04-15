using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MasterPol4.Views
{
    public partial class ProductsWindow : Page
    {
        private string connectionString = "data source=DESKTOP-L8QU7O1\\SQLEXPRESS;initial catalog=MasterPol;integrated security=True;";
        private User currentUser;
        private List<dynamic> allProducts;
        private dynamic editingProduct;
        public bool IsAdmin => currentUser?.Role == "Admin";

        public ProductsWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            btnAdd.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            LoadProducts();
        }

        private void LoadProducts()
        {
            try
            {
                var products = new List<dynamic>();
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT ProductID, ProductName, Article, MinPartnerPrice FROM Products ORDER BY ProductName";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        products.Add(new
                        {
                            ProductID = reader["ProductID"],
                            ProductName = reader["ProductName"],
                            Article = reader["Article"],
                            MinPartnerPrice = reader["MinPartnerPrice"]
                        });
                    }
                }
                allProducts = products.Cast<dynamic>().ToList();
                dgProducts.ItemsSource = products;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterProducts()
        {
            string search = txtSearch.Text?.ToLower() ?? "";
            if (string.IsNullOrWhiteSpace(search))
            {
                dgProducts.ItemsSource = allProducts;
                return;
            }
            var filtered = allProducts.Where(p =>
                (p.ProductName?.ToLower().Contains(search) == true) ||
                (p.Article?.ToString().Contains(search) == true)).ToList();
            dgProducts.ItemsSource = filtered;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => FilterProducts();
        private void BtnSearch_Click(object sender, RoutedEventArgs e) => FilterProducts();
        private void BtnReset_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; FilterProducts(); }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin) { ShowAccessDenied(); return; }
            editingProduct = null;
            EditTitle.Text = "Добавление продукта";
            txtProductName.Text = "";
            txtArticle.Text = "";
            txtPrice.Text = "";
            EditPanel.Visibility = Visibility.Visible;
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin) { ShowAccessDenied(); return; }
            Button btn = sender as Button;
            editingProduct = btn?.Tag;
            if (editingProduct == null) return;

            EditTitle.Text = "Редактирование продукта";
            txtProductName.Text = editingProduct.ProductName;
            txtArticle.Text = editingProduct.Article.ToString();
            txtPrice.Text = editingProduct.MinPartnerPrice.ToString();
            EditPanel.Visibility = Visibility.Visible;
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin) { ShowAccessDenied(); return; }
            Button btn = sender as Button;
            dynamic product = btn?.Tag;
            if (product == null) return;

            MessageBoxResult result = MessageBox.Show($"Удалить продукт '{product.ProductName}'?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = "DELETE FROM Products WHERE ProductID = @id";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", product.ProductID);
                        cmd.ExecuteNonQuery();
                    }
                    LoadProducts();
                    FilterProducts();
                    MessageBox.Show("Продукт удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProductName.Text))
            {
                MessageBox.Show("Введите наименование продукта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int article;
            if (!int.TryParse(txtArticle.Text, out article))
            {
                MessageBox.Show("Введите корректный артикул", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal price;
            if (!decimal.TryParse(txtPrice.Text, out price))
            {
                MessageBox.Show("Введите корректную цену", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    if (editingProduct == null)
                    {
                        string query = "INSERT INTO Products (ProductName, Article, MinPartnerPrice) VALUES (@name, @article, @price)";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@name", txtProductName.Text.Trim());
                        cmd.Parameters.AddWithValue("@article", article);
                        cmd.Parameters.AddWithValue("@price", price);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Продукт добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        string query = "UPDATE Products SET ProductName=@name, Article=@article, MinPartnerPrice=@price WHERE ProductID=@id";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", editingProduct.ProductID);
                        cmd.Parameters.AddWithValue("@name", txtProductName.Text.Trim());
                        cmd.Parameters.AddWithValue("@article", article);
                        cmd.Parameters.AddWithValue("@price", price);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Данные обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                EditPanel.Visibility = Visibility.Collapsed;
                LoadProducts();
                FilterProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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