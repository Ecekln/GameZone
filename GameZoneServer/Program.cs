using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Avalonia;

namespace GameZoneServer
{
    internal class Program
    {
        public static ConcurrentDictionary<string, TcpClient> ActiveClients { get; } = new ConcurrentDictionary<string, TcpClient>();
        public static ConcurrentDictionary<string, DateTime> DeskStartTime { get; } = new ConcurrentDictionary<string, DateTime>();
        public static ConcurrentDictionary<string, int> DeskAllocatedMinutes { get; } = new ConcurrentDictionary<string, int>();

        private static TcpListener? _listener;

        [STAThread]
        public static void Main(string[] args)
        {
            Console.WriteLine("Core: 🖥️ GameZone Sunucusu Başlatılıyor...");

            _listener = new TcpListener(IPAddress.Any, 5005);
            _listener.Start();
            Console.WriteLine("Core: 🛰️ Sunucu 5005 portunda kararlı dinlemede...");

            Task.Run(() => AcceptClientsAsync());

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();

        private static async Task AcceptClientsAsync()
        {
            try
            {
                while (true)
                {
                    TcpClient client = await _listener!.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Core Hatası: {ex.Message}");
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            string connectedDeskName = "Bilinmeyen Masa";
            NetworkStream stream = client.GetStream();

            // 🎯 ÇÖZÜM: StreamReader yerine ham byte okuyarak satır sonu (\n) takılmasını kökten çözüyoruz.
            byte[] buffer = new byte[1024];

            try
            {
                while (client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // Bağlantı koptu

                    string rawMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    if (string.IsNullOrWhiteSpace(rawMessage)) continue;

                    // Gelen mesajları tek tek satırlara bölüyoruz (Toplu paket koruması)
                    string[] lines = rawMessage.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        string cleanLine = line.Trim();
                        Console.WriteLine($"📥 [Soket Gelen]: '{cleanLine}'");

                        if (cleanLine.StartsWith("KAYIT:"))
                        {
                            connectedDeskName = cleanLine.Split(':').Last().Trim();

                            ActiveClients[connectedDeskName] = client;
                            DeskStartTime[connectedDeskName] = DateTime.Now; // Direkt yeşil yakmak için

                            Console.WriteLine($"✅ {connectedDeskName} başarıyla bağlandı ve AKTİF edildi.");
                            TetikleUI();
                        }
                        else if (cleanLine.StartsWith("BAKIYE_ILE_AC:"))
                        {
                            string[] parts = cleanLine.Split(':');
                            if (parts.Length > 2 && int.TryParse(parts[2], out int minutes))
                            {
                                string deskName = parts[1];
                                DeskStartTime[deskName] = DateTime.Now;
                                DeskAllocatedMinutes[deskName] = minutes;

                                string responseCmd = $"KILIDI_AC:{minutes}\n";
                                byte[] responseData = Encoding.UTF8.GetBytes(responseCmd);
                                await stream.WriteAsync(responseData, 0, responseData.Length);
                                await stream.FlushAsync();

                                Console.WriteLine($"💳 {deskName} oyuncu bakiyesiyle {minutes} dk açıldı.");
                                TetikleUI();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {connectedDeskName} Soket Hatası: {ex.Message}");
            }
            finally
            {
                if (ActiveClients.ContainsKey(connectedDeskName) && ActiveClients[connectedDeskName] == client)
                {
                    ActiveClients.TryRemove(connectedDeskName, out _);
                    DeskStartTime.TryRemove(connectedDeskName, out _);
                    DeskAllocatedMinutes.TryRemove(connectedDeskName, out _);
                    Console.WriteLine($"🔌 {connectedDeskName} bağlantısı güvenli kapatıldı.");
                }
                client.Close();
                TetikleUI();
            }
        }

        private static void TetikleUI()
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                // 🚀 ÇÖZÜM: Hem statik örneğe hem de uygulama pencerelerine doğrudan vuruyoruz
                GameZoneServer.Views.MainWindow.ForceRefreshUI();
            });
        }
    }
}