using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GameZoneClient.Views;
using System;
using System.Linq;

namespace GameZoneClient
{
    public partial class App : Application
    {
        public static LockWindow? PlayerLockWindow { get; private set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                PlayerLockWindow = new LockWindow();
                desktop.MainWindow = PlayerLockWindow;
                PlayerLockWindow.ShowWindowForPlayer();
            }

            base.OnFrameworkInitializationCompleted();
        }

        // 🎯 MERKEZİ SAYAÇ MOTORU: Projenin neresinden çağrılırsa çağrılsın, 
        // o çok istediğin yeşil sayaç kutusunu (TimeWidget) sol üst köşeye hatasız fırlatır!
        public static void LaunchTimeWidgetCentral(int minutes)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        var activeWindows = desktop.Windows.ToList();

                        // Eğer ekranda zaten bir sayaç varsa mükerrer açma, es geç
                        var existingWidget = activeWindows.FirstOrDefault(w => w.GetType().Name.Contains("Widget"));
                        if (existingWidget != null) return;

                        // Projedeki TimeWidget tipini en kararlı şekilde buluyoruz
                        var widgetType = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(assembly => assembly.GetTypes())
                            .FirstOrDefault(t => t.Name == "TimeWidget" || t.FullName!.EndsWith(".TimeWidget"));

                        if (widgetType != null)
                        {
                            Window? timeWidgetInstance = null;

                            // Kurucu metot (Constructor) tipine göre pürüzsüz eşleşme
                            try { timeWidgetInstance = Activator.CreateInstance(widgetType, minutes) as Window; }
                            catch
                            {
                                try { timeWidgetInstance = Activator.CreateInstance(widgetType, minutes.ToString()) as Window; }
                                catch { timeWidgetInstance = Activator.CreateInstance(widgetType) as Window; }
                            }

                            if (timeWidgetInstance != null)
                            {
                                // 🚀 UX AYARI: Yeşil widget'ı sol üst köşeye çivileme kuralları
                                timeWidgetInstance.WindowStartupLocation = WindowStartupLocation.Manual;
                                timeWidgetInstance.Position = new PixelPoint(20, 20); // Sol üstten 20px
                                timeWidgetInstance.Topmost = true; // Her şeyin önünde parlasın

                                timeWidgetInstance.Show();
                                timeWidgetInstance.Activate();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Merkezi sayaç başlatma hatası: {ex.Message}");
                }
            });
        }
    }
}