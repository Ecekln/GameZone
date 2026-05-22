using Avalonia.Controls;
using Avalonia.Threading;
using System;

namespace GameZoneClient.Views
{
    public partial class TimerWidget : Window
    {
        private TimeSpan _remainingTime;
        private DispatcherTimer? _timer;
        private Action _onTimeUp;

        public TimerWidget(int minutes, Action onTimeUp)
        {
            InitializeComponent();
            _remainingTime = TimeSpan.FromMinutes(minutes);
            _onTimeUp = onTimeUp;

            // MÜHENDİSLİK DOKUNUŞU: Sayacı ekranın sağ üstü yerine tam ortasında başlatıyoruz
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
            LblTimer.Text = _remainingTime.ToString(@"hh\:mm\:ss");
        }
    }
}