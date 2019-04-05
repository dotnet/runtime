// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace System.IO.Pipelines
{
    public class StreamPipeReaderOptions
    {
        private const int DefaultBufferSize = 4096;
        private const int DefaultMinimumReadSize = 1024;

        internal static readonly StreamPipeReaderOptions s_default = new StreamPipeReaderOptions();

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

        public int BufferSize { get; }
        public int MinimumReadSize { get; }
        public MemoryPool<byte> Pool { get; }
    }
}
