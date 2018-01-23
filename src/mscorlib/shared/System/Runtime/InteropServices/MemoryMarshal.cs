// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Provides a collection of methods for interoperating with <see cref="Memory{T}"/>, <see cref="ReadOnlyMemory{T}"/>,
    /// <see cref="Span{T}"/>, and <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public static partial class MemoryMarshal
    {
        /// <summary>
        /// Get an array segment from the underlying memory.
        /// If unable to get the array segment, return false with a default array segment.
        /// </summary>
        public static bool TryGetArray<T>(ReadOnlyMemory<T> readOnlyMemory, out ArraySegment<T> arraySegment)
        {
            object obj = readOnlyMemory.GetObjectStartLength(out int index, out int length);
            if (index < 0)
            {
                if (((OwnedMemory<T>)obj).TryGetArray(out var segment))
                {
                    arraySegment = new ArraySegment<T>(segment.Array, segment.Offset + (index & ReadOnlyMemory<T>.RemoveOwnedFlagBitMask), length);
                    return true;
                }
            }
            else if (obj is T[] arr)
            {
                arraySegment = new ArraySegment<T>(arr, index, length);
                return true;
            }

            arraySegment = default;
            return false;
        }

        /// <summary>
        /// Creates an <see cref="IEnumerable{T}"/> view of the given <paramref name="memory" /> to allow
        /// the <paramref name="memory" /> to be used in existing APIs that take an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The element type of the <paramref name="memory" />.</typeparam>
        /// <param name="memory">The ReadOnlyMemory to view as an <see cref="IEnumerable{T}"/></param>
        /// <returns>An <see cref="IEnumerable{T}"/> view of the given <paramref name="memory" /></returns>
        public static IEnumerable<T> ToEnumerable<T>(ReadOnlyMemory<T> memory)
        {
            for (int i = 0; i < memory.Length; i++)
                yield return memory.Span[i];
        }
    }
}
