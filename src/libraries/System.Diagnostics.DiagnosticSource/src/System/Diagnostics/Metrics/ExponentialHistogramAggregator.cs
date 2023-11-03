// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics.Metrics
{
    internal sealed class QuantileAggregation
    {
        public QuantileAggregation(params double[] quantiles)
        {
            Quantiles = quantiles;
            Array.Sort(Quantiles);
        }

        public double[] Quantiles { get; set; }
        public double MaxRelativeError { get; set; } = 0.001;
    }



    // This histogram ensures that the quantiles reported from the histogram are within some bounded % error of the correct
    // value. More mathematically, if we have a set of measurements where quantile X = Y, the histogram should always report a
    // value Y` where Y*(1-E) <= Y` <= Y*(1+E). E is our allowable error, so if E = 0.01 then the reported value Y` is
    // between 0.99*Y and 1.01*Y. We achieve this by ensuring that if a bucket holds measurements from M_min to M_max
    // then M_max - M_min <= M_min*E. We can determine which bucket must hold quantile X and we know that all values assigned to
    // the bucket are within the error bound if we approximate the result as M_min.
    // Note: we should be able to refine this to return the bucket midpoint rather than bucket lower bound, halving the number of
    // buckets to achieve the same error bound.
    //
    // Implementation: The histogram buckets are implemented as an array of arrays (a tree). The top level has a fixed 4096 entries
    // corresponding to every possible sign+exponent in the encoding of a double (IEEE 754 spec). The 2nd level has variable size
    // depending on how many buckets are needed to achieve the error bounds. For ease of insertion we round the 2nd level size up to
    // the nearest power of 2. This lets us mask off the first k bits in the mantissa to map a measurement to one of 2^k 2nd level
    // buckets. The top level array is pre-allocated but the 2nd level arrays are created on demand.
    //
    // PERF Note: This histogram has a fast Update() but the _counters array has a sizable memory footprint (32KB+ on 64 bit)
    // It is probably well suited for tracking 10s or maybe 100s of histograms but if we wanted to go higher
    // we probably want to trade a little more CPU cost in Update() + code complexity to avoid eagerly allocating 4096
    // top level entries.
    internal sealed class ExponentialHistogramAggregator : Aggregator
    {
        private const int ExponentArraySize = 4096;
        private const int ExponentShift = 52;
        private const double MinRelativeError = 0.0001;
        private const int PositiveIntAndNan = ExponentArraySize / 2 - 1;
        private const int NegativeIntAndNan = ExponentArraySize - 1;

        private readonly QuantileAggregation _config;
        private int[]?[] _counters;
        private int _count;
        private double _sum;
        private readonly int _mantissaMax;
        private readonly int _mantissaMask;
        private readonly int _mantissaShift;

        private struct Bucket
        {
            public Bucket(double value, int count)
            {
                Value = value;
                Count = count;
            }
            public double Value;
            public int Count;
        }

        public ExponentialHistogramAggregator(QuantileAggregation config)
        {
            _config = config;
            _counters = new int[ExponentArraySize][];
            if (_config.MaxRelativeError < MinRelativeError)
            {
                // Ensure that we don't create enormous histograms trying to get overly high precision
                throw new ArgumentException();
            }
            int mantissaBits = (int)Math.Ceiling(Math.Log(1 / _config.MaxRelativeError, 2)) - 1;
            _mantissaShift = 52 - mantissaBits;
            _mantissaMax = 1 << mantissaBits;
            _mantissaMask = _mantissaMax - 1;
        }

        public override IAggregationStatistics Collect()
        {
            int[]?[] counters;
            int count;
            double sum;
            lock (this)
            {
                counters = _counters;
                count = _count;
                sum = _sum;
                _counters = new int[ExponentArraySize][];
                _count = 0;
                _sum = 0;
            }

            QuantileValue[] quantiles = new QuantileValue[_config.Quantiles.Length];
            int nextQuantileIndex = 0;
            if (nextQuantileIndex == _config.Quantiles.Length)
            {
                return new HistogramStatistics(quantiles, count, sum);
            }

            // Consider each bucket to have N entries in it, and each entry has value GetBucketCanonicalValue().
            // If all these entries were inserted in a sorted array, we are trying to find the value of the entry with
            // index=target.
            int target = QuantileToRank(_config.Quantiles[nextQuantileIndex], count);

            // the total number of entries in all buckets iterated so far
            int cur = 0;
            foreach (Bucket b in IterateBuckets(counters))
            {
                cur += b.Count;
                while (cur > target)
                {
                    quantiles[nextQuantileIndex] = new QuantileValue(
                        _config.Quantiles[nextQuantileIndex], b.Value);
                    nextQuantileIndex++;
                    if (nextQuantileIndex == _config.Quantiles.Length)
                    {
                        return new HistogramStatistics(quantiles, count, sum);
                    }
                    target = QuantileToRank(_config.Quantiles[nextQuantileIndex], count);
                }
            }

            Debug.Assert(count == 0);
            return new HistogramStatistics(Array.Empty<QuantileValue>(), count, sum);
        }

        private IEnumerable<Bucket> IterateBuckets(int[]?[] counters)
        {
            // iterate over the negative exponent buckets
            const int LowestNegativeOffset = ExponentArraySize / 2;
            // exponent = ExponentArraySize-1 encodes infinity and NaN, which we want to ignore
            for (int exponent = ExponentArraySize-2; exponent >= LowestNegativeOffset; exponent--)
            {
                int[]? mantissaCounts = counters[exponent];
                if (mantissaCounts == null)
                {
                    continue;
                }
                for (int mantissa = _mantissaMax-1; mantissa >= 0; mantissa--)
                {
                    int count = mantissaCounts[mantissa];
                    if (count > 0)
                    {
                        yield return new Bucket(GetBucketCanonicalValue(exponent, mantissa), count);
                    }
                }
            }

            // iterate over the positive exponent buckets
            // exponent = lowestNegativeOffset-1 encodes infinity and NaN, which we want to ignore
            for (int exponent = 0; exponent < LowestNegativeOffset-1; exponent++)
            {
                int[]? mantissaCounts = counters[exponent];
                if (mantissaCounts == null)
                {
                    continue;
                }
                for (int mantissa = 0; mantissa < _mantissaMax; mantissa++)
                {
                    int count = mantissaCounts[mantissa];
                    if (count > 0)
                    {
                        yield return new Bucket(GetBucketCanonicalValue(exponent, mantissa), count);
                    }
                }
            }
        }

        public override void Update(double measurement)
        {
            lock (this)
            {
                // This is relying on the bit representation of IEEE 754 to decompose
                // the double. The sign bit + exponent bits land in exponent, the
                // remainder lands in mantissa.
                // the bucketing precision comes entirely from how many significant
                // bits of the mantissa are preserved.
                ulong bits = (ulong)BitConverter.DoubleToInt64Bits(measurement);
                int exponent = (int)(bits >> ExponentShift);
                int mantissa = (int)(bits >> _mantissaShift) & _mantissaMask;
                ref int[]? mantissaCounts = ref _counters[exponent];
                mantissaCounts ??= new int[_mantissaMax];
                mantissaCounts[mantissa]++;

                // Don't increase the count if there are any NaN or +/-Infinity values that were logged
                if (exponent != PositiveIntAndNan && exponent != NegativeIntAndNan)
                {
                    _count++;
                    _sum += measurement;
                }
            }
        }

        private static int QuantileToRank(double quantile, int count)
        {
            return Math.Min(Math.Max(0, (int)(quantile * count)), count - 1);
        }

        // This is the upper bound for negative valued buckets and the
        // lower bound for positive valued buckets
        private double GetBucketCanonicalValue(int exponent, int mantissa)
        {
            long bits = ((long)exponent << ExponentShift) | ((long)mantissa << _mantissaShift);
            return BitConverter.Int64BitsToDouble(bits);
        }
    }
}
