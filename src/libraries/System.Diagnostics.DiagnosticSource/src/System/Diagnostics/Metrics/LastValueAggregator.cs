// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Metrics
{
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
}
