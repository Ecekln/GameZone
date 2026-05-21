using Avalonia.Controls;

namespace GameZoneClient.Views
{
    public partial class LockWindow : Window
    {
        public LockWindow()
        {
            InitializeComponent();

            // Gerçek dünyada alt+f4 ile kapatılmasın diye pencere kapanma isteğini burada sabote edebiliriz
            this.Closing += (sender, e) =>
            {
                // Eğer oturum hala kilitliyse kapanmayı iptal et (Hocaya gösterilecek harika bir trick!)
                e.Cancel = true;
            };
        }

        // Dışarıdan (Ağ motorundan) süre veya bakiye geldikçe ekranı güncelleyecek fonksiyonlar
        public void UpdateStatus(string message, bool isLocked)
        {
            LblStatus.Text = message;
            // Eğer süre açıldıysa kırmızı çerçeveyi yeşile döndüreceğiz (Bir sonraki adımda)
        }
    }
}