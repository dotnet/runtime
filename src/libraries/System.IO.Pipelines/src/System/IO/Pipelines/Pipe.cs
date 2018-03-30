// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.IO.Pipelines
{
    /// <summary>
    /// Default <see cref="PipeWriter"/> and <see cref="PipeReader"/> implementation.
    /// </summary>
    public sealed partial class Pipe
    {
        internal const int SegmentPoolSize = 16;

        private static readonly Action<object> s_signalReaderAwaitable = state => ((Pipe)state).ReaderCancellationRequested();
        private static readonly Action<object> s_signalWriterAwaitable = state => ((Pipe)state).WriterCancellationRequested();
        private static readonly Action<object> s_invokeCompletionCallbacks = state => ((PipeCompletionCallbacks)state).Execute();

        // These callbacks all point to the same methods but are different delegate types
        private static readonly ContextCallback s_executionContextCallback = ExecuteWithExecutionContext;
        private static readonly ContextCallback s_executionContextRawCallback = ExecuteWithoutExecutionContext;
        private static readonly SendOrPostCallback s_syncContextExecutionContextCallback = ExecuteWithExecutionContext;
        private static readonly SendOrPostCallback s_syncContextExecuteWithoutExecutionContextCallback = ExecuteWithoutExecutionContext;
        private static readonly Action<object> s_scheduleWithExecutionContextCallback = ExecuteWithExecutionContext;

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
            var useSynchronizationContext = options.UseSynchronizationContext;
            _readerAwaitable = new PipeAwaitable(completed: false, useSynchronizationContext);
            _writerAwaitable = new PipeAwaitable(completed: true, useSynchronizationContext);
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

        internal Memory<byte> GetMemory(int sizeHint)
        {
            if (_writerCompletion.IsCompleted)
            {
                ThrowHelper.ThrowInvalidOperationException_NoWritingAllowed();
            }

            if (sizeHint < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.minimumSize);
            }

            lock (_sync)
            {
                BufferSegment segment = _writingHead ?? AllocateWriteHeadUnsynchronized(sizeHint);

                int bytesLeftInBuffer = segment.WritableBytes;

                // If inadequate bytes left or if the segment is readonly
                if (bytesLeftInBuffer == 0 || bytesLeftInBuffer < sizeHint || segment.ReadOnly)
                {
                    BufferSegment nextSegment = CreateSegmentUnsynchronized();
                    nextSegment.SetMemory(_pool.Rent(GetSegmentSize(sizeHint)));

                    segment.SetNext(nextSegment);

                    _writingHead = nextSegment;
                }
            }

            return _writingHead.AvailableMemory.Slice(_writingHead.End, _writingHead.WritableBytes);
        }

        private BufferSegment AllocateWriteHeadUnsynchronized(int sizeHint)
        {
            BufferSegment segment = null;

            if (_commitHead != null && !_commitHead.ReadOnly)
            {
                // Try to return the tail so the calling code can append to it
                int remaining = _commitHead.WritableBytes;

                if (sizeHint <= remaining && remaining > 0)
                {
                    // Free tail space of the right amount, use that
                    segment = _commitHead;
                }
            }

            if (segment == null)
            {
                // No free tail space, allocate a new segment
                segment = CreateSegmentUnsynchronized();
                segment.SetMemory(_pool.Rent(GetSegmentSize(sizeHint)));
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

        private int GetSegmentSize(int sizeHint)
        {
            // First we need to handle case where hint is smaller than minimum segment size
            var adjustedToMinimumSize = Math.Max(_minimumSegmentSize, sizeHint);
            // After that adjust it to fit into pools max buffer size
            var adjustedToMaximumSize = Math.Min(_pool.MaxBufferSize, adjustedToMinimumSize);
            return adjustedToMaximumSize;
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

        internal bool CommitUnsynchronized()
        {
            if (_writingHead == null)
            {
                // Nothing written to commit
                return true;
            }

            if (_readHead == null)
            {
                // Update the head to point to the head of the buffer.
                // This happens if we called alloc(0) then write
                _readHead = _commitHead;
                _readHeadIndex = 0;
            }

            // Always move the commit head to the write head
            var bytesWritten = _currentWriteLength;
            _commitHead = _writingHead;
            _commitHeadIndex = _writingHead.End;
            _length += bytesWritten;

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

            return bytesWritten == 0;
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

        internal ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken)
        {
            CompletionData completionData;
            CancellationTokenRegistration cancellationTokenRegistration;
            ValueTask<FlushResult> result;
            lock (_sync)
            {
                var wasEmpty = CommitUnsynchronized();

                // AttachToken before completing reader awaiter in case cancellationToken is already completed
                cancellationTokenRegistration = _writerAwaitable.AttachToken(cancellationToken, s_signalWriterAwaitable, this);

                // Complete reader only if new data was pushed into the pipe
                if (!wasEmpty)
                {
                    _readerAwaitable.Complete(out completionData);
                }
                else
                {
                    completionData = default;
                }

                // I couldn't find a way for flush to induce backpressure deadlock
                // if it always adds new data to pipe and wakes up the reader but assert anyway
                Debug.Assert(_writerAwaitable.IsCompleted || _readerAwaitable.IsCompleted);

                // If the writer is completed (which it will be most of the time) the return a completed ValueTask
                if (_writerAwaitable.IsCompleted)
                {
                    var flushResult = new FlushResult();
                    GetFlushResult(ref flushResult);
                    result = new ValueTask<FlushResult>(flushResult);
                }
                else
                {
                    // Otherwise it's async
                    result = new ValueTask<FlushResult>(_writer, token: 0);
                }
            }

            cancellationTokenRegistration.Dispose();

            TrySchedule(_readerScheduler, completionData);

            return result;
        }

        internal void CompleteWriter(Exception exception)
        {
            CompletionData completionData;
            PipeCompletionCallbacks completionCallbacks;
            bool readerCompleted;

            lock (_sync)
            {
                // Commit any pending buffers
                CommitUnsynchronized();

                completionCallbacks = _writerCompletion.TryComplete(exception);
                _readerAwaitable.Complete(out completionData);
                readerCompleted = _readerCompletion.IsCompleted;
            }

            if (completionCallbacks != null)
            {
                TrySchedule(_readerScheduler, s_invokeCompletionCallbacks, completionCallbacks);
            }

            TrySchedule(_readerScheduler, completionData);

            if (readerCompleted)
            {
                CompletePipe();
            }
        }

        internal void AdvanceReader(in SequencePosition consumed)
        {
            AdvanceReader(consumed, consumed);
        }

        internal void AdvanceReader(in SequencePosition consumed, in SequencePosition examined)
        {
            // If the reader is completed
            if (_readerCompletion.IsCompleted)
            {
                ThrowHelper.ThrowInvalidOperationException_NoReadingAllowed();
            }

            // TODO: Use new SequenceMarshal.TryGetReadOnlySequenceSegment to get the correct data
            // directly casting only works because the type value in ReadOnlySequenceSegment is 0
            AdvanceReader((BufferSegment)consumed.GetObject(), consumed.GetInteger(), (BufferSegment)examined.GetObject(), examined.GetInteger());
        }

        internal void AdvanceReader(BufferSegment consumedSegment, int consumedIndex, BufferSegment examinedSegment, int examinedIndex)
        {
            BufferSegment returnStart = null;
            BufferSegment returnEnd = null;

            CompletionData completionData = default;

            lock (_sync)
            {
                var examinedEverything = false;
                if (examinedSegment == _commitHead)
                {
                    examinedEverything = _commitHead != null ? examinedIndex == _commitHeadIndex - _commitHead.Start : examinedIndex == 0;
                }

                if (consumedSegment != null)
                {
                    if (_readHead == null)
                    {
                        ThrowHelper.ThrowInvalidOperationException_AdvanceToInvalidCursor();
                        return;
                    }

                    returnStart = _readHead;
                    returnEnd = consumedSegment;

                    // Check if we crossed _maximumSizeLow and complete backpressure
                    long consumedBytes = new ReadOnlySequence<byte>(returnStart, _readHeadIndex, consumedSegment, consumedIndex).Length;
                    long oldLength = _length;
                    _length -= consumedBytes;

                    if (oldLength >= _resumeWriterThreshold &&
                        _length < _resumeWriterThreshold)
                    {
                        _writerAwaitable.Complete(out completionData);
                    }

                    // Check if we consumed entire last segment
                    // if we are going to return commit head we need to check that there is no writing operation that
                    // might be using tailspace
                    if (consumedIndex == returnEnd.Length && _writingHead != returnEnd)
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
                        _readHeadIndex = consumedIndex;
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

            TrySchedule(_writerScheduler, completionData);
        }

        internal void CompleteReader(Exception exception)
        {
            PipeCompletionCallbacks completionCallbacks;
            CompletionData completionData;
            bool writerCompleted;

            lock (_sync)
            {
                // If we're reading, treat clean up that state before continuting
                if (_readingState.IsActive)
                {
                    _readingState.End();
                }

                // REVIEW: We should consider cleaning up all of the allocated memory
                // on the reader side now.

                completionCallbacks = _readerCompletion.TryComplete(exception);
                _writerAwaitable.Complete(out completionData);
                writerCompleted = _writerCompletion.IsCompleted;
            }

            if (completionCallbacks != null)
            {
                TrySchedule(_writerScheduler, s_invokeCompletionCallbacks, completionCallbacks);
            }

            TrySchedule(_writerScheduler, completionData);

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
            CompletionData completionData;
            lock (_sync)
            {
                _readerAwaitable.Cancel(out completionData);
            }
            TrySchedule(_readerScheduler, completionData);
        }

        internal void CancelPendingFlush()
        {
            CompletionData completionData;
            lock (_sync)
            {
                _writerAwaitable.Cancel(out completionData);
            }
            TrySchedule(_writerScheduler, completionData);
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

        internal ValueTask<ReadResult> ReadAsync(CancellationToken token)
        {
            CancellationTokenRegistration cancellationTokenRegistration;
            if (_readerCompletion.IsCompleted)
            {
                ThrowHelper.ThrowInvalidOperationException_NoReadingAllowed();
            }

            ValueTask<ReadResult> result;
            lock (_sync)
            {
                cancellationTokenRegistration = _readerAwaitable.AttachToken(token, s_signalReaderAwaitable, this);

                // If the awaitable is already complete then return the value result directly
                if (_readerAwaitable.IsCompleted)
                {
                    var readResult = new ReadResult();
                    GetReadResult(ref readResult);
                    result = new ValueTask<ReadResult>(readResult);
                }
                else
                {
                    // Otherwise it's async
                    result = new ValueTask<ReadResult>(_reader, token: 0);
                }
            }
            cancellationTokenRegistration.Dispose();

            return result;
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
                    GetReadResult(ref result);
                    return true;
                }

                if (_readerAwaitable.HasContinuation)
                {
                    ThrowHelper.ThrowInvalidOperationException_AlreadyReading();
                }
                return false;
            }
        }

        private static void TrySchedule(PipeScheduler scheduler, Action<object> action, object state)
        {
            if (action != null)
            {
                scheduler.Schedule(action, state);
            }
        }

        private static void TrySchedule(PipeScheduler scheduler, in CompletionData completionData)
        {
            // Nothing to do
            if (completionData.Completion == null)
            {
                return;
            }

            // Ultimately, we need to call either
            // 1. The sync context with a delegate
            // 2. The scheduler with a delegate
            // That delegate and state will either be the action passed in directly
            // or it will be that specified delegate wrapped in ExecutionContext.Run

            if (completionData.SynchronizationContext == null)
            {
                // We don't have a SynchronizationContext so execute on the specified scheduler
                if (completionData.ExecutionContext == null)
                {
                    // We can run directly, this should be the default fast path
                    scheduler.Schedule(completionData.Completion, completionData.CompletionState);
                    return;
                }

                // We also have to run on the specified execution context so run the scheduler and execute the
                // delegate on the execution context
                scheduler.Schedule(s_scheduleWithExecutionContextCallback, completionData);
            }
            else
            {
                if (completionData.ExecutionContext == null)
                {
                    // We need to box the struct here since there's no generic overload for state
                    completionData.SynchronizationContext.Post(s_syncContextExecuteWithoutExecutionContextCallback, completionData);
                }
                else
                {
                    // We need to execute the callback with the execution context
                    completionData.SynchronizationContext.Post(s_syncContextExecutionContextCallback, completionData);
                }
            }
        }

        private static void ExecuteWithoutExecutionContext(object state)
        {
            CompletionData completionData = (CompletionData)state;
            completionData.Completion(completionData.CompletionState);
        }

        private static void ExecuteWithExecutionContext(object state)
        {
            CompletionData completionData = (CompletionData)state;
            Debug.Assert(completionData.ExecutionContext != null);
            ExecutionContext.Run(completionData.ExecutionContext, s_executionContextRawCallback, state);
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

        internal ValueTaskSourceStatus GetReadAsyncStatus()
        {
            if (_readerAwaitable.IsCompleted)
            {
                if (_writerCompletion.IsFaulted)
                {
                    return ValueTaskSourceStatus.Faulted;
                }

                return ValueTaskSourceStatus.Succeeded;
            }
            return ValueTaskSourceStatus.Pending;
        }

        internal void OnReadAsyncCompleted(Action<object> continuation, object state, ValueTaskSourceOnCompletedFlags flags)
        {
            CompletionData completionData;
            bool doubleCompletion;
            lock (_sync)
            {
                _readerAwaitable.OnCompleted(continuation, state, flags, out completionData, out doubleCompletion);
            }
            if (doubleCompletion)
            {
                Writer.Complete(ThrowHelper.CreateInvalidOperationException_NoConcurrentOperation());
            }
            TrySchedule(_readerScheduler, completionData);
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
                GetReadResult(ref result);
            }
            return result;
        }

        private void GetReadResult(ref ReadResult result)
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

        internal ValueTaskSourceStatus GetFlushAsyncStatus()
        {
            if (_writerAwaitable.IsCompleted)
            {
                if (_readerCompletion.IsFaulted)
                {
                    return ValueTaskSourceStatus.Faulted;
                }

                return ValueTaskSourceStatus.Succeeded;
            }
            return ValueTaskSourceStatus.Pending;
        }

        internal FlushResult GetFlushAsyncResult()
        {
            var result = new FlushResult();
            lock (_sync)
            {
                if (!_writerAwaitable.IsCompleted)
                {
                    ThrowHelper.ThrowInvalidOperationException_GetResultNotCompleted();
                }

                GetFlushResult(ref result);
            }

            return result;
        }

        private void GetFlushResult(ref FlushResult result)
        {
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

        internal void OnFlushAsyncCompleted(Action<object> continuation, object state, ValueTaskSourceOnCompletedFlags flags)
        {
            CompletionData completionData;
            bool doubleCompletion;
            lock (_sync)
            {
                _writerAwaitable.OnCompleted(continuation, state, flags, out completionData, out doubleCompletion);
            }
            if (doubleCompletion)
            {
                Reader.Complete(ThrowHelper.CreateInvalidOperationException_NoConcurrentOperation());
            }
            TrySchedule(_writerScheduler, completionData);
        }

        private void ReaderCancellationRequested()
        {
            CompletionData completionData;
            lock (_sync)
            {
                _readerAwaitable.Cancel(out completionData);
            }
            TrySchedule(_readerScheduler, completionData);
        }

        private void WriterCancellationRequested()
        {
            CompletionData completionData;
            lock (_sync)
            {
                _writerAwaitable.Cancel(out completionData);
            }
            TrySchedule(_writerScheduler, completionData);
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
