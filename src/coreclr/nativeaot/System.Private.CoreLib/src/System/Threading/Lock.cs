// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    [ReflectionBlocked]
    public sealed class Lock : IDisposable
    {
        // The following constants define characteristics of spinning logic in the Lock class
        private const int SpinningNotInitialized = 0;
        private const int SpinningDisabled = -1;
        private const int MaxSpinningValue = 10000;

        //
        // NOTE: Lock must not have a static (class) constructor, as Lock itself is used to synchronize
        // class construction.  If Lock has its own class constructor, this can lead to infinite recursion.
        // All static data in Lock must be pre-initialized.
        //
        private static int s_maxSpinCount;

        //
        // m_state layout:
        //
        // bit 0: True if the lock is held, false otherwise.
        //
        // bit 1: True if we've set the event to wake a waiting thread.  The waiter resets this to false when it
        //        wakes up.  This avoids the overhead of setting the event multiple times.
        //
        // everything else: A count of the number of threads waiting on the event.
        //
        private const int Locked = 1;
        private const int WaiterWoken = 2;
        private const int WaiterCountIncrement = 4;

        private const int Uncontended = 0;

        private volatile int _state;

        private uint _recursionCount;
        private IntPtr _owningThreadId;
        private volatile AutoResetEvent? _lazyEvent;

        private AutoResetEvent Event
        {
            get
            {
                //
                // Can't use LazyInitializer.EnsureInitialized because Lock needs to stay low level enough
                // for the purposes of lazy generic lookups. LazyInitializer uses a generic delegate.
                //
                if (_lazyEvent == null)
                    Interlocked.CompareExchange(ref _lazyEvent, new AutoResetEvent(false), null);

                return _lazyEvent;
            }
        }

        public void Dispose()
        {
            _lazyEvent?.Dispose();
        }

        private static IntPtr CurrentNativeThreadId => (IntPtr)RuntimeImports.RhCurrentNativeThreadId();

        // On platforms where CurrentNativeThreadId redirects to ManagedThreadId.Current the inlined
        // version of Lock.Acquire has the ManagedThreadId.Current call not inlined, while the non-inlined
        // version has it inlined.  So it saves code to keep this function not inlined while having
        // the same runtime cost.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Acquire()
        {
            IntPtr currentThreadId = CurrentNativeThreadId;

            //
            // Make one quick attempt to acquire an uncontended lock
            //
            if (Interlocked.CompareExchange(ref _state, Locked, Uncontended) == Uncontended)
            {
                Debug.Assert(_owningThreadId == IntPtr.Zero);
                Debug.Assert(_recursionCount == 0);
                _owningThreadId = currentThreadId;
                return;
            }

            //
            // Fall back to the slow path for contention
            //
            bool success = TryAcquireContended(currentThreadId, Timeout.Infinite);
            Debug.Assert(success);
        }

        public bool TryAcquire(TimeSpan timeout)
        {
            return TryAcquire(WaitHandle.ToTimeoutMilliseconds(timeout));
        }

        public bool TryAcquire(int millisecondsTimeout, bool trackContentions = false)
        {
            if (millisecondsTimeout < -1)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);

            IntPtr currentThreadId = CurrentNativeThreadId;

            //
            // Make one quick attempt to acquire an uncontended lock
            //
            if (Interlocked.CompareExchange(ref _state, Locked, Uncontended) == Uncontended)
            {
                Debug.Assert(_owningThreadId == IntPtr.Zero);
                Debug.Assert(_recursionCount == 0);
                _owningThreadId = currentThreadId;
                return true;
            }

            //
            // Fall back to the slow path for contention
            //
            return TryAcquireContended(currentThreadId, millisecondsTimeout, trackContentions);
        }

        private bool TryAcquireContended(IntPtr currentThreadId, int millisecondsTimeout, bool trackContentions = false)
        {
            //
            // If we already own the lock, just increment the recursion count.
            //
            if (_owningThreadId == currentThreadId)
            {
                checked { _recursionCount++; }
                return true;
            }

            //
            // We've already made one lock attempt at this point, so bail early if the timeout is zero.
            //
            if (millisecondsTimeout == 0)
                return false;

            int spins = 1;

            if (s_maxSpinCount == SpinningNotInitialized)
            {
                // Use RhGetProcessCpuCount directly to avoid Environment.ProcessorCount->ClassConstructorRunner->Lock->Environment.ProcessorCount cycle
                s_maxSpinCount = (RuntimeImports.RhGetProcessCpuCount() > 1) ? MaxSpinningValue : SpinningDisabled;
            }

            while (true)
            {
                //
                // Try to grab the lock.  We may take the lock here even if there are existing waiters.  This creates the possibility
                // of starvation of waiters, but it also prevents lock convoys from destroying perf.
                // The starvation issue is largely mitigated by the priority boost the OS gives to a waiter when we set
                // the event, after we release the lock.  Eventually waiters will be boosted high enough to preempt this thread.
                //
                int oldState = _state;
                if ((oldState & Locked) == 0 && Interlocked.CompareExchange(ref _state, oldState | Locked, oldState) == oldState)
                    goto GotTheLock;

                //
                // Back off by a factor of 2 for each attempt, up to MaxSpinCount
                //
                if (spins <= s_maxSpinCount)
                {
                    RuntimeImports.RhSpinWait(spins);
                    spins *= 2;
                }
                else if (oldState != 0)
                {
                    //
                    // We reached our spin limit, and need to wait.  Increment the waiter count.
                    // Note that we do not do any overflow checking on this increment.  In order to overflow,
                    // we'd need to have about 1 billion waiting threads, which is inconceivable anytime in the
                    // forseeable future.
                    //
                    int newState = (oldState + WaiterCountIncrement) & ~WaiterWoken;
                    if (Interlocked.CompareExchange(ref _state, newState, oldState) == oldState)
                        break;
                }
            }

            //
            // Now we wait.
            //

            if (trackContentions)
            {
                Monitor.IncrementLockContentionCount();
            }

            TimeoutTracker timeoutTracker = TimeoutTracker.Start(millisecondsTimeout);
            AutoResetEvent ev = Event;

            while (true)
            {
                Debug.Assert(_state >= WaiterCountIncrement);

                bool waitSucceeded = ev.WaitOne(timeoutTracker.Remaining);

                while (true)
                {
                    int oldState = _state;
                    Debug.Assert(oldState >= WaiterCountIncrement);

                    // Clear the "waiter woken" bit.
                    int newState = oldState & ~WaiterWoken;

                    if ((oldState & Locked) == 0)
                    {
                        // The lock is available, try to get it.
                        newState |= Locked;
                        newState -= WaiterCountIncrement;

                        if (Interlocked.CompareExchange(ref _state, newState, oldState) == oldState)
                            goto GotTheLock;
                    }
                    else if (!waitSucceeded)
                    {
                        // The lock is not available, and we timed out.  We're not going to wait agin.
                        newState -= WaiterCountIncrement;

                        if (Interlocked.CompareExchange(ref _state, newState, oldState) == oldState)
                            return false;
                    }
                    else
                    {
                        // The lock is not available, and we didn't time out.  We're going to wait again.
                        if (Interlocked.CompareExchange(ref _state, newState, oldState) == oldState)
                            break;
                    }
                }
            }

        GotTheLock:
            Debug.Assert((_state | Locked) != 0);
            Debug.Assert(_owningThreadId == IntPtr.Zero);
            Debug.Assert(_recursionCount == 0);
            _owningThreadId = currentThreadId;
            return true;
        }

        public bool IsAcquired
        {
            get
            {
                //
                // The comment below is for platforms where CurrentNativeThreadId redirects to
                // ManagedThreadId.Current instead of being a compiler intrinsic.
                //
                // Compare the current owning thread ID with the current thread ID.  We need
                // to read the current thread's ID before we read m_owningThreadId.  Otherwise,
                // the following might happen:
                //
                // 1) We read m_owningThreadId, and get, say 42, which belongs to another thread.
                // 2) Thread 42 releases the lock, and exits.
                // 3) We call ManagedThreadId.Current.  If this is the first time it's been called
                //    on this thread, we'll go get a new ID.  We may reuse thread 42's ID, since
                //    that thread is dead.
                // 4) Now we're thread 42, and it looks like we own the lock, even though we don't.
                //
                // However, as long as we get this thread's ID first, we know it won't be reused,
                // because while we're doing this check the current thread is definitely still
                // alive.
                //
                IntPtr currentThreadId = CurrentNativeThreadId;
                bool acquired = (currentThreadId == _owningThreadId);
                if (acquired)
                    Debug.Assert((_state & Locked) != 0);
                return acquired;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Release()
        {
            if (!IsAcquired)
                throw new SynchronizationLockException();

            if (_recursionCount > 0)
                _recursionCount--;
            else
                ReleaseCore();
        }

        internal uint ReleaseAll()
        {
            Debug.Assert(IsAcquired);

            uint recursionCount = _recursionCount;
            _recursionCount = 0;

            ReleaseCore();

            return recursionCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReleaseCore()
        {
            Debug.Assert(_recursionCount == 0);
            _owningThreadId = IntPtr.Zero;

            //
            // Make one quick attempt to release an uncontended lock
            //
            if (Interlocked.CompareExchange(ref _state, Uncontended, Locked) == Locked)
                return;

            //
            // We have waiters; take the slow path.
            //
            ReleaseContended();
        }

        private void ReleaseContended()
        {
            Debug.Assert(_recursionCount == 0);
            Debug.Assert(_owningThreadId == IntPtr.Zero);

            while (true)
            {
                int oldState = _state;

                // clear the lock bit.
                int newState = oldState & ~Locked;

                if (oldState >= WaiterCountIncrement && (oldState & WaiterWoken) == 0)
                {
                    // there are waiters, and nobody has woken one.
                    newState |= WaiterWoken;
                    if (Interlocked.CompareExchange(ref _state, newState, oldState) == oldState)
                    {
                        Event.Set();
                        return;
                    }
                }
                else
                {
                    // no need to wake a waiter.
                    if (Interlocked.CompareExchange(ref _state, newState, oldState) == oldState)
                        return;
                }
            }
        }

        internal void Reacquire(uint previousRecursionCount)
        {
            Acquire();
            Debug.Assert(_recursionCount == 0);
            _recursionCount = previousRecursionCount;
        }

        internal struct TimeoutTracker
        {
            private int _start;
            private int _timeout;

            public static TimeoutTracker Start(int timeout)
            {
                TimeoutTracker tracker = new TimeoutTracker();
                tracker._timeout = timeout;
                if (timeout != Timeout.Infinite)
                    tracker._start = Environment.TickCount;
                return tracker;
            }

            public int Remaining
            {
                get
                {
                    if (_timeout == Timeout.Infinite)
                        return Timeout.Infinite;
                    int elapsed = Environment.TickCount - _start;
                    if (elapsed > _timeout)
                        return 0;
                    return _timeout - elapsed;
                }
            }
        }
    }
}
