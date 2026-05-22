using System;
using System.Collections.Concurrent;
using System.IO;
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
                Console.WriteLine($"[SUNUCU] TCP dinlemesi başlatıldı. Port: {port}");

                while (true)
                {
                    TcpClient client = await server.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AĞ HATASI] Dinleme motoru hatası: {ex.Message}");
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            string? currentDeskName = null;

            try
            {
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    while (true)
                    {
                        string? message = await reader.ReadLineAsync();
                        if (message == null) break;

                        message = message.Trim();

                        if (message.StartsWith("GIRIS:"))
                        {
                            currentDeskName = message.Split(':')[1];
                            Console.WriteLine($"[BAĞLANTI] {currentDeskName} bağlandı.");
                            ActiveClients.AddOrUpdate(currentDeskName, client, (key, oldClient) => client);

                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                if (App.CurrentMainWindow is Views.MainWindow mainWin)
                                    mainWin.ActivateDeskOnUI(currentDeskName, client);
                            });
                        }
                        // OYUNCU MASASININ SÜRESİ BİTTİĞİNDE BURASI TETİKLENİR
                        else if (message == "SURE_BITTI")
                        {
                            Console.WriteLine($"[OTOMASYON] {currentDeskName} süresi dolduğu için kilitlendi.");
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                if (App.CurrentMainWindow is Views.MainWindow mainWin)
                                    mainWin.DeactivateDeskOnUI(currentDeskName!);
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KOPMA] {currentDeskName} bağlantısı kesildi: {ex.Message}");
                if (!string.IsNullOrEmpty(currentDeskName))
                {
                    ActiveClients.TryRemove(currentDeskName, out _);
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (App.CurrentMainWindow is Views.MainWindow mainWin)
                            mainWin.DeactivateDeskOnUI(currentDeskName);
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