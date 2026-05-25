using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameZoneServer.Views
{
    public partial class MainWindow : Window
    {
        private string _userRole = "Admin";

        // 🚀 ADIM 2: Program geneline yayılması için fiyat değişkenini public static yapıyoruz
        public static int GlobalHourlyRate = 50;
        private int _hourlyRate => GlobalHourlyRate; // Mevcut eski bağımlılıkları bozmamak için senkronize köprü

        private static MainWindow? _instance;

        public MainWindow()
        {
            InitializeComponent();
            _instance = this;
            LoadMockDesks();
            InitializeCustomSettings(); // Buton dinleyicisini ateşle
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
            InitializeCustomSettings(); // Buton dinleyicisini ateşle
        }

        // 🚀 ADIM 2: XAML dosyasındaki yeni 'BtnSaveRate' butonunu eski yapıya hiç dokunmadan bağlayan metot
        private void InitializeCustomSettings()
        {
            // Avalonia'nın Lifecycle sürecinde nesnelerin yüklenmesini garantiye almak için constructor sonrasında çalışır
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var btnSaveRate = this.FindControl<Button>("BtnSaveRate");
                if (btnSaveRate != null)
                {
                    btnSaveRate.Click += OnSaveRateClick;
                }
            });
        }

        // 🚀 ADIM 2: Kaydet butonuna basıldığında tetiklenen, ağdaki tüm masaların fiyatlarını canlı güncelleyen soket tetiği
        private async void OnSaveRateClick(object? sender, RoutedEventArgs e)
        {
            var txtHourlyRate = this.FindControl<TextBox>("TxtHourlyRate");
            if (txtHourlyRate != null && int.TryParse(txtHourlyRate.Text, out int newRate) && newRate > 0)
            {
                // 1. Sunucunun merkezi fiyat havuzunu güncelle
                GlobalHourlyRate = newRate;

                // 2. 🎯 ESKİ YAPILARI SARSMAZ: Ağda o an aktif olan tüm client'ları tarayıp yeni fiyat komutunu iletir
                var activeClientsList = Program.ActiveClients.ToList();
                foreach (var kvp in activeClientsList)
                {
                    try
                    {
                        if (kvp.Value != null && kvp.Value.Connected)
                        {
                            NetworkStream stream = kvp.Value.GetStream();
                            // İstemciye "GUNCEL_FIYAT:70" protokolü fırlatılıyor
                            byte[] cmdMsg = Encoding.UTF8.GetBytes($"GUNCEL_FIYAT:{GlobalHourlyRate}\n");
                            await stream.WriteAsync(cmdMsg, 0, cmdMsg.Length);
                            await stream.FlushAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Fiyat güncelleme soket iletim hatası ({kvp.Key}): {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"💰 [BAŞARILI] Tüm masaların saatlik ücreti {GlobalHourlyRate} TL olarak güncellendi ve soketle dağıtıldı.");

                // 🚀 YENİ BİLDİRİM DETAYI: Arayüzde fiyat güncelleme mesajını yakalayıp ekrana basıyoruz
                var lblStatus = this.FindControl<TextBlock>("LblSettingsStatus");
                if (lblStatus != null)
                {
                    lblStatus.Text = $"✅ Güncel fiyat artık {GlobalHourlyRate} TL olarak belirlendi ve tüm istemcilere dağıtıldı!";
                    lblStatus.Foreground = Brushes.LightGreen;

                    // 3 saniye ekranda kaldıktan sonra yazıyı temizler
                    await Task.Delay(3000);
                    lblStatus.Text = string.Empty;
                }
            }
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
            // Tasarımda açtığımız panellerin görünürlük ayarları
            var panelSettings = this.FindControl<StackPanel>("PanelSettings");
            if (panelSettings != null) panelSettings.IsVisible = false;

            var panelReports = this.FindControl<StackPanel>("PanelReports");
            if (panelReports != null) panelReports.IsVisible = false;

            if (DesksGrid != null) DesksGrid.IsVisible = true;
            LoadMockDesks();
        }

        // 🚀 METOT GÜNCELLEMESİ: XAML dosyasındaki geniş 'PanelReports' alanını aktif eden ve sığmama pürüzünü çözen metot
        private void OnReportsButtonClick(object? sender, RoutedEventArgs e)
        {
            // Daraltıcı olan DesksGrid ve PanelSettings'i kapat, esnek PanelReports'u tetikle
            var panelSettings = this.FindControl<StackPanel>("PanelSettings");
            if (panelSettings != null) panelSettings.IsVisible = false;

            if (DesksGrid != null) DesksGrid.IsVisible = false;

            var panelReports = this.FindControl<StackPanel>("PanelReports");
            if (panelReports != null) panelReports.IsVisible = true;

            LblWelcome.Text = "📊 Hasılat Raporları (Canlı Kasa ve Oturum Verileri)";

            // 1. XAML içindeki toplam ciro TextBlock alanını güncelle
            var txtTotalCiro = this.FindControl<TextBlock>("TxtTotalCiro");
            if (txtTotalCiro != null)
            {
                txtTotalCiro.Text = $"Bugünkü Toplam Kasa Cirosu: {Program.TotalRevenue:0.00} TL";
            }

            // 2. XAML içindeki ListBox nesnesine logları doldur
            var logListBox = this.FindControl<ListBox>("LogListBox");
            if (logListBox != null)
            {
                logListBox.Items.Clear();

                if (Program.RevenueLogs.Count == 0)
                {
                    logListBox.Items.Add(new TextBlock { Text = "⚠️ Henüz kasaya giren bir ciro veya açılan oturum yok.", Foreground = Brushes.Gray, Margin = new Avalonia.Thickness(5) });
                }
                else
                {
                    // En yeni kayıt en üstte görünecek şekilde ters sıralı akıtıyoruz
                    var reversedLogs = Program.RevenueLogs.AsEnumerable().Reverse();
                    foreach (var log in reversedLogs)
                    {
                        // 🎯 KESİN GÖRSEL ÇÖZÜM: Yazıların sağ sınırda kesilmesini engelleyen TextWrapping sarmallayıcısı
                        logListBox.Items.Add(new TextBlock
                        {
                            Text = log,
                            TextWrapping = TextWrapping.Wrap,
                            FontSize = 13,
                            Margin = new Avalonia.Thickness(2)
                        });
                    }
                }
            }
        }

        private void OnSettingsButtonClick(object? sender, RoutedEventArgs e)
        {
            LblWelcome.Text = "⚙️ Sistem Ayarları ve Fiyatlandırma";

            if (DesksGrid != null) DesksGrid.IsVisible = false;

            var panelReports = this.FindControl<StackPanel>("PanelReports");
            if (panelReports != null) panelReports.IsVisible = false;

            var panelSettings = this.FindControl<StackPanel>("PanelSettings");
            if (panelSettings != null)
            {
                panelSettings.IsVisible = true;
                if (_userRole != "Admin") return;

                // XAML içindeki TextBox'ın değerini hafızadaki güncel fiyata eşitle
                var txtHourlyRate = this.FindControl<TextBox>("TxtHourlyRate");
                if (txtHourlyRate != null)
                {
                    txtHourlyRate.Text = GlobalHourlyRate.ToString();
                }
            }
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
                    if (Program.DeskStartTime.TryGetValue(deskName, out DateTime startTime))
                    {
                        // 1. Masanın toplam kaç dakika açık kaldığını milimetrik hesapla
                        double elapsedMinutes = (DateTime.Now - startTime).TotalMinutes;
                        if (elapsedMinutes < 0.1) elapsedMinutes = 0.1; // Hızlı testler için emniyet koruması

                        // İstemcinin talep ettiği maksimum süreyi çek (Ayrılan süreyi taşmasın)
                        if (Program.DeskAllocatedMinutes.TryGetValue(deskName, out int allocatedMins))
                        {
                            if (elapsedMinutes > allocatedMins) elapsedMinutes = allocatedMins;
                        }

                        // 2. MİLİMETRİK CİRO: Sadece kullanılan dakikaya düşen gerçek ücret miktarını hesapla
                        double hourlyRate = GlobalHourlyRate;
                        double finalEarnedMoney = (elapsedMinutes / 60.0) * hourlyRate;

                        // Kasaya net tutarı işle ve log havuzuna ekle
                        Program.TotalRevenue += finalEarnedMoney;
                        Program.RevenueLogs.Add($"🛑 [{DateTime.Now:HH:mm}] {deskName} kapatıldı. Kullanılan Süre: {Math.Ceiling(elapsedMinutes)} Dk. Alınan Ücret: {finalEarnedMoney:0.00} TL");

                        // Hafıza havuzlarını güvenle temizle
                        Program.DeskStartTime.TryRemove(deskName, out _);
                        Program.DeskAllocatedMinutes.TryRemove(deskName, out _);

                        // İstemci tarafını kilitlemesi için komutu fırlatıyoruz
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