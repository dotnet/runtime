// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Diagnostics.Tracing
{
    /// <summary>
    /// IncrementingEventCounter is a variant of EventCounter for variables that are ever-increasing.
    /// Ex) # of exceptions in the runtime.
    /// It does not calculate statistics like mean, standard deviation, etc. because it only accumulates
    /// the counter value.
    /// </summary>
#if !ES_BUILD_STANDALONE
#if !FEATURE_WASM_PERFTRACING
    [System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
#endif
#endif
    public partial class IncrementingEventCounter : DiagnosticCounter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IncrementingEventCounter"/> class.
        /// IncrementingEventCounter live as long as the EventSource that they are attached to unless they are
        /// explicitly Disposed.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="eventSource">The event source.</param>
        public IncrementingEventCounter(string name, EventSource eventSource) : base(name, eventSource)
        {
            Publish();
        }

        /// <summary>
        /// Writes 'value' to the stream of values tracked by the counter.  This updates the sum and other statistics that will
        /// be logged on the next timer interval.
        /// </summary>
        /// <param name="increment">The value to increment by.</param>
        public void Increment(double increment = 1)
        {
            lock (this)
            {
                _increment += increment;
            }
        }

        public TimeSpan DisplayRateTimeScale { get; set; }
        private double _increment;
        private double _prevIncrement;

        public override string ToString() => $"IncrementingEventCounter '{Name}' Increment {_increment}";

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The DynamicDependency will preserve the properties of IncrementingCounterPayload")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(IncrementingCounterPayload))]
        internal override void WritePayload(float intervalSec, int pollingIntervalMillisec)
        {
            lock (this)     // Lock the counter
            {
                IncrementingCounterPayload payload = new IncrementingCounterPayload();
                payload.Name = Name;
                payload.IntervalSec = intervalSec;
                payload.DisplayName = DisplayName ?? "";
                payload.DisplayRateTimeScale = (DisplayRateTimeScale == TimeSpan.Zero) ? "" : DisplayRateTimeScale.ToString("c");
                payload.Series = $"Interval={pollingIntervalMillisec}"; // TODO: This may need to change when we support multi-session
                payload.CounterType = "Sum";
                payload.Metadata = GetMetadataString();
                payload.Increment = _increment - _prevIncrement;
                payload.DisplayUnits = DisplayUnits ?? "";
                _prevIncrement = _increment;
                EventSource.Write("EventCounters", new EventSourceOptions() { Level = EventLevel.LogAlways }, new IncrementingEventCounterPayloadType(payload));
            }
        }

        // Updates the value.
        internal void UpdateMetric()
        {
            lock (this)
            {
                _prevIncrement = _increment;
            }
        }
    }


    /// <summary>
    /// This is the payload that is sent in the with EventSource.Write
    /// </summary>
    [EventData]
    internal sealed class IncrementingEventCounterPayloadType
    {
        public IncrementingEventCounterPayloadType(IncrementingCounterPayload payload) { Payload = payload; }
        public IncrementingCounterPayload Payload { get; set; }
    }
}
