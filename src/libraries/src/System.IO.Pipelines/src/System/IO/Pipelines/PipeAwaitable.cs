// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Threading;

namespace System.IO.Pipelines
{
    internal struct PipeAwaitable
    {
        private static readonly Action _awaitableIsCompleted = () => { };
        private static readonly Action _awaitableIsNotCompleted = () => { };

        private CanceledState _canceledState;
        private Action _state;
        private CancellationToken _cancellationToken;
        private CancellationTokenRegistration _cancellationTokenRegistration;

        public PipeAwaitable(bool completed)
        {
            _canceledState = CanceledState.NotCanceled;
            _state = completed ? _awaitableIsCompleted : _awaitableIsNotCompleted;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CancellationTokenRegistration AttachToken(CancellationToken cancellationToken, Action<object> callback, object state)
        {
            CancellationTokenRegistration oldRegistration;
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
        public Action Complete()
        {
            Action awaitableState = _state;
            _state = _awaitableIsCompleted;

            if (!ReferenceEquals(awaitableState, _awaitableIsCompleted) &&
                !ReferenceEquals(awaitableState, _awaitableIsNotCompleted))
            {
                return awaitableState;
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            if (ReferenceEquals(_state, _awaitableIsCompleted) &&
                _canceledState < CanceledState.CancellationPreRequested)
            {
                _state = _awaitableIsNotCompleted;
            }

            // Change the state from observed -> not cancelled.
            // We only want to reset the cancelled state if it was observed
            if (_canceledState == CanceledState.CancellationObserved)
            {
                _canceledState = CanceledState.NotCanceled;
            }
        }

        public bool IsCompleted => ReferenceEquals(_state, _awaitableIsCompleted);
        internal bool HasContinuation => !ReferenceEquals(_state, _awaitableIsNotCompleted);

        public Action OnCompleted(Action continuation, out bool doubleCompletion)
        {
            doubleCompletion = false;
            Action awaitableState = _state;
            if (ReferenceEquals(awaitableState, _awaitableIsNotCompleted))
            {
                _state = continuation;
            }

            if (ReferenceEquals(awaitableState, _awaitableIsCompleted))
            {
                return continuation;
            }

            if (!ReferenceEquals(awaitableState, _awaitableIsNotCompleted))
            {
                doubleCompletion = true;
                return continuation;
            }

            return null;
        }

        public Action Cancel()
        {
            Action action = Complete();
            _canceledState = action == null ?
                CanceledState.CancellationPreRequested :
                CanceledState.CancellationRequested;
            return action;
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
                _canceledState = CanceledState.CancellationObserved;

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

        public override string ToString()
        {
            return $"CancelledState: {_canceledState}, {nameof(IsCompleted)}: {IsCompleted}";
        }

        private enum CanceledState
        {
            NotCanceled = 0,
            CancellationObserved = 1,
            CancellationPreRequested = 2,
            CancellationRequested = 3,
        }
    }
}
