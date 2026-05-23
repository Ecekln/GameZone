using Avalonia;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameZoneServer
{
    internal class Program
    {
        public static ConcurrentDictionary<string, TcpClient> ActiveClients = new ConcurrentDictionary<string, TcpClient>();
        public static ConcurrentDictionary<string, DateTime> DeskStartTime = new ConcurrentDictionary<string, DateTime>();
        public static ConcurrentDictionary<string, int> DeskAllocatedMinutes = new ConcurrentDictionary<string, int>();

        [STAThread]
        public static void Main(string[] args)
        {
            Task.Run(() => StartTcpServerAsync());
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();

        private static async Task StartTcpServerAsync()
        {
            try
            {
                TcpListener listener = new TcpListener(IPAddress.Any, 5000);
                listener.Start();
                Console.WriteLine("🌐 GameZone TCP Sunucusu 5000 portunda dinlemede...");

                while (true)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Soket Dinleme Hatası: {ex.Message}");
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            string? registeredDeskName = null;
            var stream = client.GetStream();
            var reader = new StreamReader(stream, Encoding.UTF8);

            try
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (line.StartsWith("KAYIT:"))
                    {
                        registeredDeskName = line.Split(':')[1];
                        ActiveClients[registeredDeskName] = client;

                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            GameZoneServer.Views.MainWindow.ActivateDeskOnUIFromSocket(registeredDeskName, client);
                        });
                    }
                    else if (line.StartsWith("SURE_BITTI:"))
                    {
                        string desk = line.Split(':')[1];
                        DeskStartTime.TryRemove(desk, out _);
                        DeskAllocatedMinutes.TryRemove(desk, out _);

                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            GameZoneServer.Views.MainWindow.DeactivateDeskOnUIFromSocket(desk);
                        });
                    }
                    // 🎯 KİLİT NOKTA: Mesaj geldiğinde masayı hem ağda aktif ediyoruz hem de yeşile boyuyoruz!
                    else if (line.StartsWith("BAKIYE_ILE_AC:"))
                    {
                        string[] parts = line.Split(':');
                        if (parts.Length > 2)
                        {
                            string incomingDeskName = parts[1];
                            if (int.TryParse(parts[2], out int minutes))
                            {
                                // Masayı sunucuya sahte bir soket kaydıyla bile olsa "Bağlı" olarak ekliyoruz
                                ActiveClients[incomingDeskName] = client;

                                // Süre süreçlerini başlatıyoruz
                                DeskStartTime[incomingDeskName] = DateTime.Now;
                                DeskAllocatedMinutes[incomingDeskName] = minutes;

                                // Arayüzü tetikleyip rengi yeşile döndürüyoruz
                                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                {
                                    GameZoneServer.Views.MainWindow.ActivateDeskOnUIFromSocket(incomingDeskName, client);
                                });
                            }
                        }
                    }
                }
            }
            catch
            {
                if (!string.IsNullOrEmpty(registeredDeskName))
                {
                    ActiveClients.TryRemove(registeredDeskName, out _);
                    DeskStartTime.TryRemove(registeredDeskName, out _);
                    DeskAllocatedMinutes.TryRemove(registeredDeskName, out _);

                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        GameZoneServer.Views.MainWindow.DeactivateDeskOnUIFromSocket(registeredDeskName);
                    });
                }
            }
        }
    }
}