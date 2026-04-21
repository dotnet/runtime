// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    /// <summary>
    /// Enables implementing asynchronous and synchronous I/O operations triggered by OS readiness notifications (epoll/kqueue).
    /// </summary>
    public sealed partial class PollableHandle : IDisposable
    {
        private enum QueueResult
        {
            Pending = 0,
            Completed = 1,
            Aborted = 2
        }

        // In debug builds, this struct guards against:
        // (1) Unexpected lock reentrancy, which should never happen
        // (2) Deadlock, by setting a reasonably large timeout
        private readonly struct LockToken : IDisposable
        {
            private readonly object _lockObject;

            public LockToken(object lockObject)
            {
                Debug.Assert(lockObject != null);

                _lockObject = lockObject;

                Debug.Assert(!Monitor.IsEntered(_lockObject));

#if DEBUG
                bool success = Monitor.TryEnter(_lockObject, 10000);
                Debug.Assert(success, "Timed out waiting for queue lock");
#else
                Monitor.Enter(_lockObject);
#endif
            }

            public void Dispose()
            {
                Debug.Assert(Monitor.IsEntered(_lockObject));
                Monitor.Exit(_lockObject);
            }
        }

        private struct OperationQueue
        {
            // Quick overview:
            //
            // When attempting to perform an IO operation, the caller first checks IsReady,
            // and if true, attempts to perform the operation itself.
            // If this returns EWOULDBLOCK, or if the queue was not ready, then the operation
            // is enqueued by calling StartAsyncOperation and the state becomes Waiting.
            // When an epoll notification is received, we check if the state is Waiting,
            // and if so, change the state to Processing and enqueue a workitem to the threadpool
            // to try to perform the enqueued operations.
            // If an operation is successfully performed, we remove it from the queue,
            // enqueue another threadpool workitem to process the next item in the queue (if any),
            // and call the user's completion callback.
            // If we successfully process all enqueued operations, then the state becomes Ready;
            // otherwise, the state becomes Waiting and we wait for another epoll notification.

            private enum QueueState : int
            {
                Ready = 0,          // Indicates that data MAY be available on the handle.
                                    // Queue must be empty.
                Waiting = 1,        // Indicates that data is definitely not available on the handle.
                                    // Queue must not be empty.
                Processing = 2,     // Indicates that a thread pool item has been scheduled (and may
                                    // be executing) to process the IO operations in the queue.
                                    // Queue must not be empty.
                Stopped = 3,        // Indicates that the queue has been stopped because the
                                    // instance has been disposed.
                                    // Queue must be empty.
            }

            // These fields define the queue state.

            private QueueState _state;
            private bool _nextMayBeInline;
            private int _sequenceNumber;
            private PollTriggeredOperation? _tail;

            private object _queueLock;

            internal LockToken Lock()
                => new LockToken(_queueLock);

            public bool NextMayBeInline
                => _nextMayBeInline;

            public bool IsStopped => _state == QueueState.Stopped;

            public void Init()
            {
                Debug.Assert(_queueLock == null);
                _queueLock = new object();

                _state = QueueState.Ready;
                _sequenceNumber = 0;
            }

            // IsReady returns whether an operation can be executed immediately.
            // observedSequenceNumber must be passed to StartAsyncOperation.
            public bool IsReady(out int observedSequenceNumber)
            {
                Debug.Assert(sizeof(QueueState) == sizeof(int));
                QueueState state = (QueueState)Volatile.Read(ref Unsafe.As<QueueState, int>(ref _state));
                observedSequenceNumber = Volatile.Read(ref _sequenceNumber);

                bool isReady = state == QueueState.Ready;
                if (!isReady)
                {
                    observedSequenceNumber--;
                }

                return isReady;
            }

            public PollOperationAsyncResult StartOperation(PollableHandle pollHandle, PollTriggeredOperation node, bool isReadOperation, int observedSequenceNumber, CancellationToken cancellationToken)
            {
                if (!pollHandle.IsRegistered && !pollHandle.Register(node))
                {
                    return PollOperationAsyncResult.Completed;
                }

                while (true)
                {
                    bool doAbort = false;
                    using (Lock())
                    {
                        switch (_state)
                        {
                            case QueueState.Ready:
                                if (observedSequenceNumber != _sequenceNumber)
                                {
                                    Debug.Assert(observedSequenceNumber - _sequenceNumber < 10000, "Very large sequence number increase???");
                                    observedSequenceNumber = _sequenceNumber;
                                    break;
                                }

                                _state = QueueState.Waiting;
                                goto case QueueState.Waiting;

                            case QueueState.Waiting:
                            case QueueState.Processing:
                                node.Init(pollHandle, isReadOperation);

                                if (_tail == null)
                                {
                                    Debug.Assert(!_nextMayBeInline);
                                    _nextMayBeInline = node.CanInline;
                                    node.Next = node;
                                }
                                else
                                {
                                    node.Next = _tail.Next;
                                    _tail.Next = node;
                                }

                                _tail = node;

                                if (cancellationToken.CanBeCanceled)
                                {
                                    node.CancellationRegistration = cancellationToken.UnsafeRegister(
                                        static s =>
                                        {
                                            var n = (PollTriggeredOperation)s!;
                                            n.CancellationRegistration.Dispose();
                                            n.TryCancel(abort: false);
                                        }, node);
                                }

                                return PollOperationAsyncResult.Pending;

                            case QueueState.Stopped:
                                Debug.Assert(_tail == null);
                                doAbort = true;
                                break;

                            default:
                                Environment.FailFast("unexpected queue state");
                                break;
                        }
                    }

                    if (doAbort)
                    {
                        return PollOperationAsyncResult.Aborted;
                    }

                    // Retry the operation.
                    // The node is not yet enqueued, so we can call TryCompleteOperation directly.
                    if (node.TryCompleteOperation(pollHandle.Handle))
                    {
                        return PollOperationAsyncResult.Completed;
                    }
                }
            }

            public PollTriggeredOperation? ProcessInlineOrGet(bool inlineOnly = false)
            {
                PollTriggeredOperation node;
                using (Lock())
                {
                    switch (_state)
                    {
                        case QueueState.Ready:
                            Debug.Assert(_tail == null, "State == Ready but queue is not empty!");
                            _sequenceNumber++;
                            return null;

                        case QueueState.Waiting:
                            Debug.Assert(_tail != null, "State == Waiting but queue is empty!");
                            node = _tail.Next!;
                            if (inlineOnly && !node.CanInline)
                            {
                                return node;
                            }

                            _state = QueueState.Processing;
                            break;

                        case QueueState.Processing:
                            Debug.Assert(_tail != null, "State == Processing but queue is empty!");
                            _sequenceNumber++;
                            return null;

                        case QueueState.Stopped:
                            Debug.Assert(_tail == null);
                            return null;

                        default:
                            Environment.FailFast("unexpected queue state");
                            return null;
                    }
                }

                if (node.CanInline)
                {
                    // Inline operation.  Process fully: TryExecute, remove from queue,
                    // Complete (if not canceled), and dispatch next.
                    node.DispatchProcess();
                    return null;
                }
                else
                {
                    // Async operation.  The caller will figure out how to process the IO.
                    Debug.Assert(!inlineOnly);
                    return node;
                }
            }

            public QueueResult TryCompleteQueued(PollTriggeredOperation node)
            {
                int observedSequenceNumber;
                using (Lock())
                {
                    if (_state == QueueState.Stopped)
                    {
                        Debug.Assert(_tail == null);
                        return QueueResult.Aborted;
                    }
                    else
                    {
                        Debug.Assert(_state == QueueState.Processing, $"_state={_state} while processing queue!");
                        Debug.Assert(_tail != null, "Unexpected empty queue while processing I/O");
                        Debug.Assert(node == _tail.Next, "Operation is not at head of queue???");
                        observedSequenceNumber = _sequenceNumber;
                    }
                }

                QueueResult result;
                while (true)
                {
                    PollOperationAsyncResult execResult = node.TryExecute();
                    if (execResult != PollOperationAsyncResult.Pending)
                    {
                        result = execResult == PollOperationAsyncResult.Completed
                            ? QueueResult.Completed
                            : QueueResult.Aborted;
                        break;
                    }

                    using (Lock())
                    {
                        if (_state == QueueState.Stopped)
                        {
                            Debug.Assert(_tail == null);
                            return QueueResult.Aborted;
                        }
                        else
                        {
                            Debug.Assert(_state == QueueState.Processing, $"_state={_state} while processing queue!");

                            if (observedSequenceNumber != _sequenceNumber)
                            {
                                Debug.Assert(observedSequenceNumber - _sequenceNumber < 10000, "Very large sequence number increase???");
                                observedSequenceNumber = _sequenceNumber;
                            }
                            else
                            {
                                _state = QueueState.Waiting;
                                return QueueResult.Pending;
                            }
                        }
                    }
                }

                // Remove the node from the queue and see if there's more to process.
                PollTriggeredOperation? nextNode = null;
                using (Lock())
                {
                    if (_state == QueueState.Stopped)
                    {
                        Debug.Assert(_tail == null);
                    }
                    else
                    {
                        Debug.Assert(_state == QueueState.Processing, $"_state={_state} while processing queue!");
                        Debug.Assert(_tail!.Next == node, "Queue modified while processing queue");

                        if (node == _tail)
                        {
                            _tail = null;
                            _nextMayBeInline = false;
                            _state = QueueState.Ready;
                            _sequenceNumber++;
                        }
                        else
                        {
                            nextNode = _tail.Next = node.Next;
                            _nextMayBeInline = nextNode!.CanInline;
                        }
                    }
                }

                nextNode?.DispatchProcess();

                Debug.Assert(result != QueueResult.Pending);
                return result;
            }

            // Removes a sync node from the queue on timeout and dispatches the next operation.
            public void CancelSyncAndContinue(PollTriggeredOperation node)
            {
                PollTriggeredOperation? nextNode = null;
                using (Lock())
                {
                    if (_state == QueueState.Stopped)
                    {
                        Debug.Assert(_tail == null);
                        return;
                    }

                    Debug.Assert(_tail != null, "Unexpected empty queue in CancelSyncAndContinue");

                    if (_tail.Next == node)
                    {
                        // We're the head of the queue.
                        if (node == _tail)
                        {
                            // No more operations.
                            _tail = null;
                            _nextMayBeInline = false;
                        }
                        else
                        {
                            // Pop current operation and advance to next.
                            _tail.Next = node.Next;
                            _nextMayBeInline = node.Next!.CanInline;
                        }

                        if (_state == QueueState.Processing)
                        {
                            // The queue has already handed off execution responsibility to us.
                            // We need to dispatch to the next op.
                            if (_tail == null)
                            {
                                _state = QueueState.Ready;
                                _sequenceNumber++;
                            }
                            else
                            {
                                nextNode = _tail.Next;
                            }
                        }
                        else if (_state == QueueState.Waiting)
                        {
                            if (_tail == null)
                            {
                                _state = QueueState.Ready;
                                _sequenceNumber++;
                            }
                        }
                    }
                    else
                    {
                        // We're not the head of the queue.
                        // Just find this op and remove it.
                        PollTriggeredOperation current = _tail.Next!;
                        while (current.Next != node)
                        {
                            current = current.Next!;
                        }

                        current.Next = node.Next;
                        if (_tail == node)
                        {
                            _tail = current;
                        }
                    }
                }

                nextNode?.DispatchProcess();
            }

            public bool StopAndAbort(out bool alreadyStopped)
            {
                bool aborted = false;

                using (Lock())
                {
                    alreadyStopped = _state == QueueState.Stopped;
                    if (alreadyStopped)
                    {
                        return false;
                    }

                    _state = QueueState.Stopped;

                    if (_tail != null)
                    {
                        PollTriggeredOperation node = _tail;
                        do
                        {
                            node.CancellationRegistration.Dispose();
                            aborted |= node.Abort();
                            node = node.Next!;
                        } while (node != _tail);
                    }

                    _tail = null;
                    _nextMayBeInline = false;
                }

                return aborted;
            }
        }

        private OperationQueue _readQueue;
        private OperationQueue _writeQueue;
        internal SafeHandle Handle { get; private set; }
        internal int ContextIndex = -1;

        internal bool IsDisposed => _writeQueue.IsStopped;

        private PollableHandle(SafeHandle handle)
        {
            Handle = handle;
            _readQueue.Init();
            _writeQueue.Init();
        }

        /// <summary>
        /// Creates a <see cref="PollableHandle"/> for the specified handle.
        /// </summary>
        /// <param name="handle">The OS handle to bind.</param>
        /// <param name="field">A reference to a field that will be atomically set to the new <see cref="PollableHandle"/>.</param>
        public static PollableHandle Create(SafeHandle handle, ref PollableHandle? field)
        {
            PollableHandle ph = new PollableHandle(handle);
            PollableHandle? existing = Interlocked.CompareExchange(ref field, ph, null);
            if (existing != null)
            {
                ph.Dispose();
                return existing;
            }
            return ph;
        }

        /// <summary>
        /// Gets or sets whether completion callbacks are processed inline or dispatched to the threadpool.
        /// </summary>
        public bool InlineCompletions { get; set; }

        /// <summary>
        /// Returns a sequence number to pass to <see cref="ReadAsync"/> or <see cref="ReadSync"/>.
        /// If the method returns <see langword="true"/>, the caller must first try executing the operation.
        /// If the operation is pending, then the caller calls the ReadAsync/ReadSync method to execute the operation.
        /// </summary>
        /// <param name="observedSequenceNumber">A sequence number to pass to <see cref="ReadAsync"/> or <see cref="ReadSync"/>.</param>
        /// <returns><see langword="true"/> if the handle is ready for reading; otherwise, <see langword="false"/>.</returns>
        /// <remarks>Returns <see langword="false"/> when the instance is disposed.</remarks>
        public bool IsReadReady(out int observedSequenceNumber)
            => _readQueue.IsReady(out observedSequenceNumber);

        /// <summary>
        /// Returns a sequence number to pass to <see cref="WriteAsync"/> or <see cref="WriteSync"/>.
        /// If the method returns <see langword="true"/>, the caller must first try executing the operation.
        /// If the operation is pending, then the caller calls the WriteAsync/WriteSync method to execute the operation.
        /// </summary>
        /// <param name="observedSequenceNumber">A sequence number to pass to <see cref="WriteAsync"/> or <see cref="WriteSync"/>.</param>
        /// <returns><see langword="true"/> if the handle is ready for writing; otherwise, <see langword="false"/>.</returns>
        /// <remarks>Returns <see langword="false"/> when the instance is disposed.</remarks>
        public bool IsWriteReady(out int observedSequenceNumber)
            => _writeQueue.IsReady(out observedSequenceNumber);

        /// <summary>
        /// Executes the read operation asynchronously. If the handle is not ready, returns <see cref="PollOperationAsyncResult.Pending"/>.
        /// <see cref="PollTriggeredOperation.OnCompleted"/> is then called when the operation completes/aborts/is cancelled.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="observedSequenceNumber">The sequence number obtained from <see cref="IsReadReady"/>.</param>
        /// <param name="cancellationToken">A token to cancel the pending operation.</param>
        /// <remarks>
        /// <para>The caller must first try to execute the operation after <see cref="IsReadReady"/> returns <see langword="false"/> before calling this method.</para>
        /// <para>The <paramref name="cancellationToken"/> is used when the operation executes asynchronously.
        /// If the token is cancelled, the operation completes with <see cref="PollOperationOnCompletedResult.Canceled"/>.</para>
        /// <para>Returns <see cref="PollOperationAsyncResult.Aborted"/> when the instance is disposed.</para>
        /// <para>The <paramref name="operation"/> may be reused for another operation when the method returns <see cref="PollOperationAsyncResult.Completed"/>.</para>
        /// </remarks>
        /// <returns>The result of the operation.</returns>
        public PollOperationAsyncResult ReadAsync(PollTriggeredOperation operation, int observedSequenceNumber, CancellationToken cancellationToken)
            => _readQueue.StartOperation(this, operation, isReadOperation: true, observedSequenceNumber, cancellationToken);

        /// <summary>
        /// Executes the write operation asynchronously. If the handle is not ready, returns <see cref="PollOperationAsyncResult.Pending"/>.
        /// <see cref="PollTriggeredOperation.OnCompleted"/> is then called when the operation completes/aborts/is cancelled.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="observedSequenceNumber">The sequence number obtained from <see cref="IsWriteReady"/>.</param>
        /// <param name="cancellationToken">A token to cancel the pending operation.</param>
        /// <remarks>
        /// <para>The caller must first try to execute the operation after <see cref="IsWriteReady"/> returns <see langword="false"/> before calling this method.</para>
        /// <para>The <paramref name="cancellationToken"/> is used when the operation executes asynchronously.
        /// If the token is cancelled, the operation completes with <see cref="PollOperationOnCompletedResult.Canceled"/>.</para>
        /// <para>Returns <see cref="PollOperationAsyncResult.Aborted"/> when the instance is disposed.</para>
        /// <para>The <paramref name="operation"/> may be reused for another operation when the method returns <see cref="PollOperationAsyncResult.Completed"/>.</para>
        /// </remarks>
        /// <returns>The result of the operation.</returns>
        public PollOperationAsyncResult WriteAsync(PollTriggeredOperation operation, int observedSequenceNumber, CancellationToken cancellationToken)
            => _writeQueue.StartOperation(this, operation, isReadOperation: false, observedSequenceNumber, cancellationToken);

        /// <summary>
        /// Executes the read operation synchronously. Returns when the operation completes/aborts/times out.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="observedSequenceNumber">The sequence number obtained from <see cref="IsReadReady"/>.</param>
        /// <param name="timeout">Timeout in milliseconds. Use -1 for infinite timeout.</param>
        /// <remarks>
        /// <para>The caller must first try to execute the operation after <see cref="IsReadReady"/> returns <see langword="false"/> before calling this method.</para>
        /// <para>Returns <see cref="PollOperationSyncResult.Aborted"/> when the instance is disposed.</para>
        /// <para>The <paramref name="operation"/> may be reused for another operation when the method returns <see cref="PollOperationSyncResult.Completed"/> or <see cref="PollOperationSyncResult.TimedOut"/>.</para>
        /// </remarks>
        /// <returns>The result of the operation.</returns>
        public PollOperationSyncResult ReadSync(PollTriggeredOperation operation, int observedSequenceNumber, int timeout)
            => ExecuteSync(ref _readQueue, operation, isReadOperation: true, observedSequenceNumber, timeout);

        /// <summary>
        /// Executes the write operation synchronously. Returns when the operation completes/aborts/times out.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="observedSequenceNumber">The sequence number obtained from <see cref="IsWriteReady"/>.</param>
        /// <param name="timeout">Timeout in milliseconds. Use -1 for infinite timeout.</param>
        /// <remarks>
        /// <para>The caller must first try to execute the operation after <see cref="IsWriteReady"/> returns <see langword="false"/> before calling this method.</para>
        /// <para>Returns <see cref="PollOperationSyncResult.Aborted"/> when the instance is disposed.</para>
        /// <para>The <paramref name="operation"/> may be reused for another operation when the method returns <see cref="PollOperationSyncResult.Completed"/> or <see cref="PollOperationSyncResult.TimedOut"/>.</para>
        /// </remarks>
        /// <returns>The result of the operation.</returns>
        public PollOperationSyncResult WriteSync(PollTriggeredOperation operation, int observedSequenceNumber, int timeout)
            => ExecuteSync(ref _writeQueue, operation, isReadOperation: false, observedSequenceNumber, timeout);

        private PollOperationSyncResult ExecuteSync(ref OperationQueue queue, PollTriggeredOperation operation, bool isReadOperation, int observedSequenceNumber, int timeout)
        {
            Debug.Assert(timeout == -1 || timeout > 0, $"Unexpected timeout: {timeout}");

            PollOperationSyncResult result;

            using var syncEvent = new ManualResetEventSlim(false, 0);
            operation.SyncEvent = syncEvent;

            PollOperationAsyncResult startResult = queue.StartOperation(this, operation, isReadOperation, observedSequenceNumber, default);
            if (startResult != PollOperationAsyncResult.Pending)
            {
                // Completed synchronously or aborted.
                result = (PollOperationSyncResult)startResult;
            }
            else
            {
                // Node is queued. Wait for readiness signals from the poll thread.
                while (true)
                {
                    long waitStart = Stopwatch.GetTimestamp();

                    if (!syncEvent.Wait(timeout))
                    {
                        // Timeout expired. Remove node from queue.
                        queue.CancelSyncAndContinue(operation);
                        result = PollOperationSyncResult.TimedOut;
                        break;
                    }

                    syncEvent.Reset();

                    QueueResult queueResult = queue.TryCompleteQueued(operation);
                    if (queueResult == QueueResult.Completed)
                    {
                        result = PollOperationSyncResult.Completed;
                        break;
                    }

                    if (queueResult == QueueResult.Aborted)
                    {
                        result = PollOperationSyncResult.Aborted;
                        break;
                    }

                    // EAGAIN (Pending). Adjust timeout and retry.
                    if (timeout > 0)
                    {
                        timeout -= (int)Stopwatch.GetElapsedTime(waitStart).TotalMilliseconds;
                        if (timeout <= 0)
                        {
                            queue.CancelSyncAndContinue(operation);
                            result = PollOperationSyncResult.TimedOut;
                            break;
                        }
                    }
                }
            }

            operation.SyncEvent = null;

            // The operation is in a poolable state, but we don't call ResetForReuse:
            // the Aborted result documents that the operation must not be pooled.
            // Most users don't have a use for pooling on Abort either.
            if (result != PollOperationSyncResult.Aborted)
            {
                operation.ResetForReuse();
            }

            return result;
        }

        internal void ProcessRead(PollTriggeredOperation node)
            => Process(ref _readQueue, node);
        internal void ProcessWrite(PollTriggeredOperation node)
            => Process(ref _writeQueue, node);

        private static void Process(ref OperationQueue queue, PollTriggeredOperation node)
        {
            QueueResult result = queue.TryCompleteQueued(node);

            if (result != QueueResult.Pending)
            {
                node.CancellationRegistration.Dispose();

                if (result == QueueResult.Completed)
                {
                    node.ResetForReuse();
                    node.OnCompleted(PollOperationOnCompletedResult.Completed);
                }
            }
        }

        /// <summary>
        /// Aborts all pending operations and Disposes the PollableHandle.
        /// </summary>
        /// <returns><see langword="true"/> if any operations were aborted; otherwise, <see langword="false"/>.</returns>
        public bool AbortAndDispose()
        {
            bool aborted = false;

            aborted |= _writeQueue.StopAndAbort(out bool alreadyStopped);
            Debug.Assert(IsDisposed); // Due to stopping the write queue.

            // Only unregister once.
            if (alreadyStopped)
            {
                return false;
            }

            aborted |= _readQueue.StopAndAbort(out _);

            Unregister();

            return aborted;
        }

        // Called on the epoll thread, speculatively tries to process inline events and errors,
        // and returns any remaining events that remain to be processed.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Interop.Sys.HandleEvents ProcessInlineSpeculatively(Interop.Sys.HandleEvents events)
        {
            if ((events & Interop.Sys.HandleEvents.Error) != 0)
            {
                events ^= Interop.Sys.HandleEvents.Error;
                events |= Interop.Sys.HandleEvents.Read | Interop.Sys.HandleEvents.Write;
            }

            if ((events & Interop.Sys.HandleEvents.Read) != 0 &&
                _readQueue.NextMayBeInline &&
                _readQueue.ProcessInlineOrGet(inlineOnly: true) == null)
            {
                events ^= Interop.Sys.HandleEvents.Read;
            }

            if ((events & Interop.Sys.HandleEvents.Write) != 0 &&
                _writeQueue.NextMayBeInline &&
                _writeQueue.ProcessInlineOrGet(inlineOnly: true) == null)
            {
                events ^= Interop.Sys.HandleEvents.Write;
            }

            return events;
        }

        internal void HandleEventsInline(Interop.Sys.HandleEvents events)
        {
            if ((events & Interop.Sys.HandleEvents.Error) != 0)
            {
                events ^= Interop.Sys.HandleEvents.Error;
                events |= Interop.Sys.HandleEvents.Read | Interop.Sys.HandleEvents.Write;
            }

            if ((events & Interop.Sys.HandleEvents.Read) != 0)
            {
                PollTriggeredOperation? receiveNode = _readQueue.ProcessInlineOrGet();
                if (receiveNode != null) ProcessRead(receiveNode);
            }

            if ((events & Interop.Sys.HandleEvents.Write) != 0)
            {
                PollTriggeredOperation? sendNode = _writeQueue.ProcessInlineOrGet();
                if (sendNode != null) ProcessWrite(sendNode);
            }
        }

        internal void HandleEventsOnThreadPool(Interop.Sys.HandleEvents events)
        {
            Debug.Assert((events & Interop.Sys.HandleEvents.Error) == 0);

            PollTriggeredOperation? receiveNode =
                (events & Interop.Sys.HandleEvents.Read) != 0 ? _readQueue.ProcessInlineOrGet() : null;
            PollTriggeredOperation? sendNode =
                (events & Interop.Sys.HandleEvents.Write) != 0 ? _writeQueue.ProcessInlineOrGet() : null;

            if (sendNode == null)
            {
                if (receiveNode != null) ProcessRead(receiveNode);
            }
            else
            {
                receiveNode?.ProcessOnThreadPool();
                ProcessWrite(sendNode);
            }
        }

        /// <summary>
        /// Calls <see cref="AbortAndDispose"/>.
        /// </summary>
        public void Dispose()
        {
            AbortAndDispose();
        }
    }
}
