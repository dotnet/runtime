// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;

namespace System.IO.Pipelines
{
    /// <summary>
    /// Represents a set of <see cref="Pipe"/> options
    /// </summary>
    public class PipeOptions
    {
        /// <summary>
        /// Default instance of <see cref="PipeOptions"/>
        /// </summary>
        public static PipeOptions Default { get; } = new PipeOptions();

        /// <summary>
        /// Creates a new instance of <see cref="PipeOptions"/>
        /// </summary>
        public PipeOptions(
            MemoryPool<byte> pool = null,
            PipeScheduler readerScheduler = null,
            PipeScheduler writerScheduler = null,
            long pauseWriterThreshold = 0,
            long resumeWriterThreshold = 0,
            int minimumSegmentSize = 2048)
        {
            Pool = pool ?? MemoryPool<byte>.Shared;
            ReaderScheduler = readerScheduler;
            WriterScheduler = writerScheduler;
            PauseWriterThreshold = pauseWriterThreshold;
            ResumeWriterThreshold = resumeWriterThreshold;
            MinimumSegmentSize = minimumSegmentSize;
        }

        /// <summary>
        /// Gets amount of bytes in <see cref="Pipe"/> when <see cref="PipeWriter.FlushAsync"/> starts blocking
        /// </summary>
        public long PauseWriterThreshold { get; }

        /// <summary>
        /// Gets amount of bytes in <see cref="Pipe"/> when <see cref="PipeWriter.FlushAsync"/> stops blocking
        /// </summary>
        public long ResumeWriterThreshold { get; }

        /// <summary>
        /// Gets minimum size of segment requested from <see cref="Pool"/>
        /// </summary>
        public int MinimumSegmentSize { get; }

        /// <summary>
        /// Gets the <see cref="PipeScheduler"/> used to execute <see cref="PipeWriter"/> callbacks
        /// </summary>
        public PipeScheduler WriterScheduler { get; }

        /// <summary>
        /// Gets the <see cref="PipeScheduler"/> used to execute <see cref="PipeReader"/> callbacks
        /// </summary>
        public PipeScheduler ReaderScheduler { get; }

        /// <summary>
        /// Gets the <see cref="MemoryPool{Byte}"/> instances used for buffer management
        /// </summary>
        public MemoryPool<byte> Pool { get; }
    }
}
