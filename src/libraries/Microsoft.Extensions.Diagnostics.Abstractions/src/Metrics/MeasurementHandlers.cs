// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    /// <summary>
    /// Represents a set of supported measurement types. If a listener does not support a given type, the measurement is skipped.
    /// </summary>
    public class MeasurementHandlers
    {
        /// <summary>
        /// Gets or sets a <see cref="MeasurementCallback{T}"/> for <see cref="byte"/>. If null, byte measurements are skipped.
        /// </summary>
        public MeasurementCallback<byte>? ByteHandler { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="MeasurementCallback{T}"/> for <see cref="short"/>. If null, short measurements are skipped.
        /// </summary>
        public MeasurementCallback<short>? ShortHandler { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="MeasurementCallback{T}"/> for <see cref="int"/>. If null, int measurements are skipped.
        /// </summary>
        public MeasurementCallback<int>? IntHandler { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="MeasurementCallback{T}"/> for <see cref="long"/>. If null, long measurements are skipped.
        /// </summary>
        public MeasurementCallback<long>? LongHandler { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="MeasurementCallback{T}"/> for <see cref="float"/>. If null, float measurements are skipped.
        /// </summary>
        public MeasurementCallback<float>? FloatHandler { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="MeasurementCallback{T}"/> for <see cref="double"/>. If null, double measurements are skipped.
        /// </summary>
        public MeasurementCallback<double>? DoubleHandler { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="MeasurementCallback{T}"/> for <see cref="decimal"/>. If null, decimal measurements are skipped.
        /// </summary>
        public MeasurementCallback<decimal>? DecimalHandler { get; set; }
    }
}
