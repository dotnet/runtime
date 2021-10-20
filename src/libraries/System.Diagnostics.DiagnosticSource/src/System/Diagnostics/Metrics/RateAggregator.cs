// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Metrics
{
    internal sealed class RateSumAggregator : Aggregator
    {
        private double _sum;

        public override void Update(double value)
        {
            lock (this)
            {
                _sum += value;
            }
        }

        public override IAggregationStatistics Collect()
        {
            lock (this)
            {
                RateStatistics? stats = new RateStatistics(_sum);
                _sum = 0;
                return stats;
            }
        }
    }

    internal sealed class RateAggregator : Aggregator
    {
        private double? _prevValue;
        private double _value;

        public override void Update(double value)
        {
            lock (this)
            {
                _value = value;
            }
        }

        public override IAggregationStatistics Collect()
        {
            lock (this)
            {
                double? delta = null;
                if (_prevValue.HasValue)
                {
                    delta = _value - _prevValue.Value;
                }
                RateStatistics stats = new RateStatistics(delta);
                _prevValue = _value;
                return stats;
            }
        }
    }

    internal sealed class RateStatistics : IAggregationStatistics
    {
        public RateStatistics(double? delta)
        {
            Delta = delta;
        }

        public double? Delta { get; }
    }
}
