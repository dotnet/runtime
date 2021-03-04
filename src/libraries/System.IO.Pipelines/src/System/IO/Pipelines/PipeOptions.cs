// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Threading;

namespace System.IO.Pipelines
{
    /// <summary>Represents a set of <see cref="System.IO.Pipelines.Pipe" /> options.</summary>
    public class PipeOptions
    {
        private const int DefaultMinimumSegmentSize = 4096;

        private const int DefaultResumeWriterThreshold = DefaultMinimumSegmentSize * Pipe.InitialSegmentPoolSize / 2;

        private const int DefaultPauseWriterThreshold = DefaultMinimumSegmentSize * Pipe.InitialSegmentPoolSize;

        /// <summary>Gets the default instance of <see cref="System.IO.Pipelines.PipeOptions" />.</summary>
        /// <value>A <see cref="System.IO.Pipelines.PipeOptions" /> object initialized with default parameters.</value>
        public static PipeOptions Default { get; } = new PipeOptions();

        /// <summary>Initializes a new instance of the <see cref="System.IO.Pipelines.PipeOptions" /> class with the specified parameters.</summary>
        /// <param name="pool">The pool of memory blocks to be used for buffer management.</param>
        /// <param name="readerScheduler">The <see cref="System.IO.Pipelines.PipeScheduler" /> to be used to execute <see cref="System.IO.Pipelines.PipeReader" /> callbacks and async continuations.</param>
        /// <param name="writerScheduler">The <see cref="System.IO.Pipelines.PipeScheduler" /> used to execute <see cref="System.IO.Pipelines.PipeWriter" /> callbacks and async continuations.</param>
        /// <param name="pauseWriterThreshold">The number of bytes in the <see cref="System.IO.Pipelines.Pipe" /> before <see cref="System.IO.Pipelines.PipeWriter.FlushAsync(System.Threading.CancellationToken)" /> starts blocking. A value of zero prevents <see cref="System.IO.Pipelines.PipeWriter.FlushAsync(System.Threading.CancellationToken)" /> from ever blocking, effectively making the number of bytes in the <see cref="System.IO.Pipelines.Pipe" /> unlimited.</param>
        /// <param name="resumeWriterThreshold">The number of bytes in the <see cref="System.IO.Pipelines.Pipe" /> when <see cref="System.IO.Pipelines.PipeWriter.FlushAsync(System.Threading.CancellationToken)" /> stops blocking.</param>
        /// <param name="minimumSegmentSize">The minimum size of the segment requested from <paramref name="pool" />.</param>
        /// <param name="useSynchronizationContext"><see langword="true" /> if asynchronous continuations should be executed on the <see cref="System.Threading.SynchronizationContext" /> they were captured on; <see langword="false" /> otherwise. This takes precedence over the schedulers specified in <see cref="System.IO.Pipelines.PipeOptions.ReaderScheduler" /> and <see cref="System.IO.Pipelines.PipeOptions.WriterScheduler" />.</param>
        public PipeOptions(
            MemoryPool<byte>? pool = null,
            PipeScheduler? readerScheduler = null,
            PipeScheduler? writerScheduler = null,
            long pauseWriterThreshold = -1,
            long resumeWriterThreshold = -1,
            int minimumSegmentSize = -1,
            bool useSynchronizationContext = true)
        {
            if (pauseWriterThreshold == -1)
            {
                pauseWriterThreshold = DefaultPauseWriterThreshold;
            }
            else if (pauseWriterThreshold < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.pauseWriterThreshold);
            }

            if (resumeWriterThreshold == -1)
            {
                resumeWriterThreshold = DefaultResumeWriterThreshold;
            }
            else if (resumeWriterThreshold < 0 || resumeWriterThreshold > pauseWriterThreshold)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.resumeWriterThreshold);
            }

            Pool = pool ?? MemoryPool<byte>.Shared;
            ReaderScheduler = readerScheduler ?? PipeScheduler.ThreadPool;
            WriterScheduler = writerScheduler ?? PipeScheduler.ThreadPool;
            PauseWriterThreshold = pauseWriterThreshold;
            ResumeWriterThreshold = resumeWriterThreshold;
            MinimumSegmentSize = minimumSegmentSize == -1 ? DefaultMinimumSegmentSize : minimumSegmentSize;
            UseSynchronizationContext = useSynchronizationContext;
        }

        /// <summary>Gets a value that determines if asynchronous callbacks and continuations should be executed on the <see cref="System.Threading.SynchronizationContext" /> they were captured on. This takes precedence over the schedulers specified in <see cref="System.IO.Pipelines.PipeOptions.ReaderScheduler" /> and <see cref="System.IO.Pipelines.PipeOptions.WriterScheduler" />.</summary>
        /// <value><see langword="true" /> if asynchronous callbacks and continuations should be executed on the <see cref="System.Threading.SynchronizationContext" /> they were captured on; otherwise, <see langword="false" />.</value>
        public bool UseSynchronizationContext { get; }

        /// <summary>Gets the number of bytes in the <see cref="System.IO.Pipelines.Pipe" /> when <see cref="System.IO.Pipelines.PipeWriter.FlushAsync(System.Threading.CancellationToken)" /> starts blocking.</summary>
        /// <value>The number of bytes in the <see cref="System.IO.Pipelines.Pipe" /> when <see cref="System.IO.Pipelines.PipeWriter.FlushAsync(System.Threading.CancellationToken)" /> starts blocking.</value>
        public long PauseWriterThreshold { get; }

        /// <summary>Gets the number of bytes in the <see cref="System.IO.Pipelines.Pipe" /> when <see cref="System.IO.Pipelines.PipeWriter.FlushAsync(System.Threading.CancellationToken)" /> stops blocking.</summary>
        /// <value>The number of bytes in the <see cref="System.IO.Pipelines.Pipe" /> when <see cref="System.IO.Pipelines.PipeWriter.FlushAsync(System.Threading.CancellationToken)" /> stops blocking.</value>
        public long ResumeWriterThreshold { get; }

        /// <summary>Gets the minimum size of the segment requested from the <see cref="System.IO.Pipelines.PipeOptions.Pool" />.</summary>
        /// <value>The minimum size of the segment requested from the <see cref="System.IO.Pipelines.PipeOptions.Pool" />.</value>
        public int MinimumSegmentSize { get; }

        /// <summary>Gets the <see cref="System.IO.Pipelines.PipeScheduler" /> used to execute <see cref="System.IO.Pipelines.PipeWriter" /> callbacks and async continuations.</summary>
        /// <value>A <see cref="System.IO.Pipelines.PipeScheduler" /> object used to execute <see cref="System.IO.Pipelines.PipeWriter" /> callbacks and async continuations.</value>
        public PipeScheduler WriterScheduler { get; }

        /// <summary>Gets the <see cref="System.IO.Pipelines.PipeScheduler" /> used to execute <see cref="System.IO.Pipelines.PipeReader" /> callbacks and async continuations.</summary>
        /// <value>A <see cref="System.IO.Pipelines.PipeScheduler" /> that is used to execute <see cref="System.IO.Pipelines.PipeReader" /> callbacks and async continuations.</value>
        public PipeScheduler ReaderScheduler { get; }

        /// <summary>Gets the <see cref="System.Buffers.MemoryPool{T}" /> object used for buffer management.</summary>
        /// <value>A pool of memory blocks used for buffer management.</value>
        public MemoryPool<byte> Pool { get; }
    }
}
