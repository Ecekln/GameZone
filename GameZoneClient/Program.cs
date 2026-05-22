using System;
using System.IO;
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
        public static string DeskName { get; private set; } = "Masa-01";
        public static LockWindow? lockWindow = null!;

        [STAThread]
        public static void Main(string[] args)
        {
            // Eğer terminalden "dotnet run -- Masa-02" gibi argüman verilirse masanın adı dinamik değişir
            if (args.Length > 0)
            {
                DeskName = args[0];
            }

            Task.Run(() => ConnectToServerAsync());
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
                    await client.ConnectAsync(serverIp, port);

                    using (NetworkStream stream = client.GetStream())
                    {
                        // Dinamik masa ismini sunucuya gönderiyoruz
                        string loginMessage = $"GIRIS:{DeskName}\n";
                        byte[] data = Encoding.UTF8.GetBytes(loginMessage);
                        await stream.WriteAsync(data, 0, data.Length);
                        await stream.FlushAsync();

                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            while (true)
                            {
                                string? response = await reader.ReadLineAsync();
                                if (response == null) break;

                                response = response.Trim();

                                if (response.StartsWith("KILIDI_AC:"))
                                {
                                    int minutes = int.Parse(response.Split(':')[1]);
                                    Console.WriteLine($"[İSTEMCİ] {minutes} dakikalık süre alındı, masaüstü serbest!");

                                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                    {
                                        // Sayacı (Widget) oluşturuyoruz
                                        TimerWidget widget = new TimerWidget(minutes, async () =>
                                        {
                                            // SÜRE BİTTİĞİNDE ÇALIŞACAK BLOK:
                                            Console.WriteLine("[İSTEMCİ] Süre doldu! Kilit ekranı yeniden esir alıyor.");
                                            Program.lockWindow?.ShowWindowForPlayer();

                                            // Sunucuya sürenin bittiğini bildiriyoruz ki kartı griye döndürsün
                                            try
                                            {
                                                byte[] timeoutData = Encoding.UTF8.GetBytes("SURE_BITTI\n");
                                                await stream.WriteAsync(timeoutData, 0, timeoutData.Length);
                                                await stream.FlushAsync();
                                            }
                                            catch
                                            {
                                                Console.WriteLine("[AĞ HATASI] Süre bitti bildirimi sunucuya iletilemedi.");
                                            }
                                        });

                                        // 1. Ana kilit perdesini gizle
                                        Program.lockWindow?.HideWindowForPlayer();

                                        // 2. Sayacı şimdi fırlat ve odağı (focus) almasını zorla!
                                        widget.Show();
                                        widget.Activate(); // Arkaya kaçmasını engeller, öne yapıştırır
                                        widget.Focus();    // Klavye/Fare odağını üzerine çeker
                                    });
                                }
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
                Program.lockWindow = new LockWindow();
                desktop.MainWindow = Program.lockWindow;

                // Başlık çubuğunda hangi masa olduğunu gösterir
                Program.lockWindow.Title = $"Kilit Ekranı - {Program.DeskName}";
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}