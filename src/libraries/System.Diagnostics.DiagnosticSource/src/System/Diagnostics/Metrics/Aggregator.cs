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
        public abstract AggregationStatistics? Collect();
    }

    internal abstract class AggregationStatistics
    {
        protected AggregationStatistics(MeasurementAggregation measurementAggregation)
        {
            MeasurementAggregation = measurementAggregation;
        }
        public MeasurementAggregation MeasurementAggregation { get; }
    }

    internal struct QuantileValue
    {
        public QuantileValue(double quantile, double value)
        {
            Quantile = quantile;
            Value = value;
        }
        public double Quantile { get; }
        public double Value { get; }
    }

    internal class HistogramStatistics : AggregationStatistics
    {
        internal HistogramStatistics(MeasurementAggregation measurementAggregation, QuantileValue[] quantiles) :
            base(measurementAggregation)
        {
            Quantiles = quantiles;
        }

        public QuantileValue[] Quantiles { get; }
    }

    internal class LabeledAggregationStatistics
    {
        private AggregationStatistics _aggStats;
        private KeyValuePair<string, string>[] _labels;

        public LabeledAggregationStatistics(AggregationStatistics stats, params KeyValuePair<string, string>[] labels)
        {
            _aggStats = stats;
            _labels = labels;
        }

        public KeyValuePair<string, string>[] Labels => _labels;
        public AggregationStatistics AggregationStatistics => _aggStats;
    }
}
