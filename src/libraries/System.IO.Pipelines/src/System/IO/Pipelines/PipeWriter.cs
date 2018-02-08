// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Threading;

namespace System.IO.Pipelines
{
    /// <summary>
    /// Defines a class that provides a pipeline to which data can be written.
    /// </summary>
    public abstract class PipeWriter : IBufferWriter
    {
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
        public abstract PipeAwaiter<FlushResult> FlushAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Makes bytes written available to <see cref="PipeReader"/> without running <see cref="PipeReader.ReadAsync"/> continuation.
        /// </summary>
        public abstract void Commit();

        /// <inheritdoc />
        public abstract void Advance(int bytes);

        /// <inheritdoc />
        public abstract Memory<byte> GetMemory(int minimumLength = 0);

        /// <inheritdoc />
        public abstract Span<byte> GetSpan(int minimumLength = 0);

        /// <summary>
        /// Writes <paramref name="source"/> to the pipe and makes data accessible to <see cref="PipeReader"/>
        /// </summary>
        public virtual PipeAwaiter<FlushResult> WriteAsync(ReadOnlyMemory<byte> source)
        {
            Span<byte> destination = GetSpan();
            var sourceSpan = source.Span;
            // Fast path, try copying to the available memory directly
            if (sourceSpan.Length <= destination.Length)
            {
                sourceSpan.CopyTo(destination);
                Advance(sourceSpan.Length);
                return FlushAsync();
            }

            var remaining = sourceSpan.Length;
            var offset = 0;

            while (remaining > 0)
            {
                var writable = Math.Min(remaining, destination.Length);

                destination = GetSpan(writable);

                if (writable == 0)
                {
                    continue;
                }

                sourceSpan.Slice(offset, writable).CopyTo(destination);

                remaining -= writable;
                offset += writable;

                Advance(writable);
            }

            return FlushAsync();
        }
    }
}
