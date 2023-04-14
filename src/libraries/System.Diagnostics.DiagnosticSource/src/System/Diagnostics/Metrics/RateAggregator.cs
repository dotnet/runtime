// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Metrics
{
    internal sealed class RateSumAggregator : Aggregator
    {
        private readonly bool _isMonotonic;
        private double _delta;
        private double _aggregatedValue;

        public RateSumAggregator(bool isMonotonic)
        {
            _isMonotonic = isMonotonic;
        }

        public override void Update(double value)
        {
            lock (this)
            {
                _delta += value;
            }
        }

        public override IAggregationStatistics Collect()
        {
            lock (this)
            {
                _aggregatedValue += _delta;
                RateStatistics? stats = new RateStatistics(_delta, _isMonotonic, _aggregatedValue);
                _delta = 0;
                return stats;
            }
        }
    }

    internal sealed class RateAggregator : Aggregator
    {
        private readonly bool _isMonotonic;
        private double? _prevValue;
        private double _currValue;
        private double _aggregatedValue;
        private bool _updated;

        public RateAggregator(bool isMonotonic)
        {
            _isMonotonic = isMonotonic;
        }

        public override void Update(double value)
        {
            lock (this)
            {
                _currValue = value;
                _updated = true;
            }
        }

        public override IAggregationStatistics Collect()
        {
            lock (this)
            {
                double? delta = null;
                if (_prevValue.HasValue)
                {
                    delta = _currValue - _prevValue.Value;
                }
                _aggregatedValue += _updated ? _currValue : 0;
                _updated = false;

                RateStatistics stats = new RateStatistics(delta, _isMonotonic, _aggregatedValue);
                _prevValue = _currValue;
                return stats;
            }
        }
    }

    internal sealed class RateStatistics : IAggregationStatistics
    {
        public RateStatistics(double? delta, bool isMonotonic, double value)
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
