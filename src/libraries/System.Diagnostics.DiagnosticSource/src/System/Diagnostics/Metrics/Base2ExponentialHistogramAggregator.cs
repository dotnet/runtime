// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from OpenTelemetry .NET implementation:
// https://github.com/open-telemetry/opentelemetry-dotnet/blob/805dd6b4abfa18ef2706d04c30d0ed28dbc2955e/src/OpenTelemetry/Metrics/MetricPoint/Base2ExponentialBucketHistogram.cs
// Licensed under the Apache 2.0 License. See LICENSE: https://github.com/open-telemetry/opentelemetry-dotnet/blob/805dd6b4abfa18ef2706d04c30d0ed28dbc2955e/LICENSE.TXT
// Copyright The OpenTelemetry Authors

#if NET
using System.Numerics;
#endif
using System.Runtime.CompilerServices;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// Represents an exponential bucket histogram with base = 2 ^ (2 ^ (-scale)).
    /// An exponential bucket histogram has infinite number of buckets, which are
    /// identified by <c>Bucket[index] = ( base ^ index, base ^ (index + 1) ]</c>,
    /// where <c>index</c> is an integer.
    /// </summary>
    /// <remarks>
    /// - The implementation is based on the OpenTelemetry specification for exponential histograms.
    ///     https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/metrics/data-model.md#exponentialhistogram
    /// </remarks>
    internal sealed class Base2ExponentialHistogramAggregator : Aggregator
    {
        private const int MinScale = -11;
        private const int MaxScale = 20;
        private const int MinBuckets = 2;

        private const long FractionMask = 0xFFFFFFFFFFFFFL;
        private const long ExponentMask = 0x7FF0000000000000L;
        private const int FractionWidth = 52;

        private int _scale;
        private double _maxMeasurement;
        private double _minMeasurement;
        private double _sum;
        private long _count;
        private bool _reportDeltas;

        private double _scalingFactor; // 2 ^ scale / log(2)

        /// <summary>
        /// Initializes a new instance of the Base2ExponentialHistogramAggregator class.
        /// </summary>
        /// <param name="maxBuckets">The maximum number of buckets in each of the positive ranges, not counting the special zero bucket. The default value is 160.</param>
        /// <param name="scale">Maximum scale factor. The default value is 20.</param>
        /// <param name="reportDeltas">If true, the histogram will report deltas instead of whole accumulated values. The default value is false.</param>
        public Base2ExponentialHistogramAggregator(int maxBuckets = 160, int scale = 20, bool reportDeltas = false)
        {
            /*
            The following table is calculated based on [ MapToIndex(double.Epsilon), MapToIndex(double.MaxValue) ]:

            | Scale | Index Range               |
            | ----- | ------------------------- |
            | < -11 | [-1, 0]                   |
            | -11   | [-1, 0]                   |
            | -10   | [-2, 0]                   |
            | -9    | [-3, 1]                   |
            | -8    | [-5, 3]                   |
            | -7    | [-9, 7]                   |
            | -6    | [-17, 15]                 |
            | -5    | [-34, 31]                 |
            | -4    | [-68, 63]                 |
            | -3    | [-135, 127]               |
            | -2    | [-269, 255]               |
            | -1    | [-538, 511]               |
            | 0     | [-1075, 1023]             |
            | 1     | [-2149, 2047]             |
            | 2     | [-4297, 4095]             |
            | 3     | [-8593, 8191]             |
            | 4     | [-17185, 16383]           |
            | 5     | [-34369, 32767]           |
            | 6     | [-68737, 65535]           |
            | 7     | [-137473, 131071]         |
            | 8     | [-274945, 262143]         |
            | 9     | [-549889, 524287]         |
            | 10    | [-1099777, 1048575]       |
            | 11    | [-2199553, 2097151]       |
            | 12    | [-4399105, 4194303]       |
            | 13    | [-8798209, 8388607]       |
            | 14    | [-17596417, 16777215]     |
            | 15    | [-35192833, 33554431]     |
            | 16    | [-70385665, 67108863]     |
            | 17    | [-140771329, 134217727]   |
            | 18    | [-281542657, 268435455]   |
            | 19    | [-563085313, 536870911]   |
            | 20    | [-1126170625, 1073741823] |
            | 21    | [underflow, 2147483647]   |
            | > 21  | [underflow, overflow]     |
            */

            if (scale < MinScale || scale > MaxScale)
            {
                throw new ArgumentOutOfRangeException(nameof(scale), SR.Format(SR.InvalidHistogramScale, scale, MinScale, MaxScale));
            }

            // Regardless of the scale, MapToIndex(1) will always be -1, so we need two buckets at minimum:
            //     bucket[-1] = (1/base, 1]
            //     bucket[0] = (1, base]

            if (maxBuckets < MinBuckets)
            {
                throw new ArgumentOutOfRangeException(nameof(maxBuckets), SR.Format(SR.InvalidHistogramMaxBuckets, maxBuckets, MinBuckets));
            }

            Scale = scale;

            _reportDeltas = reportDeltas;
            _minMeasurement = double.MaxValue;
            _maxMeasurement = double.MinValue;

            PositiveBuckets = new CircularBufferBuckets(maxBuckets);
        }

        internal CircularBufferBuckets PositiveBuckets { get; }

        internal int Scale
        {
            get => _scale;

            set
            {
                _scale = value;

                // A subset of Math.ScaleB(Math.Log2(Math.E), value)
                _scalingFactor = BitConverter.Int64BitsToDouble(0x71547652B82FEL | ((0x3FFL + value) << FractionWidth));
            }
        }

        internal long Count => _count;
        internal double Sum => _sum;
        internal double MinMeasurement => _minMeasurement;
        internal double MaxMeasurement => _maxMeasurement;

        internal double ScalingFactor => _scalingFactor;

        /// <summary>
        /// The number of zero values in the histogram.
        /// </summary>
        internal long ZeroCount { get; private set; }

        public override IAggregationStatistics Collect()
        {
            lock (this)
            {
                var newStats = new Base2ExponentialHistogramStatistics(
                    Scale,
                    ZeroCount,
                    _sum,
                    _count,
                    _minMeasurement,
                    _maxMeasurement,
                    PositiveBuckets.ToArray());

                if (_reportDeltas)
                {
                    // Reset the state of the aggregator.
                    _sum = 0;
                    _minMeasurement = double.MaxValue;
                    _maxMeasurement = double.MinValue;
                    ZeroCount = 0;
                    _count = 0;
                    PositiveBuckets.Clear();
                }

                return newStats;
            }
        }

        public override void Update(double measurement)
        {
            if (!IsFinite(measurement))
            {
                return;
            }

            var c = measurement.CompareTo(0);

            if (c < 0)
            {
                return; // Negative values are not supported according to the OpenTelemetry spec.
            }

            lock (this)
            {
                _maxMeasurement = Math.Max(_maxMeasurement, measurement);
                _minMeasurement = Math.Min(_minMeasurement, measurement);
                _count++;

                if (c == 0)
                {
                    ZeroCount++;
                    return;
                }

                _sum += measurement;

                var index = MapToIndex(measurement);
                var n = PositiveBuckets.TryIncrement(index);

                if (n == 0)
                {
                    return;
                }

                PositiveBuckets.ScaleDown(n);
                Scale -= n;

                if (Scale < MinScale)
                {
                    Scale = MinScale;
                }
                else if (Scale > MaxScale)
                {
                    Scale = MaxScale;
                }

                n = PositiveBuckets.TryIncrement(index >> n);
                Debug.Assert(n == 0, "Increment should always succeed after scale down.");
            }
        }

        /// <summary>
        /// Maps a finite positive IEEE 754 double-precision floating-point
        /// number to <c>Bucket[index] = ( base ^ index, base ^ (index + 1) ]</c>,
        /// where <c>index</c> is an integer.
        /// </summary>
        /// <param name="value">The value to be bucketized. Must be a finite positive number.</param>
        /// <returns>Returns the index of the bucket.</returns>
        public int MapToIndex(double value)
        {
            Debug.Assert(IsFinite(value), "IEEE-754 +Inf, -Inf and NaN should be filtered out before calling this method.");
            Debug.Assert(value != 0, "IEEE-754 zero values should be handled by ZeroCount.");
            Debug.Assert(value > 0, "IEEE-754 negative values should be normalized before calling this method.");

            var bits = BitConverter.DoubleToInt64Bits(value);
            var fraction = bits & FractionMask;

            if (Scale > 0)
            {
                if (fraction == 0)
                {
                    var exp = (int)((bits & ExponentMask) >> FractionWidth);
                    return ((exp - 1023 /* exponent bias */) << Scale) - 1;
                }

                return (int)Math.Ceiling(Math.Log(value) * _scalingFactor) - 1;
            }
            else
            {
                var exp = (int)((bits & ExponentMask) >> FractionWidth);

                if (exp == 0)
                {
                    exp -= LeadingZero64(fraction - 1) - 12 /* 64 - fraction width */;
                }
                else if (fraction == 0)
                {
                    exp--;
                }

                return (exp - 1023 /* exponent bias */) >> -Scale;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsFinite(double value)
        {
#if NET
            return double.IsFinite(value);
#else
            return !double.IsInfinity(value) && !double.IsNaN(value);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LeadingZero64(long value)
        {
#if NET
        return BitOperations.LeadingZeroCount((ulong)value);
#else
            unchecked
            {
                var high32 = (int)(value >> 32);

                if (high32 != 0)
                {
                    return LeadingZero32(high32);
                }

                return LeadingZero32((int)value) + 32;
            }
#endif
        }

#if !NET
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LeadingZero32(int value)
        {
            unchecked
            {
                var high16 = (short)(value >> 16);

                if (high16 != 0)
                {
                    return LeadingZero16(high16);
                }

                return LeadingZero16((short)value) + 16;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LeadingZero16(short value)
        {
            unchecked
            {
                var high8 = (byte)(value >> 8);

                if (high8 != 0)
                {
                    return LeadingZero8(high8);
                }

                return LeadingZero8((byte)value) + 8;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LeadingZero8(byte value) => LeadingZeroLookupTable[value];

        private static ReadOnlySpan<byte> LeadingZeroLookupTable =>
        [
            8, 7, 6, 6, 5, 5, 5, 5, 4, 4, 4, 4, 4, 4, 4, 4,
            3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        ];
#endif // !NET
    }
}
