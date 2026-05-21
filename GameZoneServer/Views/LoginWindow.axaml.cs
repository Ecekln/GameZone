using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Data.SqlClient;
using System;

namespace GameZoneServer.Views
{
    public partial class LoginWindow : Window
    {
        private string connectionString = "Server=ECEM;Database=GameZoneDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public LoginWindow()
        {
            InitializeComponent();
            // Kodla şekillendirdiğimiz butona tıklama olayını (Event) bağlıyoruz
            BtnLogin.Click += OnLoginButtonClick;
        }

        private void OnLoginButtonClick(object? sender, RoutedEventArgs e)
        {
            string username = TxtUsername.Text ?? "";
            string password = TxtPassword.Text ?? "";

            // SQL Server Doğrulaması (Authentication)
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT Role FROM Users WHERE Username = @Username AND Password = @Password";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@Password", password);

                        object? result = command.ExecuteScalar();

                        if (result != null)
                        {
                            string userRole = result.ToString() ?? "Staff";

                            // Giriş Başarılı! Rol bilgisini (Admin mi Staff mı) Ana Ekrana gönderiyoruz
                            MainWindow mainWindow = new MainWindow(userRole);
                            mainWindow.Show();
                            this.Close(); // Giriş penceresini kapat
                        }
                        else
                        {
                            // Hata durumunda küçük bir pencere veya konsol uyarısı (İleride şıklaştıracağız)
                            TxtUsername.Text = "";
                            TxtPassword.Text = "";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HATA] Veritabanı bağlantısı başarısız: {ex.Message}");
                }
            }
        }
    }
}