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
            App.SetMainWindow(this); // Referansı eşitle

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

                // KRİTİK KONTROL: Bu masa havuzda aktif olarak bekliyor mu?
                if (Program.ActiveClients.TryGetValue(deskName, out TcpClient? connectedClient))
                {
                    // Eğer havuzda varsa direkt YEŞİL başlatıyoruz!
                    deskCard.Background = new SolidColorBrush(Color.Parse("#1a3a2a"));
                    deskCard.BorderBrush = new SolidColorBrush(Color.Parse("#00ff88"));
                    txtStatus.Text = "🟢 Oturum Açık / Süre Sınırsız";
                    txtStatus.Foreground = Brushes.LightGreen;

                    BindCardClickEvent(deskCard, deskName, connectedClient);
                }
                else
                {
                    // Havuzda yoksa standart GRİ (Kilitli) başlat
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

        // Tıklama olayını bağlayan yardımcı fonksiyon
        private void BindCardClickEvent(Border card, string deskName, TcpClient client)
        {
            card.PointerPressed += async (s, e) =>
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    byte[] cmd = Encoding.UTF8.GetBytes("KILIDI_AC\n");
                    await stream.WriteAsync(cmd, 0, cmd.Length);
                    LblWelcome.Text = $"⚡ {deskName} bilgisayarına kilit açma emri gönderildi!";
                }
                catch { LblWelcome.Text = "❌ Masaya emir gönderilirken ağ hatası oluştu!"; }
            };
        }

        // Sonradan bağlanan masalar için canlı aktivasyon
        public void ActivateDeskOnUI(string deskName, TcpClient client)
        {
            foreach (var child in DesksGrid.Children)
            {
                if (child is Border card && card.Child is StackPanel content)
                {
                    foreach (var subChild in content.Children)
                    {
                        if (subChild is TextBlock txt && txt.Text == deskName)
                        {
                            card.Background = new SolidColorBrush(Color.Parse("#1a3a2a"));
                            card.BorderBrush = new SolidColorBrush(Color.Parse("#00ff88"));

                            foreach (var statusTxt in content.Children)
                            {
                                if (statusTxt is TextBlock t && t.Text.Contains("Kilitli"))
                                {
                                    t.Text = "🟢 Oturum Açık / Süre Sınırsız";
                                    t.Foreground = Brushes.LightGreen;
                                }
                            }

                            BindCardClickEvent(card, deskName, client);
                        }
                    }
                }
            }
        }

        // Bağlantısı kopan masayı canlı olarak griye döndürme
        public void DeactivateDeskOnUI(string deskName)
        {
            foreach (var child in DesksGrid.Children)
            {
                if (child is Border card && card.Child is StackPanel content)
                {
                    foreach (var subChild in content.Children)
                    {
                        if (subChild is TextBlock txt && txt.Text == deskName)
                        {
                            card.Background = new SolidColorBrush(Color.Parse("#222222"));
                            card.BorderBrush = new SolidColorBrush(Color.Parse("#444444"));

                            foreach (var statusTxt in content.Children)
                            {
                                if (statusTxt is TextBlock t && t.Text.Contains("Oturum"))
                                {
                                    t.Text = "Masa Kilitli (Süre Yok)";
                                    t.Foreground = Brushes.Gray;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}