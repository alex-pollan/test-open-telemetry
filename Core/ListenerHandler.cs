using OpenTelemetry.Trace;
using System;
using System.Diagnostics;

namespace Core
{
    internal abstract class ListenerHandler
    {
        protected readonly ITracer Tracer;

        protected readonly Func<RabbitMqOperation, ISampler> SamplerFactory;

        public ListenerHandler(string sourceName, ITracer tracer, Func<RabbitMqOperation, ISampler> samplerFactory)
        {
            this.SourceName = sourceName;
            this.Tracer = tracer;
            this.SamplerFactory = samplerFactory;
        }

        public string SourceName { get; }

        public abstract void OnStartActivity(Activity activity, object payload);

        public virtual void OnStopActivity(Activity activity, object payload)
        {
            var span = this.Tracer.CurrentSpan;

            if (span == null)
            {
                // TODO: Notify that span got lost
                return;
            }

            foreach (var tag in activity.Tags)
            {
                span.SetAttribute(tag.Key, tag.Value);
            }
        }

        public virtual void OnException(Activity activity, object payload)
        {
            var span = this.Tracer.CurrentSpan;

            // TODO: gather exception information
        }

        public virtual void OnCustom(string name, Activity activity, object payload)
        {
            // if custom handler needs to react on other events - this method should be overridden
        }
    }

}