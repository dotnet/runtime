// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Net.Http
{
    /// <summary>Represents a waiter for credit.</summary>
    internal sealed class CreditWaiter : IValueTaskSource<int>
    {
        private CancellationTokenRegistration _registration;
        private ManualResetValueTaskSourceCore<int> _source;
        private int _completionReserved;

        public int Amount;
        public CreditWaiter? Next;

        public CreditWaiter(CancellationToken cancellationToken)
        {
            _source.RunContinuationsAsynchronously = true;
            RegisterCancellation(cancellationToken);
        }

        public ValueTask<int> AsValueTask() => new ValueTask<int>(this, _source.Version);

        private bool ReserveCompletion() => Interlocked.CompareExchange(ref _completionReserved, 1, 0) == 0;

        public bool TrySetResult(int result)
        {
            if (ReserveCompletion())
            {
                _source.SetResult(result);
                return true;
            }

            return false;
        }

        public void UnregisterCancellation()
        {
            _registration.Dispose();
            _registration = default;
        }

        public void Dispose()
        {
            UnregisterCancellation();
            if (ReserveCompletion())
            {
                _source.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(nameof(CreditManager), SR.net_http_disposed_while_in_use)));
            }
        }

        public void ResetForAwait(CancellationToken cancellationToken)
        {
            Debug.Assert(_completionReserved != 0);
            Debug.Assert(Next is null);
            Debug.Assert(_registration == default);

            _completionReserved = 0;
            _source.Reset();
            RegisterCancellation(cancellationToken);
        }

        private void RegisterCancellation(CancellationToken cancellationToken)
        {
            Debug.Assert(_completionReserved == 0);
            Debug.Assert(Next is null);
            Debug.Assert(_registration == default);

            _registration = cancellationToken.UnsafeRegister(static s =>
            {
                var thisRef = (CreditWaiter)s!;
                if (thisRef.ReserveCompletion())
                {
                    // Complete the source with a cancellation exception.  We try to use the token associated with the registration,
                    // but there's a race condition here where if cancellation is requested before the _registration field is assigned
                    // above, we could be here with a default registration, in which case we just won't have the token in the exception.
                    // Similarly, there's a benign race condition with the assigning of _registration; worst case is the instance
                    // temporarily ends up holding on to a disposed registration.
                    CancellationToken token = thisRef._registration.Token;
                    thisRef._registration = default;
                    thisRef._source.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException(token)));

                    // We don't remove it from the list as we lack a prev pointer that would enable us to do so correctly,
                    // and it's not worth adding a prev pointer for the rare case of cancellation.  We instead just
                    // check when completing a waiter whether it's already been canceled.  As such, we also do not
                    // dispose it here.
                }
            }, this);
        }

        int IValueTaskSource<int>.GetResult(short token) =>
            _source.GetResult(token);
        ValueTaskSourceStatus IValueTaskSource<int>.GetStatus(short token) =>
            _source.GetStatus(token);
        void IValueTaskSource<int>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
            _source.OnCompleted(continuation, state, token, flags);
    }
}
