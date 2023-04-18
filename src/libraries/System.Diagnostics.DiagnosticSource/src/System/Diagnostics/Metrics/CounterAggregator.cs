// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Metrics
{
    internal sealed class CounterAggregator : Aggregator
    {
        private readonly bool _isMonotonic;
        private double _delta;
        private double _aggregatedValue;

        public CounterAggregator(bool isMonotonic)
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
                CounterStatistics? stats = new CounterStatistics(_delta, _isMonotonic, _aggregatedValue);
                _delta = 0;
                return stats;
            }
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
            lock (this)
            {
                _currValue = value;
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

                CounterStatistics stats = new CounterStatistics(delta, _isMonotonic, _currValue);
                _prevValue = _currValue;
                return stats;
            }
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
