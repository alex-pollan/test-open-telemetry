using Core;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ITracer _tracer;
        private readonly DiagnosticSource _diagnosticSource;

        public HomeController(ITracer tracer, DiagnosticSource diagnosticSource)
        {
            _tracer = tracer;
            _diagnosticSource = diagnosticSource;
        }

        public IActionResult Index()
        {
            Publish();
            return View();
        }

        private void Publish()
        {
            var factory = new ConnectionFactory() { HostName = "vmlinux" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var exchangeName = "";
                var routingKey = "hello";

                string message = "Hello World!";
                var body = Encoding.UTF8.GetBytes(message);

                var properties = new BasicProperties();

                if (_diagnosticSource.IsEnabled("RabbitMqHandlerDiagnosticListener.Publish"))
                {
                    _diagnosticSource.Write("RabbitMqHandlerDiagnosticListener.Publish.Start",
                        new
                        {
                            Publish = new RabbitMqPublishOperation
                            {
                                ExchangeName = exchangeName,
                                RoutingKey = routingKey,
                                Headers = properties.Headers
                            }
                        });
                }

                channel.BasicPublish(exchange: exchangeName, routingKey: routingKey,
                    basicProperties: properties, body: body);

                if (_diagnosticSource.IsEnabled("RabbitMqHandlerDiagnosticListener.Publish"))
                {
                    _diagnosticSource.Write("RabbitMqHandlerDiagnosticListener.Publish.Stop",
                        new
                        {
                            Publish = new RabbitMqPublishOperation
                            {
                                ExchangeName = exchangeName,
                                RoutingKey = routingKey,
                                IsConfirmed = true
                            }
                        });
                }


                //using (_tracer.WithSpan(_tracer.SpanBuilder("QueuePublish").StartSpan()))
                //{
                //    var context = _tracer.CurrentSpan.Context;
                //    var properties = new BasicProperties();
                //    properties.Headers = new Dictionary<string, object>
                //    {
                //        { "x-span-id", context.SpanId.ToHexString() },
                //        { "x-trace-id", context.TraceId.ToHexString() },
                //        { "x-trace-options", ((int)context.TraceOptions).ToString() }
                //    };

                //    foreach (var item in context.Tracestate.Entries)
                //    {
                //        properties.Headers.Add($"x-trace-state-entry-{item.Key}", item.Value);
                //    }

                //    channel.BasicPublish(exchange: exchangeName, routingKey: routingKey,
                //        basicProperties: properties, body: body);
                //}

            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
