// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Diagnostics.Tracing
{
    /// <summary>
    /// IncrementingPollingCounter is a variant of EventCounter for variables that are ever-increasing.
    /// Ex) # of exceptions in the runtime.
    /// It does not calculate statistics like mean, standard deviation, etc. because it only accumulates
    /// the counter value.
    /// Unlike IncrementingEventCounter, this takes in a polling callback that it can call to update
    /// its own metric periodically.
    /// </summary>
#if !ES_BUILD_STANDALONE
#if !FEATURE_WASM_PERFTRACING
    [System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
#endif
#endif
    public partial class IncrementingPollingCounter : DiagnosticCounter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IncrementingPollingCounter"/> class.
        /// IncrementingPollingCounter live as long as the EventSource that they are attached to unless they are
        /// explicitly Disposed.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="eventSource">The event source.</param>
        /// <param name="totalValueProvider">The delegate to invoke to get the total value for this counter.</param>
        public IncrementingPollingCounter(string name, EventSource eventSource, Func<double> totalValueProvider) : base(name, eventSource)
        {
            if (totalValueProvider is null)
            {
                throw new ArgumentNullException(nameof(totalValueProvider));
            }

            _totalValueProvider = totalValueProvider;
            Publish();
        }

        public override string ToString() => $"IncrementingPollingCounter '{Name}' Increment {_increment}";

        public TimeSpan DisplayRateTimeScale { get; set; }
        private double _increment;
        private double _prevIncrement;
        private readonly Func<double> _totalValueProvider;

        /// <summary>
        /// Calls "_totalValueProvider" to enqueue the counter value to the queue.
        /// </summary>
        internal void UpdateMetric()
        {
            try
            {
                lock (this)
                {
                    _prevIncrement = _increment;
                    _increment = _totalValueProvider();
                }
            }
            catch (Exception ex)
            {
                ReportOutOfBandMessage($"ERROR: Exception during EventCounter {Name} getMetricFunction callback: " + ex.Message);
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The DynamicDependency will preserve the properties of IncrementingCounterPayload")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(IncrementingCounterPayload))]
        internal override void WritePayload(float intervalSec, int pollingIntervalMillisec)
        {
            UpdateMetric();
            lock (this)     // Lock the counter
            {
                IncrementingCounterPayload payload = new IncrementingCounterPayload();
                payload.Name = Name;
                payload.DisplayName = DisplayName ?? "";
                payload.DisplayRateTimeScale = (DisplayRateTimeScale == TimeSpan.Zero) ? "" : DisplayRateTimeScale.ToString("c");
                payload.IntervalSec = intervalSec;
                payload.Series = $"Interval={pollingIntervalMillisec}"; // TODO: This may need to change when we support multi-session
                payload.CounterType = "Sum";
                payload.Metadata = GetMetadataString();
                payload.Increment = _increment - _prevIncrement;
                payload.DisplayUnits = DisplayUnits ?? "";
                EventSource.Write("EventCounters", new EventSourceOptions() { Level = EventLevel.LogAlways }, new IncrementingPollingCounterPayloadType(payload));
            }
        }
    }

    /// <summary>
    /// This is the payload that is sent in the with EventSource.Write
    /// </summary>
    [EventData]
    internal sealed class IncrementingPollingCounterPayloadType
    {
        public IncrementingPollingCounterPayloadType(IncrementingCounterPayload payload) { Payload = payload; }
        public IncrementingCounterPayload Payload { get; set; }
    }
}
