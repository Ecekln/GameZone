using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace GameZoneServer.Views
{
    public partial class MainWindow : Window
    {
        private string _userRole;
        private int _hourlyRate = 50;
        private double _totalRevenue = 0.0;
        private static MainWindow? _instance;

        public MainWindow(string userRole)
        {
            InitializeComponent();
            _userRole = userRole;
            _instance = this;

            BtnDesks.Click += OnDesksButtonClick;
            BtnReports.Click += OnReportsButtonClick;
            BtnSettings.Click += OnSettingsButtonClick;
            BtnLogout.Click += OnLogoutButtonClick;

            if (_userRole == "Staff")
            {
                BtnSettings.IsVisible = false;
            }

            LoadMockDesks();
        }

        private void OnLogoutButtonClick(object? sender, RoutedEventArgs e)
        {
            LoginWindow loginWin = new LoginWindow();
            loginWin.Show();
            this.Close();
        }

        public static void ActivateDeskOnUIFromSocket(string deskName, TcpClient client)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _instance?.LoadMockDesks();
            });
        }

        public static void DeactivateDeskOnUIFromSocket(string deskName)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _instance?.LoadMockDesks();
            });
        }

        private void OnDesksButtonClick(object? sender, RoutedEventArgs e)
        {
            LblWelcome.Text = _userRole == "Admin" ? "👑 Yönetici Paneli -> Canlı Masalar" : "🧑‍💼 Personel Ekranı -> Canlı Masalar";
            LoadMockDesks();
        }

        private void OnReportsButtonClick(object? sender, RoutedEventArgs e)
        {
            LblWelcome.Text = "📊 Hasılat Raporları (SQL Server Canlı Verileri)";
            DesksGrid.Children.Clear();

            StackPanel reportPanel = new StackPanel { Spacing = 12, Margin = new Avalonia.Thickness(20), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };

            // 🎯 DÜZELTME: Hata veren satır jilet gibi temizlendi ve FontSize doğrudan 20 olarak setlendi
            reportPanel.Children.Add(new TextBlock
            {
                Text = $"Bugünkü Toplam Kasa Cirosu: {_totalRevenue:0.00} TL",
                FontSize = 20,
                Foreground = Brushes.LightGreen,
                FontWeight = FontWeight.Bold
            });

            DesksGrid.Children.Add(reportPanel);
        }

        private void OnSettingsButtonClick(object? sender, RoutedEventArgs e)
        {
            LblWelcome.Text = "⚙️ Sistem Ayarları ve Fiyatlandırma";
            DesksGrid.Children.Clear();
        }

        public void LoadMockDesks()
        {
            DesksGrid.Children.Clear();

            for (int i = 1; i <= 4; i++)
            {
                string deskName = $"Masa-0{i}";
                Border deskCard = new Border { BorderThickness = new Avalonia.Thickness(2), CornerRadius = new Avalonia.CornerRadius(8), Margin = new Avalonia.Thickness(10), Height = 100 };
                StackPanel cardContent = new StackPanel { VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Spacing = 5 };
                TextBlock txtName = new TextBlock { Text = deskName, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Brushes.White };
                TextBlock txtStatus = new TextBlock { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, FontSize = 12 };

                if (Program.ActiveClients.TryGetValue(deskName, out TcpClient? connectedClient))
                {
                    if (Program.DeskStartTime.ContainsKey(deskName))
                    {
                        deskCard.Background = new SolidColorBrush(Color.Parse("#1a3a2a"));
                        deskCard.BorderBrush = new SolidColorBrush(Color.Parse("#00ff88"));
                        txtStatus.Text = "🟢 Kullanımda (Süre Açık)";
                        txtStatus.Foreground = Brushes.LightGreen;
                    }
                    else
                    {
                        deskCard.Background = new SolidColorBrush(Color.Parse("#2d2d2d"));
                        deskCard.BorderBrush = new SolidColorBrush(Color.Parse("#a0a0a0"));
                        txtStatus.Text = "🟡 Bağlı / Kilitli (Beklemede)";
                        txtStatus.Foreground = Brushes.Yellow;
                    }
                    BindCardClickEvent(deskCard, deskName, connectedClient);
                }
                else
                {
                    deskCard.Background = new SolidColorBrush(Color.Parse("#222222"));
                    deskCard.BorderBrush = new SolidColorBrush(Color.Parse("#444444"));
                    txtStatus.Text = "Masa Çevrimdışı";
                    txtStatus.Foreground = Brushes.DarkGray;
                }

                cardContent.Children.Add(txtName);
                cardContent.Children.Add(txtStatus);
                deskCard.Child = cardContent;
                DesksGrid.Children.Add(deskCard);
            }
        }

        private void BindCardClickEvent(Border card, string deskName, TcpClient client)
        {
            card.PointerPressed += async (s, e) =>
            {
                try
                {
                    if (Program.DeskStartTime.ContainsKey(deskName))
                    {
                        Window confirmWin = new Window { Title = "Masa Kontrolü", Width = 320, Height = 150, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = new SolidColorBrush(Color.Parse("#1e1e1e")), Topmost = true };
                        StackPanel pnl = new StackPanel { Spacing = 15, Margin = new Avalonia.Thickness(20) };
                        Button btnStop = new Button { Content = "🛑 Oturumu Bitir ve Kilitle", Background = Brushes.Red, Foreground = Brushes.White, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
                        pnl.Children.Add(btnStop);
                        confirmWin.Content = pnl;

                        btnStop.Click += async (senderWin, eWin) =>
                        {
                            Program.DeskStartTime.TryRemove(deskName, out DateTime startTime);
                            Program.DeskAllocatedMinutes.TryRemove(deskName, out _);

                            TimeSpan elapsed = DateTime.Now - startTime;
                            double usedMinutes = elapsed.TotalMinutes;
                            if (usedMinutes < 1.0) usedMinutes = 1.0;

                            // 🎯 UYARI ÇÖZÜMÜ: _hourlyRate değişkenini burada hesaplamaya dahil ederek warning'i de eritiyoruz
                            double actualCost = ((double)_hourlyRate / 60.0) * usedMinutes;
                            _totalRevenue += actualCost;

                            NetworkStream stream = client.GetStream();
                            byte[] cmd = Encoding.UTF8.GetBytes("KILIDI_AC:0\n");
                            await stream.WriteAsync(cmd, 0, cmd.Length);
                            await stream.FlushAsync();

                            confirmWin.Close();
                            LoadMockDesks();
                        };

                        await confirmWin.ShowDialog(this);
                        return;
                    }

                    DurationWindow durationDialog = new DurationWindow(deskName);
                    durationDialog.Topmost = true;
                    durationDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                    await durationDialog.ShowDialog(this);

                    if (durationDialog.SelectedMinutes > 0)
                    {
                        NetworkStream stream = client.GetStream();
                        string commandText = $"KILIDI_AC:{durationDialog.SelectedMinutes}\n";
                        byte[] cmd = Encoding.UTF8.GetBytes(commandText);

                        await stream.WriteAsync(cmd, 0, cmd.Length);
                        await stream.FlushAsync();

                        Program.DeskStartTime[deskName] = DateTime.Now;
                        Program.DeskAllocatedMinutes[deskName] = durationDialog.SelectedMinutes;

                        LoadMockDesks();
                    }
                }
                catch { }
            };
        }
    }
}