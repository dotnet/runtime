// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Collections.Generic;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// Instrument{T} is the base class from which all non-observable instruments will inherit from.
    /// </summary>
    /// <remarks>
    /// This class supports only the following generic parameter types: <see cref="byte" />, <see cref="short" />, <see cref="int" />, <see cref="long" />, <see cref="float" />, <see cref="double" />, and <see cref="decimal" />
    /// </remarks>
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
    public abstract partial class Instrument<T> : Instrument where T : struct
    {
        [ThreadStatic] private KeyValuePair<string, object?>[] ts_tags;

        private const int MaxTagsCount = 8;

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag">A key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag)
        {
            var tags = ts_tags ?? new KeyValuePair<string, object?>[MaxTagsCount];
            ts_tags = null;
            tags[0] = tag;
            RecordMeasurement(measurement, tags.AsSpan().Slice(0, 1));
            ts_tags = tags;
        }

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2)
        {
            var tags = ts_tags ?? new KeyValuePair<string, object?>[MaxTagsCount];
            ts_tags = null;
            tags[0] = tag1;
            tags[1] = tag2;
            RecordMeasurement(measurement, tags.AsSpan().Slice(0, 2));
            ts_tags = tags;
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
            var tags = ts_tags ?? new KeyValuePair<string, object?>[MaxTagsCount];
            ts_tags = null;
            tags[0] = tag1;
            tags[1] = tag2;
            tags[2] = tag3;
            RecordMeasurement(measurement, tags.AsSpan().Slice(0, 3));
            ts_tags = tags;
        }

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        /// <param name="tag3">A third key-value pair tag associated with the measurement.</param>
        /// <param name="tag4">A fourth key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3, KeyValuePair<string, object?> tag4)
        {
            var tags = ts_tags ?? new KeyValuePair<string, object?>[MaxTagsCount];
            ts_tags = null;
            tags[0] = tag1;
            tags[1] = tag2;
            tags[2] = tag3;
            tags[3] = tag4;
            RecordMeasurement(measurement, tags.AsSpan().Slice(0, 4));
            ts_tags = tags;
        }

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        /// <param name="tag3">A third key-value pair tag associated with the measurement.</param>
        /// <param name="tag4">A fourth key-value pair tag associated with the measurement.</param>
        /// <param name="tag5">A fifth key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3, KeyValuePair<string, object?> tag4,
                                         KeyValuePair<string, object?> tag5)
        {
            var tags = ts_tags ?? new KeyValuePair<string, object?>[MaxTagsCount];
            ts_tags = null;
            tags[0] = tag1;
            tags[1] = tag2;
            tags[2] = tag3;
            tags[3] = tag4;
            tags[4] = tag5;
            RecordMeasurement(measurement, tags.AsSpan().Slice(0, 5));
            ts_tags = tags;
        }

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        /// <param name="tag3">A third key-value pair tag associated with the measurement.</param>
        /// <param name="tag4">A fourth key-value pair tag associated with the measurement.</param>
        /// <param name="tag5">A fifth key-value pair tag associated with the measurement.</param>
        /// <param name="tag6">A sixth key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3, KeyValuePair<string, object?> tag4,
                                         KeyValuePair<string, object?> tag5, KeyValuePair<string, object?> tag6)
        {
            var tags = ts_tags ?? new KeyValuePair<string, object?>[MaxTagsCount];
            ts_tags = null;
            tags[0] = tag1;
            tags[1] = tag2;
            tags[2] = tag3;
            tags[3] = tag4;
            tags[4] = tag5;
            tags[5] = tag6;
            RecordMeasurement(measurement, tags.AsSpan().Slice(0, 6));
            ts_tags = tags;
        }

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        /// <param name="tag3">A third key-value pair tag associated with the measurement.</param>
        /// <param name="tag4">A fourth key-value pair tag associated with the measurement.</param>
        /// <param name="tag5">A fifth key-value pair tag associated with the measurement.</param>
        /// <param name="tag6">A sixth key-value pair tag associated with the measurement.</param>
        /// <param name="tag7">A seventh key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3, KeyValuePair<string, object?> tag4,
                                         KeyValuePair<string, object?> tag5, KeyValuePair<string, object?> tag6, KeyValuePair<string, object?> tag7)
        {
            var tags = ts_tags ?? new KeyValuePair<string, object?>[MaxTagsCount];
            ts_tags = null;
            tags[0] = tag1;
            tags[1] = tag2;
            tags[2] = tag3;
            tags[3] = tag4;
            tags[4] = tag5;
            tags[5] = tag6;
            tags[6] = tag7;
            RecordMeasurement(measurement, tags.AsSpan().Slice(0, 7));
            ts_tags = tags;
        }

        /// <summary>
        /// Record the measurement by notifying all <see cref="MeterListener" /> objects which listening to this instrument.
        /// </summary>
        /// <param name="measurement">The measurement value.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        /// <param name="tag3">A third key-value pair tag associated with the measurement.</param>
        /// <param name="tag4">A fourth key-value pair tag associated with the measurement.</param>
        /// <param name="tag5">A fifth key-value pair tag associated with the measurement.</param>
        /// <param name="tag6">A sixth key-value pair tag associated with the measurement.</param>
        /// <param name="tag7">A seventh key-value pair tag associated with the measurement.</param>
        /// <param name="tag8">An eighth key-value pair tag associated with the measurement.</param>
        protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3, KeyValuePair<string, object?> tag4,
                                         KeyValuePair<string, object?> tag5, KeyValuePair<string, object?> tag6, KeyValuePair<string, object?> tag7, KeyValuePair<string, object?> tag8)
        {
            var tags = ts_tags ?? new KeyValuePair<string, object?>[MaxTagsCount];
            ts_tags = null;
            tags[0] = tag1;
            tags[1] = tag2;
            tags[2] = tag3;
            tags[3] = tag4;
            tags[4] = tag5;
            tags[5] = tag6;
            tags[6] = tag7;
            tags[7] = tag8;
            RecordMeasurement(measurement, tags.AsSpan().Slice(0, 8));
            ts_tags = tags;
        }
    }
}