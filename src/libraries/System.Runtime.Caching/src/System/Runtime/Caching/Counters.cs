// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;

namespace System.Runtime.Caching
{
    internal sealed class Counters : EventSource
    {
#if NETCOREAPP3_1_OR_GREATER
        private const string EVENT_SOURCE_NAME_ROOT = "System.Runtime.Caching.";
        private const int NUM_COUNTERS = 7;

        private PollingCounter[] _counters;
        private long[] _counterValues;

        internal Counters(string cacheName) : base(EVENT_SOURCE_NAME_ROOT + cacheName)
        {
            if (cacheName == null)
            {
                throw new ArgumentNullException(nameof(cacheName));
            }

            InitDisposableMembers(cacheName);
        }

        private void InitDisposableMembers(string cacheName)
        {
            bool dispose = true;

            try
            {
                _counters = new PollingCounter[NUM_COUNTERS];
                _counterValues = new long[NUM_COUNTERS];
                _counters[(int)CounterName.Entries] = CreatePollingCounter("entries", "Cache Entries", (int)CounterName.Entries);
                _counters[(int)CounterName.Hits] = CreatePollingCounter("hits", "Cache Hits", (int)CounterName.Hits);
                _counters[(int)CounterName.HitRatio] = CreatePollingCounter("hit-ratio", "Cache Hit Ratio", (int)CounterName.HitRatio);
                _counters[(int)CounterName.HitRatioBase] = CreatePollingCounter("hit-ratio-base", "Cache Hit Ratio Base", (int)CounterName.HitRatioBase);
                _counters[(int)CounterName.Misses] = CreatePollingCounter("misses", "Cache Misses", (int)CounterName.Misses);
                _counters[(int)CounterName.Trims] = CreatePollingCounter("trims", "Cache Trims", (int)CounterName.Trims);
                _counters[(int)CounterName.Turnover] = CreatePollingCounter("turnover", "Cache Turnover Rate", (int)CounterName.Turnover);

                dispose = false;
            }
            finally
            {
                if (dispose)
                    Dispose();
            }
        }

        private PollingCounter CreatePollingCounter(string name, string displayName, int counterIndex)
        {
            return new PollingCounter(name, this, () => (double)_counterValues[counterIndex])
            {
                DisplayName = displayName,
            };
        }

        public new void Dispose()
        {
            PollingCounter[] counters = _counters;

            // ensure this only happens once
            if (counters != null && Interlocked.CompareExchange(ref _counters, null, counters) == counters)
            {
                for (int i = 0; i < NUM_COUNTERS; i++)
                {
                    PollingCounter counter = counters[i];
                    if (counter != null)
                    {
                        counter.Dispose();
                    }
                }
            }
        }

        internal void Increment(CounterName name)
        {
            int idx = (int)name;
            Interlocked.Increment(ref _counterValues[idx]);
        }
        internal void IncrementBy(CounterName name, long value)
        {
            int idx = (int)name;
            Interlocked.Add(ref _counterValues[idx], value);
        }
        internal void Decrement(CounterName name)
        {
            int idx = (int)name;
            Interlocked.Decrement(ref _counterValues[idx]);
        }
#else
        internal Counters(string cacheName)
        {
        }
        public new void Dispose()
        {
        }
        internal void Increment(CounterName name)
        {
        }
        internal void IncrementBy(CounterName name, long value)
        {
        }
        internal void Decrement(CounterName name)
        {
        }
#endif
    }
}
