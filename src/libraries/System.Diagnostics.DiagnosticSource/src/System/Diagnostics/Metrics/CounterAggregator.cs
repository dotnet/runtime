// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Threading;
using Internal;

namespace System.Diagnostics.Metrics
{
    internal sealed class CounterAggregator : Aggregator
    {
        private readonly bool _isMonotonic;
        private double _aggregatedValue;
        /// <summary>Per-core deltas.</summary>
        /// <remarks>
        /// Stored as an array of deltas rather than a single delta to reduce contention from
        /// highly-parallel updates. The elements are padded to reduce false sharing.
        /// The array is limited to a semi-arbitrary limit of 8 in order to avoid excessive memory
        /// consumption when many counters are being used.
        /// </remarks>
        private readonly PaddedDouble[] _deltas = new PaddedDouble[Math.Min(Environment.ProcessorCount, 8)];

        public CounterAggregator(bool isMonotonic)
        {
            _isMonotonic = isMonotonic;
        }

        public override void Update(double value)
        {
            // Get the deltas array.
            PaddedDouble[] deltas = _deltas;

            // Get the delta best associated with the current thread, preferring to use core ID rather than
            // thread ID to reduce contention.
            ref PaddedDouble delta = ref deltas[
#if NETCOREAPP2_1_OR_GREATER
                Thread.GetCurrentProcessorId()
#else
                Environment.CurrentManagedThreadId
#endif
                % deltas.Length];

            // We're not guaranteed uncontented access, so we still need to add the value
            // to the delta with synchronization. Contention could come from other threads
            // assigned to the same slot or from Collect zero'ing out the delta.
            double currentValue;
            do
            {
                currentValue = delta.Value;
            }
            while (Interlocked.CompareExchange(ref delta.Value, currentValue + value, currentValue) != currentValue);
        }

        public override IAggregationStatistics Collect()
        {
            double delta, aggregatedValue;
            lock (this)
            {
                // Sum the deltas, resetting them to zero as we go. These resets needs to synchronize
                // with the additions performed in Update.
                delta = 0;
                foreach (ref PaddedDouble paddedDelta in _deltas.AsSpan())
                {
                    delta += Interlocked.Exchange(ref paddedDelta.Value, 0);
                }

                // Add the delta to the aggregated value.
                _aggregatedValue += delta;
                aggregatedValue = _aggregatedValue;
            }

            return new CounterStatistics(delta, _isMonotonic, aggregatedValue);
        }

        // 64 bytes is the size of a cache line on many systems. We pad the double to false sharing.
        // For the rare systems with a larger cache line, we may simply incur a little more false
        // sharing. This is a trade-off between throughput and memory footprint.
        [StructLayout(LayoutKind.Explicit, Size = 64)]
        private struct PaddedDouble
        {
            [FieldOffset(0)]
            public double Value;
        }
    }

    internal sealed class ObservableCounterAggregator : Aggregator
    {
        private readonly bool _isMonotonic;
        private double? _prevValue;
        private double _currValue;

        public ObservableCounterAggregator(bool isMonotonic)
        {
            _isMonotonic = isMonotonic;
        }

        public override void Update(double value)
        {
            Volatile.Write(ref _currValue, value);
        }

        public override IAggregationStatistics Collect()
        {
            double? delta = null;
            double currentValue;
            lock (this)
            {
                currentValue = Volatile.Read(ref _currValue);

                if (_prevValue.HasValue)
                {
                    delta = currentValue - _prevValue.Value;
                }

                _prevValue = currentValue;
            }

            return new CounterStatistics(delta, _isMonotonic, currentValue);
        }
    }

    internal sealed class CounterStatistics : IAggregationStatistics
    {
        public CounterStatistics(double? delta, bool isMonotonic, double value)
        {
            Delta = delta;
            IsMonotonic = isMonotonic;
            Value = value;
        }

        public double? Delta { get; }

        public bool IsMonotonic { get; }

        public double Value { get; }
    }
}
