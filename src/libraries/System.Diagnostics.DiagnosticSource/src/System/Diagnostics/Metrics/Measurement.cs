// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// Measurement stores one observed metrics value and its associated tags. This type is used by Observable instruments' Observe() method when reporting current measurements.
    /// with the associated tags.
    /// </summary>
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
        public Measurement(T value, params ReadOnlySpan<KeyValuePair<string, object?>> tags)
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
        private static KeyValuePair<string, object?>[] ToArray(IEnumerable<KeyValuePair<string, object?>>? tags)
        {
            if (tags is null)
                return Instrument.EmptyTags;

            KeyValuePair<string, object?>[] result;

            // When the input is a collection, we can allocate a correctly sized array and copy directly into it.
            if (tags is ICollection<KeyValuePair<string, object?>> collection)
            {
                int items = collection.Count;

                if (items == 0)
                    return Instrument.EmptyTags;

                result = new KeyValuePair<string, object?>[items];
                collection.CopyTo(result, 0);
                return result;
            }

            // In any other case, we must enumerate the input.
            // We use a pooled array as a buffer to avoid allocating until we know the final size we need.
            // This assumes that there are 32 or fewer tags, which is a reasonable assumption for most cases.
            // In the worst case, we will grow the buffer by renting a larger array.
            KeyValuePair<string, object?>[] array = ArrayPool<KeyValuePair<string, object?>>.Shared.Rent(32);
            int count = 0;
            int length = array.Length;

            foreach (KeyValuePair<string, object?> item in tags)
            {
                if (count == length)
                    Grow(ref array, ref length);

                array[count++] = item;
            }

            if (count == 0)
            {
                ArrayPool<KeyValuePair<string, object?>>.Shared.Return(array);
                return Instrument.EmptyTags;
            }

            result = new KeyValuePair<string, object?>[count];
            array.AsSpan().Slice(0, count).CopyTo(result.AsSpan());

            // Note that we don't include the return of the array inside a finally block per the established guidelines for ArrayPool.
            // We don't expect the above to throw an exception, but if it does it would be infrequent and the GC will collect the array.
            ArrayPool<KeyValuePair<string, object?>>.Shared.Return(array);
            return result;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void Grow(ref KeyValuePair<string, object?>[] array, ref int length)
            {
                KeyValuePair<string, object?>[] newArray = ArrayPool<KeyValuePair<string, object?>>.Shared.Rent(length * 2);
                array.CopyTo(newArray, 0);
                ArrayPool<KeyValuePair<string, object?>>.Shared.Return(array);
                array = newArray;
                length = array.Length;
            }
        }
    }
}
