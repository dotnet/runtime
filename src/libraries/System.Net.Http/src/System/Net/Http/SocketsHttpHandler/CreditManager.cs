// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Net.Http
{
    internal sealed class CreditManager
    {
        private readonly IHttpTrace _owner;
        private readonly string _name;
        private int _current;
        private bool _disposed;
        /// <summary>Circular singly-linked list of active waiters.</summary>
        /// <remarks>If null, the list is empty.  If non-null, this is the tail.  If the list has one item, its Next is itself.</remarks>
        private Waiter _waitersTail;

        public CreditManager(IHttpTrace owner, string name, int initialCredit)
        {
            Debug.Assert(owner != null);
            Debug.Assert(!string.IsNullOrWhiteSpace(name));

            if (NetEventSource.IsEnabled) owner.Trace($"{name}. {nameof(initialCredit)}={initialCredit}");
            _owner = owner;
            _name = name;
            _current = initialCredit;
        }

        private object SyncObject
        {
            get
            {
                // Generally locking on "this" is considered poor form, but this type is internal,
                // and it's unnecessary overhead to allocate another object just for this purpose.
                return this;
            }
        }

        public ValueTask<int> RequestCreditAsync(int amount, CancellationToken cancellationToken)
        {
            lock (SyncObject)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException($"{nameof(CreditManager)}:{_owner.GetType().Name}:{_name}");
                }

                // If we can satisfy the request with credit already available, do so synchronously.
                if (_current > 0)
                {
                    Debug.Assert(_waitersTail is null, "Shouldn't have waiters when credit is available");

                    int granted = Math.Min(amount, _current);
                    if (NetEventSource.IsEnabled) _owner.Trace($"{_name}. requested={amount}, current={_current}, granted={granted}");
                    _current -= granted;
                    return new ValueTask<int>(granted);
                }

                if (NetEventSource.IsEnabled) _owner.Trace($"{_name}. requested={amount}, no credit available.");

                // Otherwise, create a new waiter.
                Waiter waiter = cancellationToken.CanBeCanceled ?
                    new CancelableWaiter(amount, SyncObject, cancellationToken) :
                    new Waiter(amount);

                // Add the waiter at the tail of the queue.
                if (_waitersTail is null)
                {
                    _waitersTail = waiter.Next = waiter;
                }
                else
                {
                    waiter.Next = _waitersTail.Next;
                    _waitersTail.Next = waiter;
                    _waitersTail = waiter;
                }

                // And return a ValueTask<int> for it.
                return waiter.AsValueTask();
            }
        }

        public void AdjustCredit(int amount)
        {
            // Note credit can be adjusted *downward* as well.
            // This can cause the current credit to become negative.

            lock (SyncObject)
            {
                if (NetEventSource.IsEnabled) _owner.Trace($"{_name}. {nameof(amount)}={amount}, current={_current}");

                if (_disposed)
                {
                    return;
                }

                Debug.Assert(_current <= 0 || _waitersTail is null, "Shouldn't have waiters when credit is available");

                _current = checked(_current + amount);

                while (_current > 0 && _waitersTail != null)
                {
                    // Get the waiter from the head of the queue.
                    Waiter waiter = _waitersTail.Next;
                    int granted = Math.Min(waiter.Amount, _current);

                    // Remove the waiter from the list.
                    if (waiter.Next == waiter)
                    {
                        Debug.Assert(_waitersTail == waiter);
                        _waitersTail = null;
                    }
                    else
                    {
                        _waitersTail.Next = waiter.Next;
                    }
                    waiter.Next = null;

                    // Ensure that we grant credit only if the task has not been canceled.
                    if (waiter.TrySetResult(granted))
                    {
                        _current -= granted;
                    }

                    waiter.Dispose();
                }
            }
        }

        public void Dispose()
        {
            lock (SyncObject)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                Waiter waiter = _waitersTail;
                if (waiter != null)
                {
                    do
                    {
                        Waiter next = waiter.Next;
                        waiter.Next = null;
                        waiter.Dispose();
                        waiter = next;
                    }
                    while (waiter != _waitersTail);

                    _waitersTail = null;
                }
            }
        }

        /// <summary>Represents a waiter for credit.</summary>
        /// <remarks>All of the public members on the instance must only be accessed while holding the CreditManager's lock.</remarks>
        private class Waiter : IValueTaskSource<int>
        {
            public readonly int Amount;
            public Waiter Next;
            protected ManualResetValueTaskSourceCore<int> _source;

            public Waiter(int amount)
            {
                Amount = amount;
                _source.RunContinuationsAsynchronously = true;
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

            public virtual void Dispose()
            {
                if (IsPending)
                {
                    _source.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(nameof(CreditManager), SR.net_http_disposed_while_in_use)));
                }
            }

            int IValueTaskSource<int>.GetResult(short token) =>
                _source.GetResult(token);
            ValueTaskSourceStatus IValueTaskSource<int>.GetStatus(short token) =>
                _source.GetStatus(token);
            void IValueTaskSource<int>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) =>
                _source.OnCompleted(continuation, state, token, flags);
        }

        private sealed class CancelableWaiter : Waiter
        {
            private readonly object _syncObj;
            private CancellationTokenRegistration _registration;

            public CancelableWaiter(int amount, object syncObj, CancellationToken cancellationToken) : base(amount)
            {
                _syncObj = syncObj;
                _registration = cancellationToken.UnsafeRegister(s =>
                {
                    CancelableWaiter thisRef = (CancelableWaiter)s!;
                    lock (thisRef._syncObj)
                    {
                        if (thisRef.IsPending)
                        {
                            thisRef._source.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException(thisRef._registration.Token)));
                            thisRef._registration = default; // benign race with setting in the ctor

                            // We don't remove it from the list as we lack a prev pointer that would enable us to do so correctly,
                            // and it's not worth adding a prev pointer for the rare case of cancellation.  We instead just
                            // check when completing a waiter whether it's already been canceled.  As such, we also do not
                            // dispose it here.
                        }
                    }
                }, this);
            }

            public override void Dispose()
            {
                Monitor.IsEntered(_syncObj);

                _registration.Dispose();
                _registration = default;

                base.Dispose();
            }
        }
    }
}
