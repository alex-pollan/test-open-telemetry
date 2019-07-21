using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Collector.AspNetCore;
using OpenTelemetry.Collector.Dependencies;
using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Sampler;
using System.IO;
using System.Threading.Tasks;

namespace QueueConsumer
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                    configHost.AddJsonFile("hostsettings.json", optional: true);
                    configHost.AddEnvironmentVariables(prefix: "PREFIX_");
                    configHost.AddCommandLine(args);
                })
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    configApp.AddJsonFile("appsettings.json", optional: true);
                    configApp.AddJsonFile(
                        $"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json",
                        optional: true);
                    configApp.AddEnvironmentVariables(prefix: "PREFIX_");
                    configApp.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<LifetimeEventsHostedService>();
                    services.AddHostedService<HostedService>();

                    services.AddSingleton<ITracer>(Tracing.Tracer);
                    services.AddSingleton<ISampler>(Samplers.AlwaysSample);
                    //services.AddSingleton<IPropagationComponent>(new DefaultPropagationComponent());

                    services.AddSingleton<RequestsCollectorOptions>(new RequestsCollectorOptions());
                    services.AddSingleton<RequestsCollector>();

                    services.AddSingleton<DependenciesCollectorOptions>(new DependenciesCollectorOptions());
                    services.AddSingleton<DependenciesCollector>();
                })
                .ConfigureLogging((hostContext, configLogging) =>
                {
                    configLogging.AddConsole();
                    configLogging.AddDebug();
                })
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();
        }
    }
}
