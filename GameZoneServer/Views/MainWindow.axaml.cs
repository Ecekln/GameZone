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
        private string _userRole;

        public MainWindow(string userRole)
        {
            InitializeComponent();
            _userRole = userRole;
            App.SetMainWindow(this);

            BtnDesks.Click += OnDesksButtonClick;
            BtnReports.Click += OnReportsButtonClick;
            BtnSettings.Click += OnSettingsButtonClick;

            LoadMockDesks();
        }

        private void OnDesksButtonClick(object? sender, RoutedEventArgs e)
        {
            LblWelcome.Text = _userRole == "Admin" ? "👑 Yönetici Paneli -> Canlı Masalar" : "🧑‍💼 Personel Ekranı -> Canlı Masalar";
            LoadMockDesks();
        }

        private void OnReportsButtonClick(object? sender, RoutedEventArgs e)
        {
            LblWelcome.Text = "📊 Hasılat Raporları (SQL Server Verileri)";
            DesksGrid.Children.Clear();
            TextBlock txtReport = new TextBlock { Text = "Bugünkü Toplam Kasa Cirosu: 1,500 TL\nAktif Oturum Sayısı: 4", FontSize = 16, Foreground = Brushes.LightGreen, Margin = new Avalonia.Thickness(0, 50, 0, 0) };
            DesksGrid.Children.Add(txtReport);
        }

        private void OnSettingsButtonClick(object? sender, RoutedEventArgs e)
        {
            LblWelcome.Text = "⚙️ Sistem Ayarları ve Fiyatlandırma";
            DesksGrid.Children.Clear();
            TextBlock txtSettings = new TextBlock { Text = "Saatlik Masa Ücreti: 50 TL\nKilit Ekranı Mesajı: SÜRENİZ DOLDU!", FontSize = 16, Foreground = Brushes.White, Margin = new Avalonia.Thickness(0, 50, 0, 0) };
            DesksGrid.Children.Add(txtSettings);
        }

        private void LoadMockDesks()
        {
            DesksGrid.Children.Clear();

            for (int i = 1; i <= 4; i++)
            {
                string deskName = $"Masa-0{i}";

                Border deskCard = new Border
                {
                    BorderThickness = new Avalonia.Thickness(2),
                    CornerRadius = new Avalonia.CornerRadius(8),
                    Margin = new Avalonia.Thickness(10),
                    Height = 100
                };

                StackPanel cardContent = new StackPanel { VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Spacing = 5 };
                TextBlock txtName = new TextBlock { Text = deskName, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Brushes.White };
                TextBlock txtStatus = new TextBlock();

                if (Program.ActiveClients.TryGetValue(deskName, out TcpClient? connectedClient))
                {
                    deskCard.Background = new SolidColorBrush(Color.Parse("#1a3a2a"));
                    deskCard.BorderBrush = new SolidColorBrush(Color.Parse("#00ff88"));
                    txtStatus.Text = "🟢 Oturum Açık / Süre Sınırsız";
                    txtStatus.Foreground = Brushes.LightGreen;

                    BindCardClickEvent(deskCard, deskName, connectedClient);
                }
                else
                {
                    deskCard.Background = new SolidColorBrush(Color.Parse("#222222"));
                    deskCard.BorderBrush = new SolidColorBrush(Color.Parse("#444444"));
                    txtStatus.Text = "Masa Kilitli (Süre Yok)";
                    txtStatus.Foreground = Brushes.Gray;
                }

                txtStatus.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
                txtStatus.FontSize = 12;

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
                    // Giriş penceresini (Popup) fırlatıyoruz
                    DurationWindow durationDialog = new DurationWindow(deskName);
                    await durationDialog.ShowDialog(this);

                    if (durationDialog.SelectedMinutes <= 0)
                    {
                        return;
                    }
                    NetworkStream stream = client.GetStream();
                    string commandText = $"KILIDI_AC:{durationDialog.SelectedMinutes}\n";
                    byte[] cmd = Encoding.UTF8.GetBytes(commandText);

                    await stream.WriteAsync(cmd, 0, cmd.Length);
                    await stream.FlushAsync();

                    // Ekrandaki durumu güncelle
                    card.Background = new SolidColorBrush(Color.Parse("#1a3a2a"));
                    card.BorderBrush = new SolidColorBrush(Color.Parse("#00ff88"));

                    foreach (var child in ((StackPanel)card.Child).Children)
                    {
                        if (child is TextBlock t && t.Text.Contains("Masa Kilitli"))
                        {
                            t.Text = $"🟢 Süre: {durationDialog.SelectedMinutes} Dk";
                            t.Foreground = Brushes.LightGreen;
                        }
                    }

                    LblWelcome.Text = $"⚡ {deskName} bilgisayarına {durationDialog.SelectedMinutes} dakikalık süre tanımlandı!";

                    foreach (var child in ((StackPanel)card.Child).Children)
                    {
                        if (child is TextBlock t && t.Text.Contains("Kilitli"))
                        {
                            // ARTIK SÜRE SINIRSIZ YAZMASIN:
                            t.Text = $"🟢 Süre: {durationDialog.SelectedMinutes} Dk";
                            t.Foreground = Brushes.LightGreen;
                        }
                    }
                }
                catch
                {
                    LblWelcome.Text = "❌ Masaya emir gönderilirken ağ hatası oluştu!";
                }
            };
        }

        public void ActivateDeskOnUI(string deskName, TcpClient client)
        {
            LoadMockDesks(); // Tüm grid yapısını güncel bağlananlara göre yeniden tazele
        }

        public void DeactivateDeskOnUI(string deskName)
        {
            LoadMockDesks(); // Süre bittiğinde veya bağlantı koptuğunda kartı griye döndür
        }
    }
}