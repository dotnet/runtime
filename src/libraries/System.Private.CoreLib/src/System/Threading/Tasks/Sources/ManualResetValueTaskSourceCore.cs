// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace System.Threading.Tasks.Sources
{
    /// <summary>Provides the core logic for implementing a manual-reset <see cref="IValueTaskSource"/> or <see cref="IValueTaskSource{TResult}"/>.</summary>
    /// <typeparam name="TResult">Specifies the type of results of the operation represented by this instance.</typeparam>
    [StructLayout(LayoutKind.Auto)]
    public struct ManualResetValueTaskSourceCore<TResult>
    {
        /// <summary>
        /// The callback to invoke when the operation completes if <see cref="OnCompleted"/> was called before the operation completed,
        /// or <see cref="ManualResetValueTaskSourceCoreShared.s_sentinel"/> if the operation completed before a callback was supplied,
        /// or null if a callback hasn't yet been provided and the operation hasn't yet completed.
        /// </summary>
        private Action<object?>? _continuation;
        /// <summary>State to pass to <see cref="_continuation"/>.</summary>
        private object? _continuationState;
        /// <summary>
        /// Null if no special context was found.
        /// ExecutionContext if one was captured due to needing to be flowed.
        /// A scheduler (TaskScheduler or SynchronizationContext) if one was captured and needs to be used for callback scheduling.
        /// Or a CapturedContext if there's both an ExecutionContext and a scheduler.
        /// The most common and the fast path case to optimize for is null.
        /// </summary>
        private object? _capturedContext;
        /// <summary>The exception with which the operation failed, or null if it hasn't yet completed or completed successfully.</summary>
        private ExceptionDispatchInfo? _error;
        /// <summary>The result with which the operation succeeded, or the default value if it hasn't yet completed or failed.</summary>
        private TResult? _result;
        /// <summary>The current version of this value, used to help prevent misuse.</summary>
        private short _version;
        /// <summary>Whether the current operation has completed.</summary>
        private bool _completed;
        /// <summary>Whether to force continuations to run asynchronously.</summary>
        private bool _runContinuationsAsynchronously;

        /// <summary>Gets or sets whether to force continuations to run asynchronously.</summary>
        /// <remarks>Continuations may run asynchronously if this is false, but they'll never run synchronously if this is true.</remarks>
        public bool RunContinuationsAsynchronously
        {
            get => _runContinuationsAsynchronously;
            set => _runContinuationsAsynchronously = value;
        }

        /// <summary>Resets to prepare for the next operation.</summary>
        public void Reset()
        {
            // Reset/update state for the next use/await of this instance.
            _version++;
            _continuation = null;
            _continuationState = null;
            _capturedContext = null;
            _error = null;
            _result = default;
            _completed = false;
            _runContinuationsAsynchronously = false;
        }

        /// <summary>Completes with a successful result.</summary>
        /// <param name="result">The result.</param>
        public void SetResult(TResult result)
        {
            _result = result;
            SignalCompletion();
        }

        /// <summary>Completes with an error.</summary>
        /// <param name="error">The exception.</param>
        public void SetException(Exception error)
        {
            _error = ExceptionDispatchInfo.Capture(error);
            SignalCompletion();
        }

        /// <summary>Gets the operation version.</summary>
        public short Version => _version;

        /// <summary>Gets the status of the operation.</summary>
        /// <param name="token">Opaque value that was provided to the <see cref="ValueTask"/>'s constructor.</param>
        public ValueTaskSourceStatus GetStatus(short token)
        {
            ValidateToken(token);
            return
                Volatile.Read(ref _continuation) is null || !_completed ? ValueTaskSourceStatus.Pending :
                _error is null ? ValueTaskSourceStatus.Succeeded :
                _error.SourceException is OperationCanceledException ? ValueTaskSourceStatus.Canceled :
                ValueTaskSourceStatus.Faulted;
        }

        /// <summary>Gets the result of the operation.</summary>
        /// <param name="token">Opaque value that was provided to the <see cref="ValueTask"/>'s constructor.</param>
        [StackTraceHidden]
        public TResult GetResult(short token)
        {
            if (token != _version || !_completed || _error is not null)
            {
                ThrowForFailedGetResult();
            }

            return _result!;
        }

        /// <summary>Throws an exception in response to a failed <see cref="GetResult"/>.</summary>
        [StackTraceHidden]
        private void ThrowForFailedGetResult()
        {
            _error?.Throw();
            throw new InvalidOperationException(); // not using ThrowHelper.ThrowInvalidOperationException so that the JIT sees ThrowForFailedGetResult as always throwing
        }

        /// <summary>Schedules the continuation action for this operation.</summary>
        /// <param name="continuation">The continuation to invoke when the operation has completed.</param>
        /// <param name="state">The state object to pass to <paramref name="continuation"/> when it's invoked.</param>
        /// <param name="token">Opaque value that was provided to the <see cref="ValueTask"/>'s constructor.</param>
        /// <param name="flags">The flags describing the behavior of the continuation.</param>
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            if (continuation is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.continuation);
            }
            ValidateToken(token);

            if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0)
            {
                _capturedContext = ExecutionContext.Capture();
            }

            if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
            {
                if (SynchronizationContext.Current is SynchronizationContext sc &&
                    sc.GetType() != typeof(SynchronizationContext))
                {
                    _capturedContext = _capturedContext is null ?
                        sc :
                        new CapturedSchedulerAndExecutionContext(sc, (ExecutionContext)_capturedContext);
                }
                else
                {
                    TaskScheduler ts = TaskScheduler.Current;
                    if (ts != TaskScheduler.Default)
                    {
                        _capturedContext = _capturedContext is null ?
                            ts :
                            new CapturedSchedulerAndExecutionContext(ts, (ExecutionContext)_capturedContext);
                    }
                }
            }

            // We need to set the continuation state before we swap in the delegate, so that
            // if there's a race between this and SetResult/Exception and SetResult/Exception
            // sees the _continuation as non-null, it'll be able to invoke it with the state
            // stored here.  However, this also means that if this is used incorrectly (e.g.
            // awaited twice concurrently), _continuationState might get erroneously overwritten.
            // To minimize the chances of that, we check preemptively whether _continuation
            // is already set to something other than the completion sentinel.
            object? storedContinuation = _continuation;
            if (storedContinuation is null)
            {
                _continuationState = state;
                storedContinuation = Interlocked.CompareExchange(ref _continuation, continuation, null);
                if (storedContinuation is null)
                {
                    // Operation hadn't already completed, so we're done. The continuation will be
                    // invoked when SetResult/Exception is called at some later point.
                    return;
                }
            }

            // Operation already completed, so we need to queue the supplied callback.
            // At this point the storedContinuation should be the sentinal; if it's not, the instance was misused.
            Debug.Assert(storedContinuation is not null, $"{nameof(storedContinuation)} is null");
            if (!ReferenceEquals(storedContinuation, ManualResetValueTaskSourceCoreShared.s_sentinel))
            {
                ThrowHelper.ThrowInvalidOperationException();
            }

            object? capturedContext = _capturedContext;
            switch (capturedContext)
            {
                case null:
                    ThreadPool.UnsafeQueueUserWorkItem(continuation, state, preferLocal: true);
                    break;

                case ExecutionContext:
                    ThreadPool.QueueUserWorkItem(continuation, state, preferLocal: true);
                    break;

                default:
                    ManualResetValueTaskSourceCoreShared.ScheduleCapturedContext(capturedContext, continuation, state);
                    break;
            }
        }

        /// <summary>Ensures that the specified token matches the current version.</summary>
        /// <param name="token">The token supplied by <see cref="ValueTask"/>.</param>
        private void ValidateToken(short token)
        {
            if (token != _version)
            {
                ThrowHelper.ThrowInvalidOperationException();
            }
        }

        /// <summary>Signals that the operation has completed.  Invoked after the result or error has been set.</summary>
        private void SignalCompletion()
        {
            if (_completed)
            {
                ThrowHelper.ThrowInvalidOperationException();
            }
            _completed = true;

            Action<object?>? continuation =
                Volatile.Read(ref _continuation) ??
                Interlocked.CompareExchange(ref _continuation, ManualResetValueTaskSourceCoreShared.s_sentinel, null);

            if (continuation is not null)
            {
                Debug.Assert(continuation is not null, $"{nameof(continuation)} is null");

                object? context = _capturedContext;
                if (context is null)
                {
                    if (_runContinuationsAsynchronously)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(continuation, _continuationState, preferLocal: true);
                    }
                    else
                    {
                        continuation(_continuationState);
                    }
                }
                else if (context is ExecutionContext or CapturedSchedulerAndExecutionContext)
                {
                    ManualResetValueTaskSourceCoreShared.InvokeContinuationWithContext(context, continuation, _continuationState, _runContinuationsAsynchronously);
                }
                else
                {
                    Debug.Assert(context is TaskScheduler or SynchronizationContext, $"context is {context}");
                    ManualResetValueTaskSourceCoreShared.ScheduleCapturedContext(context, continuation, _continuationState);
                }
            }
        }
    }

    /// <summary>A tuple of both a non-null scheduler and a non-null ExecutionContext.</summary>
    internal sealed class CapturedSchedulerAndExecutionContext
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

    internal static class ManualResetValueTaskSourceCoreShared // separated out of generic to avoid unnecessary duplication
    {
        internal static readonly Action<object?> s_sentinel = CompletionSentinel;

        private static void CompletionSentinel(object? _) // named method to aid debugging
        {
            Debug.Fail("The sentinel delegate should never be invoked.");
            ThrowHelper.ThrowInvalidOperationException();
        }

        internal static void ScheduleCapturedContext(object context, Action<object?> continuation, object? state)
        {
            Debug.Assert(
                context is SynchronizationContext or TaskScheduler or CapturedSchedulerAndExecutionContext,
                $"{nameof(context)} is {context}");

            switch (context)
            {
                case SynchronizationContext sc:
                    ScheduleSynchronizationContext(sc, continuation, state);
                    break;

                case TaskScheduler ts:
                    ScheduleTaskScheduler(ts, continuation, state);
                    break;

                default:
                    CapturedSchedulerAndExecutionContext cc = (CapturedSchedulerAndExecutionContext)context;
                    if (cc._scheduler is SynchronizationContext ccsc)
                    {
                        ScheduleSynchronizationContext(ccsc, continuation, state);
                    }
                    else
                    {
                        Debug.Assert(cc._scheduler is TaskScheduler, $"{nameof(cc._scheduler)} is {cc._scheduler}");
                        ScheduleTaskScheduler((TaskScheduler)cc._scheduler, continuation, state);
                    }
                    break;
            }

            static void ScheduleSynchronizationContext(SynchronizationContext sc, Action<object?> continuation, object? state) =>
                sc.Post(continuation.Invoke, state);

            static void ScheduleTaskScheduler(TaskScheduler scheduler, Action<object?> continuation, object? state) =>
                Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, scheduler);
        }

        internal static void InvokeContinuationWithContext(object capturedContext, Action<object?> continuation, object? continuationState, bool runContinuationsAsynchronously)
        {
            // This is in a helper as the error handling causes the generated asm
            // for the surrounding code to become less efficient (stack spills etc)
            // and it is an uncommon path.
            Debug.Assert(continuation is not null, $"{nameof(continuation)} is null");
            Debug.Assert(capturedContext is ExecutionContext or CapturedSchedulerAndExecutionContext, $"{nameof(capturedContext)} is {capturedContext}");

            // Capture the current EC.  We'll switch over to the target EC and then restore back to this one.
            ExecutionContext? currentContext = ExecutionContext.CaptureForRestore();

            if (capturedContext is ExecutionContext ec)
            {
                ExecutionContext.RestoreInternal(ec); // Restore the captured ExecutionContext before executing anything.
                if (runContinuationsAsynchronously)
                {
                    try
                    {
                        ThreadPool.QueueUserWorkItem(continuation, continuationState, preferLocal: true);
                    }
                    finally
                    {
                        ExecutionContext.RestoreInternal(currentContext); // Restore the current ExecutionContext.
                    }
                }
                else
                {
                    // Running inline may throw; capture the edi if it does as we changed the ExecutionContext,
                    // so need to restore it back before propagating the throw.
                    ExceptionDispatchInfo? edi = null;
                    SynchronizationContext? syncContext = SynchronizationContext.Current;
                    try
                    {
                        continuation(continuationState);
                    }
                    catch (Exception ex)
                    {
                        // Note: we have a "catch" rather than a "finally" because we want
                        // to stop the first pass of EH here.  That way we can restore the previous
                        // context before any of our callers' EH filters run.
                        edi = ExceptionDispatchInfo.Capture(ex);
                    }
                    finally
                    {
                        // Set sync context back to what it was prior to coming in.
                        // Then restore the current ExecutionContext.
                        SynchronizationContext.SetSynchronizationContext(syncContext);
                        ExecutionContext.RestoreInternal(currentContext);
                    }

                    // Now rethrow the exception; if there is one.
                    edi?.Throw();
                }
            }
            else
            {
                CapturedSchedulerAndExecutionContext cc = (CapturedSchedulerAndExecutionContext)capturedContext;
                ExecutionContext.Restore(cc._executionContext); // Restore the captured ExecutionContext before executing anything.
                try
                {
                    ScheduleCapturedContext(capturedContext, continuation, continuationState);
                }
                finally
                {
                    ExecutionContext.RestoreInternal(currentContext); // Restore the current ExecutionContext.
                }
            }
        }
    }
}
