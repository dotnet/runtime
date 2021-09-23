// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines
{
    /// <summary>Defines a class that provides a pipeline to which data can be written.</summary>
    public abstract partial class PipeWriter : IBufferWriter<byte>
    {
        private PipeWriterStream? _stream;

        /// <summary>Marks the <see cref="System.IO.Pipelines.PipeWriter" /> as being complete, meaning no more items will be written to it.</summary>
        /// <param name="exception">Optional <see cref="System.Exception" /> indicating a failure that's causing the pipeline to complete.</param>
        public abstract void Complete(Exception? exception = null);

        /// <summary>Marks the current pipe writer instance as being complete, meaning no more data will be written to it.</summary>
        /// <param name="exception">An optional exception that indicates the failure that caused the pipeline to complete.</param>
        /// <returns>A value task that represents the asynchronous complete operation.</returns>
        public virtual ValueTask CompleteAsync(Exception? exception = null)
        {
            try
            {
                Complete(exception);
                return default;
            }
            catch (Exception ex)
            {
                return new ValueTask(Task.FromException(ex));
            }
        }

        /// <summary>Cancels the pending <see cref="System.IO.Pipelines.PipeWriter.FlushAsync(System.Threading.CancellationToken)" /> or <see cref="System.IO.Pipelines.PipeWriter.WriteAsync(System.ReadOnlyMemory{byte},System.Threading.CancellationToken)" /> operation without causing the operation to throw and without completing the <see cref="System.IO.Pipelines.PipeWriter" />. If there is no pending operation, this cancels the next operation.</summary>
        /// <remarks>The canceled <see cref="System.IO.Pipelines.PipeWriter.FlushAsync(System.Threading.CancellationToken)" /> or <see cref="System.IO.Pipelines.PipeWriter.WriteAsync(System.ReadOnlyMemory{byte},System.Threading.CancellationToken)" /> operation returns a <see cref="System.IO.Pipelines.FlushResult" /> where <see cref="System.IO.Pipelines.FlushResult.IsCanceled" /> is <see langword="true" />.</remarks>
        public abstract void CancelPendingFlush();

        /// <summary>Gets a value that indicates whether the current <see cref="System.IO.Pipelines.PipeWriter" /> supports reporting the count of unflushed bytes.</summary>
        /// <value><see langword="true" />If a class derived from <see cref="System.IO.Pipelines.PipeWriter" /> does not support getting the unflushed bytes, calls to <see cref="System.IO.Pipelines.PipeWriter.UnflushedBytes" /> throw <see cref="System.NotImplementedException" />.</value>
        public virtual bool CanGetUnflushedBytes => false;

        /// <summary>Registers a callback that executes when the <see cref="System.IO.Pipelines.PipeReader" /> side of the pipe is completed.</summary>
        /// <param name="callback">The callback to register.</param>
        /// <param name="state">The state object to pass to <paramref name="callback" /> when it's invoked.</param>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// > [!IMPORTANT]
        /// > `OnReaderCompleted` may not be invoked on all implementations of <xref:System.IO.Pipelines.PipeWriter>. This method will be removed in a future release.
        /// ]]></format></remarks>
        [Obsolete("OnReaderCompleted has been deprecated and may not be invoked on all implementations of PipeWriter.")]
        public virtual void OnReaderCompleted(Action<Exception?, object?> callback, object? state)
        {

        }

        /// <summary>Makes bytes written available to <see cref="System.IO.Pipelines.PipeReader" /> and runs <see cref="System.IO.Pipelines.PipeReader.ReadAsync(System.Threading.CancellationToken)" /> continuation.</summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents and wraps the asynchronous flush operation.</returns>
        public abstract ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default);

        /// <summary>Notifies the <see cref="System.IO.Pipelines.PipeWriter" /> that <paramref name="bytes" /> bytes were written to the output <see cref="System.Span{T}" /> or <see cref="System.Memory{T}" />. You must request a new buffer after calling <see cref="System.IO.Pipelines.PipeWriter.Advance(int)" /> to continue writing more data; you cannot write to a previously acquired buffer.</summary>
        /// <param name="bytes">The number of bytes written to the <see cref="System.Span{T}" /> or <see cref="System.Memory{T}" />.</param>
        public abstract void Advance(int bytes);

        /// <summary>Returns a <see cref="System.Memory{T}" /> to write to that is at least the requested size, as specified by the <paramref name="sizeHint" /> parameter.</summary>
        /// <param name="sizeHint">The minimum length of the returned <see cref="System.Memory{T}" />. If 0, a non-empty memory buffer of arbitrary size is returned.</param>
        /// <returns>A memory buffer of at least <paramref name="sizeHint" /> bytes. If <paramref name="sizeHint" /> is 0, returns a non-empty buffer of arbitrary size.</returns>
        /// <remarks>There is no guarantee that successive calls will return the same buffer or the same-sized buffer.
        /// This method never returns <see cref="System.Memory{T}.Empty" />, but it throws an <see cref="System.OutOfMemoryException" /> if the requested buffer size is not available.
        /// You must request a new buffer after calling <see cref="System.IO.Pipelines.PipeWriter.Advance" /> to continue writing more data; you cannot write to a previously acquired buffer.</remarks>
        /// <exception cref="System.OutOfMemoryException">The requested buffer size is not available.</exception>
        public abstract Memory<byte> GetMemory(int sizeHint = 0);

        /// <summary>Returns a <see cref="System.Span{T}" /> to write to that is at least the requested size, as specified by the <paramref name="sizeHint" /> parameter.</summary>
        /// <param name="sizeHint">The minimum length of the returned <see cref="System.Span{T}" />. If 0, a non-empty buffer of arbitrary size is returned.</param>
        /// <returns>A buffer of at least <paramref name="sizeHint" /> bytes. If <paramref name="sizeHint" /> is 0, returns a non-empty buffer of arbitrary size.</returns>
        /// <remarks>There is no guarantee that successive calls will return the same buffer or the same-sized buffer.
        /// This method never returns <see cref="System.Span{T}.Empty" />, but it throws an <see cref="System.OutOfMemoryException" /> if the requested buffer size is not available.
        /// You must request a new buffer after calling <see cref="System.IO.Pipelines.PipeWriter.Advance(int)" /> to continue writing more data; you cannot write to a previously acquired buffer.</remarks>
        /// <exception cref="System.OutOfMemoryException">The requested buffer size is not available.</exception>
        public abstract Span<byte> GetSpan(int sizeHint = 0);

        /// <summary>Returns a <see cref="System.IO.Stream" /> representation of the <see cref="System.IO.Pipelines.PipeWriter" />.</summary>
        /// <param name="leaveOpen">An optional flag that indicates whether disposing the returned <see cref="System.IO.Stream" /> leaves <see cref="System.IO.Pipelines.PipeReader" /> open (<see langword="true" />) or completes <see cref="System.IO.Pipelines.PipeReader" /> (<see langword="false" />).</param>
        /// <returns>A stream that represents the <see cref="System.IO.Pipelines.PipeWriter" />.</returns>
        public virtual Stream AsStream(bool leaveOpen = false)
        {
            if (_stream == null)
            {
                _stream = new PipeWriterStream(this, leaveOpen);
            }
            else if (leaveOpen)
            {
                _stream.LeaveOpen = leaveOpen;
            }

            return _stream;
        }

        /// <summary>Creates a <see cref="System.IO.Pipelines.PipeWriter" /> wrapping the specified <see cref="System.IO.Stream" />.</summary>
        /// <param name="stream">The stream that the pipe writer will wrap.</param>
        /// <param name="writerOptions">The options to configure the pipe writer.</param>
        /// <returns>A <see cref="System.IO.Pipelines.PipeWriter" /> that wraps the <see cref="System.IO.Stream" />.</returns>
        public static PipeWriter Create(Stream stream, StreamPipeWriterOptions? writerOptions = null)
        {
            return new StreamPipeWriter(stream, writerOptions ?? StreamPipeWriterOptions.s_default);
        }

        /// <summary>Writes the specified byte memory range to the pipe and makes data accessible to the <see cref="System.IO.Pipelines.PipeReader" />.</summary>
        /// <param name="source">The read-only byte memory region to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous write operation, and wraps the flush asynchronous operation.</returns>
        public virtual ValueTask<FlushResult> WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            this.Write(source.Span);
            return FlushAsync(cancellationToken);
        }

        /// <summary>Asynchronously reads the bytes from the specified stream and writes them to the <see cref="System.IO.Pipelines.PipeWriter" />.</summary>
        /// <param name="source">The stream from which the contents will be copied.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
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

        /// <summary>
        /// When overridden in a derived class, gets the count of unflushed bytes within the current writer.
        /// </summary>
        /// <exception cref="System.NotImplementedException">The <see cref="System.IO.Pipelines.PipeWriter"/> does not support getting the unflushed byte count.</exception>
        public virtual long UnflushedBytes => throw ThrowHelper.CreateNotSupportedException_UnflushedBytes();
    }
}
