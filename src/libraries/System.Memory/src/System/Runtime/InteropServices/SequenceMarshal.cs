// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Provides a collection of methods for interoperating with <see cref="ReadOnlySequence{T}"/>
    /// </summary>
    public static partial class SequenceMarshal
    {
        /// <summary>
        /// Get <see cref="IMemoryList{T}"/> from the underlying <see cref="ReadOnlySequence{T}"/>.
        /// If unable to get the <see cref="IMemoryList{T}"/>, return false.
        /// </summary>
        public static bool TryGetMemoryList<T>(ReadOnlySequence<T> sequence,
            out IMemoryList<T> startSegment,
            out int startIndex,
            out IMemoryList<T> endSegment,
            out int endIndex)
        {
            return sequence.TryGetMemoryList(out startSegment, out startIndex, out endSegment, out endIndex);
        }

        /// <summary>
        /// Get an array segment from the underlying <see cref="ReadOnlySequence{T}"/>.
        /// If unable to get the array segment, return false with a default array segment.
        /// </summary>
        public static bool TryGetArray<T>(ReadOnlySequence<T> sequence, out ArraySegment<T> arraySegment)
        {
            return sequence.TryGetArray(out arraySegment);
        }

        /// <summary>
        /// Get <see cref="OwnedMemory{T}"/> from the underlying <see cref="ReadOnlySequence{T}"/>.
        /// If unable to get the <see cref="OwnedMemory{T}"/>, return false.
        /// </summary>
        public static bool TryGetOwnedMemory<T>(ReadOnlySequence<T> sequence, out OwnedMemory<T> ownedMemory, out int start, out int length)
        {
            return sequence.TryGetOwnedMemory(out ownedMemory, out start, out length);
        }

        /// <summary>
        /// Get <see cref="ReadOnlyMemory{T}"/> from the underlying <see cref="ReadOnlySequence{T}"/>.
        /// If unable to get the <see cref="ReadOnlyMemory{T}"/>, return false.
        /// </summary>
        public static bool TryGetReadOnlyMemory<T>(ReadOnlySequence<T> sequence, out ReadOnlyMemory<T> readOnlyMemory)
        {
            return sequence.TryGetReadOnlyMemory(out readOnlyMemory);
        }
    }
}
