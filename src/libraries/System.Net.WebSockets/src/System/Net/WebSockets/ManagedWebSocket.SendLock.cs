// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Net.WebSockets
{
    internal partial class ManagedWebSocket
    {
        /// <summary>
        /// Lightweight async lock that allows single owner at any given time.
        /// </summary>
        [StructLayout(LayoutKind.Auto)]
        private struct SendLock
        {
            private int _locked;

            private SpinLock _stateLock;

            // Head of list representing asynchronous waits.
            private Waiter? _asyncHead;

            // Tail of list representing asynchronous waits.
            private Waiter? _asyncTail;

            public SendLock(bool enableThreadOwnerTracking)
            {
                _stateLock = new SpinLock(enableThreadOwnerTracking);
                _locked = 0;
                _asyncHead = null;
                _asyncTail = null;
            }

            public bool IsLocked => _locked == 1;

            public bool TryWait() => Interlocked.CompareExchange(ref _locked, 1, 0) == 0;

            public ValueTask WaitAsync(CancellationToken cancellationToken)
            {
                bool stateLockTaken = false;
                try
                {
                    _stateLock.Enter(ref stateLockTaken);

                    if (TryWait())
                    {
                        return default;
                    }

                    var waiter = new Waiter(cancellationToken);

                    if (_asyncTail is null)
                    {
                        _asyncHead = waiter;
                        _asyncTail = waiter;
                    }
                    else
                    {
                        _asyncTail.Next = waiter;
                        _asyncTail = waiter;
                    }

                    return new ValueTask(waiter, 0);
                }
                finally
                {
                    if (stateLockTaken)
                        _stateLock.Exit();
                }
            }

            public void Release()
            {
                Debug.Assert(IsLocked);

                bool stateLockTaken = false;
                try
                {
                    _stateLock.Enter(ref stateLockTaken);

                    while (_asyncHead is not null)
                    {
                        if (_asyncHead.TryComplete())
                        {
                            _asyncHead = _asyncHead.Next;
                            if (_asyncHead is null)
                            {
                                _asyncTail = null;
                            }
                            return;
                        }
                    }

                    Interlocked.Exchange(ref _locked, 0);
                }
                finally
                {
                    if (stateLockTaken)
                        _stateLock.Exit();
                }
            }

            private sealed class Waiter : IValueTaskSource
            {
                // 0 = false, 1 = true
                private int _completed;

                private readonly CancellationTokenRegistration _cancellationRegistration;
                private ManualResetValueTaskSourceCore<object> _source = new()
                {
                    RunContinuationsAsynchronously = true
                };

                public Waiter(CancellationToken cancellationToken)
                {
                    _cancellationRegistration = cancellationToken.UnsafeRegister(
                        static x => ((Waiter)x!).OnCancelled(), this);
                }

                public Waiter? Next { get; set; }

                public bool TryComplete()
                {
                    _cancellationRegistration.Dispose();

                    if (Interlocked.CompareExchange(ref _completed, 1, 0) == 0)
                    {
                        _source.SetResult(this);
                        return true;
                    }

                    return false;
                }

                private void OnCancelled()
                {
                    if (Interlocked.CompareExchange(ref _completed, 1, 0) == 0)
                    {
                        _source.SetException(new OperationCanceledException());
                    }
                }

                void IValueTaskSource.GetResult(short token) => _source.GetResult(token);

                ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _source.GetStatus(token);

                void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
                    => _source.OnCompleted(continuation, state, token, flags);
            }
        }
    }
}
