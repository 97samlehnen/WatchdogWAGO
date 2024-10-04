using System;
using System.IO;
using System.Net;
using System.Net.Mail;

namespace Watchdog_Server_SL_AvantagEnergy
{
    internal static class EmailSender
    {
        public static void SendFailureEmail(WatchdogServer.ClientInfo clientInfo)
        {
            try
            {
                clientInfo = LoadClientInfoFromFile(clientInfo.IP);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Laden der Client-Informationen: {ex.Message}");
                LogDev($"Fehler beim Laden der Client-Informationen: {ex.Message}");
                return;
            }

            string to = "fernwirktechnik@avantag.energy";
            string from = "avantag.errorlog@web.de";
            string subject = "Client-Ausfallmeldung";
            string body = $"Client {clientInfo.IP} ({clientInfo.ProjectName}) hat sich nicht innerhalb von 15 Sekunden gemeldet.\nZeit: {DateTime.Now}";
            MailMessage message = new MailMessage(from, to, subject, body);
            SmtpClient client = new SmtpClient("smtp.web.de")
            {
                Port = 465, // or 587 for TLS
                Credentials = new NetworkCredential("avantag.errorlog@web.de", "Avantag123"),
                EnableSsl = true,
            };
            try
            {
                client.Send(message);
                Console.WriteLine("Ausfall-E-Mail gesendet.");
                LogDev("Ausfall-E-Mail gesendet.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Senden der E-Mail: {ex.Message}");
                LogDev($"Fehler beim Senden der E-Mail: {ex.Message}");
            }
        }

        private static WatchdogServer.ClientInfo LoadClientInfoFromFile(string ip)
        {
            string filePath = $"C:\\Users\\U23551\\Documents\\GitHub\\WatchdogWAGO\\Watchdog_Server_SL_AvantagEnergy\\Watchdog_Server_SL_AvantagEnergy\\bin\\Debug\\net8.0\\clients\\{ip}.txt";
            if (File.Exists(filePath))
            {
                var lines = File.ReadAllLines(filePath);
                var clientInfo = new WatchdogServer.ClientInfo();

                foreach (var line in lines)
                {
                    var parts = line.Split(": ");
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();

                        switch (key)
                        {
                            case "IP":
                                clientInfo.IP = value;
                                break;
                            case "ProjectName":
                                clientInfo.ProjectName = value;
                                break;
                            case "Email":
                                clientInfo.Email = value;
                                break;
                                // Fügen Sie hier weitere Fälle hinzu, falls nötig  
                        }
                    }
                }
                return clientInfo;
            }
            throw new FileNotFoundException($"Datei nicht gefunden: {filePath}");
        }

        private static void LogDev(string message)
        {
            string logMessage = $"{DateTime.Now}: {message}";
            File.AppendAllText("server_devlog.txt", logMessage + Environment.NewLine);
        }
    }
}
