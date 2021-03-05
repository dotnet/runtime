// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.IO.Pipelines
{
    /// <summary>Represents a set of options for controlling the creation of the <see cref="System.IO.Pipelines.PipeReader" />.</summary>
    public class StreamPipeReaderOptions
    {
        private const int DefaultBufferSize = 4096;
        private const int DefaultMinimumReadSize = 1024;

        internal static readonly StreamPipeReaderOptions s_default = new StreamPipeReaderOptions();

        /// <summary>Initializes a <see cref="System.IO.Pipelines.StreamPipeReaderOptions" /> instance, optionally specifying a memory pool, a minimum buffer size, a minimum read size, and whether the underlying stream should be left open after the <see cref="System.IO.Pipelines.PipeReader" /> completes.</summary>
        /// <param name="pool">The memory pool to use when allocating memory. The default value is <see langword="null" />.</param>
        /// <param name="bufferSize">The minimum buffer size to use when renting memory from the <paramref name="pool" />. The default value is 4096.</param>
        /// <param name="minimumReadSize">The threshold of remaining bytes in the buffer before a new buffer is allocated. The default value is 1024.</param>
        /// <param name="leaveOpen"><see langword="true" /> to leave the underlying stream open after the <see cref="System.IO.Pipelines.PipeReader" /> completes; <see langword="false" /> to close it. The default is <see langword="false" />.</param>
        public StreamPipeReaderOptions(MemoryPool<byte>? pool = null, int bufferSize = -1, int minimumReadSize = -1, bool leaveOpen = false)
        {
            Pool = pool ?? MemoryPool<byte>.Shared;

            BufferSize =
                bufferSize == -1 ? DefaultBufferSize :
                bufferSize <= 0 ? throw new ArgumentOutOfRangeException(nameof(bufferSize)) :
                bufferSize;

            MinimumReadSize =
                minimumReadSize == -1 ? DefaultMinimumReadSize :
                minimumReadSize <= 0 ? throw new ArgumentOutOfRangeException(nameof(minimumReadSize)) :
                minimumReadSize;

            LeaveOpen = leaveOpen;
        }

        /// <summary>Gets the minimum buffer size to use when renting memory from the <see cref="System.IO.Pipelines.StreamPipeReaderOptions.Pool" />.</summary>
        /// <value>The buffer size.</value>
        public int BufferSize { get; }

        /// <summary>Gets the threshold of remaining bytes in the buffer before a new buffer is allocated.</summary>
        /// <value>The minimum read size.</value>
        public int MinimumReadSize { get; }

        /// <summary>Gets the <see cref="System.Buffers.MemoryPool{T}" /> to use when allocating memory.</summary>
        /// <value>A memory pool instance.</value>
        public MemoryPool<byte> Pool { get; }

        /// <summary>Gets the value that indicates if the underlying stream should be left open after the <see cref="System.IO.Pipelines.PipeReader" /> completes.</summary>
        /// <value><see langword="true" /> if the underlying stream should be left open after the <see cref="System.IO.Pipelines.PipeReader" /> completes; otherwise, <see langword="false" />.</value>
        public bool LeaveOpen { get; }
    }
}
