using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Watchdog_Server_SL_AvantagEnergy
{
    internal static class EmailSender
    {
        public static async Task SendFailureEmail(WatchdogServer.ClientInfo clientInfo)
        {
            LogDev("Start des E-Mail-Versandprozesses.");

            try
            {
                LogDev("Lade Client-Informationen aus Datei.");
                clientInfo = LoadClientInfoFromFile(clientInfo.IP);
                LogDev("Client-Informationen erfolgreich geladen.");
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

            LogDev("Erstelle SmtpClient.");
            SmtpClient client = new SmtpClient("smtp.web.de")
            {
                Port = 465, // or 465 for SSL
                Credentials = new NetworkCredential("avantag.errorlog@web.de", "Avantag123"),
                EnableSsl = true,
            };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            try
            {
                LogDev("Sende E-Mail.");
                await client.SendMailAsync(message);
                Console.WriteLine("Ausfall-E-Mail gesendet.");
                LogDev("Ausfall-E-Mail gesendet.");
                LogEmailSent(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Senden der E-Mail: {ex.Message}");
                LogDev($"Fehler beim Senden der E-Mail: {ex.Message}\n{ex.StackTrace}");
            }

            LogDev("Ende des E-Mail-Versandprozesses.");
        }

        private static WatchdogServer.ClientInfo LoadClientInfoFromFile(string ip)
        {
            string filePath = $"C:\\Users\\U23551\\Documents\\GitHub\\WatchdogWAGO\\Watchdog_Server_SL_AvantagEnergy\\Watchdog_Server_SL_AvantagEnergy\\bin\\Debug\\net8.0\\clients\\{ip}.cfg";
            LogDev($"Lade Client-Informationen aus Datei: {filePath}");

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

                LogDev("Client-Informationen erfolgreich aus Datei geladen.");
                return clientInfo;
            }

            LogDev($"Datei nicht gefunden: {filePath}");
            throw new FileNotFoundException($"Datei nicht gefunden: {filePath}");
        }

        private static void LogDev(string message)
        {
            string logMessage = $"{DateTime.Now}: {message}";
            Console.WriteLine(logMessage);
            File.AppendAllText("server_devlog.txt", logMessage + Environment.NewLine);
        }

        private static void LogEmailSent(MailMessage message)
        {
            string logFilePath = "Alarmlogs\\AlarmMailSend.log";
            string logMessage = $"{DateTime.Now}: E-Mail gesendet an {message.To} mit Betreff '{message.Subject}' und Inhalt:\n{message.Body}";
            Console.WriteLine(logMessage);
            File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
        }
    }
}
