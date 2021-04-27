// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks.Sources;

namespace System.IO.Pipelines
{
    [DebuggerDisplay("CanceledState: {_awaitableState}, IsCompleted: {IsCompleted}")]
    internal struct PipeAwaitable
    {
        private AwaitableState _awaitableState;
        private Action<object?>? _completion;
        private object? _completionState;
        // It's rare to have to capture custom context here
        private SchedulingContext? _schedulingContext;
        private CancellationTokenRegistration _cancellationTokenRegistration;

#if (!NETSTANDARD2_0 && !NETFRAMEWORK)
        private CancellationToken CancellationToken => _cancellationTokenRegistration.Token;
#else
        private CancellationToken _cancellationToken;
        private CancellationToken CancellationToken => _cancellationToken;
#endif

        public PipeAwaitable(bool completed, bool useSynchronizationContext)
        {
            _awaitableState = (completed ? AwaitableState.Completed : AwaitableState.None) |
                              (useSynchronizationContext ? AwaitableState.UseSynchronizationContext : AwaitableState.None);
            _completion = null;
            _completionState = null;
            _cancellationTokenRegistration = default;
            _schedulingContext = null;
#if (NETSTANDARD2_0 || NETFRAMEWORK)
            _cancellationToken = CancellationToken.None;
#endif
        }

        public bool IsCompleted => (_awaitableState & (AwaitableState.Completed | AwaitableState.Canceled)) != 0;

        public bool IsRunning => (_awaitableState & AwaitableState.Running) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginOperation(CancellationToken cancellationToken, Action<object?> callback, object? state)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _awaitableState |= AwaitableState.Running;

            // Don't register if already completed, we would immediately unregistered in ObserveCancellation
            if (cancellationToken.CanBeCanceled && !IsCompleted)
            {
#if (NETSTANDARD2_0 || NETFRAMEWORK)
                _cancellationToken = cancellationToken;
#endif
                _cancellationTokenRegistration = cancellationToken.UnsafeRegister(callback, state);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Complete(out CompletionData completionData)
        {
            ExtractCompletion(out completionData);

            _awaitableState |= AwaitableState.Completed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExtractCompletion(out CompletionData completionData)
        {
            Action<object?>? currentCompletion = _completion;
            object? currentState = _completionState;
            SchedulingContext? schedulingContext = _schedulingContext;
            ExecutionContext? executionContext = schedulingContext?.ExecutionContext;
            SynchronizationContext? synchronizationContext = schedulingContext?.SynchronizationContext;

            _completion = null;
            _completionState = null;
            _schedulingContext = null;

            completionData = currentCompletion != null ?
                new CompletionData(currentCompletion, currentState, executionContext, synchronizationContext) :
                default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetUncompleted()
        {
            Debug.Assert(_completion == null);
            Debug.Assert(_completionState == null);
            Debug.Assert(_schedulingContext == null);

            _awaitableState &= ~AwaitableState.Completed;
        }

        public void OnCompleted(Action<object?> continuation, object? state, ValueTaskSourceOnCompletedFlags flags, out CompletionData completionData, out bool doubleCompletion)
        {
            completionData = default;
            doubleCompletion = _completion is not null;

            if (IsCompleted || doubleCompletion)
            {
                completionData = new CompletionData(continuation, state, _schedulingContext?.ExecutionContext, _schedulingContext?.SynchronizationContext);
                return;
            }

            _completion = continuation;
            _completionState = state;

            // Capture the SynchronizationContext if there's any and we're allowing capture (from pipe options)
            if ((_awaitableState & AwaitableState.UseSynchronizationContext) != 0 &&
                (flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
            {
                SynchronizationContext? sc = SynchronizationContext.Current;
                if (sc != null && sc.GetType() != typeof(SynchronizationContext))
                {
                    _schedulingContext ??= new SchedulingContext();
                    _schedulingContext.SynchronizationContext = sc;
                }
            }

            // Capture the execution context
            if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0)
            {
                _schedulingContext ??= new SchedulingContext();
                _schedulingContext.ExecutionContext = ExecutionContext.Capture();
            }
        }

        public void Cancel(out CompletionData completionData)
        {
            ExtractCompletion(out completionData);

            _awaitableState |= AwaitableState.Canceled;
        }

        public void CancellationTokenFired(out CompletionData completionData)
        {
            // We might be getting stale callbacks that we already unsubscribed from
            if (CancellationToken.IsCancellationRequested)
            {
                Cancel(out completionData);
            }
            else
            {
                completionData = default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ObserveCancellation()
        {
            bool isCanceled = (_awaitableState & AwaitableState.Canceled) == AwaitableState.Canceled;

            _awaitableState &= ~(AwaitableState.Canceled | AwaitableState.Running);

            return isCanceled;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CancellationTokenRegistration ReleaseCancellationTokenRegistration(out CancellationToken cancellationToken)
        {
            cancellationToken = CancellationToken;
            CancellationTokenRegistration cancellationTokenRegistration = _cancellationTokenRegistration;

#if (NETSTANDARD2_0 || NETFRAMEWORK)
            _cancellationToken = default;
#endif
            _cancellationTokenRegistration = default;

            return cancellationTokenRegistration;
        }

        [Flags]
        private enum AwaitableState
        {
            None = 0,
            // Marks that if logical operation (backpressure/waiting for data) is completed. Set in Complete reset in Reset
            Completed = 1,
            // Marks that operation is running. Set in *Async reset in  ObserveCancellation (GetResult)
            Running = 2,
            // Marks that operation is canceled. Set in Cancel reset in ObserveCancellation (GetResult)
            Canceled = 4,
            UseSynchronizationContext = 8
        }

        private sealed class SchedulingContext
        {
            public SynchronizationContext? SynchronizationContext { get; set; }
            public ExecutionContext? ExecutionContext { get; set; }
        }
    }
}
