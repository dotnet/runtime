// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers
{
    /// <summary>
    /// Represents a linked list of <see cref="Memory{T}"/> nodes.
    /// </summary>
    public interface IMemoryList<T>
    {
        /// <summary>
        /// The <see cref="Memory{T}"/> value for current node.
        /// </summary>
        Memory<T> Memory { get; }

        /// <summary>
        /// The next node.
        /// </summary>
        IMemoryList<T> Next { get; }

        /// <summary>
        /// The sum of node length before current.
        /// </summary>
        long RunningIndex { get; }
    }
}
