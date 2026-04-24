// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable 0420 //passing volatile fields by ref

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
            public AutoResetEvent ev = new AutoResetEvent(false);
            public bool signalled;
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

            waiter.signalled = false;
            return waiter;
        }

        private static void ReleaseWaiterForCurrentThread(Waiter waiter)
        {
            // Return the waiter to the thread-static cache for reuse.
            t_waiterForCurrentThread = waiter;
        }

        private readonly Lock _lock;
        private Waiter? _waitersHead;
        private Waiter? _waitersTail;

        internal Lock AssociatedLock => _lock;

        private unsafe void AssertIsInList(Waiter waiter)
        {
            Debug.Assert(_waitersHead != null && _waitersTail != null);
            Debug.Assert((_waitersHead == waiter) == (waiter.prev == null));
            Debug.Assert((_waitersTail == waiter) == (waiter.next == null));

            for (Waiter? current = _waitersHead; current != null; current = current.next)
                if (current == waiter)
                    return;
            Debug.Fail("Waiter is not in the waiter list");
        }

        private unsafe void AssertIsNotInList(Waiter waiter)
        {
            Debug.Assert(waiter.next == null && waiter.prev == null);
            Debug.Assert((_waitersHead == null) == (_waitersTail == null));

            for (Waiter? current = _waitersHead; current != null; current = current.next)
                if (current == waiter)
                    Debug.Fail("Waiter is in the waiter list, but should not be");
        }

        private unsafe void AddWaiter(Waiter waiter)
        {
            Debug.Assert(_lock.IsHeldByCurrentThread);
            AssertIsNotInList(waiter);

            waiter.prev = _waitersTail;
            waiter.prev?.next = waiter;

            _waitersTail = waiter;

            _waitersHead ??= waiter;
        }

        private unsafe void RemoveWaiter(Waiter waiter)
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

        public unsafe bool Wait(int millisecondsTimeout, object associatedObjectForMonitorWait)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1);

            if (!_lock.IsHeldByCurrentThread)
                throw new SynchronizationLockException();

            using ThreadBlockingInfo.Scope threadBlockingScope = new(this, millisecondsTimeout);

            Waiter waiter = GetWaiterForCurrentThread();
            AddWaiter(waiter);

            uint recursionCount = _lock.ExitAll();
            bool success = false;
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

                if (!waiter.signalled)
                {
                    RemoveWaiter(waiter);
                }
                else if (!success)
                {
                    //
                    // The wait timed out, but we were signalled before we could reacquire the lock.
                    // Since WaitOne timed out, it didn't trigger the auto-reset of the AutoResetEvent.
                    // So, we need to manually reset the event.
                    //
                    waiter.ev.Reset();
                }

                AssertIsNotInList(waiter);
                ReleaseWaiterForCurrentThread(waiter);
            }

            return waiter.signalled;
        }

        public unsafe void SignalAll()
        {
            if (!_lock.IsHeldByCurrentThread)
                throw new SynchronizationLockException();

            while (_waitersHead != null)
                SignalOne();
        }

        public unsafe void SignalOne()
        {
            if (!_lock.IsHeldByCurrentThread)
                throw new SynchronizationLockException();

            Waiter? waiter = _waitersHead;
            if (waiter != null)
            {
                RemoveWaiter(waiter);
                waiter.signalled = true;
                waiter.ev.Set();
            }
        }
    }
}
