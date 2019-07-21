using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QueueConsumer
{
    internal class HostedService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly ITracer _tracer;
        private IConnection _connection;
        private IModel _channel;

        public HostedService(ITracer tracer, ILogger<HostedService> logger)
        {
            _logger = logger;
            _tracer = tracer;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Background Service is starting.");

            Consume<string>(async message => 
            {
                Console.WriteLine(" [x] Received {0}", message);

                await Task.CompletedTask;
            });

            return Task.CompletedTask;
        }

        private void Consume<T>(Func<T, Task> handleMessage)
        {
            var factory = new ConnectionFactory() { HostName = "vmlinux", DispatchConsumersAsync = true };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: "hello", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var headers = ea.BasicProperties.Headers;

                headers.TryGetValue("x-span-id", out var spanIdHex);
                headers.TryGetValue("x-trace-id", out var traceIdHex);

                SpanContext spanContext;
                if (spanIdHex != null && traceIdHex != null)
                {
                    var spanId = ActivitySpanId.CreateFromString(new ReadOnlySpan<char>(GetHeaderHeaderValue(spanIdHex).ToCharArray()));
                    var traceId = ActivityTraceId.CreateFromString(new ReadOnlySpan<char>(GetHeaderHeaderValue(traceIdHex).ToCharArray()));

                    ActivityTraceFlags traceOptions = ActivityTraceFlags.None;

                    if (headers.TryGetValue("x-trace-options", out var traceOptionsValue))
                    {
                        if (int.TryParse(GetHeaderHeaderValue(traceOptionsValue), out int traceOptionsInt))
                        {
                            traceOptions = (ActivityTraceFlags)traceOptionsInt;
                        }
                    }

                    Tracestate.TracestateBuilder tracestateBuilder = Tracestate.Builder;

                    foreach (var item in headers.Where(h => h.Key.StartsWith("x-trace-state-entry-")))
                    {
                        tracestateBuilder = tracestateBuilder.Set(item.Key, GetHeaderHeaderValue(item.Value));
                    }

                    spanContext = SpanContext.Create(traceId, spanId, traceOptions, tracestateBuilder.Build());
                }
                else
                {
                    spanContext = SpanContext.Blank;
                }

                using (_tracer.WithSpan(_tracer.SpanBuilder("QueueConsume").SetParent(spanContext).StartSpan()))
                {
                    var body = ea.Body;
                    var message = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(body));

                    await handleMessage(message);
                }
            };
            _channel.BasicConsume(queue: "hello", autoAck: true, consumer: consumer);
        }

        private string GetHeaderHeaderValue(object value)
        {
            return Encoding.ASCII.GetString((byte[])value);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Background Service is stopping.");

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
