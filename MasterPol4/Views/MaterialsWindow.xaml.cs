using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MasterPol4.Views
{
    public partial class MaterialsWindow : Page
    {
        private string connectionString = "data source=DESKTOP-L8QU7O1\\SQLEXPRESS;initial catalog=MasterPol;integrated security=True;";
        private User currentUser;
        private List<dynamic> allMaterials;
        private dynamic editingMaterial;
        public bool IsAdmin => currentUser?.Role == "Admin";

        public MaterialsWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            btnAdd.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            LoadMaterials();
        }

        private void LoadMaterials()
        {
            try
            {
                var materials = new List<dynamic>();
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT MaterialID, MaterialName, DefectRate, Unit, CostPerUnit FROM Materials ORDER BY MaterialName";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        materials.Add(new
                        {
                            MaterialID = reader["MaterialID"],
                            MaterialName = reader["MaterialName"],
                            DefectRate = reader["DefectRate"],
                            Unit = reader["Unit"],
                            CostPerUnit = reader["CostPerUnit"]
                        });
                    }
                }
                allMaterials = materials.Cast<dynamic>().ToList();
                dgMaterials.ItemsSource = materials;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterMaterials()
        {
            string search = txtSearch.Text?.ToLower() ?? "";
            if (string.IsNullOrWhiteSpace(search))
            {
                dgMaterials.ItemsSource = allMaterials;
                return;
            }
            var filtered = allMaterials.Where(m => m.MaterialName?.ToLower().Contains(search) == true).ToList();
            dgMaterials.ItemsSource = filtered;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => FilterMaterials();
        private void BtnSearch_Click(object sender, RoutedEventArgs e) => FilterMaterials();
        private void BtnReset_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; FilterMaterials(); }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin) { ShowAccessDenied(); return; }
            editingMaterial = null;
            EditTitle.Text = "Добавление материала";
            txtMaterialName.Text = "";
            txtDefectRate.Text = "";
            txtUnit.Text = "м²";
            txtCostPerUnit.Text = "";
            EditPanel.Visibility = Visibility.Visible;
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin) { ShowAccessDenied(); return; }
            Button btn = sender as Button;
            editingMaterial = btn?.Tag;
            if (editingMaterial == null) return;

            EditTitle.Text = "Редактирование материала";
            txtMaterialName.Text = editingMaterial.MaterialName;
            txtDefectRate.Text = (editingMaterial.DefectRate * 100).ToString();
            txtUnit.Text = editingMaterial.Unit;
            txtCostPerUnit.Text = editingMaterial.CostPerUnit.ToString();
            EditPanel.Visibility = Visibility.Visible;
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin) { ShowAccessDenied(); return; }
            Button btn = sender as Button;
            dynamic material = btn?.Tag;
            if (material == null) return;

            MessageBoxResult result = MessageBox.Show($"Удалить материал '{material.MaterialName}'?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = "DELETE FROM Materials WHERE MaterialID = @id";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", material.MaterialID);
                        cmd.ExecuteNonQuery();
                    }
                    LoadMaterials();
                    MessageBox.Show("Материал удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMaterialName.Text))
            {
                MessageBox.Show("Введите наименование материала", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal defectRate;
            if (!decimal.TryParse(txtDefectRate.Text, out defectRate))
            {
                defectRate = 0;
            }
            defectRate = defectRate / 100;

            decimal cost;
            if (!decimal.TryParse(txtCostPerUnit.Text, out cost))
            {
                cost = 0;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    if (editingMaterial == null)
                    {
                        string query = "INSERT INTO Materials (MaterialName, DefectRate, Unit, CostPerUnit) VALUES (@name, @defect, @unit, @cost)";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@name", txtMaterialName.Text.Trim());
                        cmd.Parameters.AddWithValue("@defect", defectRate);
                        cmd.Parameters.AddWithValue("@unit", txtUnit.Text.Trim());
                        cmd.Parameters.AddWithValue("@cost", cost);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Материал добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        string query = "UPDATE Materials SET MaterialName=@name, DefectRate=@defect, Unit=@unit, CostPerUnit=@cost WHERE MaterialID=@id";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", editingMaterial.MaterialID);
                        cmd.Parameters.AddWithValue("@name", txtMaterialName.Text.Trim());
                        cmd.Parameters.AddWithValue("@defect", defectRate);
                        cmd.Parameters.AddWithValue("@unit", txtUnit.Text.Trim());
                        cmd.Parameters.AddWithValue("@cost", cost);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Данные обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                EditPanel.Visibility = Visibility.Collapsed;
                LoadMaterials();
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