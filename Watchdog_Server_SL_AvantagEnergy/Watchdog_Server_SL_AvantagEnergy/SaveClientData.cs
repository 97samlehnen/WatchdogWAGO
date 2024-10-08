using System;
using System.IO;

namespace Watchdog_Server_SL_AvantagEnergy
{
    internal static class SaveClientData
    {
        public static void SaveClientInfoToFile(WatchdogServer.ClientInfo clientInfo)
        {
            string directoryPath = "C:\\Users\\U23551\\Documents\\GitHub\\WatchdogWAGO\\Watchdog_Server_SL_AvantagEnergy\\Watchdog_Server_SL_AvantagEnergy\\bin\\Debug\\net8.0\\clients";
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string filePath = Path.Combine(directoryPath, $"{clientInfo.IP}.cfg");
            string newContent = $"IP={clientInfo.IP}\nProjectName={clientInfo.ProjectName}\nEmail={clientInfo.Email}\nCCEmail1={clientInfo.CCEmail1}\nCCEmail2={clientInfo.CCEmail2}";

            Console.WriteLine($"Speichere in Datei: {filePath}");
            Console.WriteLine($"Inhalt: {newContent}");

            File.WriteAllText(filePath, newContent);
            LogDev($"Client-Informationen in Datei gespeichert: {filePath}");
        }

        public static WatchdogServer.ClientInfo LoadClientInfoFromFile(string ip)
        {
            string directoryPath = "C:\\Users\\U23551\\Documents\\GitHub\\WatchdogWAGO\\Watchdog_Server_SL_AvantagEnergy\\Watchdog_Server_SL_AvantagEnergy\\bin\\Debug\\net8.0\\clients";
            string filePath = Path.Combine(directoryPath, $"{ip}.cfg");

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Datei nicht gefunden: {filePath}");
            }

            var lines = File.ReadAllLines(filePath);
            var clientInfo = new WatchdogServer.ClientInfo();

            foreach (var line in lines)
            {
                if (line.StartsWith("IP="))
                {
                    clientInfo.IP = line.Substring("IP=".Length);
                }
                else if (line.StartsWith("ProjectName="))
                {
                    clientInfo.ProjectName = line.Substring("ProjectName=".Length);
                }
                else if (line.StartsWith("Email="))
                {
                    clientInfo.Email = line.Substring("Email=".Length);
                }
                else if (line.StartsWith("CCEmail1="))
                {
                    clientInfo.CCEmail1 = line.Substring("CCEmail1=".Length);
                }
                else if (line.StartsWith("CCEmail2="))
                {
                    clientInfo.CCEmail2 = line.Substring("CCEmail2=".Length);
                }
            }

            return clientInfo;
        }

        private static void LogDev(string message)
        {
            string logMessage = $"{DateTime.Now}: {message}";
            File.AppendAllText("server_devlog.txt", logMessage + Environment.NewLine);
        }
    }
}
