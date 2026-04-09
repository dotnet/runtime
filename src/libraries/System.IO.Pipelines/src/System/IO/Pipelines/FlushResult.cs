// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Pipelines
{
    /// <summary>Result returned by <see cref="System.IO.Pipelines.PipeWriter.FlushAsync(System.Threading.CancellationToken)" /> call.</summary>
    public struct FlushResult
    {
        internal ResultFlags _resultFlags;

        /// <summary>Initializes a new instance of <see cref="System.IO.Pipelines.FlushResult" /> struct setting the <see cref="System.IO.Pipelines.FlushResult.IsCanceled" /> and <see cref="System.IO.Pipelines.FlushResult.IsCompleted" /> flags.</summary>
        /// <param name="isCanceled"><see langword="true" /> to indicate the current <see cref="System.IO.Pipelines.PipeWriter.FlushAsync(System.Threading.CancellationToken)" /> operation that produced this <see cref="System.IO.Pipelines.FlushResult" /> was canceled by <see cref="System.IO.Pipelines.PipeWriter.CancelPendingFlush" />; otherwise, <see langword="false" />.</param>
        /// <param name="isCompleted"><see langword="true" /> to indicate the reader is no longer reading data written to the <see cref="System.IO.Pipelines.PipeWriter" />.</param>
        public FlushResult(bool isCanceled, bool isCompleted)
        {
            _resultFlags = ResultFlags.None;

            if (isCanceled)
            {
                _resultFlags |= ResultFlags.Canceled;
            }

            if (isCompleted)
            {
                _resultFlags |= ResultFlags.Completed;
            }
        }

        /// <summary>Gets a value that indicates whether the current <see cref="System.IO.Pipelines.PipeWriter.FlushAsync(System.Threading.CancellationToken)" /> operation was canceled by <see cref="System.IO.Pipelines.PipeWriter.CancelPendingFlush" />.</summary>
        /// <value><see langword="true" /> if the current <see cref="System.IO.Pipelines.PipeWriter.FlushAsync(System.Threading.CancellationToken)" /> operation was canceled by <see cref="System.IO.Pipelines.PipeWriter.CancelPendingFlush" />; otherwise, <see langword="false" />.</value>
        public bool IsCanceled => (_resultFlags & ResultFlags.Canceled) != 0;

        /// <summary>Gets a value that indicates the reader is no longer reading data written to the <see cref="System.IO.Pipelines.PipeWriter" />.</summary>
        /// <value><see langword="true" /> if the reader is no longer reading data written to the <see cref="System.IO.Pipelines.PipeWriter" />; otherwise, <see langword="false" />.</value>
        public bool IsCompleted => (_resultFlags & ResultFlags.Completed) != 0;
    }
}
