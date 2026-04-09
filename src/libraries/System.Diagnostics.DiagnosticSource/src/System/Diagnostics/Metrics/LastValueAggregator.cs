// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Diagnostics.Metrics
{
    // Aggregator that keeps the last value it received until Collect().
    // This class is used with the observable gauge that always called from a single thread during the collection.
    // It is safe to use it without synchronization.
    internal sealed class LastValue : Aggregator
    {
        private double? _lastValue;

        public override void Update(double value)
        {
            _lastValue = value;
        }

        public override IAggregationStatistics Collect()
        {
            lock (this)
            {
                LastValueStatistics stats = new LastValueStatistics(_lastValue);
                _lastValue = null;
                return stats;
            }
        }
    }

    internal sealed class LastValueStatistics : IAggregationStatistics
    {
        internal LastValueStatistics(double? lastValue)
        {
            LastValue = lastValue;
        }

        public double? LastValue { get; }
    }

    // Aggregator that keeps the last value it received.
    // This class is used with the synchronous gauge that can be called from multiple threads during the collection.
    // It uses volatile read/write to ensure the visibility of the last value.
    internal sealed class SynchronousLastValue : Aggregator
    {
        private double _lastValue;

        public override void Update(double value) => Volatile.Write(ref _lastValue, value);

        public override IAggregationStatistics Collect() => new SynchronousLastValueStatistics(Volatile.Read(ref _lastValue));
    }

    internal sealed class SynchronousLastValueStatistics : IAggregationStatistics
    {
        internal SynchronousLastValueStatistics(double lastValue)
        {
            LastValue = lastValue;
        }

        public double LastValue { get; }
    }
}
