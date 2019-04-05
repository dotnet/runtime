// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines
{
    /// <summary>
    /// Defines a class that provides a pipeline to which data can be written.
    /// </summary>
    public abstract partial class PipeWriter : IBufferWriter<byte>
    {
        private PipeWriterStream _stream;

        /// <summary>
        /// Marks the <see cref="PipeWriter"/> as being complete, meaning no more items will be written to it.
        /// </summary>
        /// <param name="exception">Optional <see cref="Exception"/> indicating a failure that's causing the pipeline to complete.</param>
        public abstract void Complete(Exception exception = null);

        /// <summary>
        /// Cancel the pending <see cref="FlushAsync"/> operation. If there is none, cancels next <see cref="FlushAsync"/> operation, without completing the <see cref="PipeWriter"/>.
        /// </summary>
        public abstract void CancelPendingFlush();

        /// <summary>
        /// Registers a callback that gets executed when the <see cref="PipeReader"/> side of the pipe is completed
        /// </summary>
        public abstract void OnReaderCompleted(Action<Exception, object> callback, object state);

        /// <summary>
        /// Makes bytes written available to <see cref="PipeReader"/> and runs <see cref="PipeReader.ReadAsync"/> continuation.
        /// </summary>
        public abstract ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default);

        /// <inheritdoc />
        public abstract void Advance(int bytes);

        /// <inheritdoc />
        public abstract Memory<byte> GetMemory(int sizeHint = 0);

        /// <inheritdoc />
        public abstract Span<byte> GetSpan(int sizeHint = 0);

        /// <summary>
        /// Returns a <see cref="Stream"/> that wraps the <see cref="PipeWriter"/>.
        /// </summary>
        /// <returns>The <see cref="Stream"/>.</returns>
        public virtual Stream AsStream()
        {
            return _stream ?? (_stream = new PipeWriterStream(this));
        }

        /// <summary>
        /// Creates a <see cref="PipeWriter"/> wrapping the specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="writerOptions">The options.</param>
        /// <returns>A <see cref="PipeWriter"/> that wraps the <see cref="Stream"/>.</returns>
        public static PipeWriter Create(Stream stream, StreamPipeWriterOptions writerOptions = null)
        {
            return new StreamPipeWriter(stream, writerOptions ?? StreamPipeWriterOptions.s_default);
        }

        /// <summary>
        /// Writes <paramref name="source"/> to the pipe and makes data accessible to <see cref="PipeReader"/>
        /// </summary>
        public virtual ValueTask<FlushResult> WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            this.Write(source.Span);
            return FlushAsync(cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the bytes from the specified stream and writes them to the <see cref="PipeWriter"/>.
        /// </summary>
        /// <param name="source">The stream from which the contents will be copied.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        protected internal virtual async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                Memory<byte> buffer = GetMemory();
                int read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

                if (read == 0)
                {
                    break;
                }

                Advance(read);

                FlushResult result = await FlushAsync(cancellationToken).ConfigureAwait(false);

                if (result.IsCanceled)
                {
                    ThrowHelper.ThrowOperationCanceledException_FlushCanceled();
                }

                if (result.IsCompleted)
                {
                    break;
                }
            }
        }
    }
}
