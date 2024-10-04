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

            string filePath = Path.Combine(directoryPath, $"test.cfg");
            string newContent = $"IP={clientInfo.IP}\nProjectName={clientInfo.ProjectName}\nEmail={clientInfo.Email}";

            Console.WriteLine($"Speichere in Datei: {filePath}");
            Console.WriteLine($"Inhalt: {newContent}");

            File.WriteAllText(filePath, newContent);
            LogDev($"Client-Informationen in Datei gespeichert: {filePath}");
        }

        private static void LogDev(string message)
        {
            string logMessage = $"{DateTime.Now}: {message}";
            File.AppendAllText("server_devlog.txt", logMessage + Environment.NewLine);
        }
    }
}
