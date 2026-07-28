// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Runtime.Caching
{
#if NET
    [UnsupportedOSPlatform("browser")]
#endif
    internal sealed class Counters : EventSource
    {
#if NET
        private const string EVENT_SOURCE_NAME_ROOT = "System.Runtime.Caching.";
        private const int NUM_COUNTERS = 7;

        private DiagnosticCounter[] _counters;

        // Backing storage for the raw counter values.
        //
        // These are updated with Interlocked ops on every cache Get/Add/Remove, so the layout matters:
        //
        // 1. Named fields instead of a long[]. Indexing an array forces the JIT to emit a bounds check,
        //    which loads the array's length field. That length lives in the same cache line as the
        //    elements of a small array, so every increment performed a plain load of a line that is
        //    simultaneously the target of a contended atomic RMW. That defeats the "far atomic" handling
        //    LSE-capable hardware uses to keep contended counters resident at the shared cache, and turns
        //    each increment into a cache-line migration. Struct fields have no length to load.
        //
        // 2. Counters are grouped onto cache lines by how MemoryCacheStore actually updates them, so that
        //    unrelated operations running concurrently do not falsely share a line. Counters that are
        //    always bumped by the *same* operation stay together, because splitting those would force one
        //    operation to acquire several contended lines with fully-ordered atomics back to back:
        //      - Entries + Turnover: always bumped together by Add()/RemoveFromCache()
        //      - Hits:               bumped by a Get() that hits
        //      - Misses:             bumped by a Get() that misses
        //      - Trims:              bumped in batches by the (rare) trim path
        private CounterValues _counterValues;

        private const int CacheLineSize = Internal.PaddingHelpers.CACHE_LINE_SIZE;

        [StructLayout(LayoutKind.Explicit, Size = CacheLineSize * 4)]
        private struct CounterValues
        {
            [FieldOffset(CacheLineSize * 0)] public long Entries;
            [FieldOffset(CacheLineSize * 0 + 8)] public long Turnover;

            [FieldOffset(CacheLineSize * 1)] public long Hits;

            [FieldOffset(CacheLineSize * 2)] public long Misses;

            [FieldOffset(CacheLineSize * 3)] public long Trims;
        }

        internal Counters(string cacheName) : base(EVENT_SOURCE_NAME_ROOT + (cacheName ?? throw new ArgumentNullException(nameof(cacheName))))
        {
            InitDisposableMembers();
        }

        private void InitDisposableMembers()
        {
            bool dispose = true;

            try
            {
                _counters = new DiagnosticCounter[NUM_COUNTERS];
                _counters[(int)CounterName.Entries] = CreatePollingCounter("entries", "Cache Entries", () => Interlocked.Read(ref _counterValues.Entries));
                _counters[(int)CounterName.Hits] = CreatePollingCounter("hits", "Cache Hits", () => Interlocked.Read(ref _counterValues.Hits));
                _counters[(int)CounterName.Misses] = CreatePollingCounter("misses", "Cache Misses", () => Interlocked.Read(ref _counterValues.Misses));
                _counters[(int)CounterName.Trims] = CreatePollingCounter("trims", "Cache Trims", () => Interlocked.Read(ref _counterValues.Trims));

                _counters[(int)CounterName.Turnover] = new IncrementingPollingCounter("turnover", this,
                    () => Interlocked.Read(ref _counterValues.Turnover))
                {
                    DisplayName = "Cache Turnover Rate",
                };

                // This two-step dance with hit-ratio was an old perf-counter artifact: the ratio used to be
                // tracked as a pair of raw counters (HitRatio, incremented on every hit, and HitRatioBase,
                // incremented on every hit and every miss). Neither raw value is observable - only the
                // percentage computed below is - and they are exactly redundant with Hits and Hits + Misses.
                // Deriving the ratio lets the Get() hot path do a single Interlocked op instead of three.
                // 0 hits and 0 misses still yields NaN, as it did with the raw counters. Hits is read once
                // and reused for both the numerator and the denominator, so unlike the separate
                // HitRatio/HitRatioBase reads the result can never transiently exceed 100%.
                _counters[(int)CounterName.HitRatio] = new PollingCounter("hit-ratio", this,
                    () =>
                    {
                        double hits = Interlocked.Read(ref _counterValues.Hits);
                        return (hits / (hits + Interlocked.Read(ref _counterValues.Misses))) * 100d;
                    })
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

        private PollingCounter CreatePollingCounter(string name, string displayName, Func<double> getValue)
        {
            return new PollingCounter(name, this, getValue)
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
                    counters[i]?.Dispose();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref long GetCounterRef(CounterName name)
        {
            switch (name)
            {
                case CounterName.Entries: return ref _counterValues.Entries;
                case CounterName.Hits: return ref _counterValues.Hits;
                case CounterName.Misses: return ref _counterValues.Misses;
                case CounterName.Trims: return ref _counterValues.Trims;
                case CounterName.Turnover: return ref _counterValues.Turnover;
                default: throw new UnreachableException();
            }
        }

        internal void Increment(CounterName name) => Interlocked.Increment(ref GetCounterRef(name));

        internal void IncrementBy(CounterName name, long value) => Interlocked.Add(ref GetCounterRef(name), value);

        internal void Decrement(CounterName name) => Interlocked.Decrement(ref GetCounterRef(name));
#else
#pragma warning disable CA1822, IDE0060
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
#pragma warning restore CA1822, IDE0060
#endif
    }
}
