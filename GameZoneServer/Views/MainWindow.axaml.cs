using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Net.Sockets;
using System.Text;

namespace GameZoneServer.Views
{
    public partial class MainWindow : Window
    {
        private string _userRole = "Admin";
        private int _hourlyRate = 50;
        private double _totalRevenue = 0.0;
        private static MainWindow? _instance;

        public MainWindow()
        {
            InitializeComponent();
            _instance = this;
            LoadMockDesks();
        }

        public MainWindow(string userRole)
        {
            InitializeComponent();
            _userRole = userRole;
            _instance = this; // 🚀 ÇÖZÜM: Aktif olan son pencere örneğini buraya zımbalıyoruz.

            BtnDesks.Click += OnDesksButtonClick;
            BtnReports.Click += OnReportsButtonClick;
            BtnSettings.Click += OnSettingsButtonClick;
            BtnLogout.Click += OnLogoutButtonClick;

            if (_userRole == "Staff") BtnSettings.IsVisible = false;

            LblWelcome.Text = _userRole == "Admin" ? "👑 Yönetici Paneli -> Canlı Masalar" : "🧑‍💼 Personel Ekranı -> Canlı Masalar";
            LoadMockDesks();
        }

        public static void ForceRefreshUI()
        {
            // 🚀 ÇÖZÜM: Arka plandaki zombi tetiklemeyi statik referans üzerinden doğrudan arayüze işletiyoruz.
            if (_instance != null)
            {
                _instance.LoadMockDesks();
            }
            else
            {
                // Eğer instance null ise aktif pencerelerden bulup tetikliyoruz (Çift Güvenlik Duvarı)
                if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    foreach (var window in desktop.Windows)
                    {
                        if (window is MainWindow mainWin)
                        {
                            _instance = mainWin;
                            mainWin.LoadMockDesks();
                        }
                    }
                }
            }
        }

        private void OnLogoutButtonClick(object? sender, RoutedEventArgs e)
        {
            LoginWindow loginWin = new LoginWindow();
            loginWin.Show();
            this.Close();
        }

        private void OnDesksButtonClick(object? sender, RoutedEventArgs e)
        {
            LoadMockDesks();
        }

        private void OnReportsButtonClick(object? sender, RoutedEventArgs e)
        {
            LblWelcome.Text = "📊 Hasılat Raporları (SQL Server Canlı Verileri)";
            DesksGrid.Children.Clear();
            StackPanel reportPanel = new StackPanel { Spacing = 12, Margin = new Avalonia.Thickness(20) };
            reportPanel.Children.Add(new TextBlock { Text = $"Bugünkü Toplam Kasa Cirosu: {_totalRevenue:0.00} TL", FontSize = 20, Foreground = Brushes.LightGreen, FontWeight = FontWeight.Bold });
            DesksGrid.Children.Add(reportPanel);
        }

        private void OnSettingsButtonClick(object? sender, RoutedEventArgs e)
        {
            LblWelcome.Text = "⚙️ Sistem Ayarları ve Fiyatlandırma";
            DesksGrid.Children.Clear();
            if (_userRole != "Admin") return;

            StackPanel settingsPanel = new StackPanel { Spacing = 15, Margin = new Avalonia.Thickness(20) };
            settingsPanel.Children.Add(new TextBlock { Text = "Masa Saatlik Ücret Ayarı (TL):", FontSize = 16, Foreground = Brushes.White });
            TextBox txtHourlyRate = new TextBox { Text = _hourlyRate.ToString(), Width = 150 };
            settingsPanel.Children.Add(txtHourlyRate);
            DesksGrid.Children.Add(settingsPanel);
        }

        public void LoadMockDesks()
        {
            if (DesksGrid == null) return;
            DesksGrid.Children.Clear();

            for (int i = 1; i <= 4; i++)
            {
                string deskName = $"Masa-0{i}";
                Border deskCard = new Border { BorderThickness = new Avalonia.Thickness(2), CornerRadius = new Avalonia.CornerRadius(8), Margin = new Avalonia.Thickness(10), Height = 100 };
                StackPanel cardContent = new StackPanel { VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Spacing = 5 };
                TextBlock txtName = new TextBlock { Text = deskName, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Brushes.White };
                TextBlock txtStatus = new TextBlock { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, FontSize = 12 };

                if (Program.ActiveClients.TryGetValue(deskName, out TcpClient? connectedClient))
                {
                    if (Program.DeskStartTime.ContainsKey(deskName))
                    {
                        deskCard.Background = new SolidColorBrush(Color.Parse("#1a3a2a"));
                        deskCard.BorderBrush = new SolidColorBrush(Color.Parse("#00ff88"));
                        txtStatus.Text = "🟢 Kullanımda (Süre Açık)";
                        txtStatus.Foreground = Brushes.LightGreen;
                    }
                    else
                    {
                        deskCard.Background = new SolidColorBrush(Color.Parse("#2d2d3a"));
                        deskCard.BorderBrush = new SolidColorBrush(Color.Parse("#ffaa00"));
                        txtStatus.Text = "🟡 Bağlı / Kilitli (Beklemede)";
                        txtStatus.Foreground = Brushes.Yellow;
                    }
                    BindCardClickEvent(deskCard, deskName, connectedClient);
                }
                else
                {
                    deskCard.Background = new SolidColorBrush(Color.Parse("#222222"));
                    deskCard.BorderBrush = new SolidColorBrush(Color.Parse("#444444"));
                    txtStatus.Text = "Masa Çevrimdışı";
                    txtStatus.Foreground = Brushes.DarkGray;
                }

                cardContent.Children.Add(txtName);
                cardContent.Children.Add(txtStatus);
                deskCard.Child = cardContent;
                DesksGrid.Children.Add(deskCard);
            }
        }

        private void BindCardClickEvent(Border card, string deskName, TcpClient client)
        {
            card.PointerPressed += async (s, e) =>
            {
                try
                {
                    if (Program.DeskStartTime.ContainsKey(deskName))
                    {
                        Program.DeskStartTime.TryRemove(deskName, out _);
                        NetworkStream stream = client.GetStream();
                        byte[] cmd = Encoding.UTF8.GetBytes("KILIDI_AC:0\n");
                        await stream.WriteAsync(cmd, 0, cmd.Length);
                        await stream.FlushAsync();
                        LoadMockDesks();
                    }
                }
                catch { }
            };
        }
    }
}