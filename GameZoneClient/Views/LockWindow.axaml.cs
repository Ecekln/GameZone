using Avalonia.Controls;

namespace GameZoneClient.Views
{
    public partial class LockWindow : Window
    {
        private bool _canClose = false;

        public LockWindow()
        {
            InitializeComponent();

            // MÜHENDİSLİK DOKUNUŞU: Tasarımdaki boş etikete gerçek masa adını basıyoruz!
            LblDeskName.Text = Program.DeskName;

            // Oturum açıkken Alt+F4 ile pencerenin kapatılmasını engelleme mekanizması
            this.Closing += (sender, e) =>
            {
                if (!_canClose) e.Cancel = true;
            };
        }

        // Ağ motorundan "KİLİDİ_AC" emri geldiğinde tam ekranı gizleyen metot
        public void HideWindowForPlayer()
        {
            _canClose = true;
            this.Hide();
        }

        // Süre bittiğinde ekranı yeniden esir alan metot
        public void ShowWindowForPlayer()
        {
            _canClose = false;
            this.WindowState = WindowState.FullScreen;
            this.Show();
        }

        // Durum mesajlarını canlı güncellemek için
        public void UpdateStatus(string message, bool isLocked)
        {
            LblStatus.Text = message;
        }
    }
}