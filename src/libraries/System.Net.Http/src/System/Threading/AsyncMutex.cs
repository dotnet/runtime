// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Threading
{
    /// <summary>Provides an async mutex.</summary>
    /// <remarks>
    /// This could be achieved with a <see cref="SemaphoreSlim"/> constructed with an initial
    /// and max limit of 1.  However, this implementation is optimized to the needs of HTTP/2,
    /// where the mutex is held for a very short period of time, when it is held any other
    /// attempts to access it must wait asynchronously, where it's only binary rather than counting, and where
    /// we want to minimize contention that a releaser incurs while trying to unblock a waiter.  The primary
    /// value-add is the fast-path interlocked checks that minimize contention for these use cases (essentially
    /// making it an async futex), and then as long as we're wrapping something and we know exactly how all
    /// consumers use the type, we can offer a ValueTask-based implementation that reuses waiter nodes.
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
        /// <summary>The head of the double-linked waiting queue.  Waiters are dequeued from the head.</summary>
        private Waiter? _waitersHead;
        /// <summary>The tail of the double-linked waiting queue.  Waiters are added at the tail.</summary>
        private Waiter? _waitersTail;
        /// <summary>A pool of waiter objects that are ready to be reused.</summary>
        /// <remarks>
        /// There is no bound on this pool, but it ends up being implicitly bounded by the maximum number of concurrent
        /// waiters there ever were, which for our uses in HTTP/2 will end up being the high-water mark of concurrent streams
        /// on a single connection.
        /// </remarks>
        private readonly ConcurrentQueue<Waiter> _unusedWaiters = new ConcurrentQueue<Waiter>();

        /// <summary>Gets whether the mutex is currently held by some operation (not necessarily the caller).</summary>
        /// <remarks>This should be used only for asserts and debugging.</remarks>
        public bool IsHeld => _gate != 1;

        /// <summary>Objects used to synchronize operations on the instance.</summary>
        private object SyncObj => _unusedWaiters;

        /// <summary>Asynchronously waits to enter the mutex.</summary>
        /// <param name="cancellationToken">The CancellationToken token to observe.</param>
        /// <returns>A task that will complete when the mutex has been entered or the enter canceled.</returns>
        public ValueTask EnterAsync(CancellationToken cancellationToken)
        {
            // If cancellation was requested, bail immediately.
            // If the mutex is not currently held nor contended, enter immediately.
            // Otherwise, fall back to a more expensive likely-asynchronous wait.
            return
                cancellationToken.IsCancellationRequested ? FromCanceled(cancellationToken) :
                Interlocked.Decrement(ref _gate) >= 0 ? default :
                Contended(cancellationToken);

            // Everything that follows is the equivalent of:
            //     return _sem.WaitAsync(cancellationToken);
            // if _sem were to be constructed as `new SemaphoreSlim(0)`.

            ValueTask Contended(CancellationToken cancellationToken)
            {
                // Get a reusable waiter object.  We do this before the lock to minimize work (and especially allocation)
                // done while holding the lock.  It's possible we'll end up dequeuing a waiter and then under the lock
                // discovering the mutex is now available, at which point we will have wasted an object.  That's currently
                // showing to be the better alternative (including not trying to put it back in that case).
                if (!_unusedWaiters.TryDequeue(out Waiter? w))
                {
                    w = new Waiter(this);
                }

                lock (SyncObj)
                {
                    // Now that we're holding the lock, check to see whether the async lock is acquirable.
                    if (!_lockedSemaphoreFull)
                    {
                        _lockedSemaphoreFull = true;
                        return default;
                    }
                    else
                    {
                        // Add it to the linked list of waiters.
                        if (_waitersTail is null)
                        {
                            Debug.Assert(_waitersHead is null);
                            _waitersTail = _waitersHead = w;
                        }
                        else
                        {
                            Debug.Assert(_waitersHead != null);
                            w.Prev = _waitersTail;
                            _waitersTail.Next = w;
                            _waitersTail = w;
                        }
                    }
                }

                // At this point the waiter was added to the list of waiters, so we want to
                // register for cancellation in order to cancel it and remove it from the list
                // if cancellation is requested.  However, since we've released the lock, it's
                // possible the waiter could have actually already been completed and removed
                // from the list by another thread releasing the mutex.  That's ok; we'll
                // end up registering for cancellation here, and then when the consumer awaits
                // it, the act of awaiting it will Dispose of the registration, ensuring that
                // it won't run after that point, making it safe to pool that instance.
                w.CancellationRegistration = cancellationToken.UnsafeRegister(s => OnCancellation(s), w);

                // Return the waiter as a value task.
                return new ValueTask(w, w.Version);

                // Cancels the specified waiter if it's still in the list.
                static void OnCancellation(object? state)
                {
                    Waiter? w = (Waiter)state!;
                    AsyncMutex m = w.Owner;

                    lock (m.SyncObj)
                    {
                        bool inList = w.Next != null || w.Prev != null || m._waitersHead == w;
                        if (inList)
                        {
                            // The waiter was still in the list.
                            Debug.Assert(
                                m._waitersHead == w ||
                                (m._waitersTail == w && w.Prev != null && w.Next is null) ||
                                (w.Next != null && w.Prev != null));

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

                            // Remove it from the list.
                            if (m._waitersHead == w && m._waitersTail == w)
                            {
                                // It's the only node in the list.
                                m._waitersHead = m._waitersTail = null;
                            }
                            else if (m._waitersTail == w)
                            {
                                // It's the most recently queued item in the list.
                                m._waitersTail = w.Prev;
                                Debug.Assert(m._waitersTail != null);
                                m._waitersTail.Next = null;
                            }
                            else if (m._waitersHead == w)
                            {
                                // It's the next item to be removed from the list.
                                m._waitersHead = w.Next;
                                Debug.Assert(m._waitersHead != null);
                                m._waitersHead.Prev = null;
                            }
                            else
                            {
                                // It's in the middle of the list.
                                Debug.Assert(w.Next != null);
                                Debug.Assert(w.Prev != null);
                                w.Next.Prev = w.Prev;
                                w.Prev.Next = w.Next;
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
                    w?.Cancel();
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

                    // Wake up the next waiter in the list.
                    w = _waitersHead;
                    if (w != null)
                    {
                        // Remove the waiter.
                        _waitersHead = w.Next;
                        if (w.Next != null)
                        {
                            w.Next.Prev = null;
                        }
                        else
                        {
                            Debug.Assert(_waitersTail == w);
                            _waitersTail = null;
                        }
                        w.Next = w.Prev = null;
                    }
                    else
                    {
                        // There wasn't a waiter.  Mark that the async lock is no longer full.
                        Debug.Assert(_waitersTail is null);
                        _lockedSemaphoreFull = false;
                    }
                }

                // Either there wasn't a waiter, or we got one and successfully removed it from the list,
                // at which point we own the ability to complete it.  Do so.
                w?.Set();
            }
        }

        /// <summary>Creates a canceled ValueTask.</summary>
        /// <remarks>Separated out to reduce asm for this rare path in the call site.</remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ValueTask FromCanceled(CancellationToken cancellationToken) =>
            new ValueTask(Task.FromCanceled(cancellationToken));

        /// <summary>Represents a waiter for the mutex.</summary>
        /// <remarks>Implemented as a reusable backing source for a value task.</remarks>
        private sealed class Waiter : IValueTaskSource
        {
            private ManualResetValueTaskSourceCore<bool> _mrvtsc; // mutable struct; do not make this readonly

            public Waiter(AsyncMutex owner)
            {
                Owner = owner;
                _mrvtsc.RunContinuationsAsynchronously = true;
            }

            public AsyncMutex Owner { get; }
            public CancellationTokenRegistration CancellationRegistration { get; set; }
            public Waiter? Next { get; set; }
            public Waiter? Prev { get; set; }

            public short Version => _mrvtsc.Version;

            public void Set() => _mrvtsc.SetResult(true);
            public void Cancel() => _mrvtsc.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException(CancellationRegistration.Token)));

            void IValueTaskSource.GetResult(short token)
            {
                Debug.Assert(Next is null && Prev is null);

                // Dispose of the registration.  It's critical that this Dispose rather than Unregister,
                // so that we can be guaranteed all cancellation-related work has completed by the time
                // we return the instance to the pool.  Otherwise, a race condition could result in
                // a cancellation request for this operation canceling another unlucky request that
                // happened to reuse the same node.
                Debug.Assert(!Monitor.IsEntered(Owner.SyncObj));
                CancellationRegistration.Dispose();

                // Complete the operation, propagating any exceptions.
                _mrvtsc.GetResult(token);

                // Reset the instance and return it to the pool.
                // We don't bother with a try/finally to return instances
                // to the pool in the case of exceptions.
                _mrvtsc.Reset();
                Owner._unusedWaiters.Enqueue(this);
            }

            public ValueTaskSourceStatus GetStatus(short token) =>
                _mrvtsc.GetStatus(token);

            public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
                _mrvtsc.OnCompleted(continuation, state, token, flags);
        }
    }
}
