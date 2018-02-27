// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;

namespace System.Memory.Tests
{
    internal class BufferSegment<T> : IMemoryList<T>
    {
        public BufferSegment(Memory<T> memory)
        {
            Memory = memory;
        }

        /// <summary>
        /// Combined length of all segments before this
        /// </summary>
        public long RunningIndex { get; private set; }

        public Memory<T> Memory { get; set; }

        public IMemoryList<T> Next { get; private set; }

        public BufferSegment<T> Append(Memory<T> memory)
        {
            var segment = new BufferSegment<T>(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };
            Next = segment;
            return segment;
        }
    }
}
