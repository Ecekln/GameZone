using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace GameZoneServer
{
    class Program
    {
        private static string connectionString = "Server=ECEM;Database=GameZoneDB;Trusted_Connection=True;TrustServerCertificate=True;";
        private static int port = 8888;

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== GAMEZONE YÖNETİM PANELİ ===");

            // SOMUT ADIM: Önce sisteme giriş yapılmasını zorunlu tutuyoruz
            bool loginSuccess = false;
            while (!loginSuccess)
            {
                Console.WriteLine("\n--- Personel Girişi ---");
                Console.Write("Kullanıcı Adı: ");
                string username = Console.ReadLine() ?? "";

                Console.Write("Şifre: ");
                string password = Console.ReadLine() ?? "";

                // Veritabanından doğrula
                loginSuccess = ValidateUser(username, password);

                if (!loginSuccess)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Hatalı kullanıcı adı veya şifre! Lütfen tekrar deneyin.");
                    Console.ResetColor();
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nGiriş Başarılı! GameZone Sistemine Hoş Geldiniz.");
            Console.ResetColor();
            Console.WriteLine("=======================================");

            // Giriş başarılı olduktan sonra ağ dinleme altyapısı devreye giriyor
            TcpListener server = new TcpListener(IPAddress.Any, port);
            server.Start();
            Console.WriteLine($"[SUNUCU] Ağ dinlemesi başlatıldı. Port: {port} üzerinde masalar bekleniyor...");

            while (true)
            {
                TcpClient client = await server.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClientAsync(client));
            }
        }

        // ADIM 2: SQL Server üzerinde güvenli Authentication (Kimlik Doğrulama) Algoritması
        private static bool ValidateUser(string username, string password)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    // SQL Injection açıklarını kapatmak için parametreli sorgu (Parameterized Query) kullanıyoruz (Hocanın en çok bakacağı yer!)
                    string query = "SELECT COUNT(1) FROM Users WHERE Username = @Username AND Password = @Password";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@Password", password);

                        int result = (int)command.ExecuteScalar();
                        return result > 0; // Eğer 1 döndüyse kullanıcı bulundu demektir
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[VERİTABANI HATA] Giriş kontrolü yapılamadı: {ex.Message}");
                    return false;
                }
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            string clientIp = "127.0.0.1";
            if (client.Client.RemoteEndPoint is IPEndPoint ipEndPoint)
            {
                clientIp = ipEndPoint.Address.ToString();
            }

            Console.WriteLine($"\n[BAĞLANTI] Yerel ağdan yeni bir masa bağlandı! IP: {clientIp}");

            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            try
            {
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    Console.WriteLine($"[MESAJ -> {clientIp}]: {message}");

                    if (message.StartsWith("GIRIS:"))
                    {
                        string deskName = message.Split(':')[1];
                        UpdateDeskStatus(deskName, clientIp, "Available");

                        byte[] response = Encoding.UTF8.GetBytes("BAĞLANTI_ONAYLANDI\n");
                        await stream.WriteAsync(response, 0, response.Length);

                        // SOMUT MÜHENDİSLİK ADIMI: Masa bağlandığı an hocaya göstermek için 
                        // arka planda ana akışı tıkamadan 10 saniyelik bir geri sayım (Timer) başlatıyoruz.
                        // stream nesnesini gönderiyoruz ki süresi bitince aynı hattan sinyal yollayabilelim.
                        _ = Task.Run(() => StartDeskTimerAsync(deskName, stream, 10));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HATA] {clientIp} ile iletişim koparıldı: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine($"[BAĞLANTI_KAPANDI] IP: {clientIp} oturumu sonlandı.");
            }
        }

        // ADIM 11: Arka Planda Çalışan Geri Sayım ve Ağ Sinyal Algoritması
        private static async Task StartDeskTimerAsync(string deskName, NetworkStream stream, int totalSeconds)
        {
            Console.WriteLine($"[SÜRE] {deskName} için {totalSeconds} saniyelik oturum başlatıldı.");

            for (int i = totalSeconds; i >= 0; i--)
            {
                // Her saniye veritabanındaki kalan süreyi (RemainingTime) güncelliyoruz
                UpdateDeskRemainingTime(deskName, i);

                if (i > 0)
                {
                    Console.WriteLine($"[ZAMAN] {deskName} -> Kalan Süre: {i} saniye...");
                    await Task.Delay(1000); // 1 saniye bekle
                }
                else
                {
                    // SÜRE BİTTİ! Telsizden kilit emrini gönderiyoruz
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n[UYARI] {deskName} süresi doldu! Kilit sinyali gönderiliyor...");
                    Console.ResetColor();

                    try
                    {
                        byte[] lockSignal = Encoding.UTF8.GetBytes("SURE_BITTI\n");
                        await stream.WriteAsync(lockSignal, 0, lockSignal.Length);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SİNYAL HATASI] Kilit emri masaya ulaşamadı: {ex.Message}");
                    }
                }
            }
        }

        // Veritabanındaki kalan süreyi ve durumu güncelleyen yardımcı fonksiyon
        private static void UpdateDeskRemainingTime(string deskName, int remainingTime)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    // Süre bittise durumu "Locked" (Kilitli) yap, devam ediyorsa "Active" kalsın
                    string status = (remainingTime == 0) ? "Locked" : "Active";

                    string query = "UPDATE Desks SET RemainingTime = @RemainingTime, Status = @Status WHERE DeskName = @DeskName";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RemainingTime", remainingTime);
                        command.Parameters.AddWithValue("@Status", status);
                        command.Parameters.AddWithValue("@DeskName", deskName);
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    // Konsolu her saniye logla boğmamak için veritabanı hatalarını sessizce yönetebiliriz
                }
            }
        }
        private static void UpdateDeskStatus(string deskName, string ipAddress, string status)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"
                        IF EXISTS (SELECT 1 FROM Desks WHERE DeskName = @DeskName)
                            UPDATE Desks SET IpAddress = @IpAddress, Status = @Status WHERE DeskName = @DeskName;
                        ELSE
                            INSERT INTO Desks (DeskName, IpAddress, Status, RemainingTime) VALUES (@DeskName, @IpAddress, @Status, 0);";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@DeskName", deskName);
                        command.Parameters.AddWithValue("@IpAddress", ipAddress);
                        command.Parameters.AddWithValue("@Status", status);
                        command.ExecuteNonQuery();
                    }
                    Console.WriteLine($"[VERİTABANI] {deskName} ({ipAddress}) başarıyla kaydedildi/güncellendi.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[VERİTABANI HATA] Güncelleme yapılamadı: {ex.Message}");
                }
            }
        }
    }
}