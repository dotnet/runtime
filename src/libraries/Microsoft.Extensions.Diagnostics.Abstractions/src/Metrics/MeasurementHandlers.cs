// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    public class MeasurementHandlers
    {
        public MeasurementCallback<byte>? ByteHandler { get; set; }
        public MeasurementCallback<short>? ShortHandler { get; set; }
        public MeasurementCallback<int>? IntHandler { get; set; }
        public MeasurementCallback<long>? LongHandler { get; set; }
        public MeasurementCallback<float>? FloatHandler { get; set; }
        public MeasurementCallback<double>? DoubleHandler { get; set; }
        public MeasurementCallback<decimal>? DecimalHandler { get; set; }
    }
}
