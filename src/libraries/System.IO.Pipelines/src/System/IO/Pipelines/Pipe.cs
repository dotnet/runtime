// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.IO.Pipelines
{
    /// <summary>
    /// Default <see cref="PipeWriter"/> and <see cref="PipeReader"/> implementation.
    /// </summary>
    public sealed partial class Pipe
    {
        private const int SegmentPoolSize = 16;

        private static readonly Action<object> s_signalReaderAwaitable = state => ((Pipe)state).ReaderCancellationRequested();
        private static readonly Action<object> s_signalWriterAwaitable = state => ((Pipe)state).WriterCancellationRequested();
        private static readonly Action<object> s_invokeCompletionCallbacks = state => ((PipeCompletionCallbacks)state).Execute();

        // This sync objects protects the following state:
        // 1. _commitHead & _commitHeadIndex
        // 2. _length
        // 3. _readerAwaitable & _writerAwaitable
        private readonly object _sync = new object();

        private readonly MemoryPool<byte> _pool;
        private readonly int _minimumSegmentSize;
        private readonly long _pauseWriterThreshold;
        private readonly long _resumeWriterThreshold;

        private readonly PipeScheduler _readerScheduler;
        private readonly PipeScheduler _writerScheduler;

        private readonly BufferSegment[] _bufferSegmentPool;

        private readonly DefaultPipeReader _reader;
        private readonly DefaultPipeWriter _writer;

        private long _length;
        private long _currentWriteLength;

        private int _pooledSegmentCount;

        private PipeAwaitable _readerAwaitable;
        private PipeAwaitable _writerAwaitable;

        private PipeCompletion _writerCompletion;
        private PipeCompletion _readerCompletion;

        // The read head which is the extent of the IPipelineReader's consumed bytes
        private BufferSegment _readHead;
        private int _readHeadIndex;

        // The commit head which is the extent of the bytes available to the IPipelineReader to consume
        private BufferSegment _commitHead;
        private int _commitHeadIndex;

        // The write head which is the extent of the IPipelineWriter's written bytes
        private BufferSegment _writingHead;

        private PipeReaderState _readingState;

        private bool _disposed;

        internal long Length => _length;

        /// <summary>
        /// Initializes the <see cref="Pipe"/> using <see cref="PipeOptions.Default"/> as options.
        /// </summary>
        public Pipe() : this(PipeOptions.Default)
        {
        }

        /// <summary>
        /// Initializes the <see cref="Pipe"/> with the specified <see cref="PipeOptions"/>.
        /// </summary>
        public Pipe(PipeOptions options)
        {
            if (options == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.options);
            }

            _bufferSegmentPool = new BufferSegment[SegmentPoolSize];

            _readingState = default;
            _readerCompletion = default;
            _writerCompletion = default;

            _pool = options.Pool;
            _minimumSegmentSize = options.MinimumSegmentSize;
            _pauseWriterThreshold = options.PauseWriterThreshold;
            _resumeWriterThreshold = options.ResumeWriterThreshold;
            _readerScheduler = options.ReaderScheduler;
            _writerScheduler = options.WriterScheduler;
            _readerAwaitable = new PipeAwaitable(completed: false);
            _writerAwaitable = new PipeAwaitable(completed: true);
            _reader = new DefaultPipeReader(this);
            _writer = new DefaultPipeWriter(this);
        }

        private void ResetState()
        {
            _readerCompletion.Reset();
            _writerCompletion.Reset();
            _commitHeadIndex = 0;
            _currentWriteLength = 0;
            _length = 0;
        }

        internal Memory<byte> GetMemory(int minimumSize)
        {
            if (_writerCompletion.IsCompleted)
            {
                ThrowHelper.ThrowInvalidOperationException_NoWritingAllowed();
            }

            if (minimumSize < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.minimumSize);
            }

            lock (_sync)
            {
                BufferSegment segment = _writingHead ?? AllocateWriteHeadUnsynchronized(minimumSize);

                int bytesLeftInBuffer = segment.WritableBytes;

                // If inadequate bytes left or if the segment is readonly
                if (bytesLeftInBuffer == 0 || bytesLeftInBuffer < minimumSize || segment.ReadOnly)
                {
                    BufferSegment nextSegment = CreateSegmentUnsynchronized();

                    nextSegment.SetMemory(_pool.Rent(Math.Max(_minimumSegmentSize, minimumSize)));

                    segment.SetNext(nextSegment);

                    _writingHead = nextSegment;
                }
            }

            return _writingHead.AvailableMemory.Slice(_writingHead.End, _writingHead.WritableBytes);
        }

        private BufferSegment AllocateWriteHeadUnsynchronized(int count)
        {
            BufferSegment segment = null;

            if (_commitHead != null && !_commitHead.ReadOnly)
            {
                // Try to return the tail so the calling code can append to it
                int remaining = _commitHead.WritableBytes;

                if (count <= remaining && remaining > 0)
                {
                    // Free tail space of the right amount, use that
                    segment = _commitHead;
                }
            }

            if (segment == null)
            {
                // No free tail space, allocate a new segment
                segment = CreateSegmentUnsynchronized();
                segment.SetMemory(_pool.Rent(Math.Max(_minimumSegmentSize, count)));
            }

            if (_commitHead == null)
            {
                // No previous writes have occurred
                _commitHead = segment;
            }
            else if (segment != _commitHead && _commitHead.Next == null)
            {
                // Append the segment to the commit head if writes have been committed
                // and it isn't the same segment (unused tail space)
                _commitHead.SetNext(segment);
            }

            // Set write head to assigned segment
            _writingHead = segment;

            return segment;
        }

        private BufferSegment CreateSegmentUnsynchronized()
        {
            if (_pooledSegmentCount > 0)
            {
                _pooledSegmentCount--;
                return _bufferSegmentPool[_pooledSegmentCount];
            }

            return new BufferSegment();
        }

        private void ReturnSegmentUnsynchronized(BufferSegment segment)
        {
            if (_pooledSegmentCount < _bufferSegmentPool.Length)
            {
                _bufferSegmentPool[_pooledSegmentCount] = segment;
                _pooledSegmentCount++;
            }
        }

        internal void Commit()
        {
            // Changing commit head shared with Reader
            lock (_sync)
            {
                CommitUnsynchronized();
            }
        }

        internal void CommitUnsynchronized()
        {
            if (_writingHead == null)
            {
                // Nothing written to commit
                return;
            }

            if (_readHead == null)
            {
                // Update the head to point to the head of the buffer.
                // This happens if we called alloc(0) then write
                _readHead = _commitHead;
                _readHeadIndex = 0;
            }

            // Always move the commit head to the write head
            _commitHead = _writingHead;
            _commitHeadIndex = _writingHead.End;
            _length += _currentWriteLength;

            // Do not reset if reader is complete
            if (_pauseWriterThreshold > 0 &&
                _length >= _pauseWriterThreshold &&
                !_readerCompletion.IsCompleted)
            {
                _writerAwaitable.Reset();
            }

            // Clear the writing state
            _writingHead = null;
            _currentWriteLength = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Advance(int bytesWritten)
        {
            if (_writingHead == null)
            {
                ThrowHelper.ThrowInvalidOperationException_NotWritingNoAlloc();
            }

            if (bytesWritten > 0)
            {
                Debug.Assert(!_writingHead.ReadOnly);
                Debug.Assert(_writingHead.Next == null);

                Memory<byte> buffer = _writingHead.AvailableMemory;

                if (_writingHead.End > buffer.Length - bytesWritten)
                {
                    ThrowHelper.ThrowInvalidOperationException_AdvancingPastBufferSize();
                }

                _writingHead.End += bytesWritten;
                _currentWriteLength += bytesWritten;
            }
            else if (bytesWritten < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.bytesWritten);
            }

            // and if zero, just do nothing; don't need to validate tail etc
        }

        internal PipeAwaiter<FlushResult> FlushAsync(CancellationToken cancellationToken)
        {
            Action awaitable;
            CancellationTokenRegistration cancellationTokenRegistration;
            lock (_sync)
            {
                if (_writingHead != null)
                {
                    // Commit the data as not already committed
                    CommitUnsynchronized();
                }

                awaitable = _readerAwaitable.Complete();

                cancellationTokenRegistration = _writerAwaitable.AttachToken(cancellationToken, s_signalWriterAwaitable, this);
            }

            cancellationTokenRegistration.Dispose();

            TrySchedule(_readerScheduler, awaitable);

            return new PipeAwaiter<FlushResult>(_writer);
        }

        internal void CompleteWriter(Exception exception)
        {
            Action awaitable;
            PipeCompletionCallbacks completionCallbacks;
            bool readerCompleted;

            lock (_sync)
            {
                if (_currentWriteLength > 0)
                {
                    ThrowHelper.ThrowInvalidOperationException_CompleteWriterActiveWriter();
                }

                completionCallbacks = _writerCompletion.TryComplete(exception);
                awaitable = _readerAwaitable.Complete();
                readerCompleted = _readerCompletion.IsCompleted;
            }

            if (completionCallbacks != null)
            {
                TrySchedule(_readerScheduler, s_invokeCompletionCallbacks, completionCallbacks);
            }

            TrySchedule(_readerScheduler, awaitable);

            if (readerCompleted)
            {
                CompletePipe();
            }
        }

        internal void AdvanceReader(SequencePosition consumed)
        {
            AdvanceReader(consumed, consumed);
        }

        internal void AdvanceReader(SequencePosition consumed, SequencePosition examined)
        {
            BufferSegment returnStart = null;
            BufferSegment returnEnd = null;

            Action continuation = null;
            lock (_sync)
            {
                var examinedEverything = false;
                if (examined.Segment == _commitHead)
                {
                    examinedEverything = _commitHead != null ? examined.Index == _commitHeadIndex - _commitHead.Start : examined.Index == 0;
                }

                if (consumed.Segment != null)
                {
                    if (_readHead == null)
                    {
                        ThrowHelper.ThrowInvalidOperationException_AdvanceToInvalidCursor();
                        return;
                    }

                    var consumedSegment = (BufferSegment)consumed.Segment;

                    returnStart = _readHead;
                    returnEnd = consumedSegment;

                    // Check if we crossed _maximumSizeLow and complete backpressure
                    long consumedBytes = new ReadOnlySequence<byte>(returnStart, _readHeadIndex, consumedSegment, consumed.Index).Length;
                    long oldLength = _length;
                    _length -= consumedBytes;

                    if (oldLength >= _resumeWriterThreshold &&
                        _length < _resumeWriterThreshold)
                    {
                        continuation = _writerAwaitable.Complete();
                    }

                    // Check if we consumed entire last segment
                    // if we are going to return commit head we need to check that there is no writing operation that
                    // might be using tailspace
                    if (consumed.Index == returnEnd.Length && _writingHead != returnEnd)
                    {
                        BufferSegment nextBlock = returnEnd.NextSegment;
                        if (_commitHead == returnEnd)
                        {
                            _commitHead = nextBlock;
                            _commitHeadIndex = 0;
                        }

                        _readHead = nextBlock;
                        _readHeadIndex = 0;
                        returnEnd = nextBlock;
                    }
                    else
                    {
                        _readHead = consumedSegment;
                        _readHeadIndex = consumed.Index;
                    }
                }

                // We reset the awaitable to not completed if we've examined everything the producer produced so far
                // but only if writer is not completed yet
                if (examinedEverything && !_writerCompletion.IsCompleted)
                {
                    // Prevent deadlock where reader awaits new data and writer await backpressure
                    if (!_writerAwaitable.IsCompleted)
                    {
                        ThrowHelper.ThrowInvalidOperationException_BackpressureDeadlock();
                    }
                    _readerAwaitable.Reset();
                }

                while (returnStart != null && returnStart != returnEnd)
                {
                    returnStart.ResetMemory();
                    ReturnSegmentUnsynchronized(returnStart);
                    returnStart = returnStart.NextSegment;
                }

                _readingState.End();
            }

            TrySchedule(_writerScheduler, continuation);
        }

        internal void CompleteReader(Exception exception)
        {
            PipeCompletionCallbacks completionCallbacks;
            Action awaitable;
            bool writerCompleted;

            lock (_sync)
            {
                if (_readingState.IsActive)
                {
                    ThrowHelper.ThrowInvalidOperationException_CompleteReaderActiveReader();
                }

                completionCallbacks = _readerCompletion.TryComplete(exception);
                awaitable = _writerAwaitable.Complete();
                writerCompleted = _writerCompletion.IsCompleted;
            }

            if (completionCallbacks != null)
            {
                TrySchedule(_writerScheduler, s_invokeCompletionCallbacks, completionCallbacks);
            }

            TrySchedule(_writerScheduler, awaitable);

            if (writerCompleted)
            {
                CompletePipe();
            }
        }

        internal void OnWriterCompleted(Action<Exception, object> callback, object state)
        {
            if (callback == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callback);
            }

            PipeCompletionCallbacks completionCallbacks;
            lock (_sync)
            {
                completionCallbacks = _writerCompletion.AddCallback(callback, state);
            }

            if (completionCallbacks != null)
            {
                TrySchedule(_readerScheduler, s_invokeCompletionCallbacks, completionCallbacks);
            }
        }

        internal void CancelPendingRead()
        {
            Action awaitable;
            lock (_sync)
            {
                awaitable = _readerAwaitable.Cancel();
            }
            TrySchedule(_readerScheduler, awaitable);
        }

        internal void CancelPendingFlush()
        {
            Action awaitable;
            lock (_sync)
            {
                awaitable = _writerAwaitable.Cancel();
            }
            TrySchedule(_writerScheduler, awaitable);
        }

        internal void OnReaderCompleted(Action<Exception, object> callback, object state)
        {
            if (callback == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callback);
            }

            PipeCompletionCallbacks completionCallbacks;
            lock (_sync)
            {
                completionCallbacks = _readerCompletion.AddCallback(callback, state);
            }

            if (completionCallbacks != null)
            {
                TrySchedule(_writerScheduler, s_invokeCompletionCallbacks, completionCallbacks);
            }
        }

        internal PipeAwaiter<ReadResult> ReadAsync(CancellationToken token)
        {
            CancellationTokenRegistration cancellationTokenRegistration;
            if (_readerCompletion.IsCompleted)
            {
                ThrowHelper.ThrowInvalidOperationException_NoReadingAllowed();
            }
            lock (_sync)
            {
                cancellationTokenRegistration = _readerAwaitable.AttachToken(token, s_signalReaderAwaitable, this);
            }
            cancellationTokenRegistration.Dispose();
            return new PipeAwaiter<ReadResult>(_reader);
        }

        internal bool TryRead(out ReadResult result)
        {
            lock (_sync)
            {
                if (_readerCompletion.IsCompleted)
                {
                    ThrowHelper.ThrowInvalidOperationException_NoReadingAllowed();
                }

                result = new ReadResult();
                if (_length > 0 || _readerAwaitable.IsCompleted)
                {
                    GetResult(ref result);
                    return true;
                }

                if (_readerAwaitable.HasContinuation)
                {
                    ThrowHelper.ThrowInvalidOperationException_AlreadyReading();
                }
                return false;
            }
        }

        private static void TrySchedule(PipeScheduler scheduler, Action action)
        {
            if (action != null)
            {
                scheduler.Schedule(action);
            }
        }

        private static void TrySchedule(PipeScheduler scheduler, Action<object> action, object state)
        {
            if (action != null)
            {
                scheduler.Schedule(action, state);
            }
        }

        private void CompletePipe()
        {
            lock (_sync)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                // Return all segments
                // if _readHead is null we need to try return _commitHead
                // because there might be a block allocated for writing
                BufferSegment segment = _readHead ?? _commitHead;
                while (segment != null)
                {
                    BufferSegment returnSegment = segment;
                    segment = segment.NextSegment;

                    returnSegment.ResetMemory();
                }

                _writingHead = null;
                _readHead = null;
                _commitHead = null;
            }
        }

        internal bool IsReadAsyncCompleted => _readerAwaitable.IsCompleted;

        internal void OnReadAsyncCompleted(Action continuation)
        {
            Action awaitable;
            bool doubleCompletion;
            lock (_sync)
            {
                awaitable = _readerAwaitable.OnCompleted(continuation, out doubleCompletion);
            }
            if (doubleCompletion)
            {
                Writer.Complete(ThrowHelper.CreateInvalidOperationException_NoConcurrentOperation());
            }
            TrySchedule(_readerScheduler, awaitable);
        }

        internal ReadResult GetReadAsyncResult()
        {
            if (!_readerAwaitable.IsCompleted)
            {
                ThrowHelper.ThrowInvalidOperationException_GetResultNotCompleted();
            }

            var result = new ReadResult();
            lock (_sync)
            {
                GetResult(ref result);
            }
            return result;
        }

        private void GetResult(ref ReadResult result)
        {
            if (_writerCompletion.IsCompletedOrThrow())
            {
                result._resultFlags |= ResultFlags.Completed;
            }

            bool isCanceled = _readerAwaitable.ObserveCancelation();
            if (isCanceled)
            {
                result._resultFlags |= ResultFlags.Canceled;
            }

            // No need to read end if there is no head
            BufferSegment head = _readHead;

            if (head != null)
            {
                // Reading commit head shared with writer
                result._resultBuffer = new ReadOnlySequence<byte>(head, _readHeadIndex, _commitHead, _commitHeadIndex - _commitHead.Start);
            }

            if (isCanceled)
            {
                _readingState.BeginTentative();
            }
            else
            {
                _readingState.Begin();
            }
        }

        internal bool IsFlushAsyncCompleted => _writerAwaitable.IsCompleted;

        internal FlushResult GetFlushAsyncResult()
        {
            var result = new FlushResult();
            lock (_sync)
            {
                if (!_writerAwaitable.IsCompleted)
                {
                    ThrowHelper.ThrowInvalidOperationException_GetResultNotCompleted();
                }

                // Change the state from to be canceled -> observed
                if (_writerAwaitable.ObserveCancelation())
                {
                    result._resultFlags |= ResultFlags.Canceled;
                }
                if (_readerCompletion.IsCompletedOrThrow())
                {
                    result._resultFlags |= ResultFlags.Completed;
                }
            }

            return result;
        }

        internal void OnFlushAsyncCompleted(Action continuation)
        {
            Action awaitable;
            bool doubleCompletion;
            lock (_sync)
            {
                awaitable = _writerAwaitable.OnCompleted(continuation, out doubleCompletion);
            }
            if (doubleCompletion)
            {
                Reader.Complete(ThrowHelper.CreateInvalidOperationException_NoConcurrentOperation());
            }
            TrySchedule(_writerScheduler, awaitable);
        }

        private void ReaderCancellationRequested()
        {
            Action action;
            lock (_sync)
            {
                action = _readerAwaitable.Cancel();
            }
            TrySchedule(_readerScheduler, action);
        }

        private void WriterCancellationRequested()
        {
            Action action;
            lock (_sync)
            {
                action = _writerAwaitable.Cancel();
            }
            TrySchedule(_writerScheduler, action);
        }

        /// <summary>
        /// Gets the <see cref="PipeReader"/> for this pipe.
        /// </summary>
        public PipeReader Reader => _reader;

        /// <summary>
        /// Gets the <see cref="PipeWriter"/> for this pipe.
        /// </summary>
        public PipeWriter Writer => _writer;

        /// <summary>
        /// Resets the pipe
        /// </summary>
        public void Reset()
        {
            lock (_sync)
            {
                if (!_disposed)
                {
                    ThrowHelper.ThrowInvalidOperationException_ResetIncompleteReaderWriter();
                }

                _disposed = false;
                ResetState();
            }
        }
    }
}
