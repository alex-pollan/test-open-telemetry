using System;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Threading;

namespace Core
{
    /// <summary>
    /// EventSource listing ETW events emitted from the project.
    /// </summary>
    [EventSource(Name = "OpenTelemetry.Collector.RabbitMq")]
    internal class RabbitMqEventSource : EventSource
    {
        internal static RabbitMqEventSource Log = new RabbitMqEventSource();

        [NonEvent]
        public void ExceptionInCustomSampler(Exception ex)
        {
            if (Log.IsEnabled(EventLevel.Warning, EventKeywords.All))
            {
                this.ExceptionInCustomSampler(ToInvariantString(ex));
            }
        }

        [Event(1, Message = "Context is NULL in end callback. Span will not be recorded.", Level = EventLevel.Warning)]
        public void NullContext()
        {
            this.WriteEvent(1);
        }

        [Event(2, Message = "Error getting custom sampler, the default sampler will be used. Exception : {0}", Level = EventLevel.Warning)]
        public void ExceptionInCustomSampler(string ex)
        {
            this.WriteEvent(2, ex);
        }

        /// <summary>
        /// Returns a culture-independent string representation of the given <paramref name="exception"/> object,
        /// appropriate for diagnostics tracing.
        /// </summary>
        private static string ToInvariantString(Exception exception)
        {
            var originalUICulture = Thread.CurrentThread.CurrentUICulture;

            try
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                return exception.ToString();
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = originalUICulture;
            }
        }
    }
}