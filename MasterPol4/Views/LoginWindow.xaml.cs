using System;
using System.Data.SqlClient;
using System.Windows;

namespace MasterPol4.Views
{
    public partial class LoginWindow : Window
    {
        private string connectionString = "data source=DESKTOP-L8QU7O1\\SQLEXPRESS;initial catalog=MasterPol;integrated security=True;";

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(username)) { ShowError("Введите логин"); return; }
            if (string.IsNullOrEmpty(password)) { ShowError("Введите пароль"); return; }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT UserID, Username, Role FROM Users WHERE Username = @username AND PasswordHash = @password";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);

                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        User currentUser = new User
                        {
                            UserID = Convert.ToInt32(reader["UserID"]),
                            Username = reader["Username"].ToString(),
                            Role = reader["Role"].ToString()
                        };
                        MainWindow mainWindow = new MainWindow(currentUser);
                        mainWindow.Show();
                        this.Close();
                    }
                    else
                    {
                        ShowError("Неверный логин или пароль");
                        txtPassword.Password = "";
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка БД: {ex.Message}");
            }
        }

        private void ShowError(string msg)
        {
            lblError.Text = msg;
            lblError.Visibility = Visibility.Visible;
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, e) => { lblError.Visibility = Visibility.Collapsed; timer.Stop(); };
            timer.Start();
        }
    }

    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
    }
}