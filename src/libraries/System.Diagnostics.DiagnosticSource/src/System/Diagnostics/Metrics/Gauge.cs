// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// The Gauge is an instrument used to record non-additive values whenever changes occur. For example, record the room background noise level value when changes occur.
    /// </summary>
    /// <remarks>
    /// This class supports only the following generic parameter types: <see cref="byte" />, <see cref="short" />, <see cref="int" />, <see cref="long" />, <see cref="float" />, <see cref="double" />, and <see cref="decimal" />
    /// </remarks>
    public sealed class Gauge<T> : Instrument<T> where T : struct
    {
        internal Gauge(Meter meter, string name, string? unit, string? description) : this(meter, name, unit, description, null)
        {
        }

        internal Gauge(Meter meter, string name, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) : base(meter, name, unit, description, tags)
        {
            Publish();
        }

        /// <summary>
        /// Record the Gauge current value.
        /// </summary>
        /// <param name="value">The Gauge current value.</param>
        public void Record(T value) => RecordMeasurement(value);

        /// <summary>
        /// Record the Gauge current value.
        /// </summary>
        /// <param name="value">The Gauge current value.</param>
        /// <param name="tag">A key-value pair tag associated with the measurement.</param>
        public void Record(T value, KeyValuePair<string, object?> tag) => RecordMeasurement(value, tag);

        /// <summary>
        /// Record the Gauge current value.
        /// </summary>
        /// <param name="value">The Gauge current value.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        public void Record(T value, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2) => RecordMeasurement(value, tag1, tag2);

        /// <summary>
        /// Record the Gauge current value.
        /// </summary>
        /// <param name="value">The Gauge current value.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        /// <param name="tag3">A third key-value pair tag associated with the measurement.</param>
        public void Record(T value, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3) => RecordMeasurement(value, tag1, tag2, tag3);

        /// <summary>
        /// Record the Gauge current value.
        /// </summary>
        /// <param name="value">The Gauge current value.</param>
        /// <param name="tags">A span of key-value pair tags associated with the measurement.</param>
        public void Record(T value, params ReadOnlySpan<KeyValuePair<string, object?>> tags) => RecordMeasurement(value, tags);

        /// <summary>
        /// Record the Gauge current value.
        /// </summary>
        /// <param name="value">The Gauge current value.</param>
        /// <param name="tags">A list of key-value pair tags associated with the measurement.</param>
        public void Record(T value, params KeyValuePair<string, object?>[] tags) => RecordMeasurement(value, tags.AsSpan());

        /// <summary>
        /// Record the Gauge current value.
        /// </summary>
        /// <param name="value">The Gauge current value.</param>
        /// <param name="tagList">A <see cref="T:System.Diagnostics.TagList" /> of tags associated with the measurement.</param>
        public void Record(T value, in TagList tagList) => RecordMeasurement(value, in tagList);
    }
}
