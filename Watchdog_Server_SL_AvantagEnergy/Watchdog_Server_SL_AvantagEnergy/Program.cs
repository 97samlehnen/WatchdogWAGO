using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Watchdog_Server_SL_AvantagEnergy
{
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
            Console.WriteLine(" ######### START LOGGING ");
            LogDev($"Watchdog Server gestartet auf IP {localAddr} und Port {Port}");
            Console.WriteLine("Nach LogDev-Aufruf");

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
            Console.WriteLine("Server-Thread gestartet");

            Thread monitoringThread = new Thread(() =>
            {
                while (running)
                {
                    DateTime threshold = DateTime.Now.AddSeconds(-20);
                    foreach (var kvp in activeClients)
                    {
                        if (kvp.Value.LastActivity < threshold)
                        {
                            LogClientActivity(kvp.Value, "Client hat sich nicht innerhalb von 20 Sekunden gemeldet.");
                            Console.WriteLine($"ALARM: Kein Ping von {kvp.Value.IP} innerhalb von 20 Sekunden empfangen.");
                            EmailSender.SendFailureEmail(kvp.Value);
                            activeClients.TryRemove(kvp.Key, out _);
                        }
                    }
                    Thread.Sleep(5000);
                }
            });
            monitoringThread.Start();
            Console.WriteLine("Monitoring-Thread gestartet");

            while (running)
            {
                Thread.Sleep(100);
            }
            server.Stop();
            serverThread.Join();
            monitoringThread.Join();
            Console.WriteLine("Server gestoppt");
        }

        public static void HandleClient(object? obj)
        {
            if (obj is TcpClient client)
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead;
                ClientInfo? clientInfo = null;
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

                    clientInfo = new ClientInfo
                    {
                        //Client = client,
                        IP = clientIP,
                        ProjectName = projectName,
                        Email = email,
                        //LastActivity = DateTime.Now
                    };

                    Console.WriteLine("Speichere Client-Informationen...");
                    SaveClientData.SaveClientInfoToFile(clientInfo);
                    activeClients.TryAdd(client, clientInfo);
                    LogClientActivity(clientInfo, "Client angemeldet.");
                    LogDev($"Client angemeldet: {clientIP} ({projectName})");

                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        string message = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                        if (message == "Ping")
                        {
                            Console.WriteLine($">Ping von {clientIP} empfangen.");
                            LogDev($">Ping von {clientIP} empfangen.");
                        }
                        else
                        {
                            Console.WriteLine($">Empfangen von {clientIP} ({projectName}): {message}");
                            LogDev($">Empfangen von {clientIP} ({projectName}): {message}");
                        }
                        clientInfo.LastActivity = DateTime.Now; // Aktualisieren der letzten Aktivität
                        LogClientActivity(clientInfo, $">Empfangen: {message}");
                    }
                }
                catch (IOException ex) when (ex.InnerException is SocketException socketEx && socketEx.SocketErrorCode == SocketError.ConnectionReset)
                {
                    Console.WriteLine("Client Verbindung verloren.");
                    LogDev("Client Verbindung verloren.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fehler: {ex.Message}");
                    LogDev($"Fehler: {ex.Message}");
                }
                finally
                {
                    activeClients.TryRemove(client, out clientInfo);
                    if (clientInfo != null)
                    {
                        LogClientActivity(clientInfo, "Client getrennt.");
                        LogDev($"Client getrennt: {clientInfo.IP} ({clientInfo.ProjectName})");
                        EmailSender.SendFailureEmail(clientInfo);
                    }
                    client.Close();
                }
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
            string logFilePath = $"logs\\test.log";
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

        internal class ClientInfo
        {
            public TcpClient Client { get; set; } = null!;
            public string IP { get; set; } = null!;
            public string ProjectName { get; set; } = null!;
            public string Email { get; set; } = null!;
            public DateTime LastActivity { get; set; }
        }
    }
}
