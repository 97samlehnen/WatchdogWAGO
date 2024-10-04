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
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"----------------------- {Firma} -----------------------");
            Console.WriteLine($"Watchdog Server läuft auf IP {localAddr} und Port {Port}...");
            Console.WriteLine($"Watchdog Server Version: {Version}");
            Console.WriteLine($"---------------------------- {DEV} ----------------------------");
            Console.WriteLine(" ######### START LOGGING ");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Green;
            LogDev($"Watchdog Server gestartet auf IP {localAddr} und Port {Port}");
            Console.ResetColor();
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
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"ALARM: Kein Ping von {kvp.Value.IP} innerhalb von 20 Sekunden empfangen.");
                            Console.ResetColor();
                            LogAlarm($"ALARM: Kein Ping von {kvp.Value.IP} innerhalb von 20 Sekunden empfangen.");

                            // Client-Daten aus Datei laden
                            var clientInfo = SaveClientData.LoadClientInfoFromFile(kvp.Value.IP);
                            EmailSender.SendFailureEmail(clientInfo);

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

        private static void LogAlarm(string message)
        {
            string logFilePath = "logs\\Alarmlog.txt";
            string logMessage = $"{DateTime.Now}: {message}";
            File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
        }

        public static void HandleClient(object? obj)
        {
            if (obj is TcpClient client)
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead;
                ClientInfo clientInfo = new ClientInfo(); // Initialisierung von clientInfo

                try
                {
                    // Empfang der Daten
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string receivedData = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($">Empfangene Daten: {receivedData}");
                    Console.ResetColor();
                    LogDev($">Empfangene Daten: {receivedData}");

                    // Daten parsen
                    var dataParts = receivedData.Split(';');
                    foreach (var part in dataParts)
                    {
                        if (part.StartsWith("IP:"))
                        {
                            clientInfo.IP = part.Substring("IP:".Length);
                        }
                        else if (part.StartsWith("ProjectName:"))
                        {
                            clientInfo.ProjectName = part.Substring("ProjectName:".Length);
                        }
                        else if (part.StartsWith("Email:"))
                        {
                            clientInfo.Email = part.Substring("Email:".Length);
                        }
                    }

                    // Initialisierung der LastActivity
                    clientInfo.LastActivity = DateTime.Now;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($">Empfangene IP-Adresse: {clientInfo.IP}");
                    Console.WriteLine($">Empfangener Projektname: {clientInfo.ProjectName}");
                    Console.WriteLine($">Empfangene E-Mail-Adresse: {clientInfo.Email}");
                    Console.ResetColor();
                    LogDev($">Empfangene IP-Adresse: {clientInfo.IP}");
                    LogDev($">Empfangener Projektname: {clientInfo.ProjectName}");
                    LogDev($">Empfangene E-Mail-Adresse: {clientInfo.Email}");

                    // Speichern der Client-Informationen
                    SaveClientData.SaveClientInfoToFile(clientInfo);

                    // Hinzufügen des Clients zur aktiven Liste
                    activeClients.TryAdd(client, clientInfo);

                    // Empfang von Pings
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        string message = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                        if (message == clientInfo.ProjectName) // Überprüfung der Ping-Nachricht
                        {
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine($">Ping von {clientInfo.IP} empfangen.");
                            Console.ResetColor();
                            LogDev($">Ping von {clientInfo.IP} empfangen.");
                        }
                        else
                        {
                            Console.WriteLine($">Empfangen von {clientInfo.ProjectName}: {message}");
                            LogDev($">Empfangen von {clientInfo.IP} ({clientInfo.ProjectName}): {message}");
                        }
                        clientInfo.LastActivity = DateTime.Now; // Aktualisieren der letzten Aktivität
                        LogClientActivity(clientInfo, $">Empfangen: {message}");
                    }
                }
                catch (IOException ex) when (ex.InnerException is SocketException socketEx && socketEx.SocketErrorCode == SocketError.ConnectionReset)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Client Verbindung verloren.");
                    Console.ResetColor();
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
                        // Alarm wird nach 20 Sekunden ausgelöst, wenn der Client sich nicht wieder anmeldet
                        Task.Run(async () =>
                        {
                            await Task.Delay(2000);
                            if (!activeClients.ContainsKey(client))
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"ALARM: Client {clientInfo.IP} hat sich nicht innerhalb von 20 Sekunden wieder angemeldet.");
                                Console.ResetColor();
                                LogAlarm($"ALARM: Client {clientInfo.IP} hat sich nicht innerhalb von 20 Sekunden wieder angemeldet.");
                                await EmailSender.SendFailureEmail(clientInfo);
                            }
                        });
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
