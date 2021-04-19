// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines
{
    /// <summary>Defines a class that provides access to a read side of pipe.</summary>
    public abstract partial class PipeReader
    {
        private PipeReaderStream? _stream;

        /// <summary>Attempts to synchronously read data from the <see cref="System.IO.Pipelines.PipeReader" />.</summary>
        /// <param name="result">When this method returns <see langword="true" />, this value is set to a <see cref="System.IO.Pipelines.ReadResult" /> instance that represents the result of the read call; otherwise, this value is set to <see langword="default" />.</param>
        /// <returns><see langword="true" /> if data was available, or if the call was canceled or the writer was completed; otherwise, <see langword="false" />.</returns>
        /// <remarks>If the pipe returns <see langword="false" />, there is no need to call <see cref="System.IO.Pipelines.PipeReader.AdvanceTo(System.SequencePosition,System.SequencePosition)" />.</remarks>
        public abstract bool TryRead(out ReadResult result);

        /// <summary>Asynchronously reads a sequence of bytes from the current <see cref="System.IO.Pipelines.PipeReader" />.</summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see langword="default" />.</param>
        /// <returns>A <see cref="System.Threading.Tasks.ValueTask{T}" /> representing the asynchronous read operation.</returns>
        public abstract ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default);

        /// <summary>Moves forward the pipeline's read cursor to after the consumed data, marking the data as processed.</summary>
        /// <param name="consumed">Marks the extent of the data that has been successfully processed.</param>
        /// <remarks>The memory for the consumed data will be released and no longer available.
        /// The <see cref="System.IO.Pipelines.ReadResult.Buffer" /> previously returned from <see cref="System.IO.Pipelines.PipeReader.ReadAsync(System.Threading.CancellationToken)" /> must not be accessed after this call.
        /// This is equivalent to calling <see cref="System.IO.Pipelines.PipeReader.AdvanceTo(System.SequencePosition,System.SequencePosition)" /> with identical examined and consumed positions.
        /// The examined data communicates to the pipeline when it should signal more data is available.
        /// Because the consumed parameter doubles as the examined parameter, the consumed parameter should be greater than or equal to the examined position in the previous call to `AdvanceTo`. Otherwise, an <see cref="System.InvalidOperationException" /> is thrown.</remarks>
        public abstract void AdvanceTo(SequencePosition consumed);

        /// <summary>Moves forward the pipeline's read cursor to after the consumed data, marking the data as processed, read and examined.</summary>
        /// <param name="consumed">Marks the extent of the data that has been successfully processed.</param>
        /// <param name="examined">Marks the extent of the data that has been read and examined.</param>
        /// <remarks>The memory for the consumed data will be released and no longer available.
        /// The <see cref="System.IO.Pipelines.ReadResult.Buffer" /> previously returned from <see cref="System.IO.Pipelines.PipeReader.ReadAsync(System.Threading.CancellationToken)" /> must not be accessed after this call.
        /// The examined data communicates to the pipeline when it should signal more data is available.
        /// The examined parameter should be greater than or equal to the examined position in the previous call to `AdvanceTo`. Otherwise, an <see cref="System.InvalidOperationException" /> is thrown.</remarks>
        public abstract void AdvanceTo(SequencePosition consumed, SequencePosition examined);

        /// <summary>Returns a <see cref="System.IO.Stream" /> representation of the <see cref="System.IO.Pipelines.PipeReader" />.</summary>
        /// <param name="leaveOpen">An optional flag that indicates whether disposing the returned <see cref="System.IO.Stream" /> leaves <see cref="System.IO.Pipelines.PipeReader" /> open (<see langword="true" />) or completes <see cref="System.IO.Pipelines.PipeReader" /> (<see langword="false" />).</param>
        /// <returns>A stream that represents the <see cref="System.IO.Pipelines.PipeReader" />.</returns>
        public virtual Stream AsStream(bool leaveOpen = false)
        {
            if (_stream == null)
            {
                _stream = new PipeReaderStream(this, leaveOpen);
            }
            else if (leaveOpen)
            {
                _stream.LeaveOpen = leaveOpen;
            }

            return _stream;
        }

        /// <summary>Cancels the pending <see cref="System.IO.Pipelines.PipeReader.ReadAsync(System.Threading.CancellationToken)" /> operation without causing it to throw and without completing the <see cref="System.IO.Pipelines.PipeReader" />. If there is no pending operation, this cancels the next operation.</summary>
        /// <remarks>The canceled <see cref="System.IO.Pipelines.PipeReader.ReadAsync(System.Threading.CancellationToken)" /> operation returns a <see cref="System.IO.Pipelines.ReadResult" /> where <see cref="System.IO.Pipelines.ReadResult.IsCanceled" /> is <see langword="true" />.</remarks>
        public abstract void CancelPendingRead();

        /// <summary>Signals to the producer that the consumer is done reading.</summary>
        /// <param name="exception">Optional <see cref="System.Exception" /> indicating a failure that's causing the pipeline to complete.</param>
        public abstract void Complete(Exception? exception = null);

        /// <summary>Marks the current pipe reader instance as being complete, meaning no more data will be read from it.</summary>
        /// <param name="exception">An optional exception that indicates the failure that caused the reader to complete.</param>
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

        /// <summary>Registers a callback that executes when the <see cref="System.IO.Pipelines.PipeWriter" /> side of the pipe is completed.</summary>
        /// <param name="callback">The callback to register.</param>
        /// <param name="state">The state object to pass to <paramref name="callback" /> when it's invoked.</param>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// > [!IMPORTANT]
        /// > `OnWriterCompleted` may not be invoked on all implementations of <xref:System.IO.Pipelines.PipeWriter>. This method will be removed in a future release.
        /// ]]></format></remarks>
        [Obsolete("OnWriterCompleted may not be invoked on all implementations of PipeReader. This will be removed in a future release.")]
        public virtual void OnWriterCompleted(Action<Exception?, object?> callback, object? state)
        {

        }

        /// <summary>Creates a <see cref="System.IO.Pipelines.PipeReader" /> wrapping the specified <see cref="System.IO.Stream" />.</summary>
        /// <param name="stream">The stream that the pipe reader will wrap.</param>
        /// <param name="readerOptions">The options to configure the pipe reader.</param>
        /// <returns>A <see cref="System.IO.Pipelines.PipeReader" /> that wraps the <see cref="System.IO.Stream" />.</returns>
        public static PipeReader Create(Stream stream, StreamPipeReaderOptions? readerOptions = null)
        {
            return new StreamPipeReader(stream, readerOptions ?? StreamPipeReaderOptions.s_default);
        }

        /// <summary>
        /// Creates a <see cref="PipeReader"/> wrapping the specified <see cref="ReadOnlySequence{T}"/>.
        /// </summary>
        /// <param name="sequence">The sequence.</param>
        /// <returns>A <see cref="PipeReader"/> that wraps the <see cref="ReadOnlySequence{T}"/>.</returns>
        public static PipeReader Create(ReadOnlySequence<byte> sequence)
        {
            return new SequencePipeReader(sequence);
        }

        /// <summary>Asynchronously reads the bytes from the <see cref="System.IO.Pipelines.PipeReader" /> and writes them to the specified <see cref="System.IO.Pipelines.PipeWriter" />, using a specified buffer size and cancellation token.</summary>
        /// <param name="destination">The pipe writer to which the contents of the current stream will be copied.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        public virtual Task CopyToAsync(PipeWriter destination, CancellationToken cancellationToken = default)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            return CopyToAsyncCore(
                destination,
                (destination, memory, cancellationToken) => destination.WriteAsync(memory, cancellationToken),
                cancellationToken);
        }

        /// <summary>Asynchronously reads the bytes from the <see cref="System.IO.Pipelines.PipeReader" /> and writes them to the specified stream, using a specified cancellation token.</summary>
        /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        public virtual Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            return CopyToAsyncCore(destination, (destination, memory, cancellationToken) =>
            {
                ValueTask task = destination.WriteAsync(memory, cancellationToken);

                if (task.IsCompletedSuccessfully)
                {
                    task.GetAwaiter().GetResult();
                    return new ValueTask<FlushResult>(new FlushResult(isCanceled: false, isCompleted: false));
                }

                static async ValueTask<FlushResult> Awaited(ValueTask writeTask)
                {
                    await writeTask.ConfigureAwait(false);
                    return new FlushResult(isCanceled: false, isCompleted: false);
                }

                return Awaited(task);
            },
            cancellationToken);
        }

        private async Task CopyToAsyncCore<TStream>(TStream destination, Func<TStream, ReadOnlyMemory<byte>, CancellationToken, ValueTask<FlushResult>> writeAsync, CancellationToken cancellationToken)
        {
            while (true)
            {
                ReadResult result = await ReadAsync(cancellationToken).ConfigureAwait(false);
                ReadOnlySequence<byte> buffer = result.Buffer;
                SequencePosition position = buffer.Start;
                SequencePosition consumed = position;

                try
                {
                    if (result.IsCanceled)
                    {
                        ThrowHelper.ThrowOperationCanceledException_ReadCanceled();
                    }

                    while (buffer.TryGet(ref position, out ReadOnlyMemory<byte> memory))
                    {
                        FlushResult flushResult = await writeAsync(destination, memory, cancellationToken).ConfigureAwait(false);

                        if (flushResult.IsCanceled)
                        {
                            ThrowHelper.ThrowOperationCanceledException_FlushCanceled();
                        }

                        consumed = position;

                        if (flushResult.IsCompleted)
                        {
                            return;
                        }
                    }

                    // The while loop completed succesfully, so we've consumed the entire buffer.
                    consumed = buffer.End;

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
                finally
                {
                    // Advance even if WriteAsync throws so the PipeReader is not left in the
                    // currently reading state
                    AdvanceTo(consumed);
                }
            }
        }
    }
}
