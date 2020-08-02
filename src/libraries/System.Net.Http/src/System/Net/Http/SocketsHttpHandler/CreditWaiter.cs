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
        private readonly object _syncObj;

        private CancellationTokenRegistration _registration;
        private ManualResetValueTaskSourceCore<int> _source;

        public int Amount;
        public CreditWaiter? Next;

        public CreditWaiter(object syncObj, CancellationToken cancellationToken)
        {
            _source.RunContinuationsAsynchronously = true;
            _syncObj = syncObj;
            RegisterCancellation(cancellationToken);
        }

        public ValueTask<int> AsValueTask() => new ValueTask<int>(this, _source.Version);

        public bool IsPending => _source.GetStatus(_source.Version) == ValueTaskSourceStatus.Pending;

        public bool TrySetResult(int result)
        {
            if (IsPending)
            {
                _source.SetResult(result);
                return true;
            }

            return false;
        }

        public void UnregisterCancellation()
        {
            Monitor.IsEntered(_syncObj);
            _registration.Dispose();
            _registration = default;
        }

        public void Dispose()
        {
            UnregisterCancellation();
            if (IsPending)
            {
                _source.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(nameof(CreditManager), SR.net_http_disposed_while_in_use)));
            }
        }

        public void ResetForAwait(CancellationToken cancellationToken)
        {
            Debug.Assert(Monitor.IsEntered(_syncObj));
            Debug.Assert(!IsPending);
            Debug.Assert(Next is null);
            Debug.Assert(_registration == default);

            _source.Reset();
            RegisterCancellation(cancellationToken);
        }

        private void RegisterCancellation(CancellationToken cancellationToken)
        {
            _registration = cancellationToken.UnsafeRegister(static s =>
            {
                var thisRef = (CreditWaiter)s!;
                lock (thisRef._syncObj)
                {
                    if (thisRef.IsPending)
                    {
                        thisRef._registration = default; // benign race with setting in the ctor
                        thisRef._source.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException(thisRef._registration.Token)));

                        // We don't remove it from the list as we lack a prev pointer that would enable us to do so correctly,
                        // and it's not worth adding a prev pointer for the rare case of cancellation.  We instead just
                        // check when completing a waiter whether it's already been canceled.  As such, we also do not
                        // dispose it here.
                    }
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
