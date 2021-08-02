// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.Metrics
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct TagsBag
    {
        internal KeyValuePair<string, object?> Tag1;
        internal KeyValuePair<string, object?> Tag2;
        internal KeyValuePair<string, object?> Tag3;
    }

    /// <summary>
    /// Instrument{T} is the base class from which all non-observable instruments will inherit from.
    /// </summary>
    /// <remarks>
    /// This class supports only the following generic parameter types: <see cref="byte" />, <see cref="short" />, <see cref="int" />, <see cref="long" />, <see cref="float" />, <see cref="double" />, and <see cref="decimal" />
    /// </remarks>
    public abstract partial class Instrument<T> : Instrument where T : struct
    {
        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag">A key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag)
        {
            TagsBag tags;
            tags.Tag1 = tag;

            RecordMeasurement(measurement, MemoryMarshal.CreateReadOnlySpan(ref tags.Tag1, 1));
        }

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2)
        {
            TagsBag tags;
            tags.Tag1 = tag1;
            tags.Tag2 = tag2;

            RecordMeasurement(measurement, MemoryMarshal.CreateReadOnlySpan(ref tags.Tag1, 2));
        }

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        /// <param name="tag3">A third key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3)
        {
            TagsBag tags;
            tags.Tag1 = tag1;
            tags.Tag2 = tag2;
            tags.Tag3 = tag3;

            RecordMeasurement(measurement, MemoryMarshal.CreateReadOnlySpan(ref tags.Tag1, 3));
        }
    }
}