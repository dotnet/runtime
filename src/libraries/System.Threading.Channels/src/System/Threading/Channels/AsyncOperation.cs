// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Threading.Channels
{
    /// <summary>Represents an asynchronous operation on a channel.</summary>
    internal abstract partial class AsyncOperation
    {
        /// <summary>Sentinel object used in a field to indicate the operation is available for use.</summary>
        protected static readonly Action<object?> s_availableSentinel = AvailableSentinel; // named method to help with debugging
        private static void AvailableSentinel(object? s) => Debug.Fail($"{nameof(AsyncOperation<>)}.{nameof(AvailableSentinel)} invoked with {s}");

        /// <summary>Sentinel object used in a field to indicate the operation has completed</summary>
        protected static readonly Action<object?> s_completedSentinel = CompletedSentinel; // named method to help with debugging
        private static void CompletedSentinel(object? s) => Debug.Fail($"{nameof(AsyncOperation<>)}.{nameof(CompletedSentinel)} invoked with {s}");

        /// <summary>Throws an exception indicating that the operation's result was accessed before the operation completed.</summary>
        protected static void ThrowIncompleteOperationException() =>
            throw new InvalidOperationException(SR.InvalidOperation_IncompleteAsyncOperation);

        /// <summary>Throws an exception indicating that multiple continuations can't be set for the same operation.</summary>
        protected static void ThrowMultipleContinuations() =>
            throw new InvalidOperationException(SR.InvalidOperation_MultipleContinuations);

        /// <summary>Throws an exception indicating that the operation was used after it was supposed to be used.</summary>
        protected static void ThrowIncorrectCurrentIdException() =>
            throw new InvalidOperationException(SR.InvalidOperation_IncorrectToken);

        /// <summary>Registration with a provided cancellation token.</summary>
        private readonly CancellationTokenRegistration _cancellationRegistration;

#if !NET
        /// <summary>Callback invoked when cancellation is requested.</summary>
        /// <remarks>
        /// This is not needed on .NET, where the CancellationToken.UnsafeRegister method accepts this signature directly.
        /// On .NET Framework / .NET Standard we need to proxy this via the overload that takes only the object instance.
        /// </remarks>
        private readonly Action<object?, CancellationToken> _cancellationCallback;
#endif

        /// <summary>true if this object is pooled and reused; otherwise, false.</summary>
        /// <remarks>
        /// If the operation is cancelable, then it can't be pooled.  And if it's poolable, there must never be race conditions to complete it,
        /// which is the main reason poolable objects can't be cancelable, as then cancellation could fire, the object could get reused,
        /// and then we may end up trying to complete an object that's used by someone else.
        /// </remarks>
        private protected readonly bool _pooled;

        /// <summary>Only relevant to cancelable operations; 0 if the operation hasn't had completion reserved, 1 if it has.</summary>
        private volatile
#if NET9_0_OR_GREATER
            bool
#else
            int
#endif
            _completionReserved;

        /// <summary>Any error that occurred during the operation.</summary>
        private protected ExceptionDispatchInfo? _error;

        /// <summary>The continuation callback.</summary>
        /// <remarks>
        /// This may be the completion sentinel if the operation has already completed.
        /// This may be the available sentinel if the operation is being pooled and is available for use.
        /// This may be null if the operation is pending.
        /// This may be another callback if the operation has had a callback hooked up with OnCompleted.
        /// </remarks>
        private protected Action<object?>? _continuation;

        /// <summary>State object to be passed to <see cref="_continuation"/>.</summary>
        private protected object? _continuationState;

        /// <summary>
        /// Null if no special context was found.
        /// ExecutionContext if one was captured due to needing to be flowed.
        /// A scheduler (TaskScheduler or SynchronizationContext) if one was captured and needs to be used for callback scheduling.
        /// Or a CapturedSchedulerAndExecutionContext if there's both an ExecutionContext and a scheduler.
        /// The most common and the fast path case to optimize for is null.
        /// </summary>
        private protected object? _capturedContext;

        /// <summary>The token value associated with the current operation.</summary>
        /// <remarks>
        /// IValueTaskSource operations on this instance are only valid if the provided token matches this value,
        /// which is incremented once GetResult is called to avoid multiple awaits on the same instance.
        /// </remarks>
        private protected short _currentId;

        /// <summary>Initializes the interactor.</summary>
        /// <param name="runContinuationsAsynchronously">true if continuations should be forced to run asynchronously; otherwise, false.</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
        /// <param name="pooled">Whether this instance is pooled and reused.</param>
        /// <param name="cancellationCallback">Callback to invoke if cancellation is requested.</param>
        protected AsyncOperation(
            bool runContinuationsAsynchronously,
            CancellationToken cancellationToken,
            bool pooled,
            Action<object?, CancellationToken>? cancellationCallback)
        {
            Debug.Assert(!pooled || !cancellationToken.CanBeCanceled);

            _continuation = pooled ? s_availableSentinel : null;
            _pooled = pooled;
            RunContinuationsAsynchronously = runContinuationsAsynchronously;

            if (cancellationToken.CanBeCanceled)
            {
                Debug.Assert(cancellationCallback is not null, "Expected a non-null cancellation callback when the token is cancelable");
                Debug.Assert(!_pooled, "Cancelable operations can't be pooled");
#if NET
                _cancellationRegistration = cancellationToken.UnsafeRegister(cancellationCallback, this);
#else
                _cancellationCallback = cancellationCallback;
                CancellationToken = cancellationToken;
                _cancellationRegistration = cancellationToken.Register(static s =>
                {
                    var thisRef = (AsyncOperation)s!;
                    thisRef._cancellationCallback(thisRef, thisRef.CancellationToken);
                }, this);
#endif
            }
        }

        /// <summary>Gets whether continuations should be forced to run asynchronously.</summary>
        public bool RunContinuationsAsynchronously { get; }

        /// <summary>Gets the cancellation token associated with this operation.</summary>
        private CancellationToken CancellationToken
#if NET
            => _cancellationRegistration.Token;
#else
            { get; }
#endif

        /// <summary>Gets whether the operation has completed.</summary>
        internal bool IsCompleted => ReferenceEquals(_continuation, s_completedSentinel);

        /// <summary>Completes the operation with a failed state and the specified error.</summary>
        /// <param name="exception">The error.</param>
        /// <returns>true if the operation could be successfully transitioned to a completed state; false if it was already completed.</returns>
        public bool TrySetException(Exception exception)
        {
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
        /// to a reserved completion state.
        /// </remarks>
        public bool TryReserveCompletionIfCancelable() =>
            !CancellationToken.CanBeCanceled ||
            Interlocked.Exchange(ref _completionReserved,
#if NET9_0_OR_GREATER
                true) == false;
#else
                1) == 0;
#endif

        /// <summary>Signals to a registered continuation that the operation has now completed.</summary>
        private protected void SignalCompletion()
        {
            Debug.Assert(
                !CancellationToken.CanBeCanceled ||
#if NET9_0_OR_GREATER
                _completionReserved);
#else
                _completionReserved == 1);
#endif

            // Unregister cancellation. It's fine to use CTR.Unregister rather than CTR.Dispose and not wait for any pending
            // callback, because if we're here completion has already been reserved. If this is being called as part of the
            // cancellation callback, then Dispose wouldn't wait, anyway. And if this is being called as part of TrySetResult/Exception,
            // then they've already reserved completion, so the cancellation callback (which starts with a TrySetCanceled) will
            // be a nop, as its TrySetCanceled will return false and the callback will exit without doing further work.
            Unregister(_cancellationRegistration);

            if (_continuation is not null || Interlocked.CompareExchange(ref _continuation, s_completedSentinel, null) is not null)
            {
                Debug.Assert(_continuation != s_completedSentinel, $"The continuation was the completion sentinel.");
                Debug.Assert(_continuation != s_availableSentinel, $"The continuation was the available sentinel.");

                object? ctx = _capturedContext;
                if (ctx is null or ExecutionContext)
                {
                    // There's no captured scheduling context.  If we're forced to run continuations asynchronously, queue it.
                    // Otherwise fall through to invoke it synchronously.
                    if (RunContinuationsAsynchronously)
                    {
                        UnsafeQueueSetCompletionAndInvokeContinuation();
                        return;
                    }
                }
                else
                {
                    SynchronizationContext? sc =
                        ctx as SynchronizationContext ??
                        (ctx as CapturedSchedulerAndExecutionContext)?._scheduler as SynchronizationContext;
                    if (sc is not null)
                    {
                        // There's a captured synchronization context.  If we're forced to run continuations asynchronously,
                        // or if there's a current synchronization context that's not the one we're targeting, queue it.
                        // Otherwise fall through to invoke it synchronously.
                        if (RunContinuationsAsynchronously || sc != SynchronizationContext.Current)
                        {
                            sc.Post(static s => ((AsyncOperation)s!).SetCompletionAndInvokeContinuation(), this);
                            return;
                        }
                    }
                    else
                    {
                        // There's a captured TaskScheduler.  If we're forced to run continuations asynchronously,
                        // or if there's a current scheduler that's not the one we're targeting, queue it.
                        // Otherwise fall through to invoke it synchronously.
                        TaskScheduler? ts =
                            ctx as TaskScheduler ??
                            (ctx as CapturedSchedulerAndExecutionContext)?._scheduler as TaskScheduler;
                        Debug.Assert(ts is not null, "Expected a TaskScheduler");
                        if (RunContinuationsAsynchronously || ts != TaskScheduler.Current)
                        {
                            Task.Factory.StartNew(static s => ((AsyncOperation)s!).SetCompletionAndInvokeContinuation(), this,
                                CancellationToken.None, TaskCreationOptions.DenyChildAttach, ts);
                            return;
                        }
                    }
                }

                // Invoke the continuation synchronously.
                SetCompletionAndInvokeContinuation();
            }
        }

        private void SetCompletionAndInvokeContinuation()
        {
            object? ctx = _capturedContext;
            ExecutionContext? ec =
                ctx is null ? null :
                ctx as ExecutionContext ??
                (ctx as CapturedSchedulerAndExecutionContext)?._executionContext;

            if (ec is null)
            {
                Action<object?> c = _continuation!;
                _continuation = s_completedSentinel;
                c(_continuationState);
            }
            else
            {
                ExecutionContext.Run(ec, static s =>
                {
                    var thisRef = (AsyncOperation)s!;
                    Action<object?> c = thisRef._continuation!;
                    thisRef._continuation = s_completedSentinel;
                    c(thisRef._continuationState);
                }, this);
            }
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
            if (_continuationState is not null)
            {
                ThrowMultipleContinuations();
            }
            _continuationState = state;

            // Capture the execution context if necessary.
            Debug.Assert(_capturedContext is null);
            if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0)
            {
                _capturedContext = ExecutionContext.Capture();
            }

            // Capture the scheduling context if necessary.
            SynchronizationContext? sc = null;
            TaskScheduler? ts = null;
            if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
            {
                sc = SynchronizationContext.Current;
                if (sc is not null && sc.GetType() != typeof(SynchronizationContext))
                {
                    _capturedContext = _capturedContext is null ?
                        sc :
                        new CapturedSchedulerAndExecutionContext(sc, (ExecutionContext)_capturedContext);
                }
                else
                {
                    sc = null;
                    ts = TaskScheduler.Current;
                    if (ts != TaskScheduler.Default)
                    {
                        _capturedContext = _capturedContext is null ?
                            ts :
                            new CapturedSchedulerAndExecutionContext(ts, (ExecutionContext)_capturedContext);
                    }
                    else
                    {
                        ts = null;
                    }
                }
            }

            // Try to set the provided continuation into _continuation.  If this succeeds, that means the operation
            // has not yet completed, and the completer will be responsible for invoking the callback.  If this fails,
            // that means the operation has already completed, and we must invoke the callback, but because we're still
            // inside the awaiter's OnCompleted method and we want to avoid possible stack dives, we must invoke
            // the continuation asynchronously rather than synchronously.
            Action<object?>? prevContinuation = Interlocked.CompareExchange(ref _continuation, continuation, null);
            if (prevContinuation is not null)
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
                if (_capturedContext is null)
                {
                    ChannelUtilities.UnsafeQueueUserWorkItem(continuation, state);
                }
                else if (sc is not null)
                {
                    sc.Post(static s =>
                    {
                        var t = (KeyValuePair<Action<object?>, object?>)s!;
                        t.Key(t.Value);
                    }, new KeyValuePair<Action<object?>, object?>(continuation, state));
                }
                else if (ts is not null)
                {
                    Debug.Assert(ts is not null);
                    Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, ts);
                }
                else
                {
                    Debug.Assert(_capturedContext is ExecutionContext);
                    ChannelUtilities.QueueUserWorkItem(continuation, state);
                }
            }
        }

        /// <summary>A tuple of both a non-null scheduler and a non-null ExecutionContext.</summary>
        private protected sealed class CapturedSchedulerAndExecutionContext
        {
            internal readonly object _scheduler;
            internal readonly ExecutionContext _executionContext;

            public CapturedSchedulerAndExecutionContext(object scheduler, ExecutionContext executionContext)
            {
                Debug.Assert(scheduler is SynchronizationContext or TaskScheduler, $"{nameof(scheduler)} is {scheduler}");
                Debug.Assert(executionContext is not null, $"{nameof(executionContext)} is null");

                _scheduler = scheduler;
                _executionContext = executionContext;
            }
        }
    }

    /// <summary>Represents an asynchronous operation on a channel.</summary>
    /// <typeparam name="TSelf">The type of this instance, ala the Curiously Recurring Template Pattern.</typeparam>
    internal abstract class AsyncOperation<TSelf> : AsyncOperation, IValueTaskSource
    {
        /// <inheritdoc />
        protected AsyncOperation(bool runContinuationsAsynchronously, CancellationToken cancellationToken = default, bool pooled = false, Action<object?, CancellationToken>? cancellationCallback = null) :
            base(runContinuationsAsynchronously, cancellationToken, pooled, cancellationCallback)
        {
        }

        /// <summary>Gets or sets the next operation in the linked list of operations.</summary>
        public TSelf? Next { get; set; }

        /// <summary>Gets or sets the previous operation in the linked list of operations.</summary>
        public TSelf? Previous { get; set; }

        /// <summary>Gets a <see cref="ValueTask"/> backed by this instance and its current token.</summary>
        public ValueTask ValueTask => new ValueTask(this, _currentId);

        /// <summary>Gets the current status of the operation.</summary>
        /// <param name="token">The token that must match <see cref="AsyncOperation._currentId"/>.</param>
        public ValueTaskSourceStatus GetStatus(short token)
        {
            if (_currentId != token)
            {
                ThrowIncorrectCurrentIdException();
            }

            return
                !IsCompleted ? ValueTaskSourceStatus.Pending :
                _error is null ? ValueTaskSourceStatus.Succeeded :
                _error.SourceException is OperationCanceledException ? ValueTaskSourceStatus.Canceled :
                ValueTaskSourceStatus.Faulted;
        }

        /// <summary>Gets the result of the operation.</summary>
        /// <param name="token">The token that must match <see cref="AsyncOperation._currentId"/>.</param>
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
    }

    /// <summary>Represents an asynchronous operation with a result on a channel.</summary>
    /// <typeparam name="TSelf">The type of this instance, ala the Curiously Recurring Template Pattern.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    internal abstract class AsyncOperation<TSelf, TResult> : AsyncOperation<TSelf>, IValueTaskSource<TResult>
        where TSelf : AsyncOperation<TSelf, TResult>
    {
        /// <summary>The result of the operation.</summary>
        private TResult? _result;

        /// <inheritdoc />
        public AsyncOperation(bool runContinuationsAsynchronously, CancellationToken cancellationToken = default, bool pooled = false, Action<object?, CancellationToken>? cancellationCallback = null) :
            base(runContinuationsAsynchronously, cancellationToken, pooled, cancellationCallback)
        {
        }

        /// <summary>Gets a <see cref="ValueTask{TResult}"/> backed by this instance and its current token.</summary>
        public ValueTask<TResult> ValueTaskOfT => new ValueTask<TResult>(this, _currentId);

        /// <summary>Gets the result of the operation.</summary>
        /// <param name="token">The token that must match <see cref="AsyncOperation._currentId"/>.</param>
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
            TResult? result = _result;
            _currentId++;

            if (_pooled)
            {
                Volatile.Write(ref _continuation, s_availableSentinel); // only after fetching all needed data
            }

            error?.Throw();
            return result!;
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
                _capturedContext = null;
                return true;
            }

            return false;
        }

        /// <summary>Completes the operation with a success state and the specified result.</summary>
        /// <param name="result">The result value.</param>
        /// <returns>true if the operation could be successfully transitioned to a completed state; false if it was already completed.</returns>
        public bool TrySetResult(TResult result)
        {
            if (TryReserveCompletionIfCancelable())
            {
                DangerousSetResult(result);
                return true;
            }

            return false;
        }

        /// <summary>Completes the operation with a success state and the specified result.</summary>
        /// <param name="result">The result value.</param>
        /// <remarks>This must only be called if the caller owns the right to complete this instance.</remarks>
        public void DangerousSetResult(TResult result)
        {
            _result = result;
            SignalCompletion();
        }
    }

    /// <summary>Represents a blocked reader from <see cref="ChannelReader{T}.ReadAsync"/>.</summary>
    /// <typeparam name="TResult">The type of the data read.</typeparam>
    internal sealed class BlockedReadAsyncOperation<TResult> : AsyncOperation<BlockedReadAsyncOperation<TResult>, TResult>
    {
        /// <inheritdoc />
        public BlockedReadAsyncOperation(bool runContinuationsAsynchronously, CancellationToken cancellationToken = default, bool pooled = false, Action<object?, CancellationToken>? cancellationCallback = null) :
            base(runContinuationsAsynchronously, cancellationToken, pooled, cancellationCallback)
        {
        }
    }

    /// <summary>Represents a blocked writer from <see cref="ChannelWriter{T}.WriteAsync"/>.</summary>
    /// <typeparam name="T">The type of the data written.</typeparam>
    internal sealed class BlockedWriteAsyncOperation<T> : AsyncOperation<BlockedWriteAsyncOperation<T>, VoidResult>
    {
        /// <inheritdoc />
        public BlockedWriteAsyncOperation(bool runContinuationsAsynchronously, CancellationToken cancellationToken = default, bool pooled = false, Action<object?, CancellationToken>? cancellationCallback = null) :
            base(runContinuationsAsynchronously, cancellationToken, pooled, cancellationCallback)
        {
        }

        /// <summary>Gets or sets the item being written.</summary>
        public T? Item { get; set; }
    }

    /// <summary>Represents a waiting reader from <see cref="ChannelReader{T}.WaitToReadAsync"/>.</summary>
    internal sealed class WaitingReadAsyncOperation : AsyncOperation<WaitingReadAsyncOperation, bool>
    {
        /// <inheritdoc />
        public WaitingReadAsyncOperation(bool runContinuationsAsynchronously, CancellationToken cancellationToken = default, bool pooled = false, Action<object?, CancellationToken>? cancellationCallback = null) :
            base(runContinuationsAsynchronously, cancellationToken, pooled, cancellationCallback)
        {
        }
    }

    /// <summary>Represents a waiting writer from <see cref="ChannelWriter{T}.WaitToWriteAsync"/>.</summary>
    internal sealed class WaitingWriteAsyncOperation : AsyncOperation<WaitingWriteAsyncOperation, bool>
    {
        /// <inheritdoc />
        public WaitingWriteAsyncOperation(bool runContinuationsAsynchronously, CancellationToken cancellationToken = default, bool pooled = false, Action<object?, CancellationToken>? cancellationCallback = null) :
            base(runContinuationsAsynchronously, cancellationToken, pooled, cancellationCallback)
        {
        }
    }
}
