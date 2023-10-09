// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    /// <summary>
    /// A set of supported measurement types. If a listener does not support a given type, the measurement will be skipped.
    /// </summary>
    public class MeasurementHandlers
    {
        /// <summary>
        /// A <see cref="MeasurementCallback{T}"/> for <see cref="byte"/>. If null, byte measurements will be skipped.
        /// </summary>
        public MeasurementCallback<byte>? ByteHandler { get; set; }

        /// <summary>
        /// A <see cref="MeasurementCallback{T}"/> for <see cref="short"/>. If null, short measurements will be skipped.
        /// </summary>
        public MeasurementCallback<short>? ShortHandler { get; set; }

        /// <summary>
        /// A <see cref="MeasurementCallback{T}"/> for <see cref="int"/>. If null, int measurements will be skipped.
        /// </summary>
        public MeasurementCallback<int>? IntHandler { get; set; }

        /// <summary>
        /// A <see cref="MeasurementCallback{T}"/> for <see cref="long"/>. If null, long measurements will be skipped.
        /// </summary>
        public MeasurementCallback<long>? LongHandler { get; set; }

        /// <summary>
        /// A <see cref="MeasurementCallback{T}"/> for <see cref="float"/>. If null, float measurements will be skipped.
        /// </summary>
        public MeasurementCallback<float>? FloatHandler { get; set; }

        /// <summary>
        /// A <see cref="MeasurementCallback{T}"/> for <see cref="double"/>. If null, double measurements will be skipped.
        /// </summary>
        public MeasurementCallback<double>? DoubleHandler { get; set; }

        /// <summary>
        /// A <see cref="MeasurementCallback{T}"/> for <see cref="decimal"/>. If null, decimal measurements will be skipped.
        /// </summary>
        public MeasurementCallback<decimal>? DecimalHandler { get; set; }
    }
}
