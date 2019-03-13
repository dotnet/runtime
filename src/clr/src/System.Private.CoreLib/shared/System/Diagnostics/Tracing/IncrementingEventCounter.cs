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
    /// IncrementingEventCounter is a variant of EventCounter for variables that are ever-increasing. 
    /// Ex) # of exceptions in the runtime.
    /// It does not calculate statistics like mean, standard deviation, etc. because it only accumulates
    /// the counter value.
    /// </summary>
    internal partial class IncrementingEventCounter : BaseCounter
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
        }

        /// <summary>
        /// Writes 'value' to the stream of values tracked by the counter.  This updates the sum and other statistics that will 
        /// be logged on the next timer interval.  
        /// </summary>
        /// <param name="increment">The value to increment by.</param>
        public void Increment(float increment = 1)
        {
            lock(MyLock)
            {
                _increment += increment;
            }
        }

        internal TimeSpan DisplayRateTimeScale { get; set; }
        private float _increment;
        private float _prevIncrement;

        public override string ToString() => $"IncrementingEventCounter '{_name}' Increment {_increment}";

        internal override void WritePayload(float intervalSec)
        {
            lock (MyLock)     // Lock the counter
            {
                IncrementingCounterPayload payload = new IncrementingCounterPayload();
                payload.Name = _name;
                payload.IntervalSec = intervalSec;
                payload.DisplayName = DisplayName;
                payload.DisplayRateTimeScale = DisplayRateTimeScale;
                payload.MetaData = GetMetaDataString();
                payload.Increment = _increment - _prevIncrement;
                _prevIncrement = _increment;
                _eventSource.Write("EventCounters", new EventSourceOptions() { Level = EventLevel.LogAlways }, new IncrementingEventCounterPayloadType(payload));
            }
        }
    }


    /// <summary>
    /// This is the payload that is sent in the with EventSource.Write
    /// </summary>
    [EventData]
    class IncrementingEventCounterPayloadType
    {
        public IncrementingEventCounterPayloadType(IncrementingCounterPayload payload) { Payload = payload; }
        public IncrementingCounterPayload Payload { get; set; }
    }

}
