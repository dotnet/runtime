// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Metrics
{
    internal class RateSumAggregator : Aggregator
    {
        private double _prevSum;
        private double _sum;

        public override void Update(double value)
        {
            lock (this)
            {
                _sum += value;
            }
        }

        public override AggregationStatistics? Collect()
        {
            lock (this)
            {
                RateStatistics? stats = default;
                double delta = _sum - _prevSum;
                stats = new RateStatistics(delta);
                _prevSum = _sum;
                return stats;
            }
        }
    }

    internal class RateAggregator : Aggregator
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

        public override AggregationStatistics? Collect()
        {
            lock (this)
            {
                RateStatistics? stats = default;
                if (_prevValue.HasValue)
                {
                    double delta = _value - _prevValue.Value;
                    stats = new RateStatistics(delta);
                }
                _prevValue = _value;
                return stats;
            }
        }
    }

    internal class RateStatistics : AggregationStatistics
    {
        public RateStatistics(double delta)
            : base(MeasurementAggregations.Rate)
        {
            Delta = delta;
        }

        public double Delta { get; }
    }
}
