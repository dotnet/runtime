// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace System.Threading
{
    internal sealed class Condition
    {
        internal sealed class Waiter
        {
            public Waiter? next;
            public Waiter? prev;
            public readonly AutoResetEvent ev = new AutoResetEvent(false);
        }

        [ThreadStatic]
        private static Waiter? t_waiterForCurrentThread;

        // Takes the cached Waiter for this thread (or allocates a new one) and removes the
        // current wait's cached Waiter from the thread-static so that any reentrant
        // Monitor.Wait (for example, from a SynchronizationContext message pump) gets its own Waiter with a distinct AutoResetEvent.
        private static Waiter GetWaiterForCurrentThread()
        {
            Waiter? waiter = t_waiterForCurrentThread;
            if (waiter is not null)
            {
                t_waiterForCurrentThread = null;
            }
            else
            {
                waiter = new Waiter();
            }

            Debug.Assert(waiter.next is null && waiter.prev is null);
            return waiter;
        }

        private static void ReleaseWaiterForCurrentThread(Waiter waiter)
        {
            // Return the waiter to the thread-static cache for reuse.
            t_waiterForCurrentThread = waiter;
        }

        private readonly Lock _lock;

        // When condition is installed in a Lock it takes the same field as waitEvent would.
        // If waitEvent is also needed, it is available through here.
        internal AutoResetEvent? _waitEvent;

        private Waiter? _waitersHead;
        private Waiter? _waitersTail;

        internal Lock AssociatedLock => _lock;

        [Conditional("DEBUG")]
        private void AssertIsInList(Waiter waiter)
        {
            Debug.Assert(_waitersHead != null && _waitersTail != null);
            Debug.Assert((_waitersHead == waiter) == (waiter.prev == null));
            Debug.Assert((_waitersTail == waiter) == (waiter.next == null));

            for (Waiter? current = _waitersHead; current != null; current = current.next)
                if (current == waiter)
                    return;
            Debug.Fail("Waiter is not in the waiter list");
        }

        [Conditional("DEBUG")]
        private void AssertIsNotInList(Waiter waiter)
        {
            Debug.Assert(waiter.next == null && waiter.prev == null);
            Debug.Assert((_waitersHead == null) == (_waitersTail == null));

            for (Waiter? current = _waitersHead; current != null; current = current.next)
                if (current == waiter)
                    Debug.Fail("Waiter is in the waiter list, but should not be");
        }

        // Returns true if the waiter cannot be possibly in the list.
        // (i.e. not reachable via _waitersHead)
        internal bool NotInList(Waiter waiter)
        {
            return _waitersHead != waiter && waiter.prev == null;
        }

        private void AddWaiter(Waiter waiter)
        {
            Debug.Assert(_lock.IsHeldByCurrentThread);
            AssertIsNotInList(waiter);

            Waiter? tail = _waitersTail;
            waiter.prev = tail;
            if (tail is null)
            {
                _waitersHead = waiter;
            }
            else
            {
                tail.next = waiter;
            }
            _waitersTail = waiter;
        }

        private void RemoveWaiter(Waiter waiter)
        {
            Debug.Assert(_lock.IsHeldByCurrentThread);
            AssertIsInList(waiter);

            if (waiter.next != null)
                waiter.next.prev = waiter.prev;
            else
                _waitersTail = waiter.prev;

            if (waiter.prev != null)
                waiter.prev.next = waiter.next;
            else
                _waitersHead = waiter.next;

            waiter.next = null;
            waiter.prev = null;
        }

        public Condition(Lock @lock)
        {
#pragma warning disable CS9216 // A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
            ArgumentNullException.ThrowIfNull(@lock);
#pragma warning restore CS9216
            _lock = @lock;
        }

        public bool Wait(int millisecondsTimeout, object associatedObjectForMonitorWait)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1);

            if (!_lock.IsHeldByCurrentThread)
                throw new SynchronizationLockException();

            using ThreadBlockingInfo.Scope threadBlockingScope = new(this, millisecondsTimeout);

            Waiter waiter = GetWaiterForCurrentThread();
            AddWaiter(waiter);

            uint recursionCount = _lock.ExitAll();
            bool success = false;
            bool wasSignaled;
            try
            {
                success =
                    waiter.ev.WaitOneNoCheck(
                        millisecondsTimeout,
                        false, // useTrivialWaits
                        associatedObjectForMonitorWait,
                        NativeRuntimeEventSource.WaitHandleWaitSourceMap.MonitorWait);
            }
            finally
            {
                _lock.Reenter(recursionCount);
                Debug.Assert(_lock.IsHeldByCurrentThread);

                // If the waiter is still in the list, it was not signaled.
                wasSignaled = NotInList(waiter);
                if (!wasSignaled)
                {
                    RemoveWaiter(waiter);
                }
                else if (!success)
                {
                    //
                    // The wait timed out, but we were signaled before we could reacquire the lock.
                    // Since WaitOne timed out, it didn't trigger the auto-reset of the AutoResetEvent.
                    // So, we need to manually reset the event.
                    //
                    waiter.ev.Reset();
                }

                AssertIsNotInList(waiter);
                ReleaseWaiterForCurrentThread(waiter);
            }

            return wasSignaled;
        }

        public void SignalAll()
        {
            if (!_lock.IsHeldByCurrentThread)
                throw new SynchronizationLockException();

            Waiter? waiter = _waitersHead;
            if (waiter is null)
            {
                return;
            }

            // Detach the entire waiter list in one operation, then walk it and signal each waiter.
            // Per-waiter prev/next must be cleared BEFORE calling ev.Set() so that the woken thread
            // observes the waiter as not in the list (see NotInList) and the cached Waiter is clean.
            // Woken threads cannot make progress until the caller releases _lock, so it is safe to
            // continue walking the detached list after signaling each waiter.
            _waitersHead = null;
            _waitersTail = null;

            while (waiter is not null)
            {
                Waiter? next = waiter.next;
                waiter.next = null;
                waiter.prev = null;
                waiter.ev.Set();
                waiter = next;
            }
        }

        public void SignalOne()
        {
            if (!_lock.IsHeldByCurrentThread)
                throw new SynchronizationLockException();

            Waiter? waiter = _waitersHead;
            if (waiter != null)
            {
                RemoveWaiter(waiter);
                waiter.ev.Set();
            }
        }
    }
}
