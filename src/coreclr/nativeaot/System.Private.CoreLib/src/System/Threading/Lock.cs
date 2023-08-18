// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    public sealed class Lock : IDisposable
    {
        //
        // This lock is a hybrid spinning/blocking lock with dynamically adjusted spinning.
        // On a multiprocessor machine an acquiring thread will try to acquire multiple times
        // before going to sleep. The amount of spinning is dynamically adjusted based on past
        // history of the lock and will stay in the following range.
        //
        // We use doubling-up delays with a cap while spinning  (1,2,4,8,16,32,64,64,64,64, ...)
        // Thus 20 iterations is about 1000 speenwaits (20-50 ns each)
        // Context switch costs may vary and typically in 2-20 usec range
        // Even if we are the only thread trying to acquire the lock at 20-50 usec the cost of being
        // blocked+awaken may not be more than 2x of what we have already spent, so that is the max CPU time
        // that we will allow to burn while spinning.
        //
        // This may not be always optimal, but should be close enough.
        //     I.E. in a system consisting of exactly 2 threads, unlimited spinning may work better, but we
        //     will not optimize specifically for that.
        private const ushort MaxSpinLimit = 20;
        private const ushort MinSpinLimit = 3;
        private const ushort SpinningNotInitialized = MaxSpinLimit + 1;
        private const ushort SpinningDisabled = 0;

        //
        // We will use exponential backoff in rare cases when we need to change state atomically and cannot
        // make progress due to concurrent state changes by other threads.
        // While we cannot know the ideal amount of wait needed before making a successfull attempt,
        // the exponential backoff will generally be not more than 2X worse than the perfect guess and
        // will do a lot less attempts than an simple retry. On multiprocessor machine fruitless attempts
        // will cause unnecessary sharing of the contended state which may make modifying the state more expensive.
        // To protect against degenerate cases we will cap the per-iteration wait to 1024 spinwaits.
        //
        private const uint MaxExponentialBackoffBits = 10;

        //
        // This lock is unfair and permits acquiring a contended lock by a nonwaiter in the presence of waiters.
        // It is possible for one thread to keep holding the lock long enough that waiters go to sleep and
        // then release and reacquire fast enough that waiters have no chance to get the lock.
        // In extreme cases one thread could keep retaking the lock starving everybody else.
        // If we see woken waiters not able to take the lock for too long we will ask nonwaiters to wait.
        //
        private const uint WaiterWatchdogTicks = 100;

        //
        // NOTE: Lock must not have a static (class) constructor, as Lock itself is used to synchronize
        // class construction.  If Lock has its own class constructor, this can lead to infinite recursion.
        // All static data in Lock must be lazy-initialized.
        //
        internal static int s_processorCount;

        //
        // m_state layout:
        //
        // bit 0: True if the lock is held, false otherwise.
        //
        // bit 1: True if we've set the event to wake a waiting thread.  The waiter resets this to false when it
        //        wakes up.  This avoids the overhead of setting the event multiple times.
        //
        // bit 2: True if nonwaiters must not get ahead of waiters when acquiring a contended lock.
        //
        // everything else: A count of the number of threads waiting on the event.
        //
        private const int Uncontended = 0;
        private const int Locked = 1;
        private const int WaiterWoken = 2;
        private const int YieldToWaiters = 4;
        private const int WaiterCountIncrement = 8;

        // state of the lock
        private AutoResetEvent? _lazyEvent;
        private int _owningThreadId;
        private uint _recursionCount;
        private int _state;
        private ushort _spinLimit = SpinningNotInitialized;
        private short _wakeWatchDog;

        // used to transfer the state when inflating thin locks
        internal void InitializeLocked(int threadId, int recursionCount)
        {
            Debug.Assert(recursionCount == 0 || threadId != 0);

            _state = threadId == 0 ? Uncontended : Locked;
            _owningThreadId = threadId;
            _recursionCount = (uint)recursionCount;
        }

        private AutoResetEvent Event
        {
            get
            {
                if (_lazyEvent == null)
                    Interlocked.CompareExchange(ref _lazyEvent, new AutoResetEvent(false), null);

                return _lazyEvent;
            }
        }

        public void Dispose()
        {
            _lazyEvent?.Dispose();
        }

        private static int CurrentThreadId => Environment.CurrentManagedThreadId;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Acquire()
        {
            int currentThreadId = CurrentThreadId;
            if (TryAcquireOneShot(currentThreadId))
                return;

            //
            // Fall back to the slow path for contention
            //
            bool success = TryAcquireSlow(currentThreadId, Timeout.Infinite);
            Debug.Assert(success);
        }

        public bool TryAcquire(TimeSpan timeout)
        {
            return TryAcquire(WaitHandle.ToTimeoutMilliseconds(timeout));
        }

        public bool TryAcquire(int millisecondsTimeout)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1);

            int currentThreadId = CurrentThreadId;
            if (TryAcquireOneShot(currentThreadId))
                return true;

            //
            // Fall back to the slow path for contention
            //
            return TryAcquireSlow(currentThreadId, millisecondsTimeout, trackContentions: false);
        }

        internal bool TryAcquireNoSpin()
        {
            //
            // Make one quick attempt to acquire an uncontended lock
            //
            int currentThreadId = CurrentThreadId;
            if (TryAcquireOneShot(currentThreadId))
                return true;

            //
            // If we already own the lock, just increment the recursion count.
            //
            if (_owningThreadId == currentThreadId)
            {
                checked { _recursionCount++; }
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryAcquireOneShot(int currentThreadId)
        {
            int origState = _state;
            int expectedState = origState & ~(YieldToWaiters | Locked);
            int newState = origState | Locked;
            if (Interlocked.CompareExchange(ref _state, newState, expectedState) == expectedState)
            {
                Debug.Assert(_owningThreadId == 0);
                Debug.Assert(_recursionCount == 0);
                _owningThreadId = currentThreadId;
                return true;
            }

            return false;
        }

        private static unsafe void ExponentialBackoff(uint iteration)
        {
            if (iteration > 0)
            {
                // no need for much randomness here, we will just hash the stack address + iteration.
                uint rand = ((uint)&iteration + iteration) * 2654435769u;
                // set the highmost bit to ensure minimum number of spins is exponentialy increasing
                // that is in case some stack location results in a sequence of very low spin counts
                // it basically gurantees that we spin at least 1, 2, 4, 8, 16, times, and so on
                rand |= (1u << 31);
                uint spins = rand >> (byte)(32 - Math.Min(iteration, MaxExponentialBackoffBits));
                Thread.SpinWaitInternal((int)spins);
            }
        }

        internal bool TryAcquireSlow(int currentThreadId, int millisecondsTimeout, bool trackContentions = false)
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

            // since we have just made an attempt to accuire and failed, do a small pause
            Thread.SpinWaitInternal(1);

            if (_spinLimit == SpinningNotInitialized)
            {
                // Use RhGetProcessCpuCount directly to avoid Environment.ProcessorCount->ClassConstructorRunner->Lock->Environment.ProcessorCount cycle
                if (s_processorCount == 0)
                    s_processorCount = RuntimeImports.RhGetProcessCpuCount();

                _spinLimit = (s_processorCount > 1) ? MinSpinLimit : SpinningDisabled;
            }

            bool hasWaited = false;
            // we will retry after waking up
            while (true)
            {
                uint iteration = 0;

                // We will count when we failed to change the state of the lock and increase pauses
                // so that bursts of activity are better tolerated. This should not happen often.
                uint collisions = 0;

                // We will track the changes of ownership while we are trying to acquire the lock.
                int oldOwner = _owningThreadId;
                uint ownerChanged = 0;

                uint localSpinLimit = _spinLimit;
                // inner loop where we try acquiring the lock or registering as a waiter
                while (true)
                {
                    //
                    // Try to grab the lock.  We may take the lock here even if there are existing waiters.  This creates the possibility
                    // of starvation of waiters, but it also prevents lock convoys and preempted waiters from destroying perf.
                    // However, if we do not see _wakeWatchDog cleared for long enough, we go into YieldToWaiters mode to ensure some
                    // waiter progress.
                    //
                    int oldState = _state;
                    bool canAcquire = ((oldState & Locked) == 0) &&
                        (hasWaited || ((oldState & YieldToWaiters) == 0));

                    if (canAcquire)
                    {
                        int newState = oldState | Locked;
                        if (hasWaited)
                            newState = (newState - WaiterCountIncrement) & ~(WaiterWoken | YieldToWaiters);

                        if (Interlocked.CompareExchange(ref _state, newState, oldState) == oldState)
                        {
                            // GOT THE LOCK!!
                            if (hasWaited)
                                _wakeWatchDog = 0;

                            // now we can estimate how busy the lock is and adjust spinning accordingly
                            ushort spinLimit = _spinLimit;
                            if (ownerChanged != 0)
                            {
                                // The lock has changed ownership while we were trying to acquire it.
                                // It is a signal that we might want to spin less next time.
                                // Pursuing a lock that is being "stolen" by other threads is inefficient
                                // due to cache misses and unnecessary sharing of state that keeps invalidating.
                                if (spinLimit > MinSpinLimit)
                                {
                                    _spinLimit = (ushort)(spinLimit - 1);
                                }
                            }
                            else if (spinLimit < MaxSpinLimit && iteration > spinLimit / 2)
                            {
                                // we used more than 50% of allowed iterations, but the lock does not look very contested,
                                // we can allow a bit more spinning.
                                _spinLimit = (ushort)(spinLimit + 1);
                            }

                            Debug.Assert((_state | Locked) != 0);
                            Debug.Assert(_owningThreadId == 0);
                            Debug.Assert(_recursionCount == 0);
                            _owningThreadId = currentThreadId;
                            return true;
                        }
                    }

                    if (iteration++ < localSpinLimit)
                    {
                        int newOwner = _owningThreadId;
                        if (newOwner != 0 && newOwner != oldOwner)
                        {
                            ownerChanged++;
                            oldOwner = newOwner;
                        }

                        if (canAcquire)
                        {
                            collisions++;
                        }

                        // We failed to acquire the lock and want to retry after a pause.
                        // Ideally we will retry right when the lock becomes free, but we cannot know when that will happen.
                        // We will use a pause that doubles up on every iteration. It will not be more than 2x worse
                        // than the ideal guess, while minimizing the number of retries.
                        // We will allow pauses up to 64~128 spinwaits, or more if there are collisions.
                        ExponentialBackoff(Math.Min(iteration, 6) + collisions);
                        continue;
                    }
                    else if (!canAcquire)
                    {
                        //
                        // We reached our spin limit, and need to wait.  Increment the waiter count.
                        // Note that we do not do any overflow checking on this increment.  In order to overflow,
                        // we'd need to have about 1 billion waiting threads, which is inconceivable anytime in the
                        // forseeable future.
                        //
                        int newState = oldState + WaiterCountIncrement;
                        if (hasWaited)
                            newState = (newState - WaiterCountIncrement) & ~WaiterWoken;

                        if (Interlocked.CompareExchange(ref _state, newState, oldState) == oldState)
                            break;

                        collisions++;
                    }

                    ExponentialBackoff(collisions);
                }

                //
                // Now we wait.
                //

                if (trackContentions)
                {
                    Monitor.IncrementLockContentionCount();
                }

                TimeoutTracker timeoutTracker = TimeoutTracker.Start(millisecondsTimeout);
                Debug.Assert(_state >= WaiterCountIncrement);
                bool waitSucceeded = Event.WaitOne(millisecondsTimeout);
                Debug.Assert(_state >= WaiterCountIncrement);

                if (!waitSucceeded)
                    break;

                // we did not time out and will try acquiring the lock
                hasWaited = true;
                millisecondsTimeout = timeoutTracker.Remaining;
            }

            // We timed out.  We're not going to wait again.
            {
                uint iteration = 0;
                while (true)
                {
                    int oldState = _state;
                    Debug.Assert(oldState >= WaiterCountIncrement);

                    int newState = oldState - WaiterCountIncrement;

                    // We could not have consumed a wake, or the wait would've succeeded.
                    // If we are the last waiter though, we will clear WaiterWoken and YieldToWaiters
                    // just so that lock would not look like contended.
                    if (newState < WaiterCountIncrement)
                        newState = newState & ~WaiterWoken & ~YieldToWaiters;

                    if (Interlocked.CompareExchange(ref _state, newState, oldState) == oldState)
                        return false;

                    ExponentialBackoff(iteration++);
                }
            }
        }

        public bool IsAcquired
        {
            get
            {
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
                int currentThreadId = CurrentThreadId;
                return IsAcquiredByThread(currentThreadId);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsAcquiredByThread(int currentThreadId)
        {
            bool acquired = (currentThreadId == _owningThreadId);
            Debug.Assert(!acquired || (_state & Locked) != 0);
            return acquired;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Release()
        {
            ReleaseByThread(CurrentThreadId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ReleaseByThread(int threadId)
        {
            if (threadId != _owningThreadId)
                throw new SynchronizationLockException();

            if (_recursionCount == 0)
            {
                ReleaseCore();
                return;
            }

            _recursionCount--;
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
            _owningThreadId = 0;
            int origState = Interlocked.Decrement(ref _state);
            if (origState < WaiterCountIncrement || (origState & WaiterWoken) != 0)
            {
                return;
            }

            //
            // We have waiters; take the slow path.
            //
            AwakeWaiterIfNeeded();
        }

        private void AwakeWaiterIfNeeded()
        {
            uint iteration = 0;
            while (true)
            {
                int oldState = _state;
                if (oldState >= WaiterCountIncrement && (oldState & WaiterWoken) == 0)
                {
                    // there are waiters, and nobody has woken one.
                    int newState = oldState | WaiterWoken;

                    short lastWakeTicks = _wakeWatchDog;
                    if (lastWakeTicks != 0 && (short)Environment.TickCount - lastWakeTicks > WaiterWatchdogTicks)
                    {
                        newState |= YieldToWaiters;
                    }

                    if (Interlocked.CompareExchange(ref _state, newState, oldState) == oldState)
                    {
                        if (lastWakeTicks == 0)
                        {
                            // nonzero timestamp of the last wake
                            _wakeWatchDog = (short)(Environment.TickCount | 1);
                        }

                        Event.Set();
                        return;
                    }
                }
                else
                {
                    // no need to wake a waiter.
                    return;
                }

                ExponentialBackoff(iteration++);
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
