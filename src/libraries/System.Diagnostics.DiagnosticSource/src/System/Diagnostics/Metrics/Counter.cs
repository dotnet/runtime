// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// The counter is an instrument that supports adding non-negative values. For example you might call
    /// counter.Add(1) each time a request is processed to track the total number of requests. Most metric viewers
    /// will display counters using a rate by default (requests/sec) but can also display a cumulative total.
    /// </summary>
    /// <remarks>
    /// This class supports only the following generic parameter types: <see cref="byte" />, <see cref="short" />, <see cref="int" />, <see cref="long" />, <see cref="float" />, <see cref="double" />, and <see cref="decimal" />
    /// </remarks>
    public sealed class Counter<T> : Instrument<T> where T : struct
    {
        internal Counter(Meter meter, string name, string? unit, string? description) : this(meter, name, unit, description, null)
        {
        }

        internal Counter(Meter meter, string name, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) : base(meter, name, unit, description, tags)
        {
            Publish();
        }

        /// <summary>
        /// Record the increment value of the measurement.
        /// </summary>
        /// <param name="delta">The increment measurement.</param>
        public void Add(T delta) => RecordMeasurement(delta);

        /// <summary>
        /// Record the increment value of the measurement.
        /// </summary>
        /// <param name="delta">The increment measurement.</param>
        /// <param name="tag">A key-value pair tag associated with the measurement.</param>
        public void Add(T delta, KeyValuePair<string, object?> tag) => RecordMeasurement(delta, tag);

        /// <summary>
        /// Record the increment value of the measurement.
        /// </summary>
        /// <param name="delta">The increment measurement.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        public void Add(T delta, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2) => RecordMeasurement(delta, tag1, tag2);

        /// <summary>
        /// Record the increment value of the measurement.
        /// </summary>
        /// <param name="delta">The increment measurement.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        /// <param name="tag3">A third key-value pair tag associated with the measurement.</param>
        public void Add(T delta, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3) => RecordMeasurement(delta, tag1, tag2, tag3);

        /// <summary>
        /// Record the increment value of the measurement.
        /// </summary>
        /// <param name="delta">The increment measurement.</param>
        /// <param name="tags">A span of key-value pair tags associated with the measurement.</param>
        public void Add(T delta, ReadOnlySpan<KeyValuePair<string, object?>> tags) => RecordMeasurement(delta, tags);

        /// <summary>
        /// Record the increment value of the measurement.
        /// </summary>
        /// <param name="delta">The increment measurement.</param>
        /// <param name="tags">A list of key-value pair tags associated with the measurement.</param>
        public void Add(T delta, params KeyValuePair<string, object?>[] tags) => RecordMeasurement(delta, tags.AsSpan());

        /// <summary>
        /// Record the increment value of the measurement.
        /// </summary>
        /// <param name="delta">The measurement value.</param>
        /// <param name="tagList">A <see cref="T:System.Diagnostics.TagList" /> of tags associated with the measurement.</param>
        public void Add(T delta, in TagList tagList) => RecordMeasurement(delta, in tagList);
    }
}
