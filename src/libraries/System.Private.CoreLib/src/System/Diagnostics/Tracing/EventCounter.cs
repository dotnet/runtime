// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if ES_BUILD_STANDALONE
using System;
using System.Diagnostics;
#endif
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Threading;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// Provides the ability to collect statistics through EventSource
    ///
    /// See https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.Tracing/documentation/EventCounterTutorial.md
    /// for a tutorial guide.
    ///
    /// See https://github.com/dotnet/runtime/blob/main/src/libraries/System.Diagnostics.Tracing/tests/BasicEventSourceTest/TestEventCounter.cs
    /// which shows tests, which are also useful in seeing actual use.
    /// </summary>
#if NETCOREAPP
    [UnsupportedOSPlatform("browser")]
#endif
    public partial class EventCounter : DiagnosticCounter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventCounter"/> class.
        /// EVentCounters live as long as the EventSource that they are attached to unless they are
        /// explicitly Disposed.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="eventSource">The event source.</param>
        public EventCounter(string name, EventSource eventSource) : base(name, eventSource)
        {
            _min = double.PositiveInfinity;
            _max = double.NegativeInfinity;

            var bufferedValues = new double[BufferedSize];
            for (int i = 0; i < bufferedValues.Length; i++)
            {
                bufferedValues[i] = UnusedBufferSlotValue;
            }
            _bufferedValues = bufferedValues;

            Publish();
        }

        /// <summary>
        /// Writes 'value' to the stream of values tracked by the counter.  This updates the sum and other statistics that will
        /// be logged on the next timer interval.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteMetric(float value)
        {
            Enqueue((double)value);
        }

        public void WriteMetric(double value)
        {
            Enqueue(value);
        }

        public override string ToString()
        {
            int count = Volatile.Read(ref _count);
            return count == 0 ?
                $"EventCounter '{Name}' Count 0" :
                $"EventCounter '{Name}' Count {count} Mean {(_sum / count).ToString("n3")}";
        }

        #region Statistics Calculation

        // Statistics
        private int _count;
        private double _sum;
        private double _sumSquared;
        private double _min;
        private double _max;

        internal void OnMetricWritten(double value)
        {
            Debug.Assert(Monitor.IsEntered(this));
            _sum += value;
            _sumSquared += value * value;
            if (value > _max)
                _max = value;

            if (value < _min)
                _min = value;

            _count++;
        }

#if !ES_BUILD_STANDALONE
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The DynamicDependency will preserve the properties of CounterPayload")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(CounterPayload))]
#endif
        internal override void WritePayload(float intervalSec, int pollingIntervalMillisec)
        {
            lock (this)
            {
                Flush();
                CounterPayload payload = new CounterPayload();
                payload.Count = _count;
                payload.IntervalSec = intervalSec;
                if (0 < _count)
                {
                    payload.Mean = _sum / _count;
                    payload.StandardDeviation = Math.Sqrt(_sumSquared / _count - _sum * _sum / _count / _count);
                }
                else
                {
                    payload.Mean = 0;
                    payload.StandardDeviation = 0;
                }
                payload.Min = _min;
                payload.Max = _max;
                payload.Series = $"Interval={pollingIntervalMillisec}"; // TODO: This may need to change when we support multi-session
                payload.CounterType = "Mean";
                payload.Metadata = GetMetadataString();
                payload.DisplayName = DisplayName ?? "";
                payload.DisplayUnits = DisplayUnits ?? "";
                payload.Name = Name;
                ResetStatistics();
                EventSource.Write("EventCounters", new EventSourceOptions() { Level = EventLevel.LogAlways }, new CounterPayloadType(payload));
            }
        }

        internal void ResetStatistics()
        {
            lock (this)
            {
                _count = 0;
                _sum = 0;
                _sumSquared = 0;
                _min = double.PositiveInfinity;
                _max = double.NegativeInfinity;
            }
        }

        #endregion // Statistics Calculation

        // Values buffering
        private const int BufferedSize = 10;
        private const double UnusedBufferSlotValue = double.NegativeInfinity;
        private readonly double[] _bufferedValues;
        private volatile int _bufferedValuesIndex;

        private void Enqueue(double value)
        {
            // It is possible that two threads read the same bufferedValuesIndex, but only one will be able to write the slot, so that is okay.
            int i = _bufferedValuesIndex;
            while (true)
            {
                double result = Interlocked.CompareExchange(ref _bufferedValues[i], value, UnusedBufferSlotValue);
                i++;
                if (_bufferedValues.Length <= i)
                {
                    // It is possible that two threads both think the buffer is full, but only one get to actually flush it, the other
                    // will eventually enter this code path and potentially calling Flushing on a buffer that is not full, and that's okay too.
                    lock (this) // Lock the counter
                        Flush();
                    i = 0;
                }

                if (result == UnusedBufferSlotValue)
                {
                    // CompareExchange succeeded
                    _bufferedValuesIndex = i;
                    return;
                }
            }
        }

        protected void Flush()
        {
            Debug.Assert(Monitor.IsEntered(this));
            for (int i = 0; i < _bufferedValues.Length; i++)
            {
                double value = Interlocked.Exchange(ref _bufferedValues[i], UnusedBufferSlotValue);
                if (value != UnusedBufferSlotValue)
                {
                    OnMetricWritten(value);
                }
            }

            _bufferedValuesIndex = 0;
        }
    }


    /// <summary>
    /// This is the payload that is sent in the with EventSource.Write
    /// </summary>
    [EventData]
    internal sealed class CounterPayloadType
    {
        public CounterPayloadType(CounterPayload payload) { Payload = payload; }
        public CounterPayload Payload { get; set; }
    }
}
