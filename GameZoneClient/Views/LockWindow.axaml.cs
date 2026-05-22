using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Linq;

namespace GameZoneClient.Views
{
    public partial class LockWindow : Window
    {
        private int _serverHourlyRate = 50;
        private string _deskName = "Masa-02";

        public LockWindow()
        {
            InitializeComponent();
            var btnDeposit = this.FindControl<Button>("BtnDepositBalance");
            if (btnDeposit != null) btnDeposit.Click += OnDepositBalanceClick;
        }

        // 🎯 KİLİT EKRANI SUNUCUDAN EMİR ALIP AÇILDIĞINDA ÇALIŞACAK MOTOR
        public void ShowWindowForPlayer()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!this.IsVisible)
                {
                    this.Show();
                }

                // 🔥 SAĞ ALTTAN SAYMAYA DEVAM EDEN WIDGET'I BULUP ZORLA KAPATIYORUZ 🔥
                var activeWindows = App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.Windows.ToList()
                    : new System.Collections.Generic.List<Window>();

                foreach (var win in activeWindows)
                {
                    // Kendisi (LockWindow) hariç ekranda kalan diğer tüm pencereleri (yani o widget'ı) tarıyor
                    if (win != this)
                    {
                        win.Close(); // Sayacı tutan o pencereyi bodoslama kapat ve imha et!
                        System.Diagnostics.Debug.WriteLine("İnatçı sayaç widget'ı başarıyla kapatıldı.");
                    }
                }
            });
        }

        public void HideWindowForPlayer()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (this.IsVisible)
                {
                    this.Hide();
                }
            });
        }

        private void OnDepositBalanceClick(object? sender, RoutedEventArgs e)
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

                if (lblStatus != null)
                {
                    lblStatus.Text = $"💳 {loadedBalance} TL Onaylandı!";
                    lblStatus.Foreground = Brushes.LightGreen;
                }

                this.HideWindowForPlayer();
            }
        }

        public void UpdateHourlyRate(int newRate)
        {
            _serverHourlyRate = newRate;
        }
    }
}