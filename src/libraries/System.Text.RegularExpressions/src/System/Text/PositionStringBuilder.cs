// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text
{
    /// <summary>Provides a value type string builder composed of individual segments represented as <see cref="ReadOnlyMemory{T}"/> instances.</summary>
    [DebuggerDisplay("Count = {_count}")]
    internal struct OffsetCountStringBuilder
    {
        /// <summary>The array backing the builder, obtained from <see cref="ArrayPool{T}.Shared"/>.</summary>
        private (int Offset, int Count)[] _array;
        /// <summary>The number of items in <see cref="_array"/>, and thus also the next position in the array to be filled.</summary>
        private int _count;

        /// <summary>Creates a new builder.</summary>
        /// <remarks>Should be used instead of default struct initialization.</remarks>
        public static OffsetCountStringBuilder Create() => new OffsetCountStringBuilder() { _array = Array.Empty<(int, int)>() };

        /// <summary>Gets the number of segments added to the builder.</summary>
        public int Count => _count;

        /// <summary>Adds a segment to the builder.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int offset, int count)
        {
            (int, int)[] array = _array;
            int pos = _count;
            if ((uint)pos < (uint)array.Length)
            {
                array[pos] = (offset, count);
                _count = pos + 1;
            }
            else
            {
                GrowAndAdd(offset, count);
            }
        }

        /// <summary>Grows the builder to accommodate another segment.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void GrowAndAdd(int offset, int count)
        {
            (int Offset, int Count)[] array = _array;
            Debug.Assert(array.Length == _count);

            const int DefaultArraySize = 256;
            int newSize = array.Length == 0 ? DefaultArraySize : array.Length * 2;

            (int, int)[] newArray = _array = ArrayPool<(int, int)>.Shared.Rent(newSize);
            Array.Copy(array, newArray, _count);
            ArrayPool<(int, int)>.Shared.Return(array);
            newArray[_count++] = (offset, count);
        }

        /// <summary>Creates a string from all the segments in the builder and then disposes of the builder.</summary>
        /// <param name="originalString">The string to slice with the offset and count in each segment.</param>
        /// <param name="replacement">The string to use in place of any negative offset segments.</param>
        public unsafe string ToString(string originalString, string replacement)
        {
            (int Offset, int Count)[] array = _array;
            var span = new Span<(int Offset, int Count)>(array, 0, _count);

            int length = 0;
            for (int i = 0; i < span.Length; i++)
            {
                length += span[i].Offset >= 0 ?
                    span[i].Count :
                    replacement.Length;
            }

#pragma warning disable CS8500 // takes address of managed type
            ReadOnlySpan<(int, int)> tmpSpan = span; // avoid address exposing the span and impacting the other code in the method that uses it
            string result = string.Create(length, ((IntPtr)(&tmpSpan), originalString, replacement), static (dest, state) =>
            {
                Span<(int Offset, int Count)> span = *(Span<(int, int)>*)state.Item1;
                for (int i = 0; i < span.Length; i++)
                {
                    (int offset, int count) = span[i];
                    if (offset >= 0)
                    {
                        state.originalString.AsSpan(offset, count).CopyTo(dest);
                        dest = dest.Slice(count);
                    }
                    else
                    {
                        state.replacement.CopyTo(dest);
                        dest = dest.Slice(state.replacement.Length);
                    }
                }
            });
#pragma warning restore CS8500

            this = default;
            ArrayPool<(int, int)>.Shared.Return(array);

            return result;
        }
    }
}
