using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Monetco.Host
{
    public class Program
    {
        public static void Main(string[] args)
        { 
            var config = new ConfigurationBuilder()
#if RELEASE
                .AddCommandLine(args)
#endif
                //.AddJsonFile("hosting.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .AddJsonFile("appsettings.json")
                .Build();
            BuildWebHost(args, config).Run();
        }

        public static IWebHost BuildWebHost(string[] args, IConfiguration config) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseConfiguration(config)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
#if DEBUG
            .UseUrls("http://*:5000")
#endif
                .Build();
    }
}
