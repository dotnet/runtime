// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Metrics
{
    internal sealed class RateSumAggregator : Aggregator
    {
        private readonly bool _isMonotonic;
        private double _sum;
        private double _totalSum;

        public RateSumAggregator(bool isMonotonic)
        {
            _isMonotonic = isMonotonic;
        }

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
                _totalSum += _sum;
                RateStatistics? stats = new RateStatistics(_sum, _isMonotonic, _totalSum);
                _sum = 0;
                return stats;
            }
        }
    }

    internal sealed class RateAggregator : Aggregator
    {
        private readonly bool _isMonotonic;
        private double? _prevValue;
        private double? _value;
        private double _sum;

        public RateAggregator(bool isMonotonic)
        {
            _isMonotonic = isMonotonic;
        }

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
                    delta = _value.HasValue ? _value - _prevValue.Value : 0;
                }
                _sum += _value ?? 0;
                RateStatistics stats = new RateStatistics(delta, _isMonotonic, _sum);
                _prevValue = _value ?? _prevValue;
                _value = null;
                return stats;
            }
        }
    }

    internal sealed class RateStatistics : IAggregationStatistics
    {
        public RateStatistics(double? delta, bool isMonotonic)
        {
            Delta = delta;
            IsMonotonic = isMonotonic;
        }

        public RateStatistics(double? delta, bool isMonotonic, double value) : this(delta, isMonotonic)
        {
            Value = value;
        }

        public double? Delta { get; }

        public bool IsMonotonic { get; }

        public double Value { get; }
    }
}
