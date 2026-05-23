using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameZoneClient
{
    internal class Program
    {
        public static TcpClient? MainClient;
        public static NetworkStream? ServerStream;
        private static string _deskName = "Masa-01";

        [STAThread]
        public static void Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                string fullArgs = string.Join(" ", args).Replace("--", "").Trim();
                if (!string.IsNullOrWhiteSpace(fullArgs))
                {
                    _deskName = fullArgs.Split(' ').Last().Trim();
                }
            }

            Console.WriteLine($"🛰️ GameZone İstemcisi Başlatılıyor: [{_deskName}]");

            Task.Run(() => ConnectToServerAsync());

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args ?? Array.Empty<string>());
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();

        private static async Task ConnectToServerAsync()
        {
            while (true)
            {
                try
                {
                    if (MainClient != null) { try { MainClient.Close(); } catch { } }

                    MainClient = new TcpClient("127.0.0.1", 5000);
                    ServerStream = MainClient.GetStream();

                    string registerMsg = $"KAYIT:{_deskName}\n";
                    byte[] registerData = Encoding.UTF8.GetBytes(registerMsg);
                    await ServerStream.WriteAsync(registerData, 0, registerData.Length);
                    await ServerStream.FlushAsync();

                    var reader = new StreamReader(ServerStream, Encoding.UTF8);
                    string? line;

                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        // 🎯 1. DURUM: OTURUM KAPATILDI
                        if (line == "KILIDI_AC:0" || line.StartsWith("KILIT_LE"))
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                                {
                                    var activeWindows = desktop.Windows.ToList();
                                    foreach (var win in activeWindows)
                                    {
                                        if (win.GetType().Name.Contains("Widget"))
                                        {
                                            win.Close();
                                        }
                                    }

                                    if (App.PlayerLockWindow != null)
                                    {
                                        App.PlayerLockWindow.ShowWindowForPlayer();
                                    }
                                }
                            });
                        }
                        // 🎯 2. DURUM: SÜRE AÇILDI (HEM PERSONEL HEM BAKİYE BURAYI TETİKLER!)
                        else if (line.StartsWith("KILIDI_AC:"))
                        {
                            string[] parts = line.Split(':');
                            if (parts.Length > 1 && int.TryParse(parts[1], out int minutes) && minutes > 0)
                            {
                                Dispatcher.UIThread.Post(() =>
                                {
                                    if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                                    {
                                        if (App.PlayerLockWindow != null)
                                        {
                                            App.PlayerLockWindow.HideWindowForPlayer();
                                        }

                                        var activeWindows = desktop.Windows.ToList();
                                        var existingWidget = activeWindows.FirstOrDefault(w => w.GetType().Name.Contains("Widget"));

                                        if (existingWidget == null)
                                        {
                                            // Projedeki orijinal TimeWidget tipini buluyoruz
                                            var widgetType = AppDomain.CurrentDomain.GetAssemblies()
                                                .SelectMany(t => t.GetTypes())
                                                .FirstOrDefault(t => t.Name == "TimeWidget" || t.FullName!.EndsWith(".TimeWidget"));

                                            if (widgetType != null)
                                            {
                                                Window? timeWidgetInstance = null;
                                                try { timeWidgetInstance = Activator.CreateInstance(widgetType, minutes) as Window; }
                                                catch
                                                {
                                                    try { timeWidgetInstance = Activator.CreateInstance(widgetType, minutes.ToString()) as Window; }
                                                    catch { timeWidgetInstance = Activator.CreateInstance(widgetType) as Window; }
                                                }

                                                if (timeWidgetInstance != null)
                                                {
                                                    // 🚀 KESİN ÇÖZÜM: Bağımsız ana soket thread'i üzerinden sol üst köşeye zımbalıyoruz!
                                                    timeWidgetInstance.WindowStartupLocation = WindowStartupLocation.Manual;
                                                    timeWidgetInstance.Position = new PixelPoint(20, 20); // Sol üst 20px boşluk
                                                    timeWidgetInstance.Topmost = true; // Her şeyin önünde parlasın

                                                    timeWidgetInstance.Show();
                                                    timeWidgetInstance.Activate();
                                                }
                                            }
                                        }
                                    }
                                });
                            }
                        }
                    }
                }
                catch
                {
                    await Task.Delay(2000);
                }
            }
        }
    }
}