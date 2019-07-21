using System;
using System.Diagnostics;
using Microsoft.Extensions.DiagnosticAdapter;
using OpenTelemetry.Trace;

namespace Core
{
    internal class RabbitMqHandlerDiagnosticListener : ListenerHandler
    {
        private readonly PropertyFetcher startPublishFetcher = new PropertyFetcher("Publish");
        private readonly PropertyFetcher stopPublishFetcher = new PropertyFetcher("Publish");

        public RabbitMqHandlerDiagnosticListener(ITracer tracer, Func<RabbitMqOperation, ISampler> samplerFactory)
            : base("RabbitMqHandlerDiagnosticListener", tracer, samplerFactory)
        {
        }
        
        public override void OnStartActivity(Activity activity, object payload)
        {
            if (this.startPublishFetcher.Fetch(payload) is RabbitMqPublishOperation operation)
            {
                OnStartActivityPublish(activity, payload, operation);
            }
            else
            {
                return;
            }
        }

        private void OnStartActivityPublish(Activity activity, object payload, RabbitMqPublishOperation operation)
        {
            var span = this.Tracer.SpanBuilder($"{operation.ExchangeName}-{operation.RoutingKey}")
                .SetSpanKind(SpanKind.Producer)
                .SetSampler(this.SamplerFactory(operation))
                .StartSpan();

            this.Tracer.WithSpan(span);

            this.Tracer.TextFormat.Inject<RabbitMqPublishOperation>(span.Context, operation,
                (r, k, v) => r.Headers.Add(k, v));
        }

        public override void OnStopActivity(Activity activity, object payload)
        {
            var span = this.Tracer.CurrentSpan;

            if (span == null)
            {
                RabbitMqEventSource.Log.NullContext();
                return;
            }

            if (this.stopPublishFetcher.Fetch(payload) is RabbitMqPublishOperation operation)
            {
                OnStopActivityPublish(activity, payload, span, operation);

                span.End();
            }

            span.End();
        }

        private void OnStopActivityPublish(Activity activity, object payload, ISpan span, RabbitMqPublishOperation operation)
        {
            if (operation.IsConfirmed)
            {
                span.Status = Status.Ok;
            }
            else
            {
                span.Status = Status.Internal;
            }
        }

        //public override void OnException(Activity activity, object payload)
        //{
        //    if (!(this.stopExceptionFetcher.Fetch(payload) is Exception exc))
        //    {
        //        // Debug.WriteLine("response is null");
        //        return;
        //    }

        //    var span = this.Tracer.CurrentSpan;

        //    if (span == null)
        //    {
        //        // TODO: Notify that span got lost
        //        return;
        //    }

        //    if (exc is HttpRequestException)
        //    {
        //        // TODO: on netstandard this will be System.Net.Http.WinHttpException: The server name or address could not be resolved
        //        if (exc.InnerException is WebException &&
        //            ((WebException)exc.InnerException).Status == WebExceptionStatus.NameResolutionFailure)
        //        {
        //            span.Status = Status.InvalidArgument;
        //        }
        //        else if (exc.InnerException != null)
        //        {
        //            span.Status = Status.Unknown.WithDescription(exc.Message);
        //        }
        //    }
        //}
    }
}