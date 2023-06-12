// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers
{
    /// <summary>
    /// Represents a linked list of <see cref="ReadOnlyMemory{T}"/> nodes.
    /// </summary>
    public class ReadOnlyMemorySegment<T> : ReadOnlySequenceSegment<T>
    {
        /// <summary>
        /// Creates <see cref="ReadOnlyMemorySegment{T}"/> for <see cref="ReadOnlyMemory{T}"/> node.
        /// </summary>
        public ReadOnlyMemorySegment(ReadOnlyMemory<T> memory, ReadOnlySequenceSegment<T> next)
        {
            Memory = memory;
            Next = next;
            RunningIndex = next.RunningIndex - memory.Length;

            if (RunningIndex < 0)
                ArgumentOutOfRangeException.ThrowIfNegative(RunningIndex);
        }

        /// <summary>
        /// Creates <see cref="ReadOnlyMemorySegment{T}"/> for the last <see cref="ReadOnlyMemory{T}"/> node.
        /// </summary>
        public ReadOnlyMemorySegment(ReadOnlyMemory<T> memory, long runningIndex)
        {
            if (runningIndex < 0)
                ArgumentOutOfRangeException.ThrowIfNegative(runningIndex);

            Memory = memory;
            RunningIndex = runningIndex;
        }
    }
}
