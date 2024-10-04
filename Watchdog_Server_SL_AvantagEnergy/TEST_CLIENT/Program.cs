using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class WatchdogClient
{
    private static string ServerIP;
    private static int ServerPort;
    private static string ClientIP;
    private static string ProjectName;
    private static string Email;
    private static bool DevMode;

    static void Main(string[] args)
    {
        UpdateServer.DownloadServerIP();
        LoadConfig();
        CountdownAndConnect();
    }

    private static void CountdownAndConnect()
    {
        int countdown = 5;
        while (countdown > 0)
        {
            Console.WriteLine($"Verbindung in {countdown} Sekunden...");
            Thread.Sleep(1000); // 1 Sekunde warten
            countdown--;
        }

        try
        {
            using (TcpClient client = new TcpClient(ServerIP, ServerPort))
            {
                NetworkStream stream = client.GetStream();
                Console.WriteLine("Verbunden mit dem Watchdog-Server.");
                // Senden der IP-Adresse 
                SendMessage(stream, "IP", ClientIP);
                // Senden des Projektnamens 
                SendMessage(stream, "ProjectName", ProjectName);
                // Senden der E-Mail-Adresse 
                SendMessage(stream, "Email", Email);
                // Senden von Pings 
                while (true)
                {
                    string pingMessage =  ProjectName; // Ping-Nachricht
                    byte[] pingData = Encoding.ASCII.GetBytes(pingMessage);
                    stream.Write(pingData, 0, pingData.Length);
                    Console.WriteLine("Ping gesendet.");
                    LogDev("Ping gesendet.");
                    Thread.Sleep(5000); // Alle 5 Sekunden einen Ping senden 
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler: {ex.Message}");
            LogDev($"Fehler: {ex.Message}");
        }
    }

    private static void SendMessage(NetworkStream stream, string key, string value)
    {
        string message = $"{key}:{value};"; // Trennzeichen hinzufügen
        byte[] data = Encoding.ASCII.GetBytes(message);
        stream.Write(data, 0, data.Length);
        Console.WriteLine($"Nachricht gesendet: {message}");
        LogDev($"Nachricht gesendet: {message}");
    }


    private static void LoadConfig()
    {
        try
        {
            // Lesen der ServerIP aus der server_ip.txt Datei
            ServerIP = File.ReadAllText("server_ip.txt").Trim();
            Console.WriteLine($"ServerIP geladen: {ServerIP}");

            var configLines = File.ReadAllLines("config.txt");
            foreach (var line in configLines)
            {
                if (line.StartsWith("ServerPort="))
                {
                    ServerPort = int.Parse(line.Substring("ServerPort=".Length));
                }
                else if (line.StartsWith("ClientIP="))
                {
                    ClientIP = line.Substring("ClientIP=".Length);
                }
                else if (line.StartsWith("ProjectName="))
                {
                    ProjectName = line.Substring("ProjectName=".Length);
                }
                else if (line.StartsWith("Email="))
                {
                    Email = line.Substring("Email=".Length);
                }
                else if (line.StartsWith("DevMode="))
                {
                    DevMode = line.Substring("DevMode=".Length) == "1";
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Laden der Konfiguration: {ex.Message}");
            LogDev($"Fehler beim Laden der Konfiguration: {ex.Message}");
        }
    }

    private static void LogDev(string message)
    {
        if (DevMode)
        {
            string logMessage = $"{DateTime.Now}: {message}";
            File.AppendAllText("devlog.txt", logMessage + Environment.NewLine);
        }
    }
}
