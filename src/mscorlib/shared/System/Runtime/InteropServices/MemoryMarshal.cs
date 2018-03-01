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

            if (length == 0)
            {
#if FEATURE_PORTABLE_SPAN
                arraySegment = new ArraySegment<T>(SpanHelpers.PerTypeValues<T>.EmptyArray);
#else
                arraySegment = ArraySegment<T>.Empty;
#endif // FEATURE_PORTABLE_SPAN
                return true;
            }

            arraySegment = default;
            return false;
        }

        /// <summary>
        /// Gets an <see cref="OwnedMemory{T}"/> from the underlying readOnlyMemory.
        /// If unable to get the <typeparamref name="TOwner"/> type, returns false.
        /// </summary>
        /// <typeparam name="T">The element type of the <paramref name="readOnlyMemory" />.</typeparam>
        /// <typeparam name="TOwner">The type of <see cref="OwnedMemory{T}"/> to try and retrive.</typeparam>
        /// <param name="readOnlyMemory">The memory to get the owner for.</param>
        /// <param name="ownedMemory">The returned owner of the <see cref="ReadOnlyMemory{T}"/>.</param>
        /// <returns>A <see cref="bool"/> indicating if it was successful.</returns>
        public static bool TryGetOwnedMemory<T, TOwner>(ReadOnlyMemory<T> readOnlyMemory, out TOwner ownedMemory)
            where TOwner : OwnedMemory<T>
        {
            TOwner owner; // Use register for null comparison rather than byref
            ownedMemory = owner = readOnlyMemory.GetObjectStartLength(out int index, out int length) as TOwner;
            return !ReferenceEquals(owner, null);
        }

        /// <summary>
        /// Gets an <see cref="OwnedMemory{T}"/> and <paramref name="index" />, <paramref name="length" /> from the underlying memory.
        /// If unable to get the <typeparamref name="TOwner"/> type, returns false.
        /// </summary>
        /// <typeparam name="T">The element type of the <paramref name="readOnlyMemory" />.</typeparam>
        /// <typeparam name="TOwner">The type of <see cref="OwnedMemory{T}"/> to try and retrive.</typeparam>
        /// <param name="readOnlyMemory">The memory to get the owner for.</param>
        /// <param name="ownedMemory">The returned owner of the <see cref="ReadOnlyMemory{T}"/>.</param>
        /// <param name="index">The offset from the start of the <paramref name="ownedMemory" /> that the <paramref name="readOnlyMemory" /> represents.</param>
        /// <param name="length">The length of the <paramref name="ownedMemory" /> that the <paramref name="readOnlyMemory" /> represents.</param>
        /// <returns>A <see cref="bool"/> indicating if it was successful.</returns>
        public static bool TryGetOwnedMemory<T, TOwner>(ReadOnlyMemory<T> readOnlyMemory, out TOwner ownedMemory, out int index, out int length)
           where TOwner : OwnedMemory<T>
        {
            TOwner owner; // Use register for null comparison rather than byref
            ownedMemory = owner = readOnlyMemory.GetObjectStartLength(out index, out length) as TOwner;
            index &= ReadOnlyMemory<T>.RemoveOwnedFlagBitMask;
            return !ReferenceEquals(owner, null);
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

        /// <summary>Attempts to get the underlying <see cref="string"/> from a <see cref="ReadOnlyMemory{T}"/>.</summary>
        /// <param name="readOnlyMemory">The memory that may be wrapping a <see cref="string"/> object.</param>
        /// <param name="text">The string.</param>
        /// <param name="start">The starting location in <paramref name="text"/>.</param>
        /// <param name="length">The number of items in <paramref name="text"/>.</param>
        /// <returns></returns>
        public static bool TryGetString(ReadOnlyMemory<char> readOnlyMemory, out string text, out int start, out int length)
        {
            if (readOnlyMemory.GetObjectStartLength(out int offset, out int count) is string s)
            {
                text = s;
                start = offset;
                length = count;
                return true;
            }
            else
            {
                text = null;
                start = 0;
                length = 0;
                return false;
            }
        }
    }
}
