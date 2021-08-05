// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Runtime.Versioning;

namespace System.Runtime.Caching
{
#if NET5_0_OR_GREATER
    [UnsupportedOSPlatform("browser")]
#endif
    internal sealed class Counters : EventSource
    {
#if NETCOREAPP3_1_OR_GREATER
        private const string EVENT_SOURCE_NAME_ROOT = "System.Runtime.Caching.";
        private const int NUM_COUNTERS = 7;

        private DiagnosticCounter[] _counters;
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
                _counters = new DiagnosticCounter[NUM_COUNTERS];
                _counterValues = new long[NUM_COUNTERS];
                _counters[(int)CounterName.Entries] = CreatePollingCounter("entries", "Cache Entries", (int)CounterName.Entries);
                _counters[(int)CounterName.Hits] = CreatePollingCounter("hits", "Cache Hits", (int)CounterName.Hits);
                _counters[(int)CounterName.Misses] = CreatePollingCounter("misses", "Cache Misses", (int)CounterName.Misses);
                _counters[(int)CounterName.Trims] = CreatePollingCounter("trims", "Cache Trims", (int)CounterName.Trims);

                _counters[(int)CounterName.Turnover] = new IncrementingPollingCounter("turnover", this,
                    () => (double)_counterValues[(int)CounterName.Turnover])
                {
                    DisplayName = "Cache Turnover Rate",
                };

                // This two-step dance with hit-ratio was an old perf-counter artifact. There only needs
                // to be one polling counter here, rather than the two-part perf counter. Still keeping array
                // indexes and raw counter values consistent between NetFx and Core code though.
                _counters[(int)CounterName.HitRatio] = new PollingCounter("hit-ratio", this,
                    () =>((double)_counterValues[(int)CounterName.HitRatio]/(double)_counterValues[(int)CounterName.HitRatioBase]) * 100d)
                {
                    DisplayName = "Cache Hit Ratio",
                };
                //_counters[(int)CounterName.HitRatioBase] = n/a;

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
            DiagnosticCounter[] counters = _counters;

            // ensure this only happens once
            if (counters != null && Interlocked.CompareExchange(ref _counters, null, counters) == counters)
            {
                for (int i = 0; i < NUM_COUNTERS; i++)
                {
                    var counter = counters[i];
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
