using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Threading;


/* 
 *  Simeon Lehnen - okt.2024
 */


class WatchdogServer
{
    private static int Port;
    private static String Version;
    private static String DEV;
    private static String Firma;
    private static bool running = true;
    private static bool DevMode;
    private static ConcurrentDictionary<TcpClient, ClientInfo> activeClients = new ConcurrentDictionary<TcpClient, ClientInfo>();

    static void Main(string[] args)
    {
        LoadConfig();
        Version = "0.0.1";
        DEV = "Simeon Lehnen";
        Firma = "Avantag Energy S.á.r.l.";
        AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
        IPAddress localAddr = GetLocalIPAddress();
        TcpListener server = new TcpListener(localAddr, Port);
        server.Start();
        Console.WriteLine($"----------------------- {Firma} -----------------------");
        Console.WriteLine($"Watchdog Server läuft auf IP {localAddr} und Port {Port}...");
        Console.WriteLine($"Watchdog Server Version: {Version}");
        Console.WriteLine($"---------------------------- {DEV} ----------------------------");
        Console.WriteLine($" ######### START LOGGING ");
        LogDev($"Watchdog Server gestartet auf IP {localAddr} und Port {Port}");

        // Server-Thread um Verbindungen zu akzeptieren  
        Thread serverThread = new Thread(() =>
        {
            while (running)
            {
                if (server.Pending())
                {
                    TcpClient client = server.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(HandleClient, client);
                }
                Thread.Sleep(100);
            }
        });

        serverThread.Start();

        // Thread zur Überwachung aktiver Clients  
        Thread monitoringThread = new Thread(() =>
        {
            while (running)
            {
                DateTime threshold = DateTime.Now.AddSeconds(-15);
                foreach (var kvp in activeClients)
                {
                    if (kvp.Value.LastActivity < threshold)
                    {
                        LogClientActivity(kvp.Value, "Client hat sich nicht innerhalb von 15 Sekunden gemeldet.");
                        SendFailureEmail(kvp.Value);
                        activeClients.TryRemove(kvp.Key, out _);
                    }
                }
                Thread.Sleep(5000); // Alle 5 Sekunden aktualisieren  
            }
        });

        monitoringThread.Start();

        // Warten, bis das Programm geschlossen wird  
        while (running)
        {
            Thread.Sleep(100);
        }

        server.Stop();
        serverThread.Join();
        monitoringThread.Join();
    }

    private static void HandleClient(object? obj)
    {
        if (obj is TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024]; // Puffergröße festlegen  
            int bytesRead;

            try
            {
                // Empfang der IP-Adresse
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                string clientIP = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                Console.WriteLine($">Empfangene IP-Adresse: {clientIP}");
                LogDev($">Empfangene IP-Adresse: {clientIP}");

                // Empfang des Projektnamens
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                string projectName = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                Console.WriteLine($">Empfangener Projektname: {projectName}");
                LogDev($">Empfangener Projektname: {projectName}");

                // Empfang der E-Mail-Adresse
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                string email = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                Console.WriteLine($">Empfangene E-Mail-Adresse: {email}");
                LogDev($">Empfangene E-Mail-Adresse: {email}");

                var clientInfo = new ClientInfo
                {
                    Client = client,
                    IP = clientIP,
                    ProjectName = projectName,
                    Email = email,
                    LastActivity = DateTime.Now
                };

                // Speichern der Client-Informationen in einer Datei
                SaveClientInfoToFile(clientInfo);

                activeClients.TryAdd(client, clientInfo);
                LogClientActivity(clientInfo, "Client angemeldet.");
                LogDev($"Client angemeldet: {clientIP} ({projectName})");

                // Verarbeitung der Client-Nachrichten
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                    Console.WriteLine($">Empfangen von {clientIP} ({projectName}): {message}");
                    LogDev($">Empfangen von {clientIP} ({projectName}): {message}");
                    clientInfo.LastActivity = DateTime.Now; // Aktualisieren der letzten Aktivität  
                    LogClientActivity(clientInfo, $">Empfangen: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler: {ex.Message}");
                LogDev($"Fehler: {ex.Message}");

            }
            finally
            {
                activeClients.TryRemove(client, out var clientInfo);
                if (clientInfo != null)
                {
                    LogClientActivity(clientInfo, "Client getrennt.");
                    LogDev($"Client getrennt: {clientInfo.IP} ({clientInfo.ProjectName})");
                    SendFailureEmail(clientInfo);
                }
                client.Close();
            }
        }
    }

    private static void SaveClientInfoToFile(ClientInfo clientInfo)
    {
        string filePath = $"{clientInfo.IP}.txt";
        string content = $"IP: {clientInfo.IP}\nProjectName: {clientInfo.ProjectName}\nEmail: {clientInfo.Email}";
        File.WriteAllText(filePath, content);
        LogDev($"Client-Informationen in Datei gespeichert: {filePath}");
    }

    private static ClientInfo LoadClientInfoFromFile(string ip)
    {
        string filePath = $"{ip}.txt";
        if (File.Exists(filePath))
        {
            var lines = File.ReadAllLines(filePath);
            var clientInfo = new ClientInfo();

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
                    }
                }
            }
            return clientInfo;
        }
        throw new FileNotFoundException($"Datei nicht gefunden: {filePath}");
    }

    private static void SendFailureEmail(ClientInfo clientInfo)
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

    private static void LoadConfig()
    {
        var configLines = File.ReadAllLines("server_config.txt");
        foreach (var line in configLines)
        {
            if (line.StartsWith("Port="))
            {
                Port = int.Parse(line.Substring("Port=".Length));
            }
            else if (line.StartsWith("DevMode="))
            {
                DevMode = line.Substring("DevMode=".Length) == "1";
            }
        }
    }

    private static void OnProcessExit(object? sender, EventArgs e)
    {
        running = false;
        LogDev("Watchdog Server wird beendet.");
    }

    private static IPAddress GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip;
            }
        }
        throw new Exception("Keine IPv4-Adresse für das lokale System gefunden!");
    }

    private static void LogClientActivity(ClientInfo clientInfo, string message)
    {
        string logFilePath = $"{clientInfo.ProjectName}_{clientInfo.IP}.log";
        string logMessage = $"{DateTime.Now}: {message}";
        File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
    }

    private static void LogDev(string message)
    {
        if (DevMode)
        {
            string logMessage = $"{DateTime.Now}: {message}";
            File.AppendAllText("server_devlog.txt", logMessage + Environment.NewLine);
        }
    }

    private class ClientInfo
    {
        public TcpClient Client { get; set; } = null!;
        public string IP { get; set; } = null!;
        public string ProjectName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime LastActivity { get; set; }
    }
}
