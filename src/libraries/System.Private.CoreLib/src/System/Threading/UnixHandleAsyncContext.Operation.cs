// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Threading
{
    public sealed partial class UnixHandleAsyncContext
    {
        /// <summary>
        /// Represents an I/O operation that is triggered by an OS readiness notification.
        /// </summary>
        public abstract class Operation : IThreadPoolWorkItem
        {
            private enum State
            {
                Waiting = 0,
                Running,
                RunningWithPendingCancellation,
                RunningWithPendingAbort,
                Complete,
                Canceled,
                Aborted
            }

            private volatile State _state;

            // Node fields — set by Init before enqueuing.
            internal Operation? Next;
            internal CancellationTokenRegistration CancellationRegistration;
            internal bool IsReadOperation;
            internal ManualResetEventSlim? SyncEvent;
            private UnixHandleAsyncContext? _owner;

            /// <summary>
            /// Whether this node should be treated as inline by the poll thread.
            /// Sync nodes are always inline (they just signal the ManualResetEvent).
            /// Async nodes are inline when the owner's <see cref="UnixHandleAsyncContext.InlineCompletions"/> is set.
            /// </summary>
            internal bool CanInline => SyncEvent != null || _owner!.InlineCompletions;

            internal void Init(UnixHandleAsyncContext owner, bool isReadOperation)
            {
                Debug.Assert(_state == State.Waiting, $"Unexpected operation state: {_state}");
                _owner = owner;
                IsReadOperation = isReadOperation;
            }

            /// <summary>
            /// Clears fields that hold references to external objects so the operation
            /// can be safely returned to a pool without rooting the queue or the handle.
            /// </summary>
            internal void ResetForReuse()
            {
                _state = State.Waiting;
                Next = null;
                _owner = null;
                // CancellationRegistration is already disposed.
            }

            /// <summary>
            /// Dispatches readiness for this node.
            /// Sync nodes signal the waiting thread; inline nodes process on the poll thread;
            /// other nodes get scheduled on the thread pool.
            /// </summary>
            internal void DispatchProcess()
            {
                if (SyncEvent != null)
                {
                    // Sync operation. Signal waiting thread to try the I/O.
                    SyncEvent.Set();
                }
                else if (CanInline)
                {
                    if (IsReadOperation)
                        _owner!.ProcessRead(this);
                    else
                        _owner!.ProcessWrite(this);
                }
                else
                {
                    ProcessOnThreadPool();
                }
            }

            internal void ProcessOnThreadPool()
            {
                ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
            }

            void IThreadPoolWorkItem.Execute()
                => ExecuteThreadPoolWorkItem();

            protected virtual void ExecuteThreadPoolWorkItem()
            {
                if (IsReadOperation)
                    _owner!.ProcessRead(this);
                else
                    _owner!.ProcessWrite(this);
            }

            /// <summary>
            /// Aborts this node in the queue (called by Dispose).
            /// For sync nodes, signals the ManualResetEvent so the waiting thread wakes up
            /// and sees QueueState.Stopped in TryCompleteQueued.
            /// For async nodes, goes through the state machine to cancel and invoke the callback.
            /// </summary>
            internal bool Abort()
            {
                if (SyncEvent != null)
                {
                    // Wake up the thread. It will see the QueueState.Stopped in ProcessQueuedOperation.
                    SyncEvent.Set();
                    return true;
                }
                return TryCancel(abort: true);
            }

            internal AsyncResult TryExecute()
            {
                // Set state to Running, unless we've been canceled.
                State oldState = Interlocked.CompareExchange(ref _state, State.Running, State.Waiting);
                if (oldState is State.Canceled or State.Aborted)
                {
                    return AsyncResult.Aborted;
                }

                Debug.Assert(oldState == State.Waiting, $"Unexpected operation state: {oldState}");

                // Try to perform the I/O.
                try
                {
                    if (TryCompleteOperation(_owner!.Handle))
                    {
                        Debug.Assert(_state is State.Running or State.RunningWithPendingCancellation or State.RunningWithPendingAbort, $"Unexpected operation state: {_state}");

                        _state = State.Complete;

                        return AsyncResult.Completed;
                    }
                }
                catch (ObjectDisposedException)
                {
                    // The handle was disposed while TryCompleteOperation was running.
                    // This can happen when AbortAndDispose returns while an operation is in-flight
                    // and the caller disposes the handle before the operation finishes.
                    _state = State.RunningWithPendingAbort;
                }

                // Set state back to Waiting, unless we were canceled/aborted,
                // in which case we have to process that now.
                State newState;
                while (true)
                {
                    State state = _state;
                    Debug.Assert(state is State.Running or State.RunningWithPendingCancellation or State.RunningWithPendingAbort, $"Unexpected operation state: {state}");

                    newState = state switch
                    {
                        State.Running => State.Waiting,
                        State.RunningWithPendingCancellation => State.Canceled,
                        State.RunningWithPendingAbort => State.Aborted,
                        _ => state // unreachable
                    };
                    if (state == Interlocked.CompareExchange(ref _state, newState, state))
                    {
                        break;
                    }

                    // Race to update the state. Loop and try again.
                }

                if (newState is State.Canceled or State.Aborted)
                {
                    NotifyOnThreadPool();
                    return AsyncResult.Aborted;
                }

                return AsyncResult.Pending;
            }

            /// <summary>
            /// Attempts to cancel or abort the operation.
            /// Races with <see cref="TryExecute"/> via the state machine and invokes the
            /// continuation callback on success.
            /// </summary>
            /// <param name="abort">true to abort (handle closed); false to cancel (CancellationToken).</param>
            /// <returns>true if this call performed the cancellation; false if the operation already completed or is mid-execution.</returns>
            internal bool TryCancel(bool abort)
            {
                State intendedState = abort ? State.Aborted : State.Canceled;

                State newState;
                while (true)
                {
                    State state = _state;
                    if (state is State.Complete or State.Canceled or State.Aborted
                        or State.RunningWithPendingCancellation or State.RunningWithPendingAbort)
                    {
                        return false;
                    }

                    newState = state == State.Waiting
                        ? intendedState
                        : (abort ? State.RunningWithPendingAbort : State.RunningWithPendingCancellation);
                    if (state == Interlocked.CompareExchange(ref _state, newState, state))
                    {
                        break;
                    }

                    // Race to update the state. Loop and try again.
                }

                if (newState is State.RunningWithPendingCancellation or State.RunningWithPendingAbort)
                {
                    // TryExecute will either succeed, or it will see the pending cancellation and deal with it.
                    return false;
                }

                NotifyOnThreadPool();

                // Note, we leave the operation in the OperationQueue.
                // When we get around to processing it, we'll see it's cancelled and skip it.
                return true;
            }

            private void NotifyOnThreadPool()
            {
                ThreadPool.UnsafeQueueUserWorkItem(static s =>
                {
                    var op = (Operation)s!;
                    var result = op._state == State.Canceled ? OnCompletedResult.Canceled : OnCompletedResult.Aborted;
                    op.OnCompleted(result);
                }, this);
            }

            /// <summary>
            /// Performs the operation. Returns <see langword="true"/> if it completed, returns <see langword="false"/> if it is pending on the handle to become ready again.
            /// </summary>
            protected internal abstract bool TryCompleteOperation(SafeHandle handle);

            /// <summary>
            /// Called when the operation completes asynchronously.
            /// </summary>
            /// <remarks>The operation may be reused for another operation when <paramref name="result"/> is <see cref="OnCompletedResult.Completed"/>.</remarks>
            protected internal abstract void OnCompleted(OnCompletedResult result);
        }
    }
}
