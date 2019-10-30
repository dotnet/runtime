// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers
{
    /// <summary>
    /// Represents a linked list of <see cref="ReadOnlyMemory{T}"/> nodes.
    /// </summary>
    public abstract class ReadOnlySequenceSegment<T>
    {
        /// <summary>
        /// The <see cref="ReadOnlyMemory{T}"/> value for current node.
        /// </summary>
        public ReadOnlyMemory<T> Memory { get; protected set; }

        /// <summary>
        /// The next node.
        /// </summary>
        public ReadOnlySequenceSegment<T>? Next { get; protected set; }

        /// <summary>
        /// The sum of node length before current.
        /// </summary>
        public long RunningIndex { get; protected set; }
    }
}
