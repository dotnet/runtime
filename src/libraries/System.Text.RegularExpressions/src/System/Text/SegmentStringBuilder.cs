// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text
{
    /// <summary>Provides a value type string builder composed of individual segments represented as <see cref="ReadOnlyMemory{T}"/> instances.</summary>
    [DebuggerDisplay("Count = {_count}")]
    internal struct SegmentStringBuilder
    {
        /// <summary>The array backing the builder, obtained from <see cref="ArrayPool{T}.Shared"/>.</summary>
        private ReadOnlyMemory<char>[] _array;
        /// <summary>The number of items in <see cref="_array"/>, and thus also the next position in the array to be filled.</summary>
        private int _count;

        /// <summary>Creates a new builder.</summary>
        /// <remarks>Should be used instead of default struct initialization.</remarks>
        public static SegmentStringBuilder Create() => new SegmentStringBuilder() { _array = Array.Empty<ReadOnlyMemory<char>>() };

        /// <summary>Gets the number of segments added to the builder.</summary>
        public int Count => _count;

        /// <summary>Adds a segment to the builder.</summary>
        /// <param name="segment">The segment.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(ReadOnlyMemory<char> segment)
        {
            ReadOnlyMemory<char>[] array = _array;
            int pos = _count;
            if ((uint)pos < (uint)array.Length)
            {
                array[pos] = segment;
                _count = pos + 1;
            }
            else
            {
                GrowAndAdd(segment);
            }
        }

        /// <summary>Grows the builder to accomodate another segment.</summary>
        /// <param name="segment"></param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void GrowAndAdd(ReadOnlyMemory<char> segment)
        {
            ReadOnlyMemory<char>[] array = _array;
            Debug.Assert(array.Length == _count);

            const int DefaultArraySize = 256;
            int newSize = array.Length == 0 ? DefaultArraySize : array.Length * 2;

            ReadOnlyMemory<char>[] newArray = _array = ArrayPool<ReadOnlyMemory<char>>.Shared.Rent(newSize);
            Array.Copy(array, newArray, _count);
            ArrayPool<ReadOnlyMemory<char>>.Shared.Return(array, clearArray: true);
            newArray[_count++] = segment;
        }

        /// <summary>Gets a span of all segments in the builder.</summary>
        /// <returns></returns>
        public Span<ReadOnlyMemory<char>> AsSpan() => new Span<ReadOnlyMemory<char>>(_array, 0, _count);

        /// <summary>Creates a string from all the segments in the builder and then disposes of the builder.</summary>
        public override string ToString()
        {
            ReadOnlyMemory<char>[] array = _array;
            var span = new Span<ReadOnlyMemory<char>>(array, 0, _count);

            int length = 0;
            for (int i = 0; i < span.Length; i++)
            {
                length += span[i].Length;
            }

            string result = string.Create(length, this, static (dest, builder) =>
            {
                Span<ReadOnlyMemory<char>> localSpan = builder.AsSpan();
                for (int i = 0; i < localSpan.Length; i++)
                {
                    ReadOnlySpan<char> segment = localSpan[i].Span;
                    segment.CopyTo(dest);
                    dest = dest.Slice(segment.Length);
                }
            });

            span.Clear();
            this = default;
            ArrayPool<ReadOnlyMemory<char>>.Shared.Return(array);

            return result;
        }
    }
}
