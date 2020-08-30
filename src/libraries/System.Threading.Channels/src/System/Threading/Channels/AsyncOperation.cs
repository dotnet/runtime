// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Threading.Channels
{
    internal abstract class AsyncOperation
    {
        /// <summary>Sentinel object used in a field to indicate the operation is available for use.</summary>
        protected static readonly Action<object?> s_availableSentinel = AvailableSentinel; // named method to help with debugging
        private static void AvailableSentinel(object? s) => Debug.Fail($"{nameof(AsyncOperation)}.{nameof(AvailableSentinel)} invoked with {s}");

        /// <summary>Sentinel object used in a field to indicate the operation has completed.</summary>
        protected static readonly Action<object?> s_completedSentinel = CompletedSentinel; // named method to help with debugging
        private static void CompletedSentinel(object? s) => Debug.Fail($"{nameof(AsyncOperation)}.{nameof(CompletedSentinel)} invoked with {s}");

        /// <summary>Throws an exception indicating that the operation's result was accessed before the operation completed.</summary>
        protected static void ThrowIncompleteOperationException() =>
            throw new InvalidOperationException(SR.InvalidOperation_IncompleteAsyncOperation);

        /// <summary>Throws an exception indicating that multiple continuations can't be set for the same operation.</summary>
        protected static void ThrowMultipleContinuations() =>
            throw new InvalidOperationException(SR.InvalidOperation_MultipleContinuations);

        /// <summary>Throws an exception indicating that the operation was used after it was supposed to be used.</summary>
        protected static void ThrowIncorrectCurrentIdException() =>
            throw new InvalidOperationException(SR.InvalidOperation_IncorrectToken);
    }

    /// <summary>The representation of an asynchronous operation that has a result value.</summary>
    /// <typeparam name="TResult">Specifies the type of the result.  May be <see cref="VoidResult"/>.</typeparam>
    internal partial class AsyncOperation<TResult> : AsyncOperation, IValueTaskSource, IValueTaskSource<TResult>
    {
        /// <summary>Registration with a provided cancellation token.</summary>
        private readonly CancellationTokenRegistration _registration;
        /// <summary>true if this object is pooled and reused; otherwise, false.</summary>
        /// <remarks>
        /// If the operation is cancelable, then it can't be pooled.  And if it's poolable, there must never be race conditions to complete it,
        /// which is the main reason poolable objects can't be cancelable, as then cancellation could fire, the object could get reused,
        /// and then we may end up trying to complete an object that's used by someone else.
        /// </remarks>
        private readonly bool _pooled;
        /// <summary>Whether continuations should be forced to run asynchronously.</summary>
        private readonly bool _runContinuationsAsynchronously;

        /// <summary>Only relevant to cancelable operations; 0 if the operation hasn't had completion reserved, 1 if it has.</summary>
        private volatile int _completionReserved;
        /// <summary>The result of the operation.</summary>
        private TResult? _result;
        /// <summary>Any error that occurred during the operation.</summary>
        private ExceptionDispatchInfo? _error;
        /// <summary>The continuation callback.</summary>
        /// <remarks>
        /// This may be the completion sentinel if the operation has already completed.
        /// This may be the available sentinel if the operation is being pooled and is available for use.
        /// This may be null if the operation is pending.
        /// This may be another callback if the operation has had a callback hooked up with OnCompleted.
        /// </remarks>
        private Action<object?>? _continuation;
        /// <summary>State object to be passed to <see cref="_continuation"/>.</summary>
        private object? _continuationState;
        /// <summary>Scheduling context (a <see cref="SynchronizationContext"/> or <see cref="TaskScheduler"/>) to which to queue the continuation. May be null.</summary>
        private object? _schedulingContext;
        /// <summary>Execution context to use when invoking <see cref="_continuation"/>. May be null.</summary>
        private ExecutionContext? _executionContext;
        /// <summary>The token value associated with the current operation.</summary>
        /// <remarks>
        /// IValueTaskSource operations on this instance are only valid if the provided token matches this value,
        /// which is incremented once GetResult is called to avoid multiple awaits on the same instance.
        /// </remarks>
        private short _currentId;

        /// <summary>Initializes the interactor.</summary>
        /// <param name="runContinuationsAsynchronously">true if continuations should be forced to run asynchronously; otherwise, false.</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
        /// <param name="pooled">Whether this instance is pooled and reused.</param>
        public AsyncOperation(bool runContinuationsAsynchronously, CancellationToken cancellationToken = default, bool pooled = false)
        {
            _continuation = pooled ? s_availableSentinel : null;
            _pooled = pooled;
            _runContinuationsAsynchronously = runContinuationsAsynchronously;
            if (cancellationToken.CanBeCanceled)
            {
                Debug.Assert(!_pooled, "Cancelable operations can't be pooled");
                CancellationToken = cancellationToken;
                _registration = UnsafeRegister(cancellationToken, static s =>
                {
                    var thisRef = (AsyncOperation<TResult>)s!;
                    thisRef.TrySetCanceled(thisRef.CancellationToken);
                }, this);
            }
        }

        /// <summary>Gets or sets the next operation in the linked list of operations.</summary>
        public AsyncOperation<TResult>? Next { get; set; }
        /// <summary>Gets the cancellation token associated with this operation.</summary>
        public CancellationToken CancellationToken { get; }
        /// <summary>Gets a <see cref="ValueTask"/> backed by this instance and its current token.</summary>
        public ValueTask ValueTask => new ValueTask(this, _currentId);
        /// <summary>Gets a <see cref="ValueTask{TResult}"/> backed by this instance and its current token.</summary>
        public ValueTask<TResult> ValueTaskOfT => new ValueTask<TResult>(this, _currentId);

        /// <summary>Gets the current status of the operation.</summary>
        /// <param name="token">The token that must match <see cref="_currentId"/>.</param>
        public ValueTaskSourceStatus GetStatus(short token)
        {
            if (_currentId != token)
            {
                ThrowIncorrectCurrentIdException();
            }

            return
                !IsCompleted ? ValueTaskSourceStatus.Pending :
                _error == null ? ValueTaskSourceStatus.Succeeded :
                _error.SourceException is OperationCanceledException ? ValueTaskSourceStatus.Canceled :
                ValueTaskSourceStatus.Faulted;
        }

        /// <summary>Gets whether the operation has completed.</summary>
        internal bool IsCompleted => ReferenceEquals(_continuation, s_completedSentinel);

        /// <summary>Gets the result of the operation.</summary>
        /// <param name="token">The token that must match <see cref="_currentId"/>.</param>
        public TResult GetResult(short token)
        {
            if (_currentId != token)
            {
                ThrowIncorrectCurrentIdException();
            }

            if (!IsCompleted)
            {
                ThrowIncompleteOperationException();
            }

            ExceptionDispatchInfo? error = _error;
            TResult result = _result;
            _currentId++;

            if (_pooled)
            {
                Volatile.Write(ref _continuation, s_availableSentinel); // only after fetching all needed data
            }

            error?.Throw();
            return result!;
        }

        /// <summary>Gets the result of the operation.</summary>
        /// <param name="token">The token that must match <see cref="_currentId"/>.</param>
        void IValueTaskSource.GetResult(short token)
        {
            if (_currentId != token)
            {
                ThrowIncorrectCurrentIdException();
            }

            if (!IsCompleted)
            {
                ThrowIncompleteOperationException();
            }

            ExceptionDispatchInfo? error = _error;
            _currentId++;

            if (_pooled)
            {
                Volatile.Write(ref _continuation, s_availableSentinel); // only after fetching all needed data
            }

            error?.Throw();
        }

        /// <summary>Attempts to take ownership of the pooled instance.</summary>
        /// <returns>true if the instance is now owned by the caller, in which case its state has been reset; otherwise, false.</returns>
        public bool TryOwnAndReset()
        {
            Debug.Assert(_pooled, "Should only be used for pooled objects");
            if (ReferenceEquals(Interlocked.CompareExchange(ref _continuation, null, s_availableSentinel), s_availableSentinel))
            {
                _continuationState = null;
                _result = default;
                _error = null;
                _schedulingContext = null;
                _executionContext = null;
                return true;
            }

            return false;
        }

        /// <summary>Hooks up a continuation callback for when the operation has completed.</summary>
        /// <param name="continuation">The callback.</param>
        /// <param name="state">The state to pass to the callback.</param>
        /// <param name="token">The current token that must match <see cref="_currentId"/>.</param>
        /// <param name="flags">Flags that influence the behavior of the callback.</param>
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            if (_currentId != token)
            {
                ThrowIncorrectCurrentIdException();
            }

            // We need to store the state before the CompareExchange, so that if it completes immediately
            // after the CompareExchange, it'll find the state already stored.  If someone misuses this
            // and schedules multiple continuations erroneously, we could end up using the wrong state.
            // Make a best-effort attempt to catch such misuse.
            if (_continuationState != null)
            {
                ThrowMultipleContinuations();
            }
            _continuationState = state;

            // Capture the execution context if necessary.
            Debug.Assert(_executionContext == null);
            if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0)
            {
                _executionContext = ExecutionContext.Capture();
            }

            // Capture the scheduling context if necessary.
            Debug.Assert(_schedulingContext == null);
            SynchronizationContext? sc = null;
            TaskScheduler? ts = null;
            if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
            {
                sc = SynchronizationContext.Current;
                if (sc != null && sc.GetType() != typeof(SynchronizationContext))
                {
                    _schedulingContext = sc;
                }
                else
                {
                    sc = null;
                    ts = TaskScheduler.Current;
                    if (ts != TaskScheduler.Default)
                    {
                        _schedulingContext = ts;
                    }
                }
            }

            // Try to set the provided continuation into _continuation.  If this succeeds, that means the operation
            // has not yet completed, and the completer will be responsible for invoking the callback.  If this fails,
            // that means the operation has already completed, and we must invoke the callback, but because we're still
            // inside the awaiter's OnCompleted method and we want to avoid possible stack dives, we must invoke
            // the continuation asynchronously rather than synchronously.
            Action<object?>? prevContinuation = Interlocked.CompareExchange(ref _continuation, continuation, null);
            if (prevContinuation != null)
            {
                // If the set failed because there's already a delegate in _continuation, but that delegate is
                // something other than s_completedSentinel, something went wrong, which should only happen if
                // the instance was erroneously used, likely to hook up multiple continuations.
                Debug.Assert(IsCompleted, $"Expected IsCompleted");
                if (!ReferenceEquals(prevContinuation, s_completedSentinel))
                {
                    Debug.Assert(prevContinuation != s_availableSentinel, "Continuation was the available sentinel.");
                    ThrowMultipleContinuations();
                }

                // Queue the continuation.  We always queue here, even if !RunContinuationsAsynchronously, in order
                // to avoid stack diving; this path happens in the rare race when we're setting up to await and the
                // object is completed after the awaiter.IsCompleted but before the awaiter.OnCompleted.
                if (_schedulingContext == null)
                {
                    QueueUserWorkItem(continuation, state);
                }
                else if (sc != null)
                {
                    sc.Post(static s =>
                    {
                        var t = (Tuple<Action<object?>, object>)s!;
                        t.Item1(t.Item2);
                    }, Tuple.Create(continuation, state));
                }
                else
                {
                    Debug.Assert(ts != null);
                    Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, ts);
                }
            }
        }

        /// <summary>Unregisters from cancellation and returns whether cancellation already started.</summary>
        /// <returns>
        /// true if either the instance wasn't cancelable or cancellation successfully unregistered without cancellation having started.
        /// false if cancellation successfully unregistered after cancellation was initiated.
        /// </returns>
        /// <remarks>
        /// This is important for two reasons:
        /// 1. To avoid leaking a registration into a token, so it must be done prior to completing the operation.
        /// 2. To avoid having to worry about concurrent completion; once invoked, the caller can be guaranteed
        /// that no one else will try to complete the operation (assuming the caller is properly constructed
        /// and themselves guarantees only a single completer other than through cancellation).
        /// </remarks>
        public bool UnregisterCancellation()
        {
            if (CancellationToken.CanBeCanceled)
            {
                _registration.Dispose(); // Dispose rather than Unregister is important to know work has quiesced
                return _completionReserved == 0;
            }

            Debug.Assert(_registration == default);
            return true;
        }

        /// <summary>Completes the operation with a success state and the specified result.</summary>
        /// <param name="item">The result value.</param>
        /// <returns>true if the operation could be successfully transitioned to a completed state; false if it was already completed.</returns>
        public bool TrySetResult(TResult item)
        {
            UnregisterCancellation();

            if (TryReserveCompletionIfCancelable())
            {
                _result = item;
                SignalCompletion();
                return true;
            }

            return false;
        }

        /// <summary>Completes the operation with a failed state and the specified error.</summary>
        /// <param name="exception">The error.</param>
        /// <returns>true if the operation could be successfully transitioned to a completed state; false if it was already completed.</returns>
        public bool TrySetException(Exception exception)
        {
            UnregisterCancellation();

            if (TryReserveCompletionIfCancelable())
            {
                _error = ExceptionDispatchInfo.Capture(exception);
                SignalCompletion();
                return true;
            }

            return false;
        }

        /// <summary>Completes the operation with a failed state and a cancellation error.</summary>
        /// <param name="cancellationToken">The cancellation token that caused the cancellation.</param>
        /// <returns>true if the operation could be successfully transitioned to a completed state; false if it was already completed.</returns>
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            if (TryReserveCompletionIfCancelable())
            {
                _error = ExceptionDispatchInfo.Capture(new OperationCanceledException(cancellationToken));
                SignalCompletion();
                return true;
            }

            return false;
        }

        /// <summary>Attempts to reserve this instance for completion.</summary>
        /// <remarks>
        /// This will always return true for non-cancelable objects, as they only ever have a single owner
        /// responsible for completion.  For cancelable operations, this will attempt to atomically transition
        /// from Initialized to CompletionReserved.
        /// </remarks>
        private bool TryReserveCompletionIfCancelable() =>
            !CancellationToken.CanBeCanceled ||
            Interlocked.CompareExchange(ref _completionReserved, 1, 0) == 0;

        /// <summary>Signals to a registered continuation that the operation has now completed.</summary>
        private void SignalCompletion()
        {
            if (_continuation != null || Interlocked.CompareExchange(ref _continuation, s_completedSentinel, null) != null)
            {
                Debug.Assert(_continuation != s_completedSentinel, $"The continuation was the completion sentinel.");
                Debug.Assert(_continuation != s_availableSentinel, $"The continuation was the available sentinel.");

                if (_schedulingContext == null)
                {
                    // There's no captured scheduling context.  If we're forced to run continuations asynchronously, queue it.
                    // Otherwise fall through to invoke it synchronously.
                    if (_runContinuationsAsynchronously)
                    {
                        UnsafeQueueSetCompletionAndInvokeContinuation();
                        return;
                    }
                }
                else if (_schedulingContext is SynchronizationContext sc)
                {
                    // There's a captured synchronization context.  If we're forced to run continuations asynchronously,
                    // or if there's a current synchronization context that's not the one we're targeting, queue it.
                    // Otherwise fall through to invoke it synchronously.
                    if (_runContinuationsAsynchronously || sc != SynchronizationContext.Current)
                    {
                        sc.Post(static s => ((AsyncOperation<TResult>)s!).SetCompletionAndInvokeContinuation(), this);
                        return;
                    }
                }
                else
                {
                    // There's a captured TaskScheduler.  If we're forced to run continuations asynchronously,
                    // or if there's a current scheduler that's not the one we're targeting, queue it.
                    // Otherwise fall through to invoke it synchronously.
                    TaskScheduler ts = (TaskScheduler)_schedulingContext;
                    Debug.Assert(ts != null, "Expected a TaskScheduler");
                    if (_runContinuationsAsynchronously || ts != TaskScheduler.Current)
                    {
                        Task.Factory.StartNew(static s => ((AsyncOperation<TResult>)s!).SetCompletionAndInvokeContinuation(), this,
                            CancellationToken.None, TaskCreationOptions.DenyChildAttach, ts);
                        return;
                    }
                }

                // Invoke the continuation synchronously.
                SetCompletionAndInvokeContinuation();
            }
        }

        private void SetCompletionAndInvokeContinuation()
        {
            if (_executionContext == null)
            {
                Action<object?> c = _continuation!;
                _continuation = s_completedSentinel;
                c(_continuationState);
            }
            else
            {
                ExecutionContext.Run(_executionContext, static s =>
                {
                    var thisRef = (AsyncOperation<TResult>)s!;
                    Action<object?> c = thisRef._continuation!;
                    thisRef._continuation = s_completedSentinel;
                    c(thisRef._continuationState);
                }, this);
            }
        }
    }

    /// <summary>The representation of an asynchronous operation that has a result value and carries additional data with it.</summary>
    /// <typeparam name="TData">Specifies the type of data being written.</typeparam>
    internal sealed class VoidAsyncOperationWithData<TData> : AsyncOperation<VoidResult>
    {
        /// <summary>Initializes the interactor.</summary>
        /// <param name="runContinuationsAsynchronously">true if continuations should be forced to run asynchronously; otherwise, false.</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
        /// <param name="pooled">Whether this instance is pooled and reused.</param>
        public VoidAsyncOperationWithData(bool runContinuationsAsynchronously, CancellationToken cancellationToken = default, bool pooled = false) :
            base(runContinuationsAsynchronously, cancellationToken, pooled)
        {
        }

        /// <summary>The item being written.</summary>
        public TData? Item { get; set; }
    }
}
