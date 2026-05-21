using Avalonia.Controls;

namespace GameZoneServer.Views
{
    public partial class MainWindow : Window
    {
        private string _userRole;

        // Yapıcı metoda rol bilgisini zorunlu tutarak dışarıdan alıyoruz
        public MainWindow(string userRole)
        {
            InitializeComponent();
            _userRole = userRole;

            // İSTERE UYGUN ROL KONTROLÜ:
            if (_userRole == "Admin")
            {
                LblWelcome.Text = "👑 Yönetici Paneli (Tam Yetki)";
                // Admin her şeyi görür
                BtnReports.IsVisible = true;
                BtnSettings.IsVisible = true;
            }
            else // Staff / Personel ise
            {
                LblWelcome.Text = "🧑‍💼 Personel Ekranı (Sınırlı Yetki)";
                // MÜHENDİSLİK ADIMI: Kritik butonları kodla görünmez yapıyoruz!
                BtnReports.IsVisible = false;
                BtnSettings.IsVisible = false;
            }
        }
    }
}