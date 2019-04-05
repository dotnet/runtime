// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace System.IO.Pipelines
{
    public class StreamPipeWriterOptions
    {
        private const int DefaultMinimumBufferSize = 4096;

        internal static StreamPipeWriterOptions s_default = new StreamPipeWriterOptions();

        public StreamPipeWriterOptions(MemoryPool<byte> pool = null, int minimumBufferSize = DefaultMinimumBufferSize)
        {
            Pool = pool ?? MemoryPool<byte>.Shared;

            if (minimumBufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumBufferSize));
            }

            MinimumBufferSize = minimumBufferSize;
        }

        public int MinimumBufferSize { get; }
        public MemoryPool<byte> Pool { get; }
    }
}
