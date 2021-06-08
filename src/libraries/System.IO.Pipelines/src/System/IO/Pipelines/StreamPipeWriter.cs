// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines
{
    internal sealed class StreamPipeWriter : PipeWriter
    {
        internal const int InitialSegmentPoolSize = 4; // 16K
        internal const int MaxSegmentPoolSize = 256; // 1MB

        private readonly int _minimumBufferSize;

        private BufferSegment? _head;
        private BufferSegment? _tail;
        private Memory<byte> _tailMemory;
        private int _tailBytesBuffered;
        private int _bytesBuffered;

        private readonly MemoryPool<byte>? _pool;
        private readonly int _maxPooledBufferSize;

        private CancellationTokenSource? _internalTokenSource;
        private bool _isCompleted;
        private readonly object _lockObject = new object();

        private BufferSegmentStack _bufferSegmentPool;
        private readonly bool _leaveOpen;

        private CancellationTokenSource InternalTokenSource
        {
            get
            {
                lock (_lockObject)
                {
                    if (_internalTokenSource == null)
                    {
                        _internalTokenSource = new CancellationTokenSource();
                    }
                    return _internalTokenSource;
                }
            }
        }

        public StreamPipeWriter(Stream writingStream, StreamPipeWriterOptions options)
        {
            InnerStream = writingStream ?? throw new ArgumentNullException(nameof(writingStream));

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _minimumBufferSize = options.MinimumBufferSize;
            _pool = options.Pool == MemoryPool<byte>.Shared ? null : options.Pool;
            _maxPooledBufferSize = _pool?.MaxBufferSize ?? -1;
            _bufferSegmentPool = new BufferSegmentStack(InitialSegmentPoolSize);
            _leaveOpen = options.LeaveOpen;
        }

        /// <summary>
        /// Gets the inner stream that is being written to.
        /// </summary>
        public Stream InnerStream { get; }

        /// <inheritdoc />
        public override void Advance(int bytes)
        {
            if ((uint)bytes > (uint)_tailMemory.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.bytes);
            }

            _tailBytesBuffered += bytes;
            _bytesBuffered += bytes;
            _tailMemory = _tailMemory.Slice(bytes);
        }

        /// <inheritdoc />
        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            if (_isCompleted)
            {
                ThrowHelper.ThrowInvalidOperationException_NoWritingAllowed();
            }

            if (sizeHint < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.sizeHint);
            }

            AllocateMemory(sizeHint);

            return _tailMemory;
        }

        /// <inheritdoc />
        public override Span<byte> GetSpan(int sizeHint = 0)
        {
            if (_isCompleted)
            {
                ThrowHelper.ThrowInvalidOperationException_NoWritingAllowed();
            }

            if (sizeHint < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.sizeHint);
            }

            AllocateMemory(sizeHint);

            return _tailMemory.Span;
        }

        private void AllocateMemory(int sizeHint)
        {
            if (_head == null)
            {
                // We need to allocate memory to write since nobody has written before
                BufferSegment newSegment = AllocateSegment(sizeHint);

                // Set all the pointers
                _head = _tail = newSegment;
                _tailBytesBuffered = 0;
            }
            else
            {
                Debug.Assert(_tail != null);
                int bytesLeftInBuffer = _tailMemory.Length;

                if (bytesLeftInBuffer == 0 || bytesLeftInBuffer < sizeHint)
                {
                    if (_tailBytesBuffered > 0)
                    {
                        // Flush buffered data to the segment
                        _tail.End += _tailBytesBuffered;
                        _tailBytesBuffered = 0;
                    }

                    BufferSegment newSegment = AllocateSegment(sizeHint);

                    _tail.SetNext(newSegment);
                    _tail = newSegment;
                }
            }
        }

        private BufferSegment AllocateSegment(int sizeHint)
        {
            Debug.Assert(sizeHint >= 0);
            BufferSegment newSegment = CreateSegmentUnsynchronized();

            int maxSize = _maxPooledBufferSize;
            if (sizeHint <= maxSize)
            {
                // Use the specified pool as it fits. Specified pool is not null as maxSize == -1 if _pool is null.
                newSegment.SetOwnedMemory(_pool!.Rent(GetSegmentSize(sizeHint, maxSize)));
            }
            else
            {
                // Use the array pool
                int sizeToRequest = GetSegmentSize(sizeHint);
                newSegment.SetOwnedMemory(ArrayPool<byte>.Shared.Rent(sizeToRequest));
            }

            _tailMemory = newSegment.AvailableMemory;

            return newSegment;
        }

        private int GetSegmentSize(int sizeHint, int maxBufferSize = int.MaxValue)
        {
            // First we need to handle case where hint is smaller than minimum segment size
            sizeHint = Math.Max(_minimumBufferSize, sizeHint);
            // After that adjust it to fit into pools max buffer size
            var adjustedToMaximumSize = Math.Min(maxBufferSize, sizeHint);
            return adjustedToMaximumSize;
        }

        private BufferSegment CreateSegmentUnsynchronized()
        {
            if (_bufferSegmentPool.TryPop(out BufferSegment? segment))
            {
                return segment;
            }

            return new BufferSegment();
        }

        private void ReturnSegmentUnsynchronized(BufferSegment segment)
        {
            if (_bufferSegmentPool.Count < MaxSegmentPoolSize)
            {
                _bufferSegmentPool.Push(segment);
            }
        }

        /// <inheritdoc />
        public override void CancelPendingFlush()
        {
            Cancel();
        }

        /// <inheritdoc />
        public override void Complete(Exception? exception = null)
        {
            if (_isCompleted)
            {
                return;
            }

            _isCompleted = true;

            FlushInternal(writeToStream: exception == null);

            _internalTokenSource?.Dispose();

            if (!_leaveOpen)
            {
                InnerStream.Dispose();
            }
        }

        public override async ValueTask CompleteAsync(Exception? exception = null)
        {
            if (_isCompleted)
            {
                return;
            }

            _isCompleted = true;

            await FlushAsyncInternal(writeToStream: exception == null, data: Memory<byte>.Empty).ConfigureAwait(false);

            _internalTokenSource?.Dispose();

            if (!_leaveOpen)
            {
#if (!NETSTANDARD2_0 && !NETFRAMEWORK)
                await InnerStream.DisposeAsync().ConfigureAwait(false);
#else
                InnerStream.Dispose();
#endif
            }
        }

        /// <inheritdoc />
        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_bytesBuffered == 0)
            {
                return new ValueTask<FlushResult>(new FlushResult(isCanceled: false, isCompleted: false));
            }

            return FlushAsyncInternal(writeToStream: true, data: Memory<byte>.Empty, cancellationToken);
        }

        public override ValueTask<FlushResult> WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            return FlushAsyncInternal(writeToStream: true, data: source, cancellationToken);
        }

        private void Cancel()
        {
            InternalTokenSource.Cancel();
        }

        private async ValueTask<FlushResult> FlushAsyncInternal(bool writeToStream, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            // Write all completed segments and whatever remains in the current segment
            // and flush the result.
            CancellationTokenRegistration reg = default;
            if (cancellationToken.CanBeCanceled)
            {
                reg = cancellationToken.UnsafeRegister(state => ((StreamPipeWriter)state!).Cancel(), this);
            }

            if (_tailBytesBuffered > 0)
            {
                Debug.Assert(_tail != null);

                // Update any buffered data
                _tail.End += _tailBytesBuffered;
                _tailBytesBuffered = 0;
            }

            using (reg)
            {
                CancellationToken localToken = InternalTokenSource.Token;
                try
                {
                    BufferSegment? segment = _head;
                    while (segment != null)
                    {
                        BufferSegment returnSegment = segment;
                        segment = segment.NextSegment;

                        if (returnSegment.Length > 0 && writeToStream)
                        {
                            await InnerStream.WriteAsync(returnSegment.Memory, localToken).ConfigureAwait(false);
                        }

                        returnSegment.ResetMemory();
                        ReturnSegmentUnsynchronized(returnSegment);

                        // Update the head segment after we return the current segment
                        _head = segment;
                    }

                    if (writeToStream)
                    {
                        // Write data after the buffered data
                        if (data.Length > 0)
                        {
                            await InnerStream.WriteAsync(data, localToken).ConfigureAwait(false);
                        }

                        if (_bytesBuffered > 0 || data.Length > 0)
                        {
                            await InnerStream.FlushAsync(localToken).ConfigureAwait(false);
                        }
                    }

                    // Mark bytes as written *after* flushing
                    _head = null;
                    _tail = null;
                    _tailMemory = default;
                    _bytesBuffered = 0;

                    return new FlushResult(isCanceled: false, isCompleted: false);
                }
                catch (OperationCanceledException)
                {
                    // Remove the cancellation token such that the next time Flush is called
                    // A new CTS is created.
                    lock (_lockObject)
                    {
                        _internalTokenSource = null;
                    }

                    if (localToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                    {
                        // Catch cancellation and translate it into setting isCanceled = true
                        return new FlushResult(isCanceled: true, isCompleted: false);
                    }

                    throw;
                }
            }
        }

        private void FlushInternal(bool writeToStream)
        {
            // Write all completed segments and whatever remains in the current segment
            // and flush the result.
            if (_tailBytesBuffered > 0)
            {
                Debug.Assert(_tail != null);

                // Update any buffered data
                _tail.End += _tailBytesBuffered;
                _tailBytesBuffered = 0;
            }

            BufferSegment? segment = _head;
            while (segment != null)
            {
                BufferSegment returnSegment = segment;
                segment = segment.NextSegment;

                if (returnSegment.Length > 0 && writeToStream)
                {
#if (!NETSTANDARD2_0 && !NETFRAMEWORK)
                    InnerStream.Write(returnSegment.Memory.Span);
#else
                    InnerStream.Write(returnSegment.Memory);
#endif
                }

                returnSegment.ResetMemory();
                ReturnSegmentUnsynchronized(returnSegment);

                // Update the head segment after we return the current segment
                _head = segment;
            }

            if (_bytesBuffered > 0 && writeToStream)
            {
                InnerStream.Flush();
            }

            // Mark bytes as written *after* flushing
            _head = null;
            _tail = null;
            _tailMemory = default;
            _bytesBuffered = 0;
        }
    }
}
