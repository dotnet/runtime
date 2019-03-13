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
    /// IncrementingPollingCounter is a variant of EventCounter for variables that are ever-increasing. 
    /// Ex) # of exceptions in the runtime.
    /// It does not calculate statistics like mean, standard deviation, etc. because it only accumulates
    /// the counter value.
    /// Unlike IncrementingEventCounter, this takes in a polling callback that it can call to update 
    /// its own metric periodically.
    /// </summary>
    internal partial class IncrementingPollingCounter : BaseCounter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IncrementingPollingCounter"/> class.
        /// IncrementingPollingCounter live as long as the EventSource that they are attached to unless they are
        /// explicitly Disposed.   
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="eventSource">The event source.</param>
        public IncrementingPollingCounter(string name, EventSource eventSource, Func<float> getCountFunction) : base(name, eventSource)
        {
            _getCountFunction = getCountFunction;
        }

        public override string ToString() => $"IncrementingPollingCounter '{_name}' Increment {_increment}";

        internal TimeSpan DisplayRateTimeScale { get; set; }
        private float _increment;
        private float _prevIncrement;
        private Func<float> _getCountFunction;

        /// <summary>
        /// Calls "_getCountFunction" to enqueue the counter value to the queue. 
        /// </summary>
        private void UpdateMetric()
        {
            try
            {
                lock(MyLock)
                {
                    _increment += _getCountFunction();
                }
            }
            catch (Exception ex)
            {
                ReportOutOfBandMessage($"ERROR: Exception during EventCounter {_name} getMetricFunction callback: " + ex.Message);
            }
        }

        internal override void WritePayload(float intervalSec)
        {
            UpdateMetric();
            lock (MyLock)     // Lock the counter
            {
                IncrementingCounterPayload payload = new IncrementingCounterPayload();
                payload.Name = _name;
                payload.DisplayName = DisplayName;
                payload.DisplayRateTimeScale = DisplayRateTimeScale;
                payload.IntervalSec = intervalSec;
                payload.MetaData = GetMetaDataString();
                payload.Increment = _increment - _prevIncrement;
                _prevIncrement = _increment;
                _eventSource.Write("EventCounters", new EventSourceOptions() { Level = EventLevel.LogAlways }, new IncrementingPollingCounterPayloadType(payload));
            }
        }
    }


    /// <summary>
    /// This is the payload that is sent in the with EventSource.Write
    /// </summary>
    [EventData]
    class IncrementingPollingCounterPayloadType
    {
        public IncrementingPollingCounterPayloadType(IncrementingCounterPayload payload) { Payload = payload; }
        public IncrementingCounterPayload Payload { get; set; }
    }

}
