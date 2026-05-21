using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using GameZoneClient.Views;

namespace GameZoneClient
{
    class Program
    {
        private static string serverIp = "127.0.0.1";
        private static int port = 8888;
        private static string deskName = "Masa-01";
        public static LockWindow? lockWindow = null!; // Uyarıyı susturan ve dışa açan güncel tanım

        [STAThread]
        public static void Main(string[] args)
        {
            // Arka planda sunucuyla soket bağlantısını canlı tutan motoru başlatıyoruz
            Task.Run(() => ConnectToServerAsync());

            // Ön planda aşılmaz kilit perdemizi tam ekran açıyoruz
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();

        private static async Task ConnectToServerAsync()
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    // Sunucu kapılarına dayan
                    await client.ConnectAsync(serverIp, port);

                    using (NetworkStream stream = client.GetStream())
                    {
                        // Ben Masa-01, bağlandım! de
                        string loginMessage = $"GIRIS:{deskName}\n";
                        byte[] data = Encoding.UTF8.GetBytes(loginMessage);
                        await stream.WriteAsync(data, 0, data.Length);

                        byte[] buffer = new byte[1024];
                        // GameZoneClient/Program.cs içindeki while döngüsünün içini güncelle:
                        while (true)
                        {
                            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                            if (bytesRead == 0) break;

                            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                            if (response == "KILIDI_AC")
                            {
                                // UI Thread'e sızıp kilit ekranını gizliyoruz! Windows serbest kalıyor!
                                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                {
                                    Program.lockWindow?.HideWindowForPlayer();
                                });
                            }
                            else if (response == "SURE_BITTI")
                            {
                                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                {
                                    Program.lockWindow?.ShowWindowForPlayer();
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BAĞLANTI HATASI] Sunucuya ulaşılamıyor: {ex.Message}");
            }
        }
    }

    public class App : Application
    {
        public override void Initialize()
        {
            Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
            base.Initialize();
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                // BAŞINA PROGRAM. EKLEYEREK DERLEYİCİYE YOLUNU GÖSTERİYORUZ
                Program.lockWindow = new LockWindow();
                desktop.MainWindow = Program.lockWindow;
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}