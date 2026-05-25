using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace GameZoneServer.Views
{
    public partial class DurationWindow : Window
    {
        public int SelectedMinutes { get; private set; } = 0;
        private string _deskName = "";

        // 🚀 KESİN ÇÖZÜM: AVLN:0005 uyarısını kökten silmek ve XAML Loader'ı canlandırmak 
        // için boş (parametresiz) constructor ekliyoruz.
        public DurationWindow()
        {
            InitializeComponent();
        }

        // Mevcut parametreli constructor'ın (kodunda zaten olan kısım)
        public DurationWindow(string deskName)
        {
            InitializeComponent();
            _deskName = deskName;

            // Eğer başlık veya etiket ataması yapıyorsan buradadır:
            var lblTitle = this.FindControl<TextBlock>("LblTitle");
            if (lblTitle != null) lblTitle.Text = $"{_deskName} Süre Ayarı";
        }

        // Örnek buton tıklama metotların (Değiştirmene gerek yok, aynen kalsın)
        private void OnMinutesButtonClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int mins))
            {
                SelectedMinutes = mins;
                this.Close();
            }
        }
    }
}