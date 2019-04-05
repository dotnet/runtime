// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace System.IO.Pipelines
{
    /// <summary>
    /// Represents a set of options for controlling the creation of the <see cref="PipeReader"/>.
    /// </summary>
    public class StreamPipeReaderOptions
    {
        private const int DefaultBufferSize = 4096;
        private const int DefaultMinimumReadSize = 1024;

        internal static readonly StreamPipeReaderOptions s_default = new StreamPipeReaderOptions();

        /// <summary>
        /// Creates a new instance of <see cref="StreamPipeReaderOptions"/>.
        /// </summary>
        public StreamPipeReaderOptions(MemoryPool<byte> pool = null, int bufferSize = DefaultBufferSize, int minimumReadSize = DefaultMinimumReadSize)
        {
            Pool = pool ?? MemoryPool<byte>.Shared;

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            BufferSize = bufferSize;

            if (minimumReadSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumReadSize));
            }

            MinimumReadSize = minimumReadSize;
        }

        /// <summary>
        /// The minimum buffer size to use when renting memory from the <see cref="Pool"/>.
        /// </summary>
        public int BufferSize { get; }

        /// <summary>
        /// The threshold of remaining bytes in the buffer before a new buffer is allocated.
        /// </summary>
        public int MinimumReadSize { get; }

        /// <summary>
        /// The <see cref="MemoryPool{T}"/> to use when allocating memory.
        /// </summary>
        public MemoryPool<byte> Pool { get; }
    }
}
