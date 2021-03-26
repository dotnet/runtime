// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines
{
    internal sealed class StreamPipeReader : PipeReader
    {
        internal const int InitialSegmentPoolSize = 4; // 16K
        internal const int MaxSegmentPoolSize = 256; // 1MB

        private CancellationTokenSource? _internalTokenSource;
        private bool _isReaderCompleted;
        private bool _isStreamCompleted;

        private BufferSegment? _readHead;
        private int _readIndex;

        private BufferSegment? _readTail;
        private long _bufferedBytes;
        private bool _examinedEverything;
        private readonly object _lock = new object();

        // Mutable struct! Don't make this readonly
        private BufferSegmentStack _bufferSegmentPool;

        private StreamPipeReaderOptions _options;

        /// <summary>
        /// Creates a new StreamPipeReader.
        /// </summary>
        /// <param name="readingStream">The stream to read from.</param>
        /// <param name="options">The options to use.</param>
        public StreamPipeReader(Stream readingStream, StreamPipeReaderOptions options)
        {
            InnerStream = readingStream ?? throw new ArgumentNullException(nameof(readingStream));

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options;
            _bufferSegmentPool = new BufferSegmentStack(InitialSegmentPoolSize);
        }

        // All derived from the options
        private bool LeaveOpen => _options.LeaveOpen;
        private bool UseZeroByteReads => _options.UseZeroByteReads;
        private int BufferSize => _options.BufferSize;
        private int MinimumReadThreshold => _options.MinimumReadSize;
        private MemoryPool<byte> Pool => _options.Pool;

        /// <summary>
        /// Gets the inner stream that is being read from.
        /// </summary>
        public Stream InnerStream { get; }

        /// <inheritdoc />
        public override void AdvanceTo(SequencePosition consumed)
        {
            AdvanceTo(consumed, consumed);
        }

        private CancellationTokenSource InternalTokenSource
        {
            get
            {
                lock (_lock)
                {
                    if (_internalTokenSource == null)
                    {
                        _internalTokenSource = new CancellationTokenSource();
                    }
                    return _internalTokenSource;
                }
            }
        }

        /// <inheritdoc />
        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            ThrowIfCompleted();

            AdvanceTo((BufferSegment?)consumed.GetObject(), consumed.GetInteger(), (BufferSegment?)examined.GetObject(), examined.GetInteger());
        }

        private void AdvanceTo(BufferSegment? consumedSegment, int consumedIndex, BufferSegment? examinedSegment, int examinedIndex)
        {
            if (consumedSegment == null || examinedSegment == null)
            {
                return;
            }

            if (_readHead == null)
            {
                ThrowHelper.ThrowInvalidOperationException_AdvanceToInvalidCursor();
            }

            BufferSegment returnStart = _readHead;
            BufferSegment? returnEnd = consumedSegment;

            long consumedBytes = BufferSegment.GetLength(returnStart, _readIndex, consumedSegment, consumedIndex);

            _bufferedBytes -= consumedBytes;

            Debug.Assert(_bufferedBytes >= 0);

            _examinedEverything = false;

            if (examinedSegment == _readTail)
            {
                // If we examined everything, we force ReadAsync to actually read from the underlying stream
                // instead of returning a ReadResult from TryRead.
                _examinedEverything = examinedIndex == _readTail.End;
            }

            // Two cases here:
            // 1. All data is consumed. If so, we empty clear everything so we don't hold onto any
            // excess memory.
            // 2. A segment is entirely consumed but there is still more data in nextSegments
            //  We are allowed to remove an extra segment. by setting returnEnd to be the next block.
            // 3. We are in the middle of a segment.
            //  Move _readHead and _readIndex to consumedSegment and index
            if (_bufferedBytes == 0)
            {
                returnEnd = null;
                _readHead = null;
                _readTail = null;
                _readIndex = 0;
            }
            else if (consumedIndex == returnEnd.Length)
            {
                BufferSegment? nextBlock = returnEnd.NextSegment;
                _readHead = nextBlock;
                _readIndex = 0;
                returnEnd = nextBlock;
            }
            else
            {
                _readHead = consumedSegment;
                _readIndex = consumedIndex;
            }

            // Remove all blocks that are freed (except the last one)
            while (returnStart != returnEnd)
            {
                BufferSegment next = returnStart.NextSegment!;
                returnStart.ResetMemory();
                ReturnSegmentUnsynchronized(returnStart);
                returnStart = next;
            }
        }

        /// <inheritdoc />
        public override void CancelPendingRead()
        {
            InternalTokenSource.Cancel();
        }

        /// <inheritdoc />
        public override void Complete(Exception? exception = null)
        {
            if (_isReaderCompleted)
            {
                return;
            }

            _isReaderCompleted = true;

            BufferSegment? segment = _readHead;
            while (segment != null)
            {
                BufferSegment returnSegment = segment;
                segment = segment.NextSegment;

                returnSegment.ResetMemory();
            }

            if (!LeaveOpen)
            {
                InnerStream.Dispose();
            }
        }

        /// <inheritdoc />
        public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            // TODO ReadyAsync needs to throw if there are overlapping reads.
            ThrowIfCompleted();

            // PERF: store InternalTokenSource locally to avoid querying it twice (which acquires a lock)
            CancellationTokenSource tokenSource = InternalTokenSource;
            if (TryReadInternal(tokenSource, out ReadResult readResult))
            {
                return readResult;
            }

            if (_isStreamCompleted)
            {
                return new ReadResult(buffer: default, isCanceled: false, isCompleted: true);
            }

            CancellationTokenRegistration reg = default;
            if (cancellationToken.CanBeCanceled)
            {
                reg = cancellationToken.UnsafeRegister(state => ((StreamPipeReader)state!).Cancel(), this);
            }

            using (reg)
            {
                var isCanceled = false;
                try
                {
                    // This optimization only makes sense if we don't have anything buffered
                    if (UseZeroByteReads && _bufferedBytes == 0)
                    {
                        // Wait for data by doing 0 byte read before
                        await InnerStream.ReadAsync(Memory<byte>.Empty, cancellationToken).ConfigureAwait(false);
                    }

                    AllocateReadTail();

                    Memory<byte> buffer = _readTail!.AvailableMemory.Slice(_readTail.End);

                    int length = await InnerStream.ReadAsync(buffer, tokenSource.Token).ConfigureAwait(false);

                    Debug.Assert(length + _readTail.End <= _readTail.AvailableMemory.Length);

                    _readTail.End += length;
                    _bufferedBytes += length;

                    if (length == 0)
                    {
                        _isStreamCompleted = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    ClearCancellationToken();

                    if (tokenSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                    {
                        // Catch cancellation and translate it into setting isCanceled = true
                        isCanceled = true;
                    }
                    else
                    {
                        throw;
                    }

                }

                return new ReadResult(GetCurrentReadOnlySequence(), isCanceled, _isStreamCompleted);
            }
        }

        private void ClearCancellationToken()
        {
            lock (_lock)
            {
                _internalTokenSource = null;
            }
        }

        private void ThrowIfCompleted()
        {
            if (_isReaderCompleted)
            {
                ThrowHelper.ThrowInvalidOperationException_NoReadingAllowed();
            }
        }

        public override bool TryRead(out ReadResult result)
        {
            ThrowIfCompleted();

            return TryReadInternal(InternalTokenSource, out result);
        }

        private bool TryReadInternal(CancellationTokenSource source, out ReadResult result)
        {
            bool isCancellationRequested = source.IsCancellationRequested;
            if (isCancellationRequested || _bufferedBytes > 0 && (!_examinedEverything || _isStreamCompleted))
            {
                if (isCancellationRequested)
                {
                    ClearCancellationToken();
                }

                ReadOnlySequence<byte> buffer = _readHead == null ? default : GetCurrentReadOnlySequence();

                result = new ReadResult(buffer, isCancellationRequested, _isStreamCompleted);
                return true;
            }

            result = default;
            return false;
        }

        private ReadOnlySequence<byte> GetCurrentReadOnlySequence()
        {
            Debug.Assert(_readHead != null && _readTail != null);
            return new ReadOnlySequence<byte>(_readHead, _readIndex, _readTail, _readTail.End);
        }

        private void AllocateReadTail()
        {
            if (_readHead == null)
            {
                Debug.Assert(_readTail == null);
                _readHead = AllocateSegment();
                _readTail = _readHead;
            }
            else
            {
                Debug.Assert(_readTail != null);
                if (_readTail.WritableBytes < MinimumReadThreshold)
                {
                    BufferSegment nextSegment = AllocateSegment();
                    _readTail.SetNext(nextSegment);
                    _readTail = nextSegment;
                }
            }
        }

        private BufferSegment AllocateSegment()
        {
            BufferSegment nextSegment = CreateSegmentUnsynchronized();

            if (_options.IsDefaultSharedMemoryPool)
            {
                nextSegment.SetOwnedMemory(ArrayPool<byte>.Shared.Rent(BufferSize));
            }
            else
            {
                nextSegment.SetOwnedMemory(Pool.Rent(BufferSize));
            }

            return nextSegment;
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
            Debug.Assert(segment != _readHead, "Returning _readHead segment that's in use!");
            Debug.Assert(segment != _readTail, "Returning _readTail segment that's in use!");

            if (_bufferSegmentPool.Count < MaxSegmentPoolSize)
            {
                _bufferSegmentPool.Push(segment);
            }
        }

        private void Cancel()
        {
            InternalTokenSource.Cancel();
        }
    }
}
