// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Diagnostics.Metrics
{
    internal class ExponentialHistogramAggregator : Aggregator
    {
        private const int ExponentArraySize = 2048;
        private const int ExponentShift = 52;
        private const double MinRelativeError = 0.000001;

        private PercentileAggregation _config;
        private int[][] _counters;
        private int _count;
        private int _mantissaMax;
        private int _mantissaMask;
        private int _mantissaShift;


        public ExponentialHistogramAggregator(PercentileAggregation config)
        {
            _config = config;
            _counters = new int[ExponentArraySize][];
            if (_config.MaxRelativeError < MinRelativeError)
            {
                throw new ArgumentException();
            }
            int mantissaBits = (int)Math.Ceiling(Math.Log(1 / _config.MaxRelativeError, 2)) - 1;
            _mantissaShift = 52 - mantissaBits;
            _mantissaMax = 1 << mantissaBits;
            _mantissaMask = _mantissaMax - 1;
        }

        public MeasurementAggregation MeasurementAggregation => _config;

        public override AggregationStatistics? Collect()
        {
            int[][] counters;
            int count;
            lock (this)
            {
                counters = _counters;
                count = _count;
                _counters = new int[ExponentArraySize][];
                _count = 0;
            }
            QuantileValue[] quantiles = new QuantileValue[_config.Percentiles.Length];
            int nextPercentileIdx = 0;
            int cur = 0;
            int target = Math.Max(1, (int)(_config.Percentiles[nextPercentileIdx] * count / 100.0));
            for (int exponent = 0; exponent < ExponentArraySize; exponent++)
            {
                int[] mantissaCounts = counters[exponent];
                if (mantissaCounts == null)
                {
                    continue;
                }
                for (int mantissa = 0; mantissa < _mantissaMax; mantissa++)
                {
                    cur += mantissaCounts[mantissa];
                    while (cur >= target)
                    {
                        quantiles[nextPercentileIdx] = new QuantileValue(
                            _config.Percentiles[nextPercentileIdx] / 100.0,
                            GetBucketCanonicalValue(exponent, mantissa));
                        nextPercentileIdx++;
                        if (nextPercentileIdx == _config.Percentiles.Length)
                        {
                            return new HistogramStatistics(MeasurementAggregation, quantiles);
                        }
                        target = Math.Max(1, (int)(_config.Percentiles[nextPercentileIdx] * count / 100.0));
                    }
                }
            }

            Debug.Assert(_count == 0);
#pragma warning disable CA1825 // Array.Empty<T>() doesn't exist in all configurations
            return new HistogramStatistics(MeasurementAggregation, new QuantileValue[0]);
#pragma warning restore CA1825
        }

        public override void Update(double measurement)
        {
            lock (this)
            {
                ref long bits = ref Unsafe.As<double, long>(ref measurement);
                int exponent = (int)(bits >> ExponentShift);
                int mantissa = (int)(bits >> _mantissaShift) & _mantissaMask;
                ref int[] mantissaCounts = ref _counters[exponent];
                mantissaCounts ??= new int[_mantissaMax];
                mantissaCounts[mantissa]++;
                _count++;
            }
        }

        // This is the upper bound for negative valued buckets and the
        // lower bound for positive valued buckets
        private double GetBucketCanonicalValue(int exponent, int mantissa)
        {
            double result = 0;
            ref long bits = ref Unsafe.As<double, long>(ref result);
            bits = ((long)exponent << ExponentShift) | ((long)mantissa << _mantissaShift);
            return result;
        }
    }
}
