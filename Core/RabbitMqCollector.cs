using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;

namespace Core
{
    public class RabbitMqCollector : IDisposable
    {
        private readonly DiagnosticSourceSubscriber diagnosticSourceSubscriber;

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitMqCollector"/> class.
        /// </summary>
        /// <param name="options">Configuration options for dependencies collector.</param>
        /// <param name="tracer">Tracer to record traced with.</param>
        /// <param name="sampler">Sampler to use to sample dependnecy calls.</param>
        public RabbitMqCollector(RabbitMqCollectorOptions options, ITracer tracer, ISampler sampler)
        {
            this.diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(
                new Dictionary<string, Func<ITracer, Func<RabbitMqOperation, ISampler>, ListenerHandler>>()
                { { "RabbitMqHandlerDiagnosticListener", (t, s) => new RabbitMqHandlerDiagnosticListener(t, s) } },
                tracer,
                x =>
                {
                    ISampler s = null;
                    try
                    {
                        s = options.CustomSampler(x);
                    }
                    catch (Exception e)
                    {
                        s = null;
                        RabbitMqEventSource.Log.ExceptionInCustomSampler(e);
                    }

                    return s == null ? sampler : s;
                });
            this.diagnosticSourceSubscriber.Subscribe();
        }

        public void Dispose()
        {
            this.diagnosticSourceSubscriber.Dispose();
        }
    }
}
