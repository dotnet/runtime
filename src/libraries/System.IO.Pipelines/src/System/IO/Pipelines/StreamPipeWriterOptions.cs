// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.IO.Pipelines
{
    /// <summary>Represents a set of options for controlling the creation of the <see cref="System.IO.Pipelines.PipeWriter" />.</summary>
    public class StreamPipeWriterOptions
    {
        private const int DefaultMinimumBufferSize = 4096;

        internal static StreamPipeWriterOptions s_default = new StreamPipeWriterOptions();

        /// <summary>Initializes a <see cref="System.IO.Pipelines.StreamPipeWriterOptions" /> instance, optionally specifying a memory pool, a minimum buffer size, and whether the underlying stream should be left open after the <see cref="System.IO.Pipelines.PipeWriter" /> completes.</summary>
        /// <param name="pool">The memory pool to use when allocating memory. The default value is <see langword="null" />.</param>
        /// <param name="minimumBufferSize">The minimum buffer size to use when renting memory from the <paramref name="pool" />. The default value is 4096.</param>
        /// <param name="leaveOpen"><see langword="true" /> to leave the underlying stream open after the <see cref="System.IO.Pipelines.PipeWriter" /> completes; <see langword="false" /> to close it. The default is <see langword="false" />.</param>
        public StreamPipeWriterOptions(MemoryPool<byte>? pool = null, int minimumBufferSize = -1, bool leaveOpen = false)
        {
            Pool = pool ?? MemoryPool<byte>.Shared;

            MinimumBufferSize =
                minimumBufferSize == -1 ? DefaultMinimumBufferSize :
                minimumBufferSize <= 0 ? throw new ArgumentOutOfRangeException(nameof(minimumBufferSize)) :
                minimumBufferSize;

            LeaveOpen = leaveOpen;
        }

        /// <summary>Gets the minimum buffer size to use when renting memory from the <see cref="System.IO.Pipelines.StreamPipeWriterOptions.Pool" />.</summary>
        /// <value>An integer representing the minimum buffer size.</value>
        public int MinimumBufferSize { get; }

        /// <summary>Gets the <see cref="System.Buffers.MemoryPool{T}" /> to use when allocating memory.</summary>
        /// <value>A memory pool instance.</value>
        public MemoryPool<byte> Pool { get; }

        /// <summary>Gets the value that indicates if the underlying stream should be left open after the <see cref="System.IO.Pipelines.PipeWriter" /> completes.</summary>
        /// <value><see langword="true" /> if the underlying stream should be left open after the <see cref="System.IO.Pipelines.PipeWriter" /> completes; otherwise, <see langword="false" />.</value>
        public bool LeaveOpen { get; }
    }
}
