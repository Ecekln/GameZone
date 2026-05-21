using System;
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

        // .NET ve Avalonia UI için gerekli ana giriş noktası
        [STAThread]
        public static void Main(string[] args)
        {
            // 1. ADIM: Arka planda ağ dinleme (TCP Soket) motorunu ana akışı dondurmadan başlatıyoruz
            Task.Run(() => StartNetworkListenerAsync());

            // 2. ADIM: Ön planda kodla tasarladığımız görsel pencereleri ayağa kaldırıyoruz
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Avalonia uygulamasını başlatan ve Fluent (Modern) temayı yükleyen konfigürasyon
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();

        // Arka plan ağ dinleme motoru
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
                    // Her bağlanan masa için yeni bir iş parçacığı (Thread) ayır
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
            // İleride burası sağ taraftaki Masalar Grid'ini (DesksGrid) canlı güncellemek için 
            // arayüz katmanıyla doğrudan konuşacak! Şimdilik soket hattını açık tutuyor.
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            try
            {
                while (await stream.ReadAsync(buffer, 0, buffer.Length) > 0) { /* Mesajları dinle */ }
            }
            catch { /* Kopmaları yönet */ }
        }
    }

    // Avalonia'nın pencereleri ve Fluent temasını tanıması için gerekli yardımcı uygulama sınıfı
    public class App : Application
    {
        public override void Initialize()
        {
            // Modern koyu temamızı (FluentTheme) kodla yükliyoruz
            Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
            base.Initialize();
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Program İLK AÇILDIĞINDA çalışacak pencereyi LoginWindow (Giriş Ekranı) olarak ekiyoruz!
                desktop.MainWindow = new LoginWindow();
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}