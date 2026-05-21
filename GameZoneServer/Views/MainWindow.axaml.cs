using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;

namespace GameZoneServer.Views
{
    public partial class MainWindow : Window
    {
        private string _userRole;

        public MainWindow(string userRole)
        {
            InitializeComponent();
            _userRole = userRole;

            // Rol Kontrolü Yetkilendirmesi
            if (_userRole == "Admin")
            {
                LblWelcome.Text = "👑 Yönetici Paneli (Tam Yetki)";
                BtnReports.IsVisible = true;
                BtnSettings.IsVisible = true;
            }
            else
            {
                LblWelcome.Text = "🧑‍💼 Personel Ekranı (Sınırlı Yetki)";
                BtnReports.IsVisible = false;
                BtnSettings.IsVisible = false;
            }

            // SOMUT ADIM: Butonların tıklama olaylarını (Event) C# tarafında bağlıyoruz
            BtnDesks.Click += OnDesksButtonClick;
            BtnReports.Click += OnReportsButtonClick;
            BtnSettings.Click += OnSettingsButtonClick;

            // Test amaçlı: İlk açılışta masalar ekranını otomatik yükleyelim
            LoadMockDesks();
        }

        // 1. BUTON: Masalar Tetikleyicisi
        private void OnDesksButtonClick(object? sender, RoutedEventArgs e)
        {
            LblWelcome.Text = _userRole == "Admin" ? "👑 Yönetici Paneli -> Canlı Masalar" : "🧑‍💼 Personel Ekranı -> Canlı Masalar";
            LoadMockDesks(); // Masaları ekrana dizen fonksiyonu çağırıyoruz
        }

        // 2. BUTON: Hasılat Raporları Tetikleyicisi
        private void OnReportsButtonClick(object? sender, RoutedEventArgs e)
        {
            LblWelcome.Text = "📊 Hasılat Raporları (SQL Server Verileri)";
            DesksGrid.Children.Clear(); // Masaları ekrandan temizle

            // Buraya ileride SQL'den ciro çeken kodları ekleyeceğiz, şimdilik yazı bırakıyoruz
            TextBlock txtReport = new TextBlock { Text = "Bugünkü Toplam Kasa Cirosu: 1,500 TL\nAktif Oturum Sayısı: 4", FontSize = 16, Foreground = Brushes.LightGreen, Margin = new Avalonia.Thickness(0, 50, 0, 0) };
            DesksGrid.Children.Add(txtReport);
        }

        // 3. BUTON: Sistem Ayarları Tetikleyicisi
        private void OnSettingsButtonClick(object? sender, RoutedEventArgs e)
        {
            LblWelcome.Text = "⚙️ Sistem Ayarları ve Fiyatlandırma";
            DesksGrid.Children.Clear(); // Masaları ekrandan temizle

            TextBlock txtSettings = new TextBlock { Text = "Saatlik Masa Ücreti: 50 TL\nKilit Ekranı Mesajı: SÜRENİZ DOLDU!", FontSize = 16, Foreground = Brushes.White, Margin = new Avalonia.Thickness(0, 50, 0, 0) };
            DesksGrid.Children.Add(txtSettings);
        }

        // ADIM 13: Sürükle-bırak yapmadan KODLA dinamik masa kartları oluşturma algoritması
        private void LoadMockDesks()
        {
            DesksGrid.Children.Clear(); // Önce sağ alanı temizle

            // Gerçek zamanlı ağdan bağımsız, test için 4 tane örnek masa kutusu simüle ediyoruz
            for (int i = 1; i <= 4; i++)
            {
                // Her masa için dış çerçeve (Kart Tasarımı)
                Border deskCard = new Border
                {
                    Background = new SolidColorBrush(Color.Parse("#222222")),
                    BorderBrush = new SolidColorBrush(Color.Parse("#00ff88")),
                    BorderThickness = new Avalonia.Thickness(2),
                    CornerRadius = new Avalonia.CornerRadius(8),
                    Margin = new Avalonia.Thickness(10),
                    Height = 100
                };

                // Kartın içindeki dikey yazılar için panel
                StackPanel cardContent = new StackPanel { VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Spacing = 5 };

                TextBlock txtName = new TextBlock { Text = $"Masa-0{i}", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Brushes.White };
                TextBlock txtStatus = new TextBlock { Text = "Masa Kilitli (Süre Yok)", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, FontSize = 12, Foreground = Brushes.Gray };

                cardContent.Children.Add(txtName);
                cardContent.Children.Add(txtStatus);

                deskCard.Child = cardContent;

                // Kodla oluşturduğumuz bu harika kartı arayüzdeki UniformGrid'in (DesksGrid) içine fırlatıyoruz!
                DesksGrid.Children.Add(deskCard);
            }
        }
    }
}