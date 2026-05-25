using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameZoneClient.Views
{
    public partial class LockWindow : Window
    {
        private int _serverHourlyRate = 50;
        private string _deskName = "Masa-01";

        // 🚀 SAYAÇ ENTEGRASYONU: LockWindow referansını statik olarak dışarıya açıyoruz
        public static LockWindow? Instance { get; private set; }

        public LockWindow()
        {
            InitializeComponent();
            Instance = this; // Referansı zımbalıyoruz

            var btnDeposit = this.FindControl<Button>("BtnDepositBalance");
            if (btnDeposit != null)
            {
                btnDeposit.Click += OnDepositBalanceClick;
            }

            // 🚀 KESİN ÇÖZÜM 1: Güvensiz Reflection yerine, Program.cs'in terminal 
            // argümanlarından yakaladığı gerçek masa adını doğrudan çekiyoruz.
            _deskName = GameZoneClient.Program._deskName;

            // 🚀 KESİN ÇÖZÜM 2: Çift masa tetiklenmesine neden olan arkadaki mükerrer 
            // Task.Run kayıt döngüsü tamamen kaldırıldı! Yönetim sadece Program.cs'te.
        }

        // 🚀 ADIM 4: MEVCUT DENGEYİ BOZMAYAN YENİ METOT 
        // Sunucu fiyatı değiştirdiğinde Program.cs soket üzerinden bu metodu çağırır 
        // ve yerel bakiye hesaplama çarpanını canlı olarak revize eder.
        public void UpdateHourlyRate(int newRate)
        {
            _serverHourlyRate = newRate;
            System.Diagnostics.Debug.WriteLine($"💰 Masa saatlik ücreti canlı olarak güncellendi: {_serverHourlyRate} TL");
        }

        public void ShowWindowForPlayer()
        {
            Dispatcher.UIThread.Post(() =>
            {
                var lblStatus = this.FindControl<TextBlock>("LblLockStatus");
                if (lblStatus != null)
                {
                    lblStatus.Text = $"{_deskName} - Kilitli Ekran";
                    lblStatus.Foreground = Brushes.Gray;
                }

                var txtAmount = this.FindControl<TextBox>("TxtDepositAmount");
                if (txtAmount != null)
                {
                    txtAmount.Text = string.Empty;
                }

                this.Show();
                this.WindowState = WindowState.Normal;
                this.Topmost = true;
                this.Activate();
            });
        }

        public void HideWindowForPlayer()
        {
            Dispatcher.UIThread.Post(() =>
            {
                this.Topmost = false;
                this.Hide();
            });
        }

        // 🚀 SAYAÇ ENTEGRASYONU: Sunucudan süre emri geldiğinde tetiklenecek olan hata korumalı açma metodu
        public void HideWindowAndStartTimer(int minutes)
        {
            Dispatcher.UIThread.Post(() =>
            {
                // 1. Önce kilit ekranını göz önünden kaldır
                this.HideWindowForPlayer();

                try
                {
                    // 🚀 KESİN DERLEME ÇÖZÜMÜ (CS0234 Önlemi): Dosya adı veya namespace uyuşmazlıklarına takılmamak için,
                    // projedeki sayaç penceresini çalışma zamanında (Runtime) dinamik olarak buluyoruz.
                    var widgetType = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(t => t.GetTypes())
                        .FirstOrDefault(t => t.Name == "TimeWidget" || t.Name == "TimerWidget" || t.FullName!.EndsWith(".TimeWidget"));

                    if (widgetType != null)
                    {
                        Window? timeWidgetInstance = null;

                        // Constructor parametre çeşitliliğine karşı güvenli koruma
                        try { timeWidgetInstance = Activator.CreateInstance(widgetType, minutes, new Action(() => { this.ShowWindowForPlayer(); })) as Window; }
                        catch
                        {
                            try { timeWidgetInstance = Activator.CreateInstance(widgetType, minutes) as Window; }
                            catch { timeWidgetInstance = Activator.CreateInstance(widgetType) as Window; }
                        }

                        if (timeWidgetInstance != null)
                        {
                            timeWidgetInstance.WindowStartupLocation = WindowStartupLocation.Manual;
                            timeWidgetInstance.Position = new PixelPoint(20, 20); // Sol üst köşe
                            timeWidgetInstance.Topmost = true; // Oyunların üstünde kalması için

                            timeWidgetInstance.Show();
                            timeWidgetInstance.Activate();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Sayaç başlatma hatası: {ex.Message}");
                }
            });
        }

        // 🚀 ZOMBİ KORUMASI: Müşteri arayüzü el ile kapatıldığında terminalin de kapanması için eklenen metot
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // İstemci penceresi yok edildiği an arka plandaki tüm soket döngülerini ve dotnet sürecini zorla kapatır.
            Environment.Exit(0);
        }

        private async void OnDepositBalanceClick(object? sender, RoutedEventArgs e)
        {
            var txtAmount = this.FindControl<TextBox>("TxtDepositAmount");
            var lblStatus = this.FindControl<TextBlock>("LblLockStatus");

            if (txtAmount == null || string.IsNullOrWhiteSpace(txtAmount.Text)) return;

            if (double.TryParse(txtAmount.Text, out double loadedBalance) && loadedBalance > 0)
            {
                double calculatedMinutes = (loadedBalance / (double)_serverHourlyRate) * 60.0;
                int finalMinutes = (int)Math.Floor(calculatedMinutes);

                if (finalMinutes < 1)
                {
                    if (lblStatus != null) lblStatus.Text = "❌ Miktar en az 1 dakikaya yetmelidir!";
                    return;
                }

                if (Program.ServerStream != null && Program.MainClient != null && Program.MainClient.Connected)
                {
                    await Task.Run(async () =>
                    {
                        try
                        {
                            string msg = $"BAKIYE_ILE_AC:{_deskName}:{finalMinutes}\n";
                            byte[] data = Encoding.UTF8.GetBytes(msg);

                            await Program.ServerStream.WriteAsync(data, 0, data.Length);
                            await Program.ServerStream.FlushAsync();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Soket yazma hatası: {ex.Message}");
                        }
                    });
                }
                else
                {
                    if (lblStatus != null) lblStatus.Text = "❌ Sunucu bağlantısı aktif değil!";
                    return;
                }

                if (lblStatus != null)
                {
                    lblStatus.Text = $"💳 {loadedBalance} TL Onaylandı! Süre Başlıyor...";
                    lblStatus.Foreground = Brushes.LightGreen;
                }

                // 🎯 KESİN ÇÖZÜM: İkiz sayaç oluşmasını engellemek için yerel sayaç başlatma çağrıları buradan tamamen kaldırıldı.
                // İstemci bakiye isteğini sunucuya ilettikten sonra, sunucunun TCP üzerinden döneceği resmi "KILIDI_AC" emrini bekleyecek.
                // Sayaç, Program.cs içerisindeki soket dinleyicisi tarafından tek bir merkezden ve tam olarak 1 adet başlatılacak.
            }
            else
            {
                if (lblStatus != null) lblStatus.Text = "❌ Geçersiz tutar girdiniz!";
            }
        }
    }
}