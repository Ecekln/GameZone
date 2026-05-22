using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Data.SqlClient;
using System;

namespace GameZoneServer.Views
{
    public partial class LoginWindow : Window
    {
        private readonly string _connectionString = "Server=ECEM;Database=GameZoneDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public LoginWindow()
        {
            InitializeComponent();

            var btnLogin = this.FindControl<Button>("BtnLogin");
            if (btnLogin != null) btnLogin.Click += OnLoginClick;
        }

        private void OnLoginClick(object? sender, RoutedEventArgs e)
        {
            var txtUsername = this.FindControl<TextBox>("TxtUsername");
            var txtPassword = this.FindControl<TextBox>("TxtPassword");

            string username = txtUsername?.Text ?? "";
            string password = txtPassword?.Text ?? "";

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowErrorMessage("Lütfen tüm alanları doldurun!");
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
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
                            string userRole = result.ToString()!;

                            var mainWin = new MainWindow(userRole);
                            mainWin.Show();
                            this.Close();
                        }
                        else
                        {
                            ShowErrorMessage("Kullanıcı adı veya şifre hatalı!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Veritabanı bağlantı hatası!");
                System.Diagnostics.Debug.WriteLine($"DB Hatası: {ex.Message}");
            }
        }

        private void ShowErrorMessage(string message)
        {
            var lbl = this.FindControl<TextBlock>("LblError");
            if (lbl != null)
            {
                lbl.Text = $"❌ {message}";
                lbl.IsVisible = true;
            }
            else
            {
                this.Title = $"Hata: {message}";
            }
        }
    }
}