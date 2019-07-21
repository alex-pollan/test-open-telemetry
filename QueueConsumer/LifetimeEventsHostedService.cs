using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Collector.AspNetCore;
using OpenTelemetry.Collector.Dependencies;
using OpenTelemetry.Exporter.Zipkin;
using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Sampler;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QueueConsumer
{
    internal class LifetimeEventsHostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IApplicationLifetime _appLifetime;
        private ZipkinTraceExporter _exporter;

        public LifetimeEventsHostedService(
            ILogger<LifetimeEventsHostedService> logger,
            IApplicationLifetime appLifetime,
            RequestsCollector requestCollector,
            DependenciesCollector dependenciesCollector)
        {
            _logger = logger;
            _appLifetime = appLifetime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _appLifetime.ApplicationStarted.Register(OnStarted);
            _appLifetime.ApplicationStopping.Register(OnStopping);
            _appLifetime.ApplicationStopped.Register(OnStopped);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void OnStarted()
        {
            _logger.LogInformation("OnStarted has been called.");

            // Perform post-startup activities here

            var traceConfig = Tracing.TraceConfig;
            var currentConfig = traceConfig.ActiveTraceParams;
            var newConfig = currentConfig.ToBuilder()
                .SetSampler(Samplers.AlwaysSample)
                .Build();
            traceConfig.UpdateActiveTraceParams(newConfig);

            _exporter = new ZipkinTraceExporter(
                new ZipkinTraceExporterOptions()
                {
                    Endpoint = new Uri("http://localhost:9411/api/v2/spans"),
                    ServiceName = typeof(Program).Assembly.GetName().Name,
                },
                Tracing.ExportComponent);

            _exporter.Start();
        }

        private void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called.");

            // Perform on-stopping activities here
        }

        private void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called.");

            // Perform post-stopped activities here
            _exporter.Stop();
        }
    }
}
