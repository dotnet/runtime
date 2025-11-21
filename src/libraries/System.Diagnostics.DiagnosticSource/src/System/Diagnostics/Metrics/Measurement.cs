// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// <see cref="Measurement{T}"/> stores one observed value and its associated tags for a metric.
    /// Observable instruments use this type when reporting current measurements from their <see cref="ObservableInstrument{T}.Observe()"/> implementation.
    /// </summary>
    public readonly struct Measurement<T> where T : struct
    {
        private readonly KeyValuePair<string, object?>[] _tags;

        /// <summary>
        /// Initializes a new instance of <see cref="Measurement{T}"/> with the provided <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value of the measurement.</param>
        public Measurement(T value)
        {
            _tags = Instrument.EmptyTags;
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of Measurement with the provided <paramref name="value"/> and zero or more associated <paramref name="tags"/>.
        /// </summary>
        /// <param name="value">The value of the measurement.</param>
        /// <param name="tags">The <see cref="KeyValuePair{TKey, TValue}"/> tags associated with the measurement.</param>
        public Measurement(T value, IEnumerable<KeyValuePair<string, object?>>? tags)
        {
            _tags = tags?.ToArray() ?? Instrument.EmptyTags;
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of Measurement with the provided <paramref name="value"/> and zero or more associated <paramref name="tags"/>.
        /// </summary>
        /// <param name="value">The value of the measurement.</param>
        /// <param name="tags">The <see cref="KeyValuePair{TKey, TValue}"/> tags associated with the measurement.</param>
        public Measurement(T value, params KeyValuePair<string, object?>[]? tags)
        {
            if (tags is not null)
            {
                _tags = new KeyValuePair<string, object?>[tags.Length];
                tags.CopyTo(_tags, 0);
            }
            else
            {
                _tags = Instrument.EmptyTags;
            }

            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of Measurement with the provided <paramref name="value"/> and a <see cref="ReadOnlySpan{T}"/> containing
        /// zero or more associated <paramref name="tags"/>.
        /// </summary>
        /// <param name="value">The value of the measurement.</param>
        /// <param name="tags">The <see cref="KeyValuePair{TKey, TValue}"/> tags associated with the measurement.</param>
        public Measurement(T value, params ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            _tags = tags.ToArray();
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the Measurement with the provided <paramref name="value"/> and a <see cref="TagList"/> containing
        /// zero or more associated <paramref name="tags"/>.
        /// </summary>
        /// <param name="value">The value of the measurement.</param>
        /// <param name="tags">A <see cref="TagList"/> containing the <see cref="KeyValuePair{TKey, TValue}"/> tags associated with the measurement.</param>
        public Measurement(T value, in TagList tags)
        {
            if (tags.Count > 0)
            {
                _tags = new KeyValuePair<string, object?>[tags.Count];
                tags.CopyTo(_tags.AsSpan());
            }
            else
            {
                _tags = Instrument.EmptyTags;
            }

            Value = value;
        }

        /// <summary>
        /// Gets the tags associated with the measurement.
        /// </summary>
        public ReadOnlySpan<KeyValuePair<string, object?>> Tags => _tags.AsSpan();

        /// <summary>
        /// Gets the value of the measurement.
        /// </summary>
        public T Value { get; }
    }
}
