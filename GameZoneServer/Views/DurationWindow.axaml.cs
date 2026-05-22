using Avalonia.Controls;
using System;

namespace GameZoneServer.Views
{
    public partial class DurationWindow : Window
    {
        public int SelectedMinutes { get; private set; } = 0;

        public DurationWindow(string deskName)
        {
            InitializeComponent();
            LblTitle.Text = $"⚡ {deskName} Oturumu İçin Süre (DK)";

            BtnConfirm.Click += (s, e) =>
            {
                if (int.TryParse(TxtMinutes.Text, out int mins) && mins > 0)
                {
                    SelectedMinutes = mins;
                    this.Close();
                }
            };
        }
    }
}