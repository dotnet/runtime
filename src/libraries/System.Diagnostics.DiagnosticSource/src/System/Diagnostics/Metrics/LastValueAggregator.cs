// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Metrics
{
    internal class LastValue : Aggregator
    {
        private double? _lastValue;

        public override void Update(double value)
        {
            _lastValue = value;
        }

        public override AggregationStatistics? Collect()
        {
            lock (this)
            {
                LastValueStatistics? stats = default;
                if (_lastValue.HasValue)
                {
                    stats = new LastValueStatistics(_lastValue.Value);
                }
                _lastValue = null;
                return stats;
            }
        }
    }

    internal class LastValueStatistics : AggregationStatistics
    {
        internal LastValueStatistics(double lastValue) :
            base(MeasurementAggregations.LastValue)
        {
            LastValue = lastValue;
        }

        public double LastValue { get; }
    }
}
