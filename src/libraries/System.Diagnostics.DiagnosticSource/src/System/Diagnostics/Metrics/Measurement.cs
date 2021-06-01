// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// Measurement stores one observed metrics value and its associated tags. This type is used by Observable instruments' Observe() method when reporting current measurements.
    /// with the associated tags.
    /// </summary>
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
    public readonly struct Measurement<T> where T : struct
    {
        private readonly KeyValuePair<string, object?>[] _tags;

        /// <summary>
        /// Initializes a new instance of the Measurement using the value and the list of tags.
        /// </summary>
        /// <param name="value">The measurement value.</param>
        public Measurement(T value)
        {
            _tags = Instrument.EmptyTags;
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the Measurement using the value and the list of tags.
        /// </summary>
        /// <param name="value">The measurement value.</param>
        /// <param name="tags">The measurement associated tags list.</param>
        public Measurement(T value, IEnumerable<KeyValuePair<string, object?>>? tags)
        {
            _tags = ToArray(tags);
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the Measurement using the value and the list of tags.
        /// </summary>
        /// <param name="value">The measurement value.</param>
        /// <param name="tags">The measurement associated tags list.</param>
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
        /// Initializes a new instance of the Measurement using the value and the list of tags.
        /// </summary>
        /// <param name="value">The measurement value.</param>
        /// <param name="tags">The measurement associated tags list.</param>
        public Measurement(T value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            _tags = tags.ToArray();
            Value = value;
        }

        /// <summary>
        /// Gets the measurement tags list.
        /// </summary>
        public ReadOnlySpan<KeyValuePair<string, object?>> Tags => _tags.AsSpan();

        /// <summary>
        /// Gets the measurement value.
        /// </summary>
        public T Value { get; }

        // Private helper to copy IEnumerable to array. We have it to avoid adding dependencies on System.Linq
        private static KeyValuePair<string, object?>[] ToArray(IEnumerable<KeyValuePair<string, object?>>? tags) => tags is null ? Instrument.EmptyTags : new List<KeyValuePair<string, object?>>(tags).ToArray();
    }
}
