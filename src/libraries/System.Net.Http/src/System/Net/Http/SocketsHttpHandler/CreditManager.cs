// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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
        private CreditWaiter? _waitersTail;

        public CreditManager(IHttpTrace owner, string name, int initialCredit)
        {
            Debug.Assert(owner != null);
            Debug.Assert(!string.IsNullOrWhiteSpace(name));

            if (NetEventSource.Log.IsEnabled()) owner.Trace($"{name}. {nameof(initialCredit)}={initialCredit}");
            _owner = owner;
            _name = name;
            _current = initialCredit;
        }

        public bool IsCreditAvailable => Volatile.Read(ref _current) > 0;

        private object SyncObject
        {
            // Generally locking on "this" is considered poor form, but this type is internal,
            // and it's unnecessary overhead to allocate another object just for this purpose.
            get => this;
        }

        public bool TryRequestCreditNoWait(int amount)
        {
            lock (SyncObject)
            {
                return TryRequestCreditNoLock(amount) > 0;
            }
        }

        public ValueTask<int> RequestCreditAsync(int amount, CancellationToken cancellationToken)
        {
            lock (SyncObject)
            {
                // If we can satisfy the request with credit already available, do so synchronously.
                int granted = TryRequestCreditNoLock(amount);

                if (granted > 0)
                {
                    return new ValueTask<int>(granted);
                }

                if (NetEventSource.Log.IsEnabled()) _owner.Trace($"{_name}. requested={amount}, no credit available.");

                // Otherwise, create a new waiter.
                var waiter = new CreditWaiter(cancellationToken);
                waiter.Amount = amount;

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
                if (NetEventSource.Log.IsEnabled()) _owner.Trace($"{_name}. {nameof(amount)}={amount}, current={_current}");

                if (_disposed)
                {
                    return;
                }

                Debug.Assert(_current <= 0 || _waitersTail is null, "Shouldn't have waiters when credit is available");

                _current = checked(_current + amount);

                while (_current > 0 && _waitersTail != null)
                {
                    // Get the waiter from the head of the queue.
                    CreditWaiter? waiter = _waitersTail.Next;
                    Debug.Assert(waiter != null);
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

                CreditWaiter? waiter = _waitersTail;
                if (waiter != null)
                {
                    do
                    {
                        CreditWaiter? next = waiter!.Next;
                        waiter.Next = null;
                        waiter.Dispose();
                        waiter = next;
                    }
                    while (waiter != _waitersTail);

                    _waitersTail = null;
                }
            }
        }

        private int TryRequestCreditNoLock(int amount)
        {
            Debug.Assert(Monitor.IsEntered(SyncObject), "Shouldn't be called outside lock.");

            if (_disposed)
            {
                throw new ObjectDisposedException($"{nameof(CreditManager)}:{_owner.GetType().Name}:{_name}");
            }

            if (_current > 0)
            {
                Debug.Assert(_waitersTail is null, "Shouldn't have waiters when credit is available");

                int granted = Math.Min(amount, _current);
                if (NetEventSource.Log.IsEnabled()) _owner.Trace($"{_name}. requested={amount}, current={_current}, granted={granted}");
                _current -= granted;
                return granted;
            }
            return 0;
        }
    }
}
