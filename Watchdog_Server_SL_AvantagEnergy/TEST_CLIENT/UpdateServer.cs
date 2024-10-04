using System;
using System.IO;
using System.Net;

class UpdateServer
{
    public static void DownloadServerIP()
    {
        string url = "http://172.24.131.200:8000/ae/watchdog/update/server_ip.txt"; // URL zur server_ip.txt  - OFFLINE : LABOR
        string localPath = "server_ip.txt";

        using (WebClient client = new WebClient())
        {
            try
            {
                client.DownloadFile(url, localPath);
                Console.WriteLine("server_ip.txt erfolgreich heruntergeladen.");
                LogDev("server_ip.txt erfolgreich heruntergeladen.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Herunterladen der server_ip.txt: {ex.Message}");
                LogDev($"Fehler beim Herunterladen der server_ip.txt: {ex.Message}");
            }
        }
    }

    public static void DownloadClientConfig()
    {
        string url = "http://172.24.131.200:8000/ae/watchdog/update/config.txt"; // URL zur config.txt
        string localPath = "config.txt";

        using (WebClient client = new WebClient())
        {
            try
            {
                client.DownloadFile(url, localPath);
                Console.WriteLine("config.txt erfolgreich heruntergeladen.");
                LogDev("config.txt erfolgreich heruntergeladen.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Herunterladen der config.txt: {ex.Message}");
                LogDev($"Fehler beim Herunterladen der config.txt: {ex.Message}");
            }
        }
    }

    private static void LogDev(string message)
    {
        string logMessage = $"{DateTime.Now}: {message}";
        File.AppendAllText("devlog.txt", logMessage + Environment.NewLine);
    }
}
