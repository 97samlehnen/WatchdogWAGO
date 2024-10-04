using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Text;
using System.Linq;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Watchdog_Server_SL_AvantagEnergy
{
    public class WebServer
    {
        public static void Start()
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://localhost:2009");
                    webBuilder.Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            var activeClients = WatchdogServer.GetActiveClients();
                            var html = GenerateHtml(activeClients);
                            context.Response.ContentType = "text/html";
                            await context.Response.WriteAsync(html);
                        });
                    });
                })
                .Build();

            host.Run();
        }

        private static string GenerateHtml(ConcurrentDictionary<TcpClient, WatchdogServer.ClientInfo> activeClients)
        {
            var sb = new StringBuilder();
            sb.Append("<html><head><title>Watchdog Monitoring Dashboard</title></head><body>");
            sb.Append("<h1>Watchdog Monitoring Dashboard</h1>");
            sb.Append("<table border='1'><tr><th>IP</th><th>Project Name</th><th>Email</th><th>Last Activity</th></tr>");

            foreach (var client in activeClients.Values)
            {
                sb.Append("<tr>");
                sb.Append($"<td>{client.IP}</td>");
                sb.Append($"<td>{client.ProjectName}</td>");
                sb.Append($"<td>{client.Email}</td>");
                sb.Append($"<td>{client.LastActivity}</td>");
                sb.Append("</tr>");
            }

            sb.Append("</table>");
            sb.Append("</body></html>");
            return sb.ToString();
        }
    }
}
