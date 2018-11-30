// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private Action<object> _completion;
        private object _completionState;
        private CancellationToken _cancellationToken;
        private CancellationTokenRegistration _cancellationTokenRegistration;
        private SynchronizationContext _synchronizationContext;
        private ExecutionContext _executionContext;

        public PipeAwaitable(bool completed, bool useSynchronizationContext)
        {
            _awaitableState = (completed ? AwaitableState.Completed : AwaitableState.None) |
                              (useSynchronizationContext ? AwaitableState.UseSynchronizationContext : AwaitableState.None);
            _completion = null;
            _completionState = null;
            _cancellationToken = CancellationToken.None;
            _cancellationTokenRegistration = default;
            _synchronizationContext = null;
            _executionContext = null;
        }

        public bool IsCompleted => (_awaitableState & (AwaitableState.Completed | AwaitableState.Canceled)) != 0;

        public bool IsRunning => (_awaitableState & AwaitableState.Running) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CancellationTokenRegistration BeginOperation(CancellationToken cancellationToken, Action<object> callback, object state)
        {
            _awaitableState |= AwaitableState.Running;

            CancellationTokenRegistration oldRegistration = default;
            if (!cancellationToken.Equals(_cancellationToken))
            {
                oldRegistration = _cancellationTokenRegistration;
                _cancellationToken = cancellationToken;
                if (_cancellationToken.CanBeCanceled)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    _cancellationTokenRegistration = _cancellationToken.UnsafeRegister(callback, state);
                }
            }
            return oldRegistration;
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
            Action<object> currentCompletion = _completion;
            object currentState = _completionState;

            _completion = null;
            _completionState = null;

            completionData = currentCompletion != null ?
                new CompletionData(currentCompletion, currentState, _executionContext, _synchronizationContext) :
                default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            _completion = null;
            _completionState = null;
            _synchronizationContext = null;
            _executionContext = null;

            _awaitableState &= ~AwaitableState.Completed;
        }

        public void OnCompleted(Action<object> continuation, object state, ValueTaskSourceOnCompletedFlags flags, out CompletionData completionData, out bool doubleCompletion)
        {
            completionData = default;
            doubleCompletion = _completion != null;

            if (IsCompleted || doubleCompletion)
            {
                completionData = new CompletionData(continuation, state, _executionContext, _synchronizationContext);
                return;
            }

            _completion = continuation;
            _completionState = state;

            // Capture the SynchronizationContext if there's any and we're allowing capture (from pipe options)
            if ((_awaitableState & AwaitableState.UseSynchronizationContext) != 0 &&
                (flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
            {
                SynchronizationContext sc = SynchronizationContext.Current;
                if (sc != null && sc.GetType() != typeof(SynchronizationContext))
                {
                    _synchronizationContext = SynchronizationContext.Current;
                }
            }

            // Capture the execution context
            if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0)
            {
                _executionContext = ExecutionContext.Capture();
            }
        }

        public void Cancel(out CompletionData completionData)
        {
            ExtractCompletion(out completionData);

            _awaitableState |= AwaitableState.Canceled;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ObserveCancellation()
        {
            bool isCanceled = (_awaitableState & AwaitableState.Canceled) == AwaitableState.Canceled;

            _awaitableState &= ~(AwaitableState.Canceled | AwaitableState.Running);

            _cancellationToken.ThrowIfCancellationRequested();

            return isCanceled;
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
    }
}
