// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines
{
    /// <summary>
    /// Defines a class that provides access to a read side of pipe.
    /// </summary>
    public abstract class PipeReader
    {
        /// <summary>
        /// Attempt to synchronously read data the <see cref="PipeReader"/>.
        /// </summary>
        /// <param name="result">The <see cref="ReadResult"/></param>
        /// <returns>True if data was available, or if the call was canceled or the writer was completed.</returns>
        /// <remarks>If the pipe returns false, there's no need to call <see cref="AdvanceTo(SequencePosition, SequencePosition)"/>.</remarks>
        public abstract bool TryRead(out ReadResult result);

        /// <summary>
        /// Asynchronously reads a sequence of bytes from the current <see cref="PipeReader"/>.
        /// </summary>
        /// <returns>A <see cref="ValueTask{T}"/> representing the asynchronous read operation.</returns>
        public abstract ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Moves forward the pipeline's read cursor to after the consumed data.
        /// </summary>
        /// <param name="consumed">Marks the extent of the data that has been successfully processed.</param>
        /// <remarks>
        /// The memory for the consumed data will be released and no longer available.
        /// The examined data communicates to the pipeline when it should signal more data is available.
        /// </remarks>
        public abstract void AdvanceTo(SequencePosition consumed);

        /// <summary>
        /// Moves forward the pipeline's read cursor to after the consumed data.
        /// </summary>
        /// <param name="consumed">Marks the extent of the data that has been successfully processed.</param>
        /// <param name="examined">Marks the extent of the data that has been read and examined.</param>
        /// <remarks>
        /// The memory for the consumed data will be released and no longer available.
        /// The examined data communicates to the pipeline when it should signal more data is available.
        /// </remarks>
        public abstract void AdvanceTo(SequencePosition consumed, SequencePosition examined);

        /// <summary>
        /// Cancel to currently pending or if none is pending next call to <see cref="ReadAsync"/>, without completing the <see cref="PipeReader"/>.
        /// </summary>
        public abstract void CancelPendingRead();

        /// <summary>
        /// Signal to the producer that the consumer is done reading.
        /// </summary>
        /// <param name="exception">Optional <see cref="Exception"/> indicating a failure that's causing the pipeline to complete.</param>
        public abstract void Complete(Exception exception = null);

        /// <summary>
        /// Cancel the pending <see cref="ReadAsync"/> operation. If there is none, cancels next <see cref="ReadAsync"/> operation, without completing the <see cref="PipeWriter"/>.
        /// </summary>
        public abstract void OnWriterCompleted(Action<Exception, object> callback, object state);
    }
}
