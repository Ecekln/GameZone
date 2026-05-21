using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using GameZoneServer.Views;

namespace GameZoneServer
{
    class Program
    {
        private static int port = 8888;

        // MÜHENDİSLİK ADIMI: Aktif bağlantıları tutan güvenli havuz (Registry)
        public static ConcurrentDictionary<string, TcpClient> ActiveClients { get; } = new ConcurrentDictionary<string, TcpClient>();

        [STAThread]
        public static void Main(string[] args)
        {
            Task.Run(() => StartNetworkListenerAsync());
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();

        private static async Task StartNetworkListenerAsync()
        {
            try
            {
                TcpListener server = new TcpListener(IPAddress.Any, port);
                server.Start();
                Console.WriteLine($"[SUNUCU] Arka planda TCP dinlemesi başlatıldı. Port: {port}");

                while (true)
                {
                    TcpClient client = await server.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AĞ HATASI] Dinleme motoru başlatılamadı: {ex.Message}");
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            string? currentDeskName = null;

            try
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                    if (message.StartsWith("GIRIS:"))
                    {
                        currentDeskName = message.Split(':')[1];
                        Console.WriteLine($"[BAĞLANTI] {currentDeskName} yerel ağdan bağlandı!");

                        // Bağlantıyı havuza ekle (Eğer zaten varsa güncelle)
                        ActiveClients.AddOrUpdate(currentDeskName, client, (key, oldClient) => client);

                        // Eğer arayüz zaten açıksa anlık olarak yeşile boya
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            if (App.CurrentMainWindow is Views.MainWindow mainWin)
                            {
                                mainWin.ActivateDeskOnUI(currentDeskName, client);
                            }
                        });
                    }
                }

                while (true)
                {
                    if (stream.ReadByte() == -1) break;
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KOPMA] {currentDeskName} bağlantısı kesildi: {ex.Message}");
                if (!string.IsNullOrEmpty(currentDeskName))
                {
                    ActiveClients.TryRemove(currentDeskName, out _);
                    // Arayüz açıksa kartı tekrar griye döndür
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (App.CurrentMainWindow is Views.MainWindow mainWin)
                        {
                            mainWin.DeactivateDeskOnUI(currentDeskName);
                        }
                    });
                }
            }
        }
    }

    public class App : Application
    {
        public static Views.MainWindow? CurrentMainWindow { get; private set; }

        public override void Initialize()
        {
            Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
            base.Initialize();
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new LoginWindow();
            }
            base.OnFrameworkInitializationCompleted();
        }

        public static void SetMainWindow(Views.MainWindow window)
        {
            CurrentMainWindow = window;
        }
    }
}