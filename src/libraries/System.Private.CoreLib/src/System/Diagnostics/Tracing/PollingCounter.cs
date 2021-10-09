// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if ES_BUILD_STANDALONE
using System;
#endif

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// PollingCounter is a variant of EventCounter - it collects and calculates similar statistics
    /// as EventCounter. PollingCounter differs from EventCounter in that it takes in a callback
    /// function to collect metrics on its own rather than the user having to call WriteMetric()
    /// every time.
    /// </summary>
#if NETCOREAPP
    [UnsupportedOSPlatform("browser")]
#endif
    public partial class PollingCounter : DiagnosticCounter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PollingCounter"/> class.
        /// PollingCounter live as long as the EventSource that they are attached to unless they are
        /// explicitly Disposed.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="eventSource">The event source.</param>
        /// <param name="metricProvider">The delegate to invoke to get the current metric value.</param>
        public PollingCounter(string name, EventSource eventSource, Func<double> metricProvider) : base(name, eventSource)
        {
            if (metricProvider == null)
                throw new ArgumentNullException(nameof(metricProvider));

            _metricProvider = metricProvider;
            Publish();
        }

        public override string ToString() => $"PollingCounter '{Name}' Count 1 Mean {_lastVal:n3}";

        private readonly Func<double> _metricProvider;
        private double _lastVal;

#if !ES_BUILD_STANDALONE
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The DynamicDependency will preserve the properties of CounterPayload")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(CounterPayload))]
#endif
        internal override void WritePayload(float intervalSec, int pollingIntervalMillisec)
        {
            lock (this)
            {
                double value = 0;
                try
                {
                    value = _metricProvider();
                }
                catch (Exception ex)
                {
                    ReportOutOfBandMessage($"ERROR: Exception during EventCounter {Name} metricProvider callback: " + ex.Message);
                }

                CounterPayload payload = new CounterPayload();
                payload.Name = Name;
                payload.DisplayName = DisplayName ?? "";
                payload.Count = 1; // NOTE: These dumb-looking statistics is intentional
                payload.IntervalSec = intervalSec;
                payload.Series = $"Interval={pollingIntervalMillisec}";  // TODO: This may need to change when we support multi-session
                payload.CounterType = "Mean";
                payload.Mean = value;
                payload.Max = value;
                payload.Min = value;
                payload.Metadata = GetMetadataString();
                payload.StandardDeviation = 0;
                payload.DisplayUnits = DisplayUnits ?? "";
                _lastVal = value;
                EventSource.Write("EventCounters", new EventSourceOptions() { Level = EventLevel.LogAlways }, new PollingPayloadType(payload));
            }
        }
    }

    /// <summary>
    /// This is the payload that is sent in the with EventSource.Write
    /// </summary>
    [EventData]
    internal sealed class PollingPayloadType
    {
        public PollingPayloadType(CounterPayload payload) { Payload = payload; }
        public CounterPayload Payload { get; set; }
    }
}
