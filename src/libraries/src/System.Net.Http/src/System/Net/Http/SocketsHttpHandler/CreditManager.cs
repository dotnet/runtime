// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed class CreditManager
    {
        private readonly IHttpTrace _owner;
        private readonly string _name;
        private int _current;
        private Queue<Waiter> _waiters;
        private bool _disposed;

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
                    throw CreateObjectDisposedException(forActiveWaiter: false);
                }

                if (_current > 0)
                {
                    Debug.Assert(_waiters == null || _waiters.Count == 0, "Shouldn't have waiters when credit is available");

                    int granted = Math.Min(amount, _current);
                    if (NetEventSource.IsEnabled) _owner.Trace($"{_name}. requested={amount}, current={_current}, granted={granted}");
                    _current -= granted;
                    return new ValueTask<int>(granted);
                }

                if (NetEventSource.IsEnabled) _owner.Trace($"{_name}. requested={amount}, no credit available.");

                var waiter = new Waiter { Amount = amount };
                (_waiters ??= new Queue<Waiter>()).Enqueue(waiter);

                return new ValueTask<int>(cancellationToken.CanBeCanceled ?
                                          waiter.WaitWithCancellationAsync(cancellationToken) :
                                          waiter.Task);
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

                Debug.Assert(_current <= 0 || _waiters == null || _waiters.Count == 0, "Shouldn't have waiters when credit is available");

                checked
                {
                    _current += amount;
                }

                if (_waiters != null)
                {
                    while (_current > 0 && _waiters.TryDequeue(out Waiter waiter))
                    {
                        int granted = Math.Min(waiter.Amount, _current);

                        // Ensure that we grant credit only if the task has not been canceled.
                        if (waiter.TrySetResult(granted))
                        {
                            _current -= granted;
                        }
                    }
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

                if (_waiters != null)
                {
                    while (_waiters.TryDequeue(out Waiter waiter))
                    {
                        waiter.TrySetException(CreateObjectDisposedException(forActiveWaiter: true));
                    }
                }
            }
        }

        private ObjectDisposedException CreateObjectDisposedException(bool forActiveWaiter) => forActiveWaiter ?
            new ObjectDisposedException($"{nameof(CreditManager)}:{_owner.GetType().Name}:{_name}", SR.net_http_disposed_while_in_use) :
            new ObjectDisposedException($"{nameof(CreditManager)}:{_owner.GetType().Name}:{_name}");

        private sealed class Waiter : TaskCompletionSourceWithCancellation<int>
        {
            public int Amount;
        }
    }
}
