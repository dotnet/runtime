// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    [ReflectionBlocked]
    public sealed class Lock : IDisposable
    {
        // The following constants define characteristics of spinning logic in the Lock class
        private const uint MaxSpinLimit = 200;
        private const uint MinSpinLimit = 10;
        private const uint SpinningNotInitialized = MaxSpinLimit + 1;
        private const uint SpinningDisabled = 0;

        // We will use exponential backoff in cases when we need to change state atomically and cannot
        // make progress due to contention.
        // While we cannot know how much wait we need until a successfull attempt, exponential backoff
        // should generally be not more than 2X of that and will do a lot less tries than an eager retry.
        // To protect against degenerate cases we will cap the iteration wait up to 1024 spinwaits.
        private const uint MaxExponentialBackoffBits = 10;

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
        // everything else: A count of the number of threads waiting on the event.
        //
        private const int Locked = 1;
        private const int WaiterWoken = 2;
        private const int WaiterCountIncrement = 4;
        private const int Uncontended = 0;

        // state of the lock
        private AutoResetEvent? _lazyEvent;
        private int _owningThreadId;
        private uint _recursionCount;
        private int _state;
        private uint _spinLimit;

        // used to transfer the state when inflating thin locks
        internal void InitializeLocked(int threadId, int recursionCount)
        {
            Debug.Assert(recursionCount == 0 || threadId != 0);

            _state = threadId == 0 ? Uncontended : Locked;
            _owningThreadId = threadId;
            _recursionCount = (uint)recursionCount;
            _spinLimit = SpinningNotInitialized;
        }

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

        private static int CurrentThreadId => Environment.CurrentManagedThreadId;

        // the inlined version of Lock.Acquire would not inline ManagedThreadId.Current,
        // while the non-inlined version has it inlined.
        // So it saves code to keep this function not inlined while having the same runtime cost.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Acquire()
        {
            int currentThreadId = CurrentThreadId;
            if (TryAcquireOneShot(currentThreadId))
                return;

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

            int currentThreadId = CurrentThreadId;

            //
            // Make one quick attempt to acquire an uncontended lock
            //
            if (Interlocked.CompareExchange(ref _state, Locked, Uncontended) == Uncontended)
            {
                Debug.Assert(_owningThreadId == 0);
                Debug.Assert(_recursionCount == 0);
                _owningThreadId = currentThreadId;
                return true;
            }

            //
            // Fall back to the slow path for contention
            //
            return TryAcquireContended(currentThreadId, millisecondsTimeout, trackContentions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryAcquireOneShot(int currentThreadId)
        {
            //
            // Make one quick attempt to acquire an uncontended lock
            //
            if (Interlocked.CompareExchange(ref _state, Locked, Uncontended) == Uncontended)
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
                // no need for much randomness here, we will just hash the frame address + iteration.
                uint rand = ((uint)&iteration + iteration) * 2654435769u;
                uint spins = rand >> (byte)(32 - Math.Min(iteration, MaxExponentialBackoffBits));
                Thread.SpinWaitInternal((int)spins);
            }
        }

        private bool TryAcquireContended(int currentThreadId, int millisecondsTimeout, bool trackContentions = false)
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

            if (s_processorCount == 0)
            {
                // Use RhGetProcessCpuCount directly to avoid Environment.ProcessorCount->ClassConstructorRunner->Lock->Environment.ProcessorCount cycle
                s_processorCount = RuntimeImports.RhGetProcessCpuCount();
            }

            if (_spinLimit == SpinningNotInitialized)
            {
                _spinLimit = (s_processorCount > 1) ? MaxSpinLimit : SpinningDisabled;
            }

            bool hasWaited = false;
            // we will retry after waking up
            while (true)
            {
                uint iteration = 0;
                uint localSpinLimit = _spinLimit;
                // inner loop where we try acquiring the lock or registering as a waiter
                while (true)
                {
                    //
                    // Try to grab the lock.  We may take the lock here even if there are existing waiters.  This creates the possibility
                    // of starvation of waiters, but it also prevents lock convoys and preempted waiters from destroying perf.
                    //
                    int oldState = _state;
                    if ((oldState & Locked) == 0)
                    {
                        int newState = oldState | Locked;
                        if (hasWaited)
                            newState = (newState - WaiterCountIncrement) & ~WaiterWoken;

                        if (Interlocked.CompareExchange(ref _state, newState, oldState) == oldState)
                        {
                            // spinning was successful, update spin count
                            if (iteration < localSpinLimit && localSpinLimit < MaxSpinLimit)
                                _spinLimit = localSpinLimit + 1;

                            goto GotTheLock;
                        }
                    }

                    // spinning was unsuccessful. reduce spin count.
                    if (iteration == localSpinLimit && localSpinLimit > MinSpinLimit)
                        _spinLimit = localSpinLimit - 1;

                    if (iteration++ < localSpinLimit)
                    {
                        Thread.SpinWaitInternal(1);
                        continue;
                    }
                    else if ((oldState & Locked) != 0)
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
                    }

                    Debug.Assert(iteration >= localSpinLimit);
                    ExponentialBackoff(iteration - localSpinLimit);
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
            // We could not have observed a wake, or the wait would've succeeded
            // so we do not bother about WaiterWoken
            {
                uint iteration = 0;
                while (true)
                {
                    int oldState = _state;
                    Debug.Assert(oldState >= WaiterCountIncrement);

                    int newState = oldState - WaiterCountIncrement;

                    if (Interlocked.CompareExchange(ref _state, newState, oldState) == oldState)
                        return false;

                    ExponentialBackoff(iteration++);
                }
            }

        GotTheLock:
            Debug.Assert((_state | Locked) != 0);
            Debug.Assert(_owningThreadId == 0);
            Debug.Assert(_recursionCount == 0);
            _owningThreadId = currentThreadId;
            return true;
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
            if (acquired)
                Debug.Assert((_state & Locked) != 0);
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
            Debug.Assert(_owningThreadId == 0);

            uint iteration = 0;
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
