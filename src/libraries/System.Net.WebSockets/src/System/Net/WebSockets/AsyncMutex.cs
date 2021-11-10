// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Threading
{
    /// <summary>Provides an async mutex.</summary>
    /// <remarks>
    /// This could be achieved with a <see cref="SemaphoreSlim"/> constructed with an initial
    /// and max limit of 1.  However, this implementation is optimized to the needs of ManagedWebSocket,
    /// which is that we expect zero contention in typical use cases.
    /// </remarks>
    internal sealed class AsyncMutex
    {
        /// <summary>Fast-path gate count tracking access to the mutex.</summary>
        /// <remarks>
        /// If the value is 1, the mutex can be entered atomically with an interlocked operation.
        /// If the value is less than or equal to 0, the mutex is held and requires fallback to enter it.
        /// </remarks>
        private int _gate = 1;
        /// <summary>Secondary check guarded by the lock to indicate whether the mutex is acquired.</summary>
        /// <remarks>
        /// This is only meaningful after having updated <see cref="_gate"/> via interlockeds and taken the appropriate path.
        /// If after decrementing <see cref="_gate"/> we end up with a negative count, the mutex is contended, hence
        /// <see cref="_lockedSemaphoreFull"/> starting as <c>true</c>.  The primary purpose of this field
        /// is to handle the race condition between one thread acquiring the mutex, then another thread trying to acquire
        /// and getting as far as completing the interlocked operation, and then the original thread releasing; at that point
        /// it'll hit the lock and we need to store that the mutex is available to enter.  If we instead used a
        /// SemaphoreSlim as the fallback from the interlockeds, this would have been its count, and it would have started
        /// with an initial count of 0.
        /// </remarks>
        private bool _lockedSemaphoreFull = true;
        /// <summary>The tail of the double-linked circular waiting queue.</summary>
        /// <remarks>
        /// Waiters are added at the tail.
        /// Items are dequeued from the head (tail.Prev).
        /// </remarks>
        private Waiter? _waitersTail;

        /// <summary>Gets whether the mutex is currently held by some operation (not necessarily the caller).</summary>
        /// <remarks>This should be used only for asserts and debugging.</remarks>
        public bool IsHeld => _gate != 1;

        /// <summary>Gets the object used to synchronize contended operations.</summary>
        private object SyncObj => this;

        /// <summary>Asynchronously waits to enter the mutex.</summary>
        /// <param name="cancellationToken">The CancellationToken token to observe.</param>
        /// <returns>A task that will complete when the mutex has been entered or the enter canceled.</returns>
        public Task EnterAsync(CancellationToken cancellationToken)
        {
            // If cancellation was requested, bail immediately.
            // If the mutex is not currently held nor contended, enter immediately.
            // Otherwise, fall back to a more expensive likely-asynchronous wait.
            return
                cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) :
                Interlocked.Decrement(ref _gate) >= 0 ? Task.CompletedTask :
                Contended(cancellationToken);

            // Everything that follows is the equivalent of:
            //     return _sem.WaitAsync(cancellationToken);
            // if _sem were to be constructed as `new SemaphoreSlim(0)`.

            Task Contended(CancellationToken cancellationToken)
            {
                var w = new Waiter(this);

                // We need to register for cancellation before storing the waiter into the list.
                // If we registered after, we might leak a registration if the mutex was exited and the waiter
                // removed from the list prior to CancellationRegistration being properly assigned. By registering before,
                // there's a different race condition, that of cancellation being requested prior to storing the waiter into
                // the list; if that happens, we could end up adding the waiter and have it still stored in the list even
                // though OnCancellation was called. So once we hold the lock, which OnCancellation also needs to take, we
                // check again whether cancellation has been requested,and avoid storing the waiter if it has.
                w.CancellationRegistration = cancellationToken.UnsafeRegister((s, token) => OnCancellation(s, token), w);

                lock (SyncObj)
                {
                    // Now that we're holding the lock, check to see whether the async lock is acquirable.
                    if (!_lockedSemaphoreFull)
                    {
                        // If we are able to acquire the lock, we're done; we just need to clean up after the registration.
                        w.CancellationRegistration.Unregister();
                        _lockedSemaphoreFull = true;
                        return Task.CompletedTask;
                    }

                    // Now that we're holding the lock and thus synchronized with OnCancellation, check to see
                    // if cancellation has been requested.
                    if (cancellationToken.IsCancellationRequested)
                    {
                        w.TrySetCanceled(cancellationToken);
                        return w.Task;
                    }

                    // The lock couldn't be acquired.
                    // Add the waiter to the linked list of waiters.
                    if (_waitersTail is null)
                    {
                        w.Next = w.Prev = w;
                    }
                    else
                    {
                        Debug.Assert(_waitersTail.Next != null && _waitersTail.Prev != null);
                        w.Next = _waitersTail;
                        w.Prev = _waitersTail.Prev;
                        w.Prev.Next = w.Next.Prev = w;
                    }
                    _waitersTail = w;
                }

                // Return the waiter as a value task.
                return w.Task;

                // Cancels the specified waiter if it's still in the list.
                static void OnCancellation(object? state, CancellationToken cancellationToken)
                {
                    Waiter? w = (Waiter)state!;
                    AsyncMutex m = w.Owner;

                    lock (m.SyncObj)
                    {
                        bool inList = w.Next != null;
                        if (inList)
                        {
                            // The waiter is in the list.
                            Debug.Assert(w.Prev != null);

                            // The gate counter was decremented when this waiter was added.  We need
                            // to undo that.  Since the waiter is still in the list, the lock must
                            // still be held by someone, which means we don't need to do anything with
                            // the result of this increment.  If it increments to < 1, then there are
                            // still other waiters.  If it increments to 1, we're in a rare race condition
                            // where there are no other waiters and the owner just incremented the gate
                            // count; they would have seen it be < 1, so they will proceed to take the
                            // contended code path and synchronize on the lock we're holding... once we
                            // release it, they will appropriately update state.
                            Interlocked.Increment(ref m._gate);

                            if (w.Next == w)
                            {
                                Debug.Assert(m._waitersTail == w);
                                m._waitersTail = null;
                            }
                            else
                            {
                                w.Next!.Prev = w.Prev;
                                w.Prev.Next = w.Next;
                                if (m._waitersTail == w)
                                {
                                    m._waitersTail = w.Next;
                                }
                            }

                            // Remove it from the list.
                            w.Next = w.Prev = null;
                        }
                        else
                        {
                            // The waiter was no longer in the list.  We must not cancel it.
                            w = null;
                        }
                    }

                    // If the waiter was in the list, we removed it under the lock and thus own
                    // the ability to cancel it.  Do so.
                    w?.TrySetCanceled(cancellationToken);
                }
            }
        }

        /// <summary>Releases the mutex.</summary>
        /// <remarks>The caller must logically own the mutex.  This is not validated.</remarks>
        public void Exit()
        {
            if (Interlocked.Increment(ref _gate) < 1)
            {
                // This is the equivalent of:
                //     _sem.Release();
                // if _sem were to be constructed as `new SemaphoreSlim(0)`.
                Contended();
            }

            void Contended()
            {
                Waiter? w;
                lock (SyncObj)
                {
                    Debug.Assert(_lockedSemaphoreFull);

                    w = _waitersTail;
                    if (w is null)
                    {
                        _lockedSemaphoreFull = false;
                    }
                    else
                    {
                        Debug.Assert(w.Next != null && w.Prev != null);
                        Debug.Assert(w.Next != w || w.Prev == w);
                        Debug.Assert(w.Prev != w || w.Next == w);

                        if (w.Next == w)
                        {
                            _waitersTail = null;
                        }
                        else
                        {
                            w = w.Prev; // get the head
                            Debug.Assert(w.Next != null && w.Prev != null);
                            Debug.Assert(w.Next != w && w.Prev != w);

                            w.Next.Prev = w.Prev;
                            w.Prev.Next = w.Next;
                        }

                        w.Next = w.Prev = null;
                    }
                }

                // Either there wasn't a waiter, or we got one and successfully removed it from the list,
                // at which point we own the ability to complete it.  Do so.
                if (w is not null)
                {
                    w.CancellationRegistration.Unregister();
                    w.TrySetResult();
                }
            }
        }

        /// <summary>Represents a waiter for the mutex.</summary>
        private sealed class Waiter : TaskCompletionSource
        {
            public Waiter(AsyncMutex owner) : base(TaskCreationOptions.RunContinuationsAsynchronously) => Owner = owner;
            public AsyncMutex Owner { get; }
            public CancellationTokenRegistration CancellationRegistration { get; set; }
            public Waiter? Next { get; set; }
            public Waiter? Prev { get; set; }
        }
    }
}
