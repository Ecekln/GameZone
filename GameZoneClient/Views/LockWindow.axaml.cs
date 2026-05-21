using Avalonia.Controls;

namespace GameZoneClient.Views
{
    public partial class LockWindow : Window
    {
        private bool _canClose = false;

        // TEK VE NET YAPICI METOT (CS0111 hatasını çözen kısım)
        public LockWindow()
        {
            InitializeComponent();

            // Oturum açıkken Alt+F4 ile pencerenin kapatılmasını engelleme mekanizması
            this.Closing += (sender, e) =>
            {
                if (!_canClose) e.Cancel = true;
            };
        }

        // Ağ motorundan "KİLİDİ_AC" emri geldiğinde tam ekranı gizleyen metot
        public void HideWindowForPlayer()
        {
            _canClose = true; // Kapanma engelini geçici olarak devre dışı bırak
            this.Hide();      // Pencereyi arka plana gizle (Masaüstü serbest kalır)
        }

        // Süre bittiğinde ekranı yeniden esir alan metot
        public void ShowWindowForPlayer()
        {
            _canClose = false; // Kapanma engelini tekrar devreye al
            this.WindowState = WindowState.FullScreen;
            this.Show();       // Perdeyi yeniden tam ekran fırlat
        }

        // Durum mesajlarını canlı güncellemek için
        public void UpdateStatus(string message, bool isLocked)
        {
            LblStatus.Text = message;
        }
    }
}