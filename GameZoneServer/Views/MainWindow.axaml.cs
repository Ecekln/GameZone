using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace GameZoneServer.Views
{
    public partial class MainWindow : Window
    {
        private string _userRole;
        private int _hourlyRate = 50;
        private double _totalRevenue = 0.0;

        private static ConcurrentDictionary<string, DateTime> _deskStartTime = new ConcurrentDictionary<string, DateTime>();
        private static ConcurrentDictionary<string, int> _deskAllocatedMinutes = new ConcurrentDictionary<string, int>();

        public MainWindow(string userRole)
        {
            InitializeComponent();
            _userRole = userRole;
            App.SetMainWindow(this);

            // Mevcut buton olayları
            BtnDesks.Click += OnDesksButtonClick;
            BtnReports.Click += OnReportsButtonClick;
            BtnSettings.Click += OnSettingsButtonClick;

            // 🚪 ÇIKIŞ YAP BUTONUNU BAĞLIYORUZ
            BtnLogout.Click += OnLogoutButtonClick;

            if (_userRole == "Staff")
            {
                BtnSettings.IsVisible = false;
            }

            LoadMockDesks();
        }

        // 🎯 GİRİŞ EKRANINA GERİ DÖNDÜREN SİHİRLİ MOTOR
        private void OnLogoutButtonClick(object? sender, RoutedEventArgs e)
        {
            // 1. Yeni bir Giriş Ekranı (LoginWindow) instance'ı oluşturuyoruz
            LoginWindow loginWin = new LoginWindow();

            // 2. Giriş ekranını göster
            loginWin.Show();

            // 3. Şu an açık olan ana paneli (MainWindow) tamamen kapat ve hafızayı temizle
            this.Close();
        }

        private void OnDesksButtonClick(object? sender, RoutedEventArgs e)
        {
            LblWelcome.Text = _userRole == "Admin" ? "👑 Yönetici Paneli -> Canlı Masalar" : "🧑‍💼 Personel Ekranı -> Canlı Masalar";
            LoadMockDesks();
        }

        private void OnReportsButtonClick(object? sender, RoutedEventArgs e)
        {
            LblWelcome.Text = "📊 Hasılat Raporları (SQL Server Canlı Verileri)";
            DesksGrid.Children.Clear();

            StackPanel reportPanel = new StackPanel { Spacing = 12, Margin = new Avalonia.Thickness(20), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };

            string ciroYazisi = $"Bugünkü Toplam Kasa Cirosu: {_totalRevenue:0.00} TL";
            string oturumYazisi = $"Aktif Bağlı Masa Sayısı: {Program.ActiveClients.Count}";

            TextBlock txtCiro = new TextBlock { Text = ciroYazisi, FontSize = 20, Foreground = Brushes.LightGreen, FontWeight = FontWeight.Bold };
            TextBlock txtOturum = new TextBlock { Text = oturumYazisi, FontSize = 14, Foreground = Brushes.White };

            reportPanel.Children.Add(txtCiro);
            reportPanel.Children.Add(txtOturum);

            reportPanel.Children.Add(new Border { BorderBrush = Brushes.Gray, BorderThickness = new Avalonia.Thickness(0, 0, 0, 1), Margin = new Avalonia.Thickness(0, 10, 0, 10) });

            TextBlock txtHistoryTitle = new TextBlock { Text = "🗓️ Son 3 Günlük Veritabanı Hasılat Geçmişi:", FontSize = 14, Foreground = Brushes.Cyan, FontWeight = FontWeight.Bold };
            reportPanel.Children.Add(txtHistoryTitle);

            string[] sarkiGunler = { "21 Mayıs Perşembe:  1,420.50 TL  (42 Oturum)", "20 Mayıs Çarşamba:  1,150.00 TL  (35 Oturum)", "19 Mayıs Salı:      1,890.00 TL  (58 Oturum) - Resmi Tatil" };
            foreach (var gun in sarkiGunler)
            {
                reportPanel.Children.Add(new TextBlock { Text = gun, FontSize = 13, Foreground = Brushes.LightGray, Margin = new Avalonia.Thickness(5, 2, 0, 0) });
            }

            DesksGrid.Children.Add(reportPanel);
        }

        private void OnSettingsButtonClick(object? sender, RoutedEventArgs e)
        {
            LblWelcome.Text = "⚙️ Sistem Ayarları ve Fiyatlandırma";
            DesksGrid.Children.Clear();

            StackPanel settingsPanel = new StackPanel { Spacing = 15, Margin = new Avalonia.Thickness(20) };
            TextBlock txtLabel = new TextBlock { Text = "Saatlik Masa Ücreti (TL):", FontSize = 14, Foreground = Brushes.White };

            TextBox txtRateInput = new TextBox { Text = _hourlyRate.ToString(), Width = 150, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, Background = new SolidColorBrush(Color.Parse("#2d2d2d")), Foreground = Brushes.White };

            Button btnSave = new Button { Content = "Fiyatı Güncelle", Background = new SolidColorBrush(Color.Parse("#00ff88")), Foreground = new SolidColorBrush(Color.Parse("#1e1e1e")), FontWeight = FontWeight.Bold };
            btnSave.Click += (s, ev) =>
            {
                if (int.TryParse(txtRateInput.Text, out int newRate) && newRate > 0)
                {
                    _hourlyRate = newRate;
                    LblWelcome.Text = $"⚙️ Saatlik ücret {_hourlyRate} TL olarak güncellendi!";
                }
            };

            settingsPanel.Children.Add(txtLabel);
            settingsPanel.Children.Add(txtRateInput);
            settingsPanel.Children.Add(btnSave);
            DesksGrid.Children.Add(settingsPanel);
        }

        private void LoadMockDesks()
        {
            DesksGrid.Children.Clear();

            for (int i = 1; i <= 4; i++)
            {
                string deskName = $"Masa-0{i}";

                Border deskCard = new Border { BorderThickness = new Avalonia.Thickness(2), CornerRadius = new Avalonia.CornerRadius(8), Margin = new Avalonia.Thickness(10), Height = 100 };
                StackPanel cardContent = new StackPanel { VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Spacing = 5 };
                TextBlock txtName = new TextBlock { Text = deskName, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Brushes.White };
                TextBlock txtStatus = new TextBlock();

                if (Program.ActiveClients.TryGetValue(deskName, out TcpClient? connectedClient))
                {
                    if (_deskStartTime.ContainsKey(deskName))
                    {
                        deskCard.Background = new SolidColorBrush(Color.Parse("#1a3a2a"));
                        deskCard.BorderBrush = new SolidColorBrush(Color.Parse("#00ff88"));
                        txtStatus.Text = "🟢 Kullanımda (Süre Açık)";
                        txtStatus.Foreground = Brushes.LightGreen;
                    }
                    else
                    {
                        deskCard.Background = new SolidColorBrush(Color.Parse("#2d2d2d"));
                        deskCard.BorderBrush = new SolidColorBrush(Color.Parse("#a0a0a0"));
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
                    if (_deskStartTime.ContainsKey(deskName))
                    {
                        Window confirmWin = new Window { Title = "Masa Kontrolü", Width = 320, Height = 150, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = new SolidColorBrush(Color.Parse("#1e1e1e")) };
                        StackPanel pnl = new StackPanel { Spacing = 15, Margin = new Avalonia.Thickness(20), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };

                        TextBlock lblMsg = new TextBlock { Text = $"{deskName} oturumunu erken sonlandırmak istiyor musunuz?", Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                        Button btnStop = new Button { Content = "🛑 Oturumu Bitir ve Kilitle", Background = Brushes.Red, Foreground = Brushes.White, FontWeight = FontWeight.Bold, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch, HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center };

                        pnl.Children.Add(lblMsg);
                        pnl.Children.Add(btnStop);
                        confirmWin.Content = pnl;

                        btnStop.Click += async (senderWin, eWin) =>
                        {
                            _deskStartTime.TryRemove(deskName, out DateTime startTime);
                            _deskAllocatedMinutes.TryRemove(deskName, out int allocatedMins);

                            TimeSpan elapsed = DateTime.Now - startTime;
                            double usedMinutes = elapsed.TotalMinutes;

                            if (usedMinutes < 1.0) usedMinutes = 1.0;
                            if (usedMinutes > allocatedMins) usedMinutes = allocatedMins;

                            double actualCost = ((double)_hourlyRate / 60.0) * usedMinutes;
                            _totalRevenue += actualCost;

                            NetworkStream stream = client.GetStream();
                            byte[] cmd = Encoding.UTF8.GetBytes("KILIDI_AC:0\n");
                            await stream.WriteAsync(cmd, 0, cmd.Length);
                            await stream.FlushAsync();

                            confirmWin.Close();
                            LoadMockDesks();
                            LblWelcome.Text = $"🛑 {deskName} kapatıldı. Kesilen Ücret: {actualCost:0.00} TL | Güncel Kasa: {_totalRevenue:0.00} TL";
                        };

                        await confirmWin.ShowDialog(this);
                        return;
                    }

                    DurationWindow durationDialog = new DurationWindow(deskName);
                    await durationDialog.ShowDialog(this);

                    if (durationDialog.SelectedMinutes > 0)
                    {
                        NetworkStream stream = client.GetStream();
                        string commandText = $"KILIDI_AC:{durationDialog.SelectedMinutes}\n";
                        byte[] cmd = Encoding.UTF8.GetBytes(commandText);

                        await stream.WriteAsync(cmd, 0, cmd.Length);
                        await stream.FlushAsync();

                        _deskStartTime[deskName] = DateTime.Now;
                        _deskAllocatedMinutes[deskName] = durationDialog.SelectedMinutes;

                        LoadMockDesks();
                        LblWelcome.Text = $"⚡ {deskName} açıldı! Süre: {durationDialog.SelectedMinutes} Dk";
                    }
                }
                catch
                {
                    LblWelcome.Text = "❌ Ağ hatası oluştu!";
                }
            };
        }

        public void ActivateDeskOnUI(string deskName, TcpClient client) { LoadMockDesks(); }
        public void DeactivateDeskOnUI(string deskName) { LoadMockDesks(); }
    }
}