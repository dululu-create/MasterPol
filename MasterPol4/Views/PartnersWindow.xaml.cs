using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MasterPol4.Views
{
    public partial class PartnersWindow : Page
    {
        private string connectionString = "data source=DESKTOP-L8QU7O1\\SQLEXPRESS;initial catalog=MasterPol;integrated security=True;";
        private User currentUser;
        private List<dynamic> allPartners;
        private dynamic editingPartner;
        public bool IsAdmin => currentUser?.Role == "Admin";

        public PartnersWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            btnAdd.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            LoadPartners();
        }

        private void LoadPartners()
        {
            try
            {
                var partners = new List<dynamic>();
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT PartnerID, PartnerType, PartnerName, Director, Phone, Rating, Email, LegalAddress, INN FROM Partners ORDER BY PartnerName";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        partners.Add(new
                        {
                            PartnerID = reader["PartnerID"],
                            PartnerType = reader["PartnerType"],
                            PartnerName = reader["PartnerName"],
                            Director = reader["Director"],
                            Phone = reader["Phone"],
                            Rating = reader["Rating"],
                            Email = reader["Email"],
                            LegalAddress = reader["LegalAddress"],
                            INN = reader["INN"]
                        });
                    }
                }
                allPartners = partners.Cast<dynamic>().ToList();
                dgPartners.ItemsSource = partners;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterPartners()
        {
            string search = txtSearch.Text?.ToLower() ?? "";
            if (string.IsNullOrWhiteSpace(search))
            {
                dgPartners.ItemsSource = allPartners;
                return;
            }
            var filtered = allPartners.Where(p =>
                (p.PartnerName?.ToLower().Contains(search) == true) ||
                (p.Director?.ToLower().Contains(search) == true) ||
                (p.Phone?.Contains(search) == true)).ToList();
            dgPartners.ItemsSource = filtered;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => FilterPartners();
        private void BtnSearch_Click(object sender, RoutedEventArgs e) => FilterPartners();
        private void BtnReset_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; FilterPartners(); }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin) { ShowAccessDenied(); return; }
            editingPartner = null;
            EditTitle.Text = "Добавление партнера";
            ClearEditForm();
            EditPanel.Visibility = Visibility.Visible;
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin) { ShowAccessDenied(); return; }
            Button btn = sender as Button;
            editingPartner = btn?.Tag;
            if (editingPartner == null) return;

            EditTitle.Text = "Редактирование партнера";
            txtPartnerType.Text = editingPartner.PartnerType;
            txtPartnerName.Text = editingPartner.PartnerName;
            txtDirector.Text = editingPartner.Director;
            txtEmail.Text = editingPartner.Email;
            txtPhone.Text = editingPartner.Phone;
            txtLegalAddress.Text = editingPartner.LegalAddress;
            txtINN.Text = editingPartner.INN;
            txtRating.Text = editingPartner.Rating.ToString();

            EditPanel.Visibility = Visibility.Visible;
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin) { ShowAccessDenied(); return; }
            Button btn = sender as Button;
            dynamic partner = btn?.Tag;
            if (partner == null) return;

            MessageBoxResult result = MessageBox.Show($"Удалить партнера '{partner.PartnerName}'?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = "DELETE FROM Partners WHERE PartnerID = @id";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", partner.PartnerID);
                        cmd.ExecuteNonQuery();
                    }
                    LoadPartners();
                    FilterPartners();
                    MessageBox.Show("Партнер удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPartnerName.Text))
            {
                MessageBox.Show("Введите наименование партнера", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int rating;
            if (!int.TryParse(txtRating.Text, out rating) || rating < 1 || rating > 10)
            {
                MessageBox.Show("Рейтинг должен быть числом от 1 до 10", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    if (editingPartner == null)
                    {
                        string query = @"INSERT INTO Partners (PartnerType, PartnerName, Director, Email, Phone, LegalAddress, INN, Rating) 
                                        VALUES (@type, @name, @director, @email, @phone, @address, @inn, @rating)";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@type", txtPartnerType.Text.Trim());
                        cmd.Parameters.AddWithValue("@name", txtPartnerName.Text.Trim());
                        cmd.Parameters.AddWithValue("@director", txtDirector.Text.Trim());
                        cmd.Parameters.AddWithValue("@email", txtEmail.Text.Trim());
                        cmd.Parameters.AddWithValue("@phone", txtPhone.Text.Trim());
                        cmd.Parameters.AddWithValue("@address", txtLegalAddress.Text.Trim());
                        cmd.Parameters.AddWithValue("@inn", txtINN.Text.Trim());
                        cmd.Parameters.AddWithValue("@rating", rating);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Партнер добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Редактирование
                        string query = @"UPDATE Partners SET PartnerType=@type, PartnerName=@name, Director=@director, 
                                        Email=@email, Phone=@phone, LegalAddress=@address, INN=@inn, Rating=@rating 
                                        WHERE PartnerID=@id";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", editingPartner.PartnerID);
                        cmd.Parameters.AddWithValue("@type", txtPartnerType.Text.Trim());
                        cmd.Parameters.AddWithValue("@name", txtPartnerName.Text.Trim());
                        cmd.Parameters.AddWithValue("@director", txtDirector.Text.Trim());
                        cmd.Parameters.AddWithValue("@email", txtEmail.Text.Trim());
                        cmd.Parameters.AddWithValue("@phone", txtPhone.Text.Trim());
                        cmd.Parameters.AddWithValue("@address", txtLegalAddress.Text.Trim());
                        cmd.Parameters.AddWithValue("@inn", txtINN.Text.Trim());
                        cmd.Parameters.AddWithValue("@rating", rating);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Данные обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                EditPanel.Visibility = Visibility.Collapsed;
                LoadPartners();
                FilterPartners();
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

        private void ClearEditForm()
        {
            txtPartnerType.Text = "";
            txtPartnerName.Text = "";
            txtDirector.Text = "";
            txtEmail.Text = "";
            txtPhone.Text = "";
            txtLegalAddress.Text = "";
            txtINN.Text = "";
            txtRating.Text = "";
        }

        private void ShowAccessDenied()
        {
            MessageBox.Show("У вас нет прав на это действие", "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}