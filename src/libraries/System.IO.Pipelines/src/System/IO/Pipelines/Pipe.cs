// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.IO.Pipelines
{
    /// <summary>The default <see cref="System.IO.Pipelines.PipeWriter" /> and <see cref="System.IO.Pipelines.PipeReader" /> implementation.</summary>
    public sealed partial class Pipe
    {
        private static readonly Action<object?> s_signalReaderAwaitable = state => ((Pipe)state!).ReaderCancellationRequested();
        private static readonly Action<object?> s_signalWriterAwaitable = state => ((Pipe)state!).WriterCancellationRequested();
        private static readonly Action<object?> s_invokeCompletionCallbacks = state => ((PipeCompletionCallbacks)state!).Execute();

        // These callbacks all point to the same methods but are different delegate types
        private static readonly ContextCallback s_executionContextRawCallback = ExecuteWithoutExecutionContext!;
        private static readonly SendOrPostCallback s_syncContextExecutionContextCallback = ExecuteWithExecutionContext!;
        private static readonly SendOrPostCallback s_syncContextExecuteWithoutExecutionContextCallback = ExecuteWithoutExecutionContext!;
        private static readonly Action<object?> s_scheduleWithExecutionContextCallback = ExecuteWithExecutionContext!;

        // Mutable struct! Don't make this readonly
        private BufferSegmentStack _bufferSegmentPool;

        private readonly DefaultPipeReader _reader;
        private readonly DefaultPipeWriter _writer;

        // The options instance
        private readonly PipeOptions _options;
        private readonly object _sync = new object();

        // Computed state from the options instance
        private bool UseSynchronizationContext => _options.UseSynchronizationContext;
        private int MinimumSegmentSize => _options.MinimumSegmentSize;
        private long PauseWriterThreshold => _options.PauseWriterThreshold;
        private long ResumeWriterThreshold => _options.ResumeWriterThreshold;

        private PipeScheduler ReaderScheduler => _options.ReaderScheduler;
        private PipeScheduler WriterScheduler => _options.WriterScheduler;

        // This sync objects protects the shared state between the writer and reader (most of this class)
        private object SyncObj => _sync;

        // The number of bytes flushed but not consumed by the reader
        private long _unconsumedBytes;

        // The number of bytes written but not flushed
        private long _unflushedBytes;

        private PipeAwaitable _readerAwaitable;
        private PipeAwaitable _writerAwaitable;

        private PipeCompletion _writerCompletion;
        private PipeCompletion _readerCompletion;

        // Stores the last examined position, used to calculate how many bytes were to release
        // for back pressure management
        private long _lastExaminedIndex = -1;

        // The read head which is the start of the PipeReader's consumed bytes
        private BufferSegment? _readHead;
        private int _readHeadIndex;

        private bool _disposed;

        // The extent of the bytes available to the PipeReader to consume
        private BufferSegment? _readTail;
        private int _readTailIndex;
        private int _minimumReadBytes;

        // The write head which is the extent of the PipeWriter's written bytes
        private BufferSegment? _writingHead;
        private Memory<byte> _writingHeadMemory;
        private int _writingHeadBytesBuffered;

        // Determines what current operation is in flight (reading/writing)
        private PipeOperationState _operationState;

        internal long Length => _unconsumedBytes;

        /// <summary>Initializes a new instance of the <see cref="System.IO.Pipelines.Pipe" /> class using <see cref="System.IO.Pipelines.PipeOptions.Default" /> as options.</summary>
        public Pipe() : this(PipeOptions.Default)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="System.IO.Pipelines.Pipe" /> class with the specified options.</summary>
        /// <param name="options">The set of options for this pipe.</param>
        public Pipe(PipeOptions options)
        {
            if (options == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.options);
            }

            _bufferSegmentPool = new BufferSegmentStack(options.InitialSegmentPoolSize);

            _operationState = default;
            _readerCompletion = default;
            _writerCompletion = default;

            _options = options;
            _readerAwaitable = new PipeAwaitable(completed: false, UseSynchronizationContext);
            _writerAwaitable = new PipeAwaitable(completed: true, UseSynchronizationContext);
            _reader = new DefaultPipeReader(this);
            _writer = new DefaultPipeWriter(this);
        }

        private void ResetState()
        {
            _readerCompletion.Reset();
            _writerCompletion.Reset();
            _readerAwaitable = new PipeAwaitable(completed: false, UseSynchronizationContext);
            _writerAwaitable = new PipeAwaitable(completed: true, UseSynchronizationContext);
            _readTailIndex = 0;
            _readHeadIndex = 0;
            _lastExaminedIndex = -1;
            _unflushedBytes = 0;
            _unconsumedBytes = 0;
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
            lock (SyncObj)
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
            Debug.Assert(sizeHint >= 0);
            BufferSegment newSegment = CreateSegmentUnsynchronized();

            MemoryPool<byte>? pool = null;
            int maxSize = -1;

            if (!_options.IsDefaultSharedMemoryPool)
            {
                pool = _options.Pool;
                maxSize = pool.MaxBufferSize;
            }

            if (sizeHint <= maxSize)
            {
                // Use the specified pool as it fits. Specified pool is not null as maxSize == -1 if _pool is null.
                newSegment.SetOwnedMemory(pool!.Rent(GetSegmentSize(sizeHint, maxSize)));
            }
            else
            {
                // Use the array pool
                int sizeToRequest = GetSegmentSize(sizeHint);
                newSegment.SetOwnedMemory(ArrayPool<byte>.Shared.Rent(sizeToRequest));
            }

            _writingHeadMemory = newSegment.AvailableMemory;

            return newSegment;
        }

        private int GetSegmentSize(int sizeHint, int maxBufferSize = int.MaxValue)
        {
            // First we need to handle case where hint is smaller than minimum segment size
            sizeHint = Math.Max(MinimumSegmentSize, sizeHint);
            // After that adjust it to fit into pools max buffer size
            int adjustedToMaximumSize = Math.Min(maxBufferSize, sizeHint);
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
            Debug.Assert(segment != _readHead, "Returning _readHead segment that's in use!");
            Debug.Assert(segment != _readTail, "Returning _readTail segment that's in use!");
            Debug.Assert(segment != _writingHead, "Returning _writingHead segment that's in use!");

            if (_bufferSegmentPool.Count < _options.MaxSegmentPoolSize)
            {
                _bufferSegmentPool.Push(segment);
            }
        }

        internal bool CommitUnsynchronized()
        {
            _operationState.EndWrite();

            if (_unflushedBytes == 0)
            {
                // Nothing written to commit
                return false;
            }

            // Update the writing head
            Debug.Assert(_writingHead != null);
            _writingHead.End += _writingHeadBytesBuffered;

            // Always move the read tail to the write head
            _readTail = _writingHead;
            _readTailIndex = _writingHead.End;

            long oldLength = _unconsumedBytes;
            _unconsumedBytes += _unflushedBytes;

            bool resumeReader = true;

            if (_unconsumedBytes < _minimumReadBytes)
            {
                // Don't yield the reader if we haven't written enough
                resumeReader = false;
            }
            // We only apply back pressure if the reader isn't paused. This is important
            // because if it is blocked then this could cause a deadlock (if resumeReader is false).
            // If we are resuming the reader, then we can look at the pause threshold to know
            // if we should pause the writer.
            else if (PauseWriterThreshold > 0 &&
                oldLength < PauseWriterThreshold &&
                _unconsumedBytes >= PauseWriterThreshold &&
                !_readerCompletion.IsCompleted)
            {
                _writerAwaitable.SetUncompleted();
            }

            _unflushedBytes = 0;
            _writingHeadBytesBuffered = 0;

            return resumeReader;
        }

        internal void Advance(int bytes)
        {
            lock (SyncObj)
            {
                if ((uint)bytes > (uint)_writingHeadMemory.Length)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.bytes);
                }

                // If the reader is completed we no-op Advance but leave GetMemory and FlushAsync alone
                if (_readerCompletion.IsCompleted)
                {
                    return;
                }

                AdvanceCore(bytes);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AdvanceCore(int bytesWritten)
        {
            _unflushedBytes += bytesWritten;
            _writingHeadBytesBuffered += bytesWritten;
            _writingHeadMemory = _writingHeadMemory.Slice(bytesWritten);
        }

        internal ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken)
        {
            CompletionData completionData;
            ValueTask<FlushResult> result;
            lock (SyncObj)
            {
                PrepareFlush(out completionData, out result, cancellationToken);
            }

            TrySchedule(ReaderScheduler, completionData);

            return result;
        }

        private void PrepareFlush(out CompletionData completionData, out ValueTask<FlushResult> result, CancellationToken cancellationToken)
        {
            var completeReader = CommitUnsynchronized();

            // AttachToken before completing reader awaiter in case cancellationToken is already completed
            _writerAwaitable.BeginOperation(cancellationToken, s_signalWriterAwaitable, this);

            // If the writer is completed (which it will be most of the time) then return a completed ValueTask
            if (_writerAwaitable.IsCompleted)
            {
                FlushResult flushResult = default;
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
            if (completeReader)
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

        internal void CompleteWriter(Exception? exception)
        {
            CompletionData completionData;
            PipeCompletionCallbacks? completionCallbacks;
            bool readerCompleted;

            lock (SyncObj)
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
                ScheduleCallbacks(ReaderScheduler, completionCallbacks);
            }

            TrySchedule(ReaderScheduler, completionData);
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
            AdvanceReader((BufferSegment?)consumed.GetObject(), consumed.GetInteger(), (BufferSegment?)examined.GetObject(), examined.GetInteger());
        }

        private void AdvanceReader(BufferSegment? consumedSegment, int consumedIndex, BufferSegment? examinedSegment, int examinedIndex)
        {
            // Throw if examined < consumed
            if (consumedSegment != null && examinedSegment != null && BufferSegment.GetLength(consumedSegment, consumedIndex, examinedSegment, examinedIndex) < 0)
            {
                ThrowHelper.ThrowInvalidOperationException_InvalidExaminedOrConsumedPosition();
            }

            BufferSegment? returnStart = null;
            BufferSegment? returnEnd = null;

            CompletionData completionData = default;

            lock (SyncObj)
            {
                var examinedEverything = false;
                if (examinedSegment == _readTail)
                {
                    examinedEverything = examinedIndex == _readTailIndex;
                }

                if (examinedSegment != null && _lastExaminedIndex >= 0)
                {
                    long examinedBytes = BufferSegment.GetLength(_lastExaminedIndex, examinedSegment, examinedIndex);
                    long oldLength = _unconsumedBytes;

                    if (examinedBytes < 0)
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidExaminedPosition();
                    }

                    _unconsumedBytes -= examinedBytes;

                    // Store the absolute position
                    _lastExaminedIndex = examinedSegment.RunningIndex + examinedIndex;

                    Debug.Assert(_unconsumedBytes >= 0, "Length has gone negative");

                    if (oldLength >= ResumeWriterThreshold &&
                        _unconsumedBytes < ResumeWriterThreshold)
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

                    void MoveReturnEndToNextBlock()
                    {
                        BufferSegment? nextBlock = returnEnd!.NextSegment;
                        if (_readTail == returnEnd)
                        {
                            _readTail = nextBlock;
                            _readTailIndex = 0;
                        }

                        _readHead = nextBlock;
                        _readHeadIndex = 0;

                        returnEnd = nextBlock;
                    }

                    if (consumedIndex == returnEnd.Length)
                    {
                        // If the writing head isn't block we're about to return, then we can move to the next one
                        // and return this block safely
                        if (_writingHead != returnEnd)
                        {
                            MoveReturnEndToNextBlock();
                        }
                        // If the writing head is the same as the block to be returned, then we need to make sure
                        // there's no pending write and that there's no buffered data for the writing head
                        else if (_writingHeadBytesBuffered == 0 && !_operationState.IsWritingActive)
                        {
                            // Reset the writing head to null if it's the return block and we've consumed everything
                            _writingHead = null;
                            _writingHeadMemory = default;

                            MoveReturnEndToNextBlock();
                        }
                        else
                        {
                            _readHead = consumedSegment;
                            _readHeadIndex = consumedIndex;
                        }
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
                    BufferSegment? next = returnStart.NextSegment;
                    returnStart.ResetMemory();
                    ReturnSegmentUnsynchronized(returnStart);
                    returnStart = next;
                }

                _operationState.EndRead();
            }

            TrySchedule(WriterScheduler, completionData);
        }

        internal void CompleteReader(Exception? exception)
        {
            PipeCompletionCallbacks? completionCallbacks;
            CompletionData completionData;
            bool writerCompleted;

            lock (SyncObj)
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
                ScheduleCallbacks(WriterScheduler, completionCallbacks);
            }

            TrySchedule(WriterScheduler, completionData);
        }

        internal void OnWriterCompleted(Action<Exception?, object?> callback, object? state)
        {
            if (callback is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callback);
            }

            PipeCompletionCallbacks? completionCallbacks;
            lock (SyncObj)
            {
                completionCallbacks = _writerCompletion.AddCallback(callback, state);
            }

            if (completionCallbacks != null)
            {
                ScheduleCallbacks(ReaderScheduler, completionCallbacks);
            }
        }

        internal void CancelPendingRead()
        {
            CompletionData completionData;
            lock (SyncObj)
            {
                _readerAwaitable.Cancel(out completionData);
            }
            TrySchedule(ReaderScheduler, completionData);
        }

        internal void CancelPendingFlush()
        {
            CompletionData completionData;
            lock (SyncObj)
            {
                _writerAwaitable.Cancel(out completionData);
            }
            TrySchedule(WriterScheduler, completionData);
        }

        internal void OnReaderCompleted(Action<Exception?, object?> callback, object? state)
        {
            if (callback is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callback);
            }

            PipeCompletionCallbacks? completionCallbacks;
            lock (SyncObj)
            {
                completionCallbacks = _readerCompletion.AddCallback(callback, state);
            }

            if (completionCallbacks != null)
            {
                ScheduleCallbacks(WriterScheduler, completionCallbacks);
            }
        }

        internal ValueTask<ReadResult> ReadAtLeastAsync(int minimumBytes, CancellationToken token)
        {
            if (_readerCompletion.IsCompleted)
            {
                ThrowHelper.ThrowInvalidOperationException_NoReadingAllowed();
            }

            CompletionData completionData = default;
            ValueTask<ReadResult> result;
            lock (SyncObj)
            {
                _readerAwaitable.BeginOperation(token, s_signalReaderAwaitable, this);

                // If the awaitable is already complete then return the value result directly
                if (_readerAwaitable.IsCompleted)
                {
                    GetReadResult(out ReadResult readResult);

                    // Short circuit if we have the data or if we enter another terminal state
                    if (_unconsumedBytes >= minimumBytes || readResult.IsCanceled || readResult.IsCompleted)
                    {
                        return new ValueTask<ReadResult>(readResult);
                    }

                    // We don't have enough data so we need to reset the reader awaitable
                    _readerAwaitable.SetUncompleted();

                    // We also need to flip the reading state off
                    _operationState.EndRead();
                }

                // If the writer is currently paused and we are about the wait for more data then this would deadlock.
                // The writer is paused at the pause threshold but the reader needs a minimum amount in order to make progress.
                // We resume the writer so that we can unblock this read.
                if (!_writerAwaitable.IsCompleted)
                {
                    _writerAwaitable.Complete(out completionData);
                }

                // Set the minimum read bytes if we need to wait
                _minimumReadBytes = minimumBytes;

                // Otherwise it's async
                result = new ValueTask<ReadResult>(_reader, token: 0);
            }

            TrySchedule(WriterScheduler, in completionData);

            return result;
        }

        internal ValueTask<ReadResult> ReadAsync(CancellationToken token)
        {
            if (_readerCompletion.IsCompleted)
            {
                ThrowHelper.ThrowInvalidOperationException_NoReadingAllowed();
            }

            ValueTask<ReadResult> result;
            lock (SyncObj)
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
            lock (SyncObj)
            {
                if (_readerCompletion.IsCompleted)
                {
                    ThrowHelper.ThrowInvalidOperationException_NoReadingAllowed();
                }

                if (_unconsumedBytes > 0 || _readerAwaitable.IsCompleted)
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
            Action<object?> completion = completionData.Completion;
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
            lock (SyncObj)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                // Return all segments
                // if _readHead is null we need to try return _commitHead
                // because there might be a block allocated for writing
                BufferSegment? segment = _readHead ?? _readTail;
                while (segment != null)
                {
                    BufferSegment returnSegment = segment;
                    segment = segment.NextSegment;

                    returnSegment.ResetMemory();
                }

                _writingHead = null;
                _writingHeadMemory = default;
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

        internal void OnReadAsyncCompleted(Action<object?> continuation, object? state, ValueTaskSourceOnCompletedFlags flags)
        {
            CompletionData completionData;
            bool doubleCompletion;
            lock (SyncObj)
            {
                _readerAwaitable.OnCompleted(continuation, state, flags, out completionData, out doubleCompletion);
            }
            if (doubleCompletion)
            {
                Writer.Complete(ThrowHelper.CreateInvalidOperationException_NoConcurrentOperation());
            }
            TrySchedule(ReaderScheduler, completionData);
        }

        internal ReadResult GetReadAsyncResult()
        {
            ReadResult result;
            CancellationTokenRegistration cancellationTokenRegistration = default;
            CancellationToken cancellationToken = default;
            try
            {
                lock (SyncObj)
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
            }

            if (result.IsCanceled)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            return result;
        }

        private void GetReadResult(out ReadResult result)
        {
            bool isCompleted = _writerCompletion.IsCompletedOrThrow();
            bool isCanceled = _readerAwaitable.ObserveCancellation();

            // No need to read end if there is no head
            BufferSegment? head = _readHead;
            if (head != null)
            {
                Debug.Assert(_readTail != null);
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

            // Reset the minimum read bytes when read yields
            _minimumReadBytes = 0;
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
                lock (SyncObj)
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

            if (_readerCompletion.IsCompletedOrThrow())
            {
                return new ValueTask<FlushResult>(new FlushResult(isCanceled: false, isCompleted: true));
            }

            CompletionData completionData;
            ValueTask<FlushResult> result;

            lock (SyncObj)
            {
                // Allocate whatever the pool gives us so we can write, this also marks the
                // state as writing
                AllocateWriteHeadIfNeeded(0);

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

                PrepareFlush(out completionData, out result, cancellationToken);
            }

            TrySchedule(ReaderScheduler, completionData);
            return result;
        }

        private void WriteMultiSegment(ReadOnlySpan<byte> source)
        {
            Debug.Assert(_writingHead != null);
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

        internal void OnFlushAsyncCompleted(Action<object?> continuation, object? state, ValueTaskSourceOnCompletedFlags flags)
        {
            CompletionData completionData;
            bool doubleCompletion;
            lock (SyncObj)
            {
                _writerAwaitable.OnCompleted(continuation, state, flags, out completionData, out doubleCompletion);
            }
            if (doubleCompletion)
            {
                Reader.Complete(ThrowHelper.CreateInvalidOperationException_NoConcurrentOperation());
            }
            TrySchedule(WriterScheduler, completionData);
        }

        private void ReaderCancellationRequested()
        {
            CompletionData completionData;
            lock (SyncObj)
            {
                _readerAwaitable.CancellationTokenFired(out completionData);
            }
            TrySchedule(ReaderScheduler, completionData);
        }

        private void WriterCancellationRequested()
        {
            CompletionData completionData;
            lock (SyncObj)
            {
                _writerAwaitable.CancellationTokenFired(out completionData);
            }
            TrySchedule(WriterScheduler, completionData);
        }

        /// <summary>Gets the <see cref="System.IO.Pipelines.PipeReader" /> for this pipe.</summary>
        /// <value>A <see cref="System.IO.Pipelines.PipeReader" /> instance for this pipe.</value>
        public PipeReader Reader => _reader;

        /// <summary>Gets the <see cref="System.IO.Pipelines.PipeWriter" /> for this pipe.</summary>
        /// <value>A <see cref="System.IO.Pipelines.PipeWriter" /> instance for this pipe.</value>
        public PipeWriter Writer => _writer;

        /// <summary>Resets the pipe.</summary>
        public void Reset()
        {
            lock (SyncObj)
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
