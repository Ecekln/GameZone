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

        public LockWindow()
        {
            InitializeComponent();

            var btnDeposit = this.FindControl<Button>("BtnDepositBalance");
            if (btnDeposit != null)
            {
                btnDeposit.Click += OnDepositBalanceClick;
            }

            try
            {
                var field = typeof(Program).GetField("_deskName", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                if (field != null)
                {
                    _deskName = field.GetValue(null)?.ToString() ?? "Masa-01";
                }
            }
            catch { }
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

                await Task.Delay(1000);

                // Sadece gizleniyoruz. Sayacı açma görevini tamamen Program.cs'e devrediyoruz!
                this.HideWindowForPlayer();
            }
            else
            {
                if (lblStatus != null) lblStatus.Text = "❌ Geçersiz tutar girdiniz!";
            }
        }
    }
}