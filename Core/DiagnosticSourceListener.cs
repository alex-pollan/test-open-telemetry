using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Core
{
    internal class DiagnosticSourceListener : IObserver<KeyValuePair<string, object>>, IDisposable
    {
        private readonly string sourceName;
        private readonly ListenerHandler handler;

        public DiagnosticSourceListener(string sourceName, ListenerHandler handler)
        {
            this.sourceName = sourceName;
            this.handler = handler;
        }

        public IDisposable Subscription { get; set; }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            if (Activity.Current == null)
            {
                Debug.WriteLine("Activity is null " + value.Key);
                return;
            }

            try
            {
                if (value.Key.EndsWith("Start"))
                {
                    this.handler.OnStartActivity(Activity.Current, value.Value);
                }
                else if (value.Key.EndsWith("Stop"))
                {
                    this.handler.OnStopActivity(Activity.Current, value.Value);
                }
                else if (value.Key.EndsWith("Exception"))
                {
                    this.handler.OnException(Activity.Current, value.Value);
                }
                else
                {
                    this.handler.OnCustom(value.Key, Activity.Current, value.Value);
                }
            }
            catch (Exception)
            {
                // Debug.WriteLine(e);
                // TODO: make sure to output the handler name as part of error message
            }
        }

        public void Dispose()
        {
            this.Subscription?.Dispose();
        }
    }

}