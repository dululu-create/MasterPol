using System.Windows;

namespace MasterPol4.Views
{
    public partial class MainWindow : Window
    {
        private User currentUser;

        public MainWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            lblUser.Text = $"{user.Username} ({user.Role})";
            ShowPartnersWindow();
        }

        private void BtnPartners_Click(object sender, RoutedEventArgs e) => ShowPartnersWindow();
        private void BtnProducts_Click(object sender, RoutedEventArgs e) => ShowProductsWindow();
        private void BtnMaterials_Click(object sender, RoutedEventArgs e) => ShowMaterialsWindow();
        private void BtnWarehouse_Click(object sender, RoutedEventArgs e) => ShowWarehouseWindow();
        private void BtnProduction_Click(object sender, RoutedEventArgs e) => ShowProductionWindow();
        private void BtnEmployees_Click(object sender, RoutedEventArgs e) => ShowEmployeesWindow();
        private void BtnSales_Click(object sender, RoutedEventArgs e) => ShowSalesWindow();

        private void ShowPartnersWindow() => MainFrame.Navigate(new PartnersWindow(currentUser));
        private void ShowProductsWindow() => MainFrame.Navigate(new ProductsWindow(currentUser));
        private void ShowMaterialsWindow() => MainFrame.Navigate(new MaterialsWindow(currentUser));
        private void ShowWarehouseWindow() => MainFrame.Navigate(new WarehouseWindow(currentUser));
        private void ShowProductionWindow() => MainFrame.Navigate(new ProductionWindow(currentUser));
        private void ShowEmployeesWindow() => MainFrame.Navigate(new EmployeesWindow(currentUser));
        private void ShowSalesWindow() => MainFrame.Navigate(new SalesWindow(currentUser));

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }
    }
}