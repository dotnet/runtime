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
