// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;

namespace System.Memory.Tests
{
    internal class BufferSegment : IMemoryList<byte>
    {
        public BufferSegment(Memory<byte> memory)
        {
            Memory = memory;
        }

        /// <summary>
        /// Combined length of all segments before this
        /// </summary>
        public long RunningIndex { get; private set; }

        public Memory<byte> Memory { get; set; }

        public IMemoryList<byte> Next { get; private set; }

        public BufferSegment Append(Memory<byte> memory)
        {
            var segment = new BufferSegment(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };
            Next = segment;
            return segment;
        }
    }
}
