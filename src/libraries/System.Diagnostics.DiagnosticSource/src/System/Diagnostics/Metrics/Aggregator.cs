// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace System.Diagnostics.Metrics
{
    internal abstract class Aggregator
    {
        // This can be called concurrently with Collect()
        public abstract void Update(double measurement);

        // This can be called concurrently with Update()
        public abstract IAggregationStatistics Collect();
    }

    internal interface IAggregationStatistics { }

    internal readonly struct QuantileValue
    {
        public QuantileValue(double quantile, double value)
        {
            Quantile = quantile;
            Value = value;
        }
        public double Quantile { get; }
        public double Value { get; }
    }

    internal sealed class HistogramStatistics : IAggregationStatistics
    {
        internal HistogramStatistics(QuantileValue[] quantiles, int count, double sum)
        {
            Quantiles = quantiles;
            Count = count;
            Sum = sum;
        }

        public QuantileValue[] Quantiles { get; }
        public int Count { get; }
        public double Sum { get; }
    }

    /// <summary>
    /// Represents the statistics of a base 2 exponential histogram.
    /// </summary>
    internal sealed class Base2ExponentialHistogramStatistics : IAggregationStatistics
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Base2ExponentialHistogramStatistics"/> class capturing the current collected statistics.
        /// </summary>
        /// <param name="scale">Maximum scale factor.</param>
        /// <param name="zeroCount">The number of zero values in the histogram.</param>
        /// <param name="sum">The sum of all values in the histogram.</param>
        /// <param name="count">The count of all values in the histogram.</param>
        /// <param name="min">The minimum value in the histogram.</param>
        /// <param name="max">The maximum value in the histogram.</param>
        /// <param name="buckets">The buckets in the histogram.</param>
        internal Base2ExponentialHistogramStatistics(int scale, long zeroCount, double sum, long count, double min, double max, long[] buckets)
        {
            Scale = scale;
            ZeroCount = zeroCount;
            Sum = sum;
            Count = count;
            Minimum = min;
            Maximum = max;
            PositiveBuckets = buckets;
        }

        /// <summary>
        /// Gets the maximum scale factor.
        /// </summary>
        public int Scale { get; }

        /// <summary>
        /// Gets the number of zero values in the histogram.
        /// </summary>
        public long ZeroCount { get; }

        /// <summary>
        /// Gets the sum of all values in the histogram.
        /// </summary>
        public double Sum { get; }

        /// <summary>
        /// Gets the count of all values in the histogram.
        /// </summary>
        public long Count { get; }

        /// <summary>
        /// Gets the minimum value in the histogram.
        /// </summary>
        public double Minimum { get; }

        /// <summary>
        /// Gets the maximum value in the histogram.
        /// </summary>
        public double Maximum { get; }

        /// <summary>
        /// Gets the positive measurement buckets in the histogram.
        /// </summary>
        public long[] PositiveBuckets { get; }
    }

    internal sealed class LabeledAggregationStatistics
        {
            public LabeledAggregationStatistics(IAggregationStatistics stats, params KeyValuePair<string, string>[] labels)
            {
                AggregationStatistics = stats;
                Labels = labels;
            }

            public KeyValuePair<string, string>[] Labels { get; }
            public IAggregationStatistics AggregationStatistics { get; }
        }
}
