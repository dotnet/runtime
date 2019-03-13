// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
#if ES_BUILD_PCL
    using System.Threading.Tasks;
#endif

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// PollingCounter is a variant of EventCounter - it collects and calculates similar statistics 
    /// as EventCounter. PollingCounter differs from EventCounter in that it takes in a callback
    /// function to collect metrics on its own rather than the user having to call WriteMetric() 
    /// every time.
    /// </summary>
    internal partial class PollingCounter : BaseCounter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PollingCounter"/> class.
        /// PollingCounter live as long as the EventSource that they are attached to unless they are
        /// explicitly Disposed.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="eventSource">The event source.</param>
        public PollingCounter(string name, EventSource eventSource, Func<float> getMetricFunction) : base(name, eventSource)
        {
            _getMetricFunction = getMetricFunction;
        }

        public override string ToString() => $"PollingCounter '{_name}' Count {1} Mean {_lastVal.ToString("n3")}";

        private Func<float> _getMetricFunction;
        private float _lastVal;

        internal override void WritePayload(float intervalSec)
        {
            lock (MyLock)
            {
                float value = 0;
                try 
                {
                    value = _getMetricFunction();
                }
                catch (Exception ex)
                {
                    ReportOutOfBandMessage($"ERROR: Exception during EventCounter {_name} getMetricFunction callback: " + ex.Message);
                }

                CounterPayload payload = new CounterPayload();
                payload.Name = _name;
                payload.Count = 1; // NOTE: These dumb-looking statistics is intentional
                payload.IntervalSec = intervalSec;
                payload.Mean = value;
                payload.Max = value;
                payload.Min = value;
                payload.StandardDeviation = 0;
                _lastVal = value;
                _eventSource.Write("EventCounters", new EventSourceOptions() { Level = EventLevel.LogAlways }, new PollingPayloadType(payload));
            }
        }
    }

    /// <summary>
    /// This is the payload that is sent in the with EventSource.Write
    /// </summary>
    [EventData]
    class PollingPayloadType
    {
        public PollingPayloadType(CounterPayload payload) { Payload = payload; }
        public CounterPayload Payload { get; set; }
    }
}
