// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// This describes the type of aggregation we want to do for this instrument
    /// </summary>
    /// <remarks>
    /// Right now we only support "temporal aggregation" - combining the values from multiple measurements taken at different
    /// times with the same set of labels
    /// In the future we could also support "spatial aggregation" - combining the values from multiple measurements that have
    /// different sets of labels
    /// </remarks>
    internal class InstrumentAggregation
    {
        public InstrumentAggregation(MeasurementAggregation measurementAggregation)
        {
            MeasurementAggregation = measurementAggregation;
        }
        public MeasurementAggregation MeasurementAggregation { get; }
    }

    internal static class MeasurementAggregations
    {
        public static RateAggregation Rate = new RateAggregation();
        public static RateSumAggregation RateSum = new RateSumAggregation();
        public static LastValueAggregation LastValue = new LastValueAggregation();
    }

    internal class MeasurementAggregation
    {
    }

    internal class PercentileAggregation : MeasurementAggregation
    {
        public PercentileAggregation(double[] percentiles)
        {
            Percentiles = percentiles;
        }

        public double[] Percentiles { get; set; }
        public double MaxRelativeError { get; set; } = 0.001;
        public double MinValue { get; set; } = double.MinValue;
        public double MaxValue { get; set; } = double.MaxValue;

    }

    internal class RateAggregation : MeasurementAggregation { }
    internal class RateSumAggregation : MeasurementAggregation { }

    internal class LastValueAggregation : MeasurementAggregation { }
}
