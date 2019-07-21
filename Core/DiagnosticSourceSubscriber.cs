using OpenTelemetry.Trace;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Core
{
    internal class DiagnosticSourceSubscriber : IDisposable, IObserver<DiagnosticListener>
    {
        private readonly Dictionary<string, Func<ITracer, Func<RabbitMqOperation, ISampler>, ListenerHandler>> handlers;
        private readonly ITracer tracer;
        private readonly Func<RabbitMqOperation, ISampler> sampler;
        private ConcurrentDictionary<string, DiagnosticSourceListener> subscriptions;
        private bool disposing;
        private IDisposable subscription;

        public DiagnosticSourceSubscriber(Dictionary<string, Func<ITracer, Func<RabbitMqOperation, ISampler>, ListenerHandler>> handlers, ITracer tracer, Func<RabbitMqOperation, ISampler> sampler)
        {
            this.subscriptions = new ConcurrentDictionary<string, DiagnosticSourceListener>();
            this.handlers = handlers;
            this.tracer = tracer;
            this.sampler = sampler;
        }

        public void Subscribe()
        {
            if (this.subscription == null)
            {
                this.subscription = DiagnosticListener.AllListeners.Subscribe(this);
            }
        }

        public void OnNext(DiagnosticListener value)
        {
            if (!Volatile.Read(ref this.disposing) && this.subscriptions != null)
            {
                if (this.handlers.ContainsKey(value.Name))
                {
                    this.subscriptions.GetOrAdd(value.Name, name =>
                    {
                        var dl = new DiagnosticSourceListener(value.Name, this.handlers[value.Name](this.tracer, this.sampler));
                        dl.Subscription = value.Subscribe(dl);
                        return dl;
                    });
                }
            }
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void Dispose()
        {
            Volatile.Write(ref this.disposing, true);

            var subsCopy = this.subscriptions;
            this.subscriptions = null;

            var keys = subsCopy.Keys;
            foreach (var key in keys)
            {
                if (subsCopy.TryRemove(key, out var sub))
                {
                    sub?.Dispose();
                }
            }

            this.subscription?.Dispose();
            this.subscription = null;
        }
    }

}
