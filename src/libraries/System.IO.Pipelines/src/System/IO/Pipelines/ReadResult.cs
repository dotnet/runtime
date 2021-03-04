// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.IO.Pipelines
{
    /// <summary>Represents the result of a <see cref="System.IO.Pipelines.PipeReader.ReadAsync(System.Threading.CancellationToken)" /> call.</summary>
    public readonly struct ReadResult
    {
        internal readonly ReadOnlySequence<byte> _resultBuffer;
        internal readonly ResultFlags _resultFlags;

        /// <summary>Creates a new instance of <see cref="System.IO.Pipelines.ReadResult" /> setting <see cref="System.IO.Pipelines.ReadResult.IsCanceled" /> and <see cref="System.IO.Pipelines.ReadResult.IsCompleted" /> flags.</summary>
        /// <param name="buffer">The read-only sequence containing the bytes of data that were read in the <see cref="System.IO.Pipelines.PipeReader.ReadAsync(System.Threading.CancellationToken)" /> call.</param>
        /// <param name="isCanceled">A flag that indicates if the <see cref="System.IO.Pipelines.PipeReader.ReadAsync(System.Threading.CancellationToken)" /> operation that produced this <see cref="System.IO.Pipelines.ReadResult" /> was canceled by <see cref="System.IO.Pipelines.PipeReader.CancelPendingRead" />.</param>
        /// <param name="isCompleted">A flag that indicates whether the end of the data stream has been reached.</param>
        public ReadResult(ReadOnlySequence<byte> buffer, bool isCanceled, bool isCompleted)
        {
            _resultBuffer = buffer;
            _resultFlags = ResultFlags.None;

            if (isCompleted)
            {
                _resultFlags |= ResultFlags.Completed;
            }
            if (isCanceled)
            {
                _resultFlags |= ResultFlags.Canceled;
            }
        }

        /// <summary>Gets the <see cref="System.Buffers.ReadOnlySequence{T}" /> that was read.</summary>
        /// <value>A read-only sequence containing the bytes of data that were read in the <see cref="System.IO.Pipelines.PipeReader.ReadAsync(System.Threading.CancellationToken)" /> call.</value>
        public ReadOnlySequence<byte> Buffer => _resultBuffer;

        /// <summary>Gets a value that indicates whether the current <see cref="System.IO.Pipelines.PipeReader.ReadAsync(System.Threading.CancellationToken)" /> operation was canceled by <see cref="System.IO.Pipelines.PipeReader.CancelPendingRead" />.</summary>
        /// <value><see langword="true" /> if the <see cref="System.IO.Pipelines.PipeReader.ReadAsync(System.Threading.CancellationToken)" /> operation that produced this <see cref="System.IO.Pipelines.ReadResult" /> was canceled by <see cref="System.IO.Pipelines.PipeReader.CancelPendingRead" />; otherwise, <see langword="false" />.</value>
        public bool IsCanceled => (_resultFlags & ResultFlags.Canceled) != 0;

        /// <summary>Gets a value that indicates whether the end of the data stream has been reached.</summary>
        /// <value><see langword="true" /> if the end of the data stream has been reached; otherwise, <see langword="false" />.</value>
        public bool IsCompleted => (_resultFlags & ResultFlags.Completed) != 0;
    }
}
