using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/*  *  Simeon Lehnen - okt.2024 */
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
        LoadConfig();

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
                    string pingMessage = "Ping"; // Ping-Nachricht
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
        string message = $"{key}: {value}";
        byte[] data = Encoding.ASCII.GetBytes(message);
        stream.Write(data, 0, data.Length);
        Console.WriteLine($"Nachricht gesendet: {message}");
        LogDev($"Nachricht gesendet: {message}");
    }

    private static void LoadConfig()
    {
        var configLines = File.ReadAllLines("config.txt");
        foreach (var line in configLines)
        {
            if (line.StartsWith("ServerIP="))
            {
                ServerIP = line.Substring("ServerIP=".Length);
            }
            else if (line.StartsWith("ServerPort="))
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

    private static void LogDev(string message)
    {
        if (DevMode)
        {
            string logMessage = $"{DateTime.Now}: {message}";
            File.AppendAllText("devlog.txt", logMessage + Environment.NewLine);
        }
    }
}
