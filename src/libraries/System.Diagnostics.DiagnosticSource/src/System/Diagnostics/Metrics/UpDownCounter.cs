// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// The UpDownCounter is an instrument that supports reporting positive or negative metric values.
    /// UpDownCounter may be used in scenarios like reporting the change in active requests or queue size.
    /// </summary>
    /// <remarks>
    /// This class supports only the following generic parameter types: <see cref="byte" />, <see cref="short" />, <see cref="int" />, <see cref="long" />, <see cref="float" />, <see cref="double" />, and <see cref="decimal" />
    /// </remarks>
    public sealed class UpDownCounter<T> : Instrument<T> where T : struct
    {
        internal UpDownCounter(Meter meter, string name, string? unit, string? description) : this(meter, name, unit, description, tags: null)
        {
        }

        internal UpDownCounter(Meter meter, string name, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) : base(meter, name, unit, description, tags)
        {
            Publish();
        }

        /// <summary>
        /// Record the delta value of the measurement. The delta can be positive, negative or zero.
        /// </summary>
        /// <param name="delta">The amount to be added which can be positive, negative or zero.</param>
        public void Add(T delta) => RecordMeasurement(delta);

        /// <summary>
        /// Record the delta value of the measurement. The delta can be positive, negative or zero.
        /// </summary>
        /// <param name="delta">The amount to be added which can be positive, negative or zero.</param>
        /// <param name="tag">A key-value pair tag associated with the measurement.</param>
        public void Add(T delta, KeyValuePair<string, object?> tag) => RecordMeasurement(delta, tag);

        /// <summary>
        /// Record the delta value of the measurement. The delta can be positive, negative or zero.
        /// </summary>
        /// <param name="delta">The amount to be added which can be positive, negative or zero.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        public void Add(T delta, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2) => RecordMeasurement(delta, tag1, tag2);

        /// <summary>
        /// Record the delta value of the measurement. The delta can be positive, negative or zero.
        /// </summary>
        /// <param name="delta">The amount to be added which can be positive, negative or zero.</param>
        /// <param name="tag1">A first key-value pair tag associated with the measurement.</param>
        /// <param name="tag2">A second key-value pair tag associated with the measurement.</param>
        /// <param name="tag3">A third key-value pair tag associated with the measurement.</param>
        public void Add(T delta, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3) => RecordMeasurement(delta, tag1, tag2, tag3);

        /// <summary>
        /// Record the delta value of the measurement. The delta can be positive, negative or zero.
        /// </summary>
        /// <param name="delta">The amount to be added which can be positive, negative or zero.</param>
        /// <param name="tags">A span of key-value pair tags associated with the measurement.</param>
        public void Add(T delta, ReadOnlySpan<KeyValuePair<string, object?>> tags) => RecordMeasurement(delta, tags);

        /// <summary>
        /// Record the delta value of the measurement. The delta can be positive, negative or zero.
        /// </summary>
        /// <param name="delta">The amount to be added which can be positive, negative or zero.</param>
        /// <param name="tags">A list of key-value pair tags associated with the measurement.</param>
        public void Add(T delta, params KeyValuePair<string, object?>[] tags) => RecordMeasurement(delta, tags.AsSpan());

        /// <summary>
        /// Record the delta value of the measurement. The delta can be positive, negative or zero.
        /// </summary>
        /// <param name="delta">The amount to be added which can be positive, negative or zero.</param>
        /// <param name="tagList">A <see cref="T:System.Diagnostics.TagList" /> of tags associated with the measurement.</param>
        public void Add(T delta, in TagList tagList) => RecordMeasurement(delta, in tagList);
    }
}
