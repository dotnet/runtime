// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
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
        internal const int InitialSegmentPoolSize = 16; // 65K
        internal const int MaxSegmentPoolSize = 256; // 1MB

        private static readonly Action<object> s_signalReaderAwaitable = state => ((Pipe)state).ReaderCancellationRequested();
        private static readonly Action<object> s_signalWriterAwaitable = state => ((Pipe)state).WriterCancellationRequested();
        private static readonly Action<object> s_invokeCompletionCallbacks = state => ((PipeCompletionCallbacks)state).Execute();

        // These callbacks all point to the same methods but are different delegate types
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

        // Mutable struct! Don't make this readonly
        private BufferSegmentStack _bufferSegmentPool;

        private readonly DefaultPipeReader _reader;
        private readonly DefaultPipeWriter _writer;

        private readonly bool _useSynchronizationContext;

        private long _length;
        private long _currentWriteLength;

        private PipeAwaitable _readerAwaitable;
        private PipeAwaitable _writerAwaitable;

        private PipeCompletion _writerCompletion;
        private PipeCompletion _readerCompletion;

        // Stores the last examined position, used to calculate how many bytes were to release
        // for back pressure management
        private long _lastExaminedIndex = -1;

        // The read head which is the extent of the PipeReader's consumed bytes
        private BufferSegment _readHead;
        private int _readHeadIndex;

        // The commit head which is the extent of the bytes available to the PipeReader to consume
        private BufferSegment _readTail;
        private int _readTailIndex;

        // The write head which is the extent of the PipeWriter's written bytes
        private BufferSegment _writingHead;
        private Memory<byte> _writingHeadMemory;
        private int _writingHeadBytesBuffered;

        private PipeOperationState _operationState;

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

            _bufferSegmentPool = new BufferSegmentStack(InitialSegmentPoolSize);

            _operationState = default;
            _readerCompletion = default;
            _writerCompletion = default;

            // If we're using the default pool then mark it as null since we're just going to use the 
            // array pool under the covers
            _pool = options.Pool == MemoryPool<byte>.Shared ? null : options.Pool;
            _minimumSegmentSize = options.MinimumSegmentSize;
            _pauseWriterThreshold = options.PauseWriterThreshold;
            _resumeWriterThreshold = options.ResumeWriterThreshold;
            _readerScheduler = options.ReaderScheduler;
            _writerScheduler = options.WriterScheduler;
            _useSynchronizationContext = options.UseSynchronizationContext;
            _readerAwaitable = new PipeAwaitable(completed: false, _useSynchronizationContext);
            _writerAwaitable = new PipeAwaitable(completed: true, _useSynchronizationContext);
            _reader = new DefaultPipeReader(this);
            _writer = new DefaultPipeWriter(this);
        }

        private void ResetState()
        {
            _readerCompletion.Reset();
            _writerCompletion.Reset();
            _readerAwaitable = new PipeAwaitable(completed: false, _useSynchronizationContext);
            _writerAwaitable = new PipeAwaitable(completed: true, _useSynchronizationContext);
            _readTailIndex = 0;
            _readHeadIndex = 0;
            _lastExaminedIndex = -1;
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

            AllocateWriteHeadIfNeeded(sizeHint);

            return _writingHeadMemory;
        }

        internal Span<byte> GetSpan(int sizeHint)
        {
            if (_writerCompletion.IsCompleted)
            {
                ThrowHelper.ThrowInvalidOperationException_NoWritingAllowed();
            }

            if (sizeHint < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.minimumSize);
            }

            AllocateWriteHeadIfNeeded(sizeHint);

            return _writingHeadMemory.Span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AllocateWriteHeadIfNeeded(int sizeHint)
        {
            // If writing is currently active and enough space, don't need to take the lock to just set WritingActive.
            // IsWritingActive is needed to prevent the reader releasing the writers memory when it fully consumes currently written.
            if (!_operationState.IsWritingActive ||
                _writingHeadMemory.Length == 0 || _writingHeadMemory.Length < sizeHint)
            {
                AllocateWriteHeadSynchronized(sizeHint);
            }
        }

        private void AllocateWriteHeadSynchronized(int sizeHint)
        {
            lock (_sync)
            {
                _operationState.BeginWrite();

                if (_writingHead == null)
                {
                    // We need to allocate memory to write since nobody has written before
                    BufferSegment newSegment = AllocateSegment(sizeHint);

                    // Set all the pointers
                    _writingHead = _readHead = _readTail = newSegment;
                    _lastExaminedIndex = 0;
                }
                else
                {
                    int bytesLeftInBuffer = _writingHeadMemory.Length;

                    if (bytesLeftInBuffer == 0 || bytesLeftInBuffer < sizeHint)
                    {
                        if (_writingHeadBytesBuffered > 0)
                        {
                            // Flush buffered data to the segment
                            _writingHead.End += _writingHeadBytesBuffered;
                            _writingHeadBytesBuffered = 0;
                        }

                        BufferSegment newSegment = AllocateSegment(sizeHint);

                        _writingHead.SetNext(newSegment);
                        _writingHead = newSegment;
                    }
                }
            }
        }

        private BufferSegment AllocateSegment(int sizeHint)
        {
            BufferSegment newSegment = CreateSegmentUnsynchronized();

            if (_pool is null)
            {
                // Use the array pool
                newSegment.SetOwnedMemory(ArrayPool<byte>.Shared.Rent(GetSegmentSize(sizeHint)));
            }
            else if (sizeHint <= _pool.MaxBufferSize)
            {
                // Use the specified pool if it fits
                newSegment.SetOwnedMemory(_pool.Rent(GetSegmentSize(sizeHint, _pool.MaxBufferSize)));
            }
            else
            {
                // We can't use the pool so allocate an array
                newSegment.SetUnownedMemory(new byte[sizeHint]);
            }

            _writingHeadMemory = newSegment.AvailableMemory;

            return newSegment;
        }

        private int GetSegmentSize(int sizeHint, int maxBufferSize = int.MaxValue)
        {
            // First we need to handle case where hint is smaller than minimum segment size
            sizeHint = Math.Max(_minimumSegmentSize, sizeHint);
            // After that adjust it to fit into pools max buffer size
            var adjustedToMaximumSize = Math.Min(maxBufferSize, sizeHint);
            return adjustedToMaximumSize;
        }

        private BufferSegment CreateSegmentUnsynchronized()
        {
            if (_bufferSegmentPool.TryPop(out BufferSegment segment))
            {
                return segment;
            }

            return new BufferSegment();
        }

        private void ReturnSegmentUnsynchronized(BufferSegment segment)
        {
            Debug.Assert(segment != _readHead, "Returning _readHead segment that's in use!");
            Debug.Assert(segment != _readTail, "Returning _readTail segment that's in use!");
            Debug.Assert(segment != _writingHead, "Returning _writingHead segment that's in use!");

            if (_bufferSegmentPool.Count < MaxSegmentPoolSize)
            {
                _bufferSegmentPool.Push(segment);
            }
        }

        internal bool CommitUnsynchronized()
        {
            _operationState.EndWrite();

            if (_currentWriteLength == 0)
            {
                // Nothing written to commit
                return true;
            }

            // Update the writing head
            _writingHead.End += _writingHeadBytesBuffered;

            // Always move the read tail to the write head
            _readTail = _writingHead;
            _readTailIndex = _writingHead.End;

            long oldLength = _length;
            _length += _currentWriteLength;

            // Do not reset if reader is complete
            if (_pauseWriterThreshold > 0 &&
                oldLength < _pauseWriterThreshold &&
                _length >= _pauseWriterThreshold &&
                !_readerCompletion.IsCompleted)
            {
                _writerAwaitable.SetUncompleted();
            }

            _currentWriteLength = 0;
            _writingHeadBytesBuffered = 0;

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Advance(int bytes)
        {
            if ((uint)bytes > (uint)_writingHeadMemory.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.bytes);
            }

            AdvanceCore(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AdvanceCore(int bytesWritten)
        {
            _currentWriteLength += bytesWritten;
            _writingHeadBytesBuffered += bytesWritten;
            _writingHeadMemory = _writingHeadMemory.Slice(bytesWritten);
        }

        internal ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken)
        {
            CompletionData completionData;
            ValueTask<FlushResult> result;
            lock (_sync)
            {
                var wasEmpty = CommitUnsynchronized();

                // AttachToken before completing reader awaiter in case cancellationToken is already completed
                _writerAwaitable.BeginOperation(cancellationToken, s_signalWriterAwaitable, this);

                // If the writer is completed (which it will be most of the time) then return a completed ValueTask
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

                // Complete reader only if new data was pushed into the pipe
                // Avoid throwing in between completing the reader and scheduling the callback
                // if the intent is to allow pipe to continue reading the data
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
            }

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

            if (readerCompleted)
            {
                CompletePipe();
            }

            if (completionCallbacks != null)
            {
                ScheduleCallbacks(_readerScheduler, completionCallbacks);
            }

            TrySchedule(_readerScheduler, completionData);
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

        private void AdvanceReader(BufferSegment consumedSegment, int consumedIndex, BufferSegment examinedSegment, int examinedIndex)
        {
            // Throw if examined < consumed
            if (consumedSegment != null && examinedSegment != null && BufferSegment.GetLength(consumedSegment, consumedIndex, examinedSegment, examinedIndex) < 0)
            {
                ThrowHelper.ThrowInvalidOperationException_InvalidExaminedOrConsumedPosition();
            }

            BufferSegment returnStart = null;
            BufferSegment returnEnd = null;

            CompletionData completionData = default;

            lock (_sync)
            {
                var examinedEverything = false;
                if (examinedSegment == _readTail)
                {
                    examinedEverything = examinedIndex == _readTailIndex;
                }

                if (examinedSegment != null && _lastExaminedIndex >= 0)
                {
                    long examinedBytes = BufferSegment.GetLength(_lastExaminedIndex, examinedSegment, examinedIndex);
                    long oldLength = _length;

                    if (examinedBytes < 0)
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidExaminedPosition();
                    }

                    _length -= examinedBytes;

                    // Store the absolute position
                    _lastExaminedIndex = examinedSegment.RunningIndex + examinedIndex;

                    Debug.Assert(_length >= 0, "Length has gone negative");

                    if (oldLength >= _resumeWriterThreshold &&
                        _length < _resumeWriterThreshold)
                    {
                        _writerAwaitable.Complete(out completionData);
                    }
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

                    // Check if we consumed entire last segment
                    // if we are going to return commit head we need to check that there is no writing operation that
                    // might be using tailspace
                    if (consumedIndex == returnEnd.Length && !_operationState.IsWritingActive)
                    {
                        BufferSegment nextBlock = returnEnd.NextSegment;
                        if (_readTail == returnEnd)
                        {
                            _readTail = nextBlock;
                            _readTailIndex = 0;
                        }

                        _readHead = nextBlock;
                        _readHeadIndex = 0;

                        // Reset the writing head to null if it's the return block
                        // then null it out as we're about to reset that memory
                        if (_writingHead == returnEnd)
                        {
                            // If we're about to null out the _writingHead then assert the list is empty
                            Debug.Assert(_readHead == null);
                            Debug.Assert(_readTail == null);
                            _writingHead = null;
                            _writingHeadMemory = default;
                        }

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
                    Debug.Assert(_writerAwaitable.IsCompleted, "PipeWriter.FlushAsync is isn't completed and will deadlock");

                    _readerAwaitable.SetUncompleted();
                }

                while (returnStart != null && returnStart != returnEnd)
                {
                    BufferSegment next = returnStart.NextSegment;
                    returnStart.ResetMemory();
                    ReturnSegmentUnsynchronized(returnStart);
                    returnStart = next;
                }

                _operationState.EndRead();
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
                if (_operationState.IsReadingActive)
                {
                    _operationState.EndRead();
                }

                // REVIEW: We should consider cleaning up all of the allocated memory
                // on the reader side now.

                completionCallbacks = _readerCompletion.TryComplete(exception);
                _writerAwaitable.Complete(out completionData);
                writerCompleted = _writerCompletion.IsCompleted;
            }

            if (writerCompleted)
            {
                CompletePipe();
            }

            if (completionCallbacks != null)
            {
                ScheduleCallbacks(_writerScheduler, completionCallbacks);
            }

            TrySchedule(_writerScheduler, completionData);
        }

        internal void OnWriterCompleted(Action<Exception, object> callback, object state)
        {
            if (callback is null)
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
                ScheduleCallbacks(_readerScheduler, completionCallbacks);
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
            if (callback is null)
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
                ScheduleCallbacks(_writerScheduler, completionCallbacks);
            }
        }

        internal ValueTask<ReadResult> ReadAsync(CancellationToken token)
        {
            if (_readerCompletion.IsCompleted)
            {
                ThrowHelper.ThrowInvalidOperationException_NoReadingAllowed();
            }

            ValueTask<ReadResult> result;
            lock (_sync)
            {
                _readerAwaitable.BeginOperation(token, s_signalReaderAwaitable, this);

                // If the awaitable is already complete then return the value result directly
                if (_readerAwaitable.IsCompleted)
                {
                    GetReadResult(out ReadResult readResult);
                    result = new ValueTask<ReadResult>(readResult);
                }
                else
                {
                    // Otherwise it's async
                    result = new ValueTask<ReadResult>(_reader, token: 0);
                }
            }

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

                if (_length > 0 || _readerAwaitable.IsCompleted)
                {
                    GetReadResult(out result);
                    return true;
                }

                if (_readerAwaitable.IsRunning)
                {
                    ThrowHelper.ThrowInvalidOperationException_AlreadyReading();
                }

                _operationState.BeginReadTentative();
                result = default;
                return false;
            }
        }

        private static void ScheduleCallbacks(PipeScheduler scheduler, PipeCompletionCallbacks completionCallbacks)
        {
            Debug.Assert(completionCallbacks != null);

            scheduler.UnsafeSchedule(s_invokeCompletionCallbacks, completionCallbacks);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TrySchedule(PipeScheduler scheduler, in CompletionData completionData)
        {
            Action<object> completion = completionData.Completion;
            // Nothing to do
            if (completion is null)
            {
                return;
            }

            // Ultimately, we need to call either
            // 1. The sync context with a delegate
            // 2. The scheduler with a delegate
            // That delegate and state will either be the action passed in directly
            // or it will be that specified delegate wrapped in ExecutionContext.Run
            if (completionData.SynchronizationContext is null && completionData.ExecutionContext is null)
            {
                // Common fast-path
                scheduler.UnsafeSchedule(completion, completionData.CompletionState);
            }
            else
            {
                ScheduleWithContext(scheduler, in completionData);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ScheduleWithContext(PipeScheduler scheduler, in CompletionData completionData)
        {
            Debug.Assert(completionData.SynchronizationContext != null || completionData.ExecutionContext != null);

            if (completionData.SynchronizationContext is null)
            {
                // We also have to run on the specified execution context so run the scheduler and execute the
                // delegate on the execution context
                scheduler.UnsafeSchedule(s_scheduleWithExecutionContextCallback, completionData);
            }
            else
            {
                if (completionData.ExecutionContext is null)
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
                BufferSegment segment = _readHead ?? _readTail;
                while (segment != null)
                {
                    BufferSegment returnSegment = segment;
                    segment = segment.NextSegment;

                    returnSegment.ResetMemory();
                }

                _writingHead = null;
                _readHead = null;
                _readTail = null;
                _lastExaminedIndex = -1;
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
            ReadResult result;
            CancellationTokenRegistration cancellationTokenRegistration = default;
            CancellationToken cancellationToken = default;
            try
            {
                lock (_sync)
                {
                    if (!_readerAwaitable.IsCompleted)
                    {
                        ThrowHelper.ThrowInvalidOperationException_GetResultNotCompleted();
                    }

                    cancellationTokenRegistration = _readerAwaitable.ReleaseCancellationTokenRegistration(out cancellationToken);
                    GetReadResult(out result);
                }
            }
            finally
            {
                cancellationTokenRegistration.Dispose();
                cancellationToken.ThrowIfCancellationRequested();
            }

            return result;
        }

        private void GetReadResult(out ReadResult result)
        {
            bool isCompleted = _writerCompletion.IsCompletedOrThrow();
            bool isCanceled = _readerAwaitable.ObserveCancellation();

            // No need to read end if there is no head
            BufferSegment head = _readHead;
            if (head != null)
            {
                // Reading commit head shared with writer
                var readOnlySequence = new ReadOnlySequence<byte>(head, _readHeadIndex, _readTail, _readTailIndex);
                result = new ReadResult(readOnlySequence, isCanceled, isCompleted);
            }
            else
            {
                result = new ReadResult(default, isCanceled, isCompleted);
            }

            if (isCanceled)
            {
                _operationState.BeginReadTentative();
            }
            else
            {
                _operationState.BeginRead();
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
            FlushResult result = default;
            CancellationToken cancellationToken = default;
            CancellationTokenRegistration cancellationTokenRegistration = default;

            try
            {
                lock (_sync)
                {
                    if (!_writerAwaitable.IsCompleted)
                    {
                        ThrowHelper.ThrowInvalidOperationException_GetResultNotCompleted();
                    }

                    GetFlushResult(ref result);

                    cancellationTokenRegistration = _writerAwaitable.ReleaseCancellationTokenRegistration(out cancellationToken);
                }
            }
            finally
            {
                cancellationTokenRegistration.Dispose();
                cancellationToken.ThrowIfCancellationRequested();
            }

            return result;
        }

        private void GetFlushResult(ref FlushResult result)
        {
            // Change the state from to be canceled -> observed
            if (_writerAwaitable.ObserveCancellation())
            {
                result._resultFlags |= ResultFlags.Canceled;
            }
            if (_readerCompletion.IsCompletedOrThrow())
            {
                result._resultFlags |= ResultFlags.Completed;
            }
        }

        internal ValueTask<FlushResult> WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            if (_writerCompletion.IsCompleted)
            {
                ThrowHelper.ThrowInvalidOperationException_NoWritingAllowed();
            }

            // Allocate whatever the pool gives us so we can write, this also marks the
            // state as writing
            AllocateWriteHeadIfNeeded(0);

            lock (_sync)
            {
                if (source.Length <= _writingHeadMemory.Length)
                {
                    source.CopyTo(_writingHeadMemory);

                    AdvanceCore(source.Length);
                }
                else
                {
                    // This is the multi segment copy
                    WriteMultiSegment(source.Span);
                }
            }

            return FlushAsync(cancellationToken);
        }

        private void WriteMultiSegment(ReadOnlySpan<byte> source)
        {
            Span<byte> destination = _writingHeadMemory.Span;

            while (true)
            {
                int writable = Math.Min(destination.Length, source.Length);
                source.Slice(0, writable).CopyTo(destination);
                source = source.Slice(writable);
                AdvanceCore(writable);

                if (source.Length == 0)
                {
                    break;
                }

                // We filled the segment
                _writingHead.End += writable;
                _writingHeadBytesBuffered = 0;

                // This is optimized to use pooled memory. That's why we pass 0 instead of
                // source.Length
                BufferSegment newSegment = AllocateSegment(0);

                _writingHead.SetNext(newSegment);
                _writingHead = newSegment;

                destination = _writingHeadMemory.Span;
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
                _readerAwaitable.CancellationTokenFired(out completionData);
            }
            TrySchedule(_readerScheduler, completionData);
        }

        private void WriterCancellationRequested()
        {
            CompletionData completionData;
            lock (_sync)
            {
                _writerAwaitable.CancellationTokenFired(out completionData);
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
