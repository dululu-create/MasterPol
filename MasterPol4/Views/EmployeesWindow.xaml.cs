using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MasterPol4.Views
{
    public partial class EmployeesWindow : Page
    {
        private string connectionString = "data source=DESKTOP-L8QU7O1\\SQLEXPRESS;initial catalog=MasterPol;integrated security=True;";
        private User currentUser;
        private List<dynamic> allEmployees;
        private dynamic editingEmployee;
        public bool IsAdmin => currentUser?.Role == "Admin";

        public EmployeesWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            btnAdd.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            LoadEmployees();
        }

        private void LoadEmployees()
        {
            try
            {
                var employees = new List<dynamic>();
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT EmployeeID, FullName, Position, Phone, Email, HireDate, Salary FROM Employees ORDER BY FullName";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        employees.Add(new
                        {
                            EmployeeID = reader["EmployeeID"],
                            FullName = reader["FullName"],
                            Position = reader["Position"],
                            Phone = reader["Phone"],
                            Email = reader["Email"],
                            HireDate = reader["HireDate"],
                            Salary = reader["Salary"]
                        });
                    }
                }
                allEmployees = employees.Cast<dynamic>().ToList();
                dgEmployees.ItemsSource = employees;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterEmployees()
        {
            string search = txtSearch.Text?.ToLower() ?? "";
            if (string.IsNullOrWhiteSpace(search))
            {
                dgEmployees.ItemsSource = allEmployees;
                return;
            }
            var filtered = allEmployees.Where(e =>
                e.FullName?.ToLower().Contains(search) == true ||
                e.Position?.ToLower().Contains(search) == true ||
                e.Phone?.Contains(search) == true).ToList();
            dgEmployees.ItemsSource = filtered;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => FilterEmployees();
        private void BtnSearch_Click(object sender, RoutedEventArgs e) => FilterEmployees();
        private void BtnReset_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; FilterEmployees(); }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin) { ShowAccessDenied(); return; }
            editingEmployee = null;
            EditTitle.Text = "Добавление сотрудника";
            txtFullName.Text = "";
            txtPosition.Text = "";
            txtPhone.Text = "";
            txtEmail.Text = "";
            dpHireDate.SelectedDate = DateTime.Now;
            txtSalary.Text = "";
            EditPanel.Visibility = Visibility.Visible;
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin) { ShowAccessDenied(); return; }
            Button btn = sender as Button;
            editingEmployee = btn?.Tag;
            if (editingEmployee == null) return;

            EditTitle.Text = "Редактирование сотрудника";
            txtFullName.Text = editingEmployee.FullName;
            txtPosition.Text = editingEmployee.Position;
            txtPhone.Text = editingEmployee.Phone;
            txtEmail.Text = editingEmployee.Email;
            dpHireDate.SelectedDate = editingEmployee.HireDate;
            txtSalary.Text = editingEmployee.Salary.ToString();
            EditPanel.Visibility = Visibility.Visible;
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin) { ShowAccessDenied(); return; }
            Button btn = sender as Button;
            dynamic employee = btn?.Tag;
            if (employee == null) return;

            MessageBoxResult result = MessageBox.Show($"Удалить сотрудника '{employee.FullName}'?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = "DELETE FROM Employees WHERE EmployeeID = @id";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", employee.EmployeeID);
                        cmd.ExecuteNonQuery();
                    }
                    LoadEmployees();
                    MessageBox.Show("Сотрудник удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Введите ФИО сотрудника", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dpHireDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату найма", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal salary;
            if (!decimal.TryParse(txtSalary.Text, out salary))
            {
                salary = 0;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    if (editingEmployee == null)
                    {
                        string query = "INSERT INTO Employees (FullName, Position, Phone, Email, HireDate, Salary) VALUES (@name, @position, @phone, @email, @date, @salary)";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@name", txtFullName.Text.Trim());
                        cmd.Parameters.AddWithValue("@position", txtPosition.Text.Trim());
                        cmd.Parameters.AddWithValue("@phone", txtPhone.Text.Trim());
                        cmd.Parameters.AddWithValue("@email", txtEmail.Text.Trim());
                        cmd.Parameters.AddWithValue("@date", dpHireDate.SelectedDate.Value);
                        cmd.Parameters.AddWithValue("@salary", salary);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Сотрудник добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        string query = "UPDATE Employees SET FullName=@name, Position=@position, Phone=@phone, Email=@email, HireDate=@date, Salary=@salary WHERE EmployeeID=@id";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", editingEmployee.EmployeeID);
                        cmd.Parameters.AddWithValue("@name", txtFullName.Text.Trim());
                        cmd.Parameters.AddWithValue("@position", txtPosition.Text.Trim());
                        cmd.Parameters.AddWithValue("@phone", txtPhone.Text.Trim());
                        cmd.Parameters.AddWithValue("@email", txtEmail.Text.Trim());
                        cmd.Parameters.AddWithValue("@date", dpHireDate.SelectedDate.Value);
                        cmd.Parameters.AddWithValue("@salary", salary);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Данные обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                EditPanel.Visibility = Visibility.Collapsed;
                LoadEmployees();
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