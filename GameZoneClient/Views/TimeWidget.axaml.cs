using Avalonia.Controls;
using Avalonia.Threading;
using System;

namespace GameZoneClient.Views
{
    public partial class TimerWidget : Window
    {
        private TimeSpan _remainingTime;
        private DispatcherTimer? _timer;
        private Action? _onTimeUp;

        // 🚀 KESİN ÇÖZÜM: Avalonia RuntimeLoader'ın AVLN:0005 hatası vermesini
        // engellemek ve istemci projesinin derleme kilidini açmak için boş constructor eklendi.
        public TimerWidget()
        {
            InitializeComponent();
            _remainingTime = TimeSpan.FromMinutes(30); // Varsayılan süre önlemi
        }

        public TimerWidget(int minutes, Action onTimeUp)
        {
            InitializeComponent();
            _remainingTime = TimeSpan.FromMinutes(minutes);
            _onTimeUp = onTimeUp;

            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            UpdateText();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_remainingTime.TotalSeconds > 0)
            {
                _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));
                UpdateText();
            }
            else
            {
                _timer?.Stop();
                this.Close();
                _onTimeUp?.Invoke();
            }
        }

        private void UpdateText()
        {
            // 🎯 GÜVENLİK: Eğer XAML tarafında LblTimer yüklenirken gecikirse null referans hatası vermemesi için koruma ekledim.
            var lbl = this.FindControl<TextBlock>("LblTimer") ?? this.Find<TextBlock>("LblTimer");
            if (lbl != null)
            {
                lbl.Text = _remainingTime.ToString(@"hh\:mm\:ss");
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // Sayaç saymayı durdursun ve hafıza referansı temizlensin
            _timer?.Stop();
            _timer = null;
        }
    }
}