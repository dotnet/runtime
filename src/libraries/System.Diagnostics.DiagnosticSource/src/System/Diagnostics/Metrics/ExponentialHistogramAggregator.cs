// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

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
    // The uncontended implementation uses a 4096-entry array indexed by the sign and exponent from the IEEE 754 representation,
    // with mantissa bucket arrays allocated on demand. After repeated contention it switches permanently to a bounded set of
    // stripes. Each stripe uses a sparse two-level exponent tree so that parallelism does not multiply the 32KB top-level array.
    // Collect locks all active state, atomically detaches the interval, and merges the stripes outside the locks.
    internal sealed class ExponentialHistogramAggregator : Aggregator
    {
        private const int ExponentArraySize = 4096;
        private const int ExponentGroupShift = 6;
        private const int ExponentGroupSize = 1 << ExponentGroupShift;
        private const int ExponentGroupMask = ExponentGroupSize - 1;
        private const int ContentionThreshold = 64;
        private const int MaxStripeCount = 8;
        private const int ExponentShift = 52;
        private const double MinRelativeError = 0.0001;
        private const int PositiveIntAndNan = ExponentArraySize / 2 - 1;
        private const int NegativeIntAndNan = ExponentArraySize - 1;

        [ThreadStatic]
        private static int t_stripeIndex;

        private readonly QuantileAggregation _config;
        private readonly object _singleLock = new object();
        private readonly HistogramStripe[] _stripes;
        private int[]?[]? _singleCounters = new int[ExponentArraySize][];
        private int _singleCount;
        private double _singleSum;
        private int _contentionCount;
        private bool _collecting;
        private bool _useStripes;
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

        private sealed class HistogramStripe
        {
            public HistogramBuckets? Buckets;
            public int Count;
            public double Sum;
        }

        private sealed class ExponentGroup
        {
            public readonly int[]?[] Exponents = new int[ExponentGroupSize][];
        }

        private sealed class HistogramBuckets
        {
            private readonly ExponentGroup?[] _groups = new ExponentGroup[ExponentArraySize / ExponentGroupSize];
            private readonly int _mantissaMax;

            public HistogramBuckets(int mantissaMax)
            {
                _mantissaMax = mantissaMax;
            }

            public void Increment(int exponent, int mantissa)
            {
                ExponentGroup group = _groups[exponent >> ExponentGroupShift] ??= new ExponentGroup();
                ref int[]? counts = ref group.Exponents[exponent & ExponentGroupMask];
                counts ??= new int[_mantissaMax];
                counts[mantissa]++;
            }

            public int[]? GetCounts(int exponent) =>
                _groups[exponent >> ExponentGroupShift]?.Exponents[exponent & ExponentGroupMask];

            public void MergeFrom(HistogramBuckets source)
            {
                for (int groupIndex = 0; groupIndex < source._groups.Length; groupIndex++)
                {
                    ExponentGroup? sourceGroup = source._groups[groupIndex];
                    if (sourceGroup is null)
                    {
                        continue;
                    }

                    ExponentGroup targetGroup = _groups[groupIndex] ??= new ExponentGroup();
                    for (int exponentIndex = 0; exponentIndex < sourceGroup.Exponents.Length; exponentIndex++)
                    {
                        int[]? sourceCounts = sourceGroup.Exponents[exponentIndex];
                        if (sourceCounts is null)
                        {
                            continue;
                        }

                        ref int[]? targetCounts = ref targetGroup.Exponents[exponentIndex];
                        targetCounts ??= new int[_mantissaMax];
                        for (int mantissa = 0; mantissa < sourceCounts.Length; mantissa++)
                        {
                            targetCounts[mantissa] += sourceCounts[mantissa];
                        }
                    }
                }
            }
        }

        public ExponentialHistogramAggregator(QuantileAggregation config)
        {
            _config = config;
            if (_config.MaxRelativeError < MinRelativeError)
            {
                // Ensure that we don't create enormous histograms trying to get overly high precision
                throw new ArgumentException();
            }
            int mantissaBits = (int)Math.Ceiling(Math.Log(1 / _config.MaxRelativeError, 2)) - 1;
            _mantissaShift = 52 - mantissaBits;
            _mantissaMax = 1 << mantissaBits;
            _mantissaMask = _mantissaMax - 1;

            _stripes = new HistogramStripe[Math.Min(Environment.ProcessorCount, MaxStripeCount)];
            for (int i = 0; i < _stripes.Length; i++)
            {
                _stripes[i] = new HistogramStripe();
            }
        }

        public override IAggregationStatistics Collect()
        {
            int[]?[]? singleCounters = null;
            HistogramBuckets?[] snapshots = new HistogramBuckets?[_stripes.Length];
            int count = 0;
            double sum = 0;
            bool singleLockTaken = false;
            int locksTaken = 0;
            Volatile.Write(ref _collecting, true);
            try
            {
                Monitor.Enter(_singleLock, ref singleLockTaken);
                for (; locksTaken < _stripes.Length; locksTaken++)
                {
                    Monitor.Enter(_stripes[locksTaken]);
                }

                singleCounters = _singleCounters;
                count = _singleCount;
                sum = _singleSum;
                _singleCounters = Volatile.Read(ref _useStripes) ? null : new int[ExponentArraySize][];
                _singleCount = 0;
                _singleSum = 0;
                _contentionCount = 0;

                for (int i = 0; i < _stripes.Length; i++)
                {
                    HistogramStripe stripe = _stripes[i];
                    snapshots[i] = stripe.Buckets;
                    count += stripe.Count;
                    sum += stripe.Sum;
                    stripe.Buckets = null;
                    stripe.Count = 0;
                    stripe.Sum = 0;
                }
            }
            finally
            {
                while (locksTaken != 0)
                {
                    Monitor.Exit(_stripes[--locksTaken]);
                }

                if (singleLockTaken)
                {
                    Monitor.Exit(_singleLock);
                }

                Volatile.Write(ref _collecting, false);
            }

            HistogramBuckets? counters = null;
            foreach (HistogramBuckets? snapshot in snapshots)
            {
                if (snapshot is not null)
                {
                    if (counters is null)
                    {
                        counters = snapshot;
                    }
                    else
                    {
                        counters.MergeFrom(snapshot);
                    }
                }
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
            if (singleCounters is null && counters is null)
            {
                Debug.Assert(count == 0);
                return new HistogramStatistics(Array.Empty<QuantileValue>(), count, sum);
            }

            foreach (Bucket b in IterateBuckets(singleCounters, counters))
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

        private IEnumerable<Bucket> IterateBuckets(int[]?[]? singleCounters, HistogramBuckets? counters)
        {
            // iterate over the negative exponent buckets
            const int LowestNegativeOffset = ExponentArraySize / 2;
            // exponent = ExponentArraySize-1 encodes infinity and NaN, which we want to ignore
            for (int exponent = ExponentArraySize - 2; exponent >= LowestNegativeOffset; exponent--)
            {
                int[]? singleMantissaCounts = singleCounters?[exponent];
                int[]? stripedMantissaCounts = counters?.GetCounts(exponent);
                if (singleMantissaCounts is null && stripedMantissaCounts is null)
                {
                    continue;
                }
                for (int mantissa = _mantissaMax - 1; mantissa >= 0; mantissa--)
                {
                    int count = (singleMantissaCounts?[mantissa] ?? 0) + (stripedMantissaCounts?[mantissa] ?? 0);
                    if (count > 0)
                    {
                        yield return new Bucket(GetBucketCanonicalValue(exponent, mantissa), count);
                    }
                }
            }

            // iterate over the positive exponent buckets
            // exponent = lowestNegativeOffset-1 encodes infinity and NaN, which we want to ignore
            for (int exponent = 0; exponent < LowestNegativeOffset - 1; exponent++)
            {
                int[]? singleMantissaCounts = singleCounters?[exponent];
                int[]? stripedMantissaCounts = counters?.GetCounts(exponent);
                if (singleMantissaCounts is null && stripedMantissaCounts is null)
                {
                    continue;
                }
                for (int mantissa = 0; mantissa < _mantissaMax; mantissa++)
                {
                    int count = (singleMantissaCounts?[mantissa] ?? 0) + (stripedMantissaCounts?[mantissa] ?? 0);
                    if (count > 0)
                    {
                        yield return new Bucket(GetBucketCanonicalValue(exponent, mantissa), count);
                    }
                }
            }
        }

        public override void Update(double measurement)
        {
            // This is relying on the bit representation of IEEE 754 to decompose
            // the double. The sign bit + exponent bits land in exponent, the
            // remainder lands in mantissa.
            // the bucketing precision comes entirely from how many significant
            // bits of the mantissa are preserved.
            ulong bits = (ulong)BitConverter.DoubleToInt64Bits(measurement);
            int exponent = (int)(bits >> ExponentShift);
            int mantissa = (int)(bits >> _mantissaShift) & _mantissaMask;

            if (!Volatile.Read(ref _useStripes))
            {
                bool lockTaken = false;
                try
                {
                    Monitor.TryEnter(_singleLock, ref lockTaken);
                    if (lockTaken && !Volatile.Read(ref _useStripes))
                    {
                        UpdateSingle(exponent, mantissa, measurement);
                        return;
                    }
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(_singleLock);
                    }
                }

                if (Volatile.Read(ref _collecting))
                {
                    lock (_singleLock)
                    {
                        if (!Volatile.Read(ref _useStripes))
                        {
                            UpdateSingle(exponent, mantissa, measurement);
                            return;
                        }
                    }
                }
                else if (Interlocked.Increment(ref _contentionCount) < ContentionThreshold)
                {
                    lock (_singleLock)
                    {
                        if (!Volatile.Read(ref _useStripes))
                        {
                            UpdateSingle(exponent, mantissa, measurement);
                            return;
                        }
                    }
                }

                Volatile.Write(ref _useStripes, true);
            }

            int stripeIndex = t_stripeIndex;
            if (stripeIndex == 0)
            {
                stripeIndex = t_stripeIndex = (Environment.CurrentManagedThreadId % MaxStripeCount) + 1;
            }

            HistogramStripe stripe = _stripes[(stripeIndex - 1) % _stripes.Length];

            lock (stripe)
            {
                (stripe.Buckets ??= new HistogramBuckets(_mantissaMax)).Increment(exponent, mantissa);

                // Don't increase the count if there are any NaN or +/-Infinity values that were logged
                if (exponent != PositiveIntAndNan && exponent != NegativeIntAndNan)
                {
                    stripe.Count++;
                    stripe.Sum += measurement;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateSingle(int exponent, int mantissa, double measurement)
        {
            ref int[]? mantissaCounts = ref _singleCounters![exponent];
            mantissaCounts ??= new int[_mantissaMax];
            mantissaCounts[mantissa]++;

            if (exponent != PositiveIntAndNan && exponent != NegativeIntAndNan)
            {
                _singleCount++;
                _singleSum += measurement;
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
