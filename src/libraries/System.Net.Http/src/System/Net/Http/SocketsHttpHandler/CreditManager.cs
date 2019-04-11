// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed class CreditManager : IDisposable
    {
        private struct Waiter
        {
            public int Amount;
            public TaskCompletionSourceWithCancellation<int> TaskCompletionSource;
        }

        private int _current;
        private Queue<Waiter> _waiters;
        private bool _disposed;

        public CreditManager(int initialCredit)
        {
            _current = initialCredit;
            _waiters = null;
            _disposed = false;
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
                    throw new ObjectDisposedException(nameof(CreditManager));
                }

                if (_current > 0)
                {
                    Debug.Assert(_waiters == null || _waiters.Count == 0, "Shouldn't have waiters when credit is available");

                    int granted = Math.Min(amount, _current);
                    _current -= granted;
                    return new ValueTask<int>(granted);
                }

                // Uses RunContinuationsAsynchronously internally.
                var tcs = new TaskCompletionSourceWithCancellation<int>();

                if (_waiters == null)
                {
                    _waiters = new Queue<Waiter>();
                }

                Waiter waiter = new Waiter { Amount = amount, TaskCompletionSource = tcs };

                _waiters.Enqueue(waiter);

                return new ValueTask<int>(cancellationToken.CanBeCanceled ?
                                          tcs.WaitWithCancellationAsync(cancellationToken) :
                                          tcs.Task);
            }
        }

        public void AdjustCredit(int amount)
        {
            // Note credit can be adjusted *downward* as well.
            // This can cause the current credit to become negative.

            lock (SyncObject)
            {
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
                        if (waiter.TaskCompletionSource.TrySetResult(granted))
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
                        waiter.TaskCompletionSource.TrySetException(new ObjectDisposedException(nameof(CreditManager)));
                    }
                }
            }
        }
    }
}
