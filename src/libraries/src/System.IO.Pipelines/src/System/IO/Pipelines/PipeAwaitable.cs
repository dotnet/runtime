// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks.Sources;

namespace System.IO.Pipelines
{
    [DebuggerDisplay("CanceledState: {_canceledState}, IsCompleted: {IsCompleted}")]
    internal struct PipeAwaitable
    {
        private static readonly Action<object> s_awaitableIsCompleted = _ => { };
        private static readonly Action<object> s_awaitableIsNotCompleted = _ => { };

        private CanceledState _canceledState;
        private Action<object> _completion;
        private object _completionState;
        private CancellationToken _cancellationToken;
        private CancellationTokenRegistration _cancellationTokenRegistration;
        private SynchronizationContext _synchronizationContext;
        private ExecutionContext _executionContext;
        private bool _useSynchronizationContext;

        public PipeAwaitable(bool completed, bool useSynchronizationContext)
        {
            _canceledState = CanceledState.NotCanceled;
            _completion = completed ? s_awaitableIsCompleted : s_awaitableIsNotCompleted;
            _completionState = null;
            _cancellationTokenRegistration = default;
            _synchronizationContext = null;
            _executionContext = null;
            _useSynchronizationContext = useSynchronizationContext;
        }

        public bool IsCompleted => ReferenceEquals(_completion, s_awaitableIsCompleted);

        public bool HasContinuation => !ReferenceEquals(_completion, s_awaitableIsNotCompleted);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CancellationTokenRegistration AttachToken(CancellationToken cancellationToken, Action<object> callback, object state)
        {
            CancellationTokenRegistration oldRegistration = default;
            if (!cancellationToken.Equals(_cancellationToken))
            {
                oldRegistration = _cancellationTokenRegistration;
                _cancellationToken = cancellationToken;
                if (_cancellationToken.CanBeCanceled)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    _cancellationTokenRegistration = _cancellationToken.Register(callback, state);
                }
            }
            return oldRegistration;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Complete(out CompletionData completionData)
        {
            Action<object> currentCompletion = _completion;
            object currentState = _completionState;

            _completion = s_awaitableIsCompleted;
            _completionState = null;

            completionData = default;
            
            if (!ReferenceEquals(currentCompletion, s_awaitableIsCompleted) &&
                !ReferenceEquals(currentCompletion, s_awaitableIsNotCompleted))
            {
                completionData = new CompletionData(currentCompletion, currentState, _executionContext, _synchronizationContext);
            }
            else if (_canceledState == CanceledState.CancellationRequested)
            {
                // Make sure we won't reset the awaitable in ObserveCancellation
                // If Complete happens in between Cancel and ObserveCancellation
                _canceledState = CanceledState.CancellationPreRequested;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            if (ReferenceEquals(_completion, s_awaitableIsCompleted) &&
                _canceledState < CanceledState.CancellationPreRequested)
            {
                _completion = s_awaitableIsNotCompleted;
                _completionState = null;
                _synchronizationContext = null;
                _executionContext = null;
            }

            // Change the state from observed -> not cancelled.
            // We only want to reset the cancelled state if it was observed
            if (_canceledState == CanceledState.CancelationObserved)
            {
                _canceledState = CanceledState.NotCanceled;
            }
        }

        public void OnCompleted(Action<object> continuation, object state, ValueTaskSourceOnCompletedFlags flags, out CompletionData completionData, out bool doubleCompletion)
        {
            completionData = default;

            doubleCompletion = false;
            Action<object> awaitableState = _completion;
            if (ReferenceEquals(awaitableState, s_awaitableIsNotCompleted))
            {
                _completion = continuation;
                _completionState = state;

                // Capture the SynchronizationContext if there's any and we're allowing capture (from pipe options)
                if (_useSynchronizationContext && (flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
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

            if (ReferenceEquals(awaitableState, s_awaitableIsCompleted))
            {
                completionData = new CompletionData(continuation, state, _executionContext, _synchronizationContext);
                return;
            }

            if (!ReferenceEquals(awaitableState, s_awaitableIsNotCompleted))
            {
                doubleCompletion = true;
                completionData = new CompletionData(continuation, state, _executionContext, _synchronizationContext);
            }
        }

        public void Cancel(out CompletionData completionData)
        {
            Complete(out completionData);
            _canceledState = completionData.Completion == null ?
                CanceledState.CancellationPreRequested :
                CanceledState.CancellationRequested;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ObserveCancelation()
        {
            if (_canceledState == CanceledState.NotCanceled)
            {
                return false;
            }

            bool isPrerequested = _canceledState == CanceledState.CancellationPreRequested;

            if (_canceledState >= CanceledState.CancellationPreRequested)
            {
                _canceledState = CanceledState.CancelationObserved;

                // Do not reset awaitable if we were not awaiting in the first place
                if (!isPrerequested)
                {
                    Reset();
                }

                _cancellationToken.ThrowIfCancellationRequested();

                return true;
            }

            return false;
        }

        private enum CanceledState
        {
            NotCanceled = 0,
            CancelationObserved = 1,
            CancellationPreRequested = 2,
            CancellationRequested = 3,
        }
    }
}
