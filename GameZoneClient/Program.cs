using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameZoneClient
{
    class Program
    {
        private static string serverIp = "127.0.0.1";
        private static int port = 8888;
        private static string deskName = "Masa-01";

        static async Task Main(string[] args)
        {
            Console.WriteLine($"=== GAMEZONE CLIENT ({deskName}) ===");
            Console.WriteLine("Server'a bağlanılmaya çalışılıyor...");

            try
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(serverIp, port);
                    Console.WriteLine("[BAĞLANTI] Server'a başarıyla bağlandı!");

                    using (NetworkStream stream = client.GetStream())
                    {
                        // Server'a kimlik bilgimizi gönderiyoruz
                        string loginMessage = $"GIRIS:{deskName}\n";
                        byte[] data = Encoding.UTF8.GetBytes(loginMessage);
                        await stream.WriteAsync(data, 0, data.Length);
                        Console.WriteLine($"[SİNYAL] Kimlik gönderildi: {loginMessage.Trim()}");

                        byte[] buffer = new byte[1024];

                        // SOMUT ADIM: Server'dan gelecek sinyalleri sürekli dinleyen bir döngü kuruyoruz
                        while (true)
                        {
                            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                            if (bytesRead == 0) break; // Sunucu bağlantıyı kapattıysa çık

                            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                            if (response == "BAĞLANTI_ONAYLANDI")
                            {
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine("[SERVER CEVABI]: BAĞLANTI_ONAYLANDI. Oturum başarıyla açıldı.");
                                Console.ResetColor();
                            }
                            // GERİ SAYIM ALGORİTMASI TETİKLENDİ: Süre bitti sinyali geldi!
                            else if (response == "SURE_BITTI")
                            {
                                LockComputer();
                                break; // Kilit ekranına geçildi, dinleme döngüsünden çık
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HATA] Server'a bağlanılamadı! Detay: {ex.Message}");
                Console.ReadLine();
            }
        }

        // ADIM 12: Masayı Kilitleyen ve Girdileri Simüle Olarak Donduran Fonksiyon
        private static void LockComputer()
        {
            Console.Clear();
            while (true)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Clear();

                Console.WriteLine("\n\n\n");
                Console.WriteLine("=========================================================================");
                Console.WriteLine($"        ⚠️  SÜRENİZ DOLDU! - {deskName.ToUpper()} KİLİTLENMİŞTİR ⚠️        ");
                Console.WriteLine("=========================================================================");
                Console.WriteLine("\nLütfen masayı boşaltın veya ana masadan sürenizi uzatın.");
                Console.WriteLine("Masa şu an kilitli moddadır. Herhangi bir işlem yapılamaz.");
                Console.WriteLine("\n=========================================================================");

                // Kullanıcının tuşlara basıp kilidi geçmesini engellemek için girdileri yutuyoruz
                Console.ReadKey(true);
            }
        }
    }
}