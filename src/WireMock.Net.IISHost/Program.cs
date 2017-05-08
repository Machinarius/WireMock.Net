using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using WireMock.Owin;
using WireMock.Server;

namespace WireMock.Net.IISHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var options = new WireMockMiddlewareOptions();
            var server = FluentMockServer.StartWithHostedMiddlewareOptions(options, true, false);

            var host = new WebHostBuilder()
                .ConfigureLogging(factory => factory.AddConsole(LogLevel.Information))
                .Configure(appBuilder =>
                {
                    appBuilder.UseMiddleware<WireMockMiddleware>(options);
                })
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
