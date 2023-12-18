// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    /// <summary>
    /// Provides a way to get mutual exclusion in regions of code between different threads. A lock may be held by one thread at
    /// a time.
    /// </summary>
    /// <remarks>
    /// Threads that cannot immediately enter the lock may wait for the lock to be exited or until a specified timeout. A thread
    /// that holds a lock may enter the lock repeatedly without exiting it, such as recursively, in which case the thread should
    /// eventually exit the lock the same number of times to fully exit the lock and allow other threads to enter the lock.
    /// </remarks>
    [Runtime.Versioning.RequiresPreviewFeatures]
    public sealed partial class Lock
    {
        private const short SpinCountNotInitialized = short.MinValue;

        private const short DefaultMaxSpinCount = 22;
        private const short DefaultMinSpinCount = 1;

        // While spinning is parameterized in terms of iterations,
        // the internal tuning operates with spin count at a finer scale.
        // One iteration is mapped to 64 spin count units.
        private const short SpinCountScaleShift = 6;

        private static long s_contentionCount;

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

        // The field's type is not ThreadId to try to retain the relative order of fields of intrinsic types. The type system
        // appears to place struct fields after fields of other types, in which case there can be a greater chance that
        // _owningThreadId is not in the same cache line as _state.
#if TARGET_OSX && !NATIVEAOT
        private ulong _owningThreadId;
#else
        private uint _owningThreadId;
#endif

        //
        // m_state layout:
        //
        // bit 0: True if the lock is held, false otherwise.
        //
        // bit 1: True if nonwaiters must not get ahead of waiters when acquiring a contended lock.
        //
        // sign bit: True if we've set the event to wake a waiting thread.  The waiter resets this to false when it
        //        wakes up.  This avoids the overhead of setting the event multiple times.
        //
        // everything else: A count of the number of threads waiting on the event.
        //
        private const uint Unlocked = 0;
        private const uint Locked = 1;
        private const uint YieldToWaiters = 2;
        private const uint WaiterCountIncrement = 4;
        private const uint WaiterWoken = 1u << 31;

        private uint _state;
        private uint _recursionCount;
        private short _spinCount;
        private short _wakeWatchDog;
        private AutoResetEvent? _waitEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="Lock"/> class.
        /// </summary>
        public Lock() => _spinCount = SpinCountNotInitialized;

        /// <summary>
        /// Enters the lock. Once the method returns, the calling thread would be the only thread that holds the lock.
        /// </summary>
        /// <remarks>
        /// If the lock cannot be entered immediately, the calling thread waits for the lock to be exited. If the lock is
        /// already held by the calling thread, the lock is entered again. The calling thread should exit the lock as many times
        /// as it had entered the lock to fully exit the lock and allow other threads to enter the lock.
        /// </remarks>
        /// <exception cref="LockRecursionException">
        /// The lock has reached the limit of recursive enters. The limit is implementation-defined, but is expected to be high
        /// enough that it would typically not be reached when the lock is used properly.
        /// </exception>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Enter()
        {
            bool success = TryEnter_Inlined(timeoutMs: -1);
            Debug.Assert(success);
        }

        /// <summary>
        /// Enters the lock and returns a <see cref="Scope"/> that may be disposed to exit the lock. Once the method returns,
        /// the calling thread would be the only thread that holds the lock. This method is intended to be used along with a
        /// language construct that would automatically dispose the <see cref="Scope"/>, such as with the C# <code>using</code>
        /// statement.
        /// </summary>
        /// <returns>
        /// A <see cref="Scope"/> that may be disposed to exit the lock.
        /// </returns>
        /// <remarks>
        /// If the lock cannot be entered immediately, the calling thread waits for the lock to be exited. If the lock is
        /// already held by the calling thread, the lock is entered again. The calling thread should exit the lock, such as by
        /// disposing the returned <see cref="Scope"/>, as many times as it had entered the lock to fully exit the lock and
        /// allow other threads to enter the lock.
        /// </remarks>
        /// <exception cref="LockRecursionException">
        /// The lock has reached the limit of recursive enters. The limit is implementation-defined, but is expected to be high
        /// enough that it would typically not be reached when the lock is used properly.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Scope EnterScope()
        {
            Enter();
            return new Scope(this, new ThreadId(this._owningThreadId));
        }

        /// <summary>
        /// A disposable structure that is returned by <see cref="EnterScope()"/>, which when disposed, exits the lock.
        /// </summary>
        public ref struct Scope
        {
            private Lock? _lockObj;
            private ThreadId _currentThreadId;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Scope(Lock lockObj, ThreadId currentThreadId)
            {
                _lockObj = lockObj;
                _currentThreadId = currentThreadId;
            }

            /// <summary>
            /// Exits the lock.
            /// </summary>
            /// <remarks>
            /// If the calling thread holds the lock multiple times, such as recursively, the lock is exited only once. The
            /// calling thread should ensure that each enter is matched with an exit.
            /// </remarks>
            /// <exception cref="SynchronizationLockException">
            /// The calling thread does not hold the lock.
            /// </exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                Lock? lockObj = _lockObj;
                if (lockObj != null)
                {
                    _lockObj = null;
                    lockObj.Exit(_currentThreadId);
                }
            }
        }

        /// <summary>
        /// Tries to enter the lock without waiting. If the lock is entered, the calling thread would be the only thread that
        /// holds the lock.
        /// </summary>
        /// <returns>
        /// <code>true</code> if the lock was entered, <code>false</code> otherwise.
        /// </returns>
        /// <remarks>
        /// If the lock cannot be entered immediately, the method returns <code>false</code>. If the lock is already held by the
        /// calling thread, the lock is entered again. The calling thread should exit the lock as many times as it had entered
        /// the lock to fully exit the lock and allow other threads to enter the lock.
        /// </remarks>
        /// <exception cref="LockRecursionException">
        /// The lock has reached the limit of recursive enters. The limit is implementation-defined, but is expected to be high
        /// enough that it would typically not be reached when the lock is used properly.
        /// </exception>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool TryEnter() => TryEnter_Inlined(timeoutMs: 0);

        /// <summary>
        /// Tries to enter the lock, waiting for roughly the specified duration. If the lock is entered, the calling thread
        /// would be the only thread that holds the lock.
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// The rough duration in milliseconds for which the method will wait if the lock is not available. A value of
        /// <code>0</code> specifies that the method should not wait, and a value of <see cref="Timeout.Infinite"/> or
        /// <code>-1</code> specifies that the method should wait indefinitely until the lock is entered.
        /// </param>
        /// <returns>
        /// <code>true</code> if the lock was entered, <code>false</code> otherwise.
        /// </returns>
        /// <remarks>
        /// If the lock cannot be entered immediately, the calling thread waits for roughly the specified duration for the lock
        /// to be exited. If the lock is already held by the calling thread, the lock is entered again. The calling thread
        /// should exit the lock as many times as it had entered the lock to fully exit the lock and allow other threads to
        /// enter the lock.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="millisecondsTimeout"/> is less than <code>-1</code>.
        /// </exception>
        /// <exception cref="LockRecursionException">
        /// The lock has reached the limit of recursive enters. The limit is implementation-defined, but is expected to be high
        /// enough that it would typically not be reached when the lock is used properly.
        /// </exception>
        public bool TryEnter(int millisecondsTimeout)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1);
            return TryEnter_Outlined(millisecondsTimeout);
        }

        /// <summary>
        /// Tries to enter the lock, waiting for roughly the specified duration. If the lock is entered, the calling thread
        /// would be the only thread that holds the lock.
        /// </summary>
        /// <param name="timeout">
        /// The rough duration for which the method will wait if the lock is not available. The timeout is converted to a number
        /// of milliseconds by casting <see cref="TimeSpan.TotalMilliseconds"/> of the timeout to an integer value. A value
        /// representing <code>0</code> milliseconds specifies that the method should not wait, and a value representing
        /// <see cref="Timeout.Infinite"/> or <code>-1</code> milliseconds specifies that the method should wait indefinitely
        /// until the lock is entered.
        /// </param>
        /// <returns>
        /// <code>true</code> if the lock was entered, <code>false</code> otherwise.
        /// </returns>
        /// <remarks>
        /// If the lock cannot be entered immediately, the calling thread waits for roughly the specified duration for the lock
        /// to be exited. If the lock is already held by the calling thread, the lock is entered again. The calling thread
        /// should exit the lock as many times as it had entered the lock to fully exit the lock and allow other threads to
        /// enter the lock.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="timeout"/>, after its conversion to an integer millisecond value, represents a value that is less
        /// than <code>-1</code> milliseconds or greater than <see cref="int.MaxValue"/> milliseconds.
        /// </exception>
        /// <exception cref="LockRecursionException">
        /// The lock has reached the limit of recursive enters. The limit is implementation-defined, but is expected to be high
        /// enough that it would typically not be reached when the lock is used properly.
        /// </exception>
        public bool TryEnter(TimeSpan timeout) => TryEnter_Outlined(WaitHandle.ToTimeoutMilliseconds(timeout));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool TryEnter_Outlined(int timeoutMs) => TryEnter_Inlined(timeoutMs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryEnter_Inlined(int timeoutMs)
        {
            Debug.Assert(timeoutMs >= -1);

            ThreadId currentThreadId = ThreadId.Current_NoInitialize;
            if (currentThreadId.IsInitialized && this.TryLock())
            {
                Debug.Assert(!new ThreadId(_owningThreadId).IsInitialized);
                Debug.Assert(_recursionCount == 0);
                _owningThreadId = currentThreadId.Id;
                return true;
            }

            return TryEnterSlow(timeoutMs, currentThreadId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryLock()
        {
            uint origState = _state;
            if ((origState & (YieldToWaiters | Locked)) == 0)
            {
                uint newState = origState + Locked;
                if (Interlocked.CompareExchange(ref _state, newState, origState) == origState)
                {
                    Debug.Assert(_owningThreadId == 0);
                    Debug.Assert(_recursionCount == 0);
                    return true;
                }
            }

            return false;
        }

        private bool IsLocked => (_state & Locked) != 0;

        /// <summary>
        /// Exits the lock.
        /// </summary>
        /// <remarks>
        /// If the calling thread holds the lock multiple times, such as recursively, the lock is exited only once. The
        /// calling thread should ensure that each enter is matched with an exit.
        /// </remarks>
        /// <exception cref="SynchronizationLockException">
        /// The calling thread does not hold the lock.
        /// </exception>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Exit()
        {
            var owningThreadId = new ThreadId(_owningThreadId);
            if (!owningThreadId.IsInitialized || owningThreadId.Id != ThreadId.Current_NoInitialize.Id)
            {
                ThrowHelper.ThrowSynchronizationLockException_LockExit();
            }

            ExitImpl();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Exit(ThreadId currentThreadId)
        {
            Debug.Assert(currentThreadId.IsInitialized);
            Debug.Assert(currentThreadId.Id == ThreadId.Current_NoInitialize.Id);

            if (_owningThreadId != currentThreadId.Id)
            {
                ThrowHelper.ThrowSynchronizationLockException_LockExit();
            }

            ExitImpl();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExitImpl()
        {
            Debug.Assert(new ThreadId(_owningThreadId).IsInitialized);
            Debug.Assert(_owningThreadId == ThreadId.Current_NoInitialize.Id);
            Debug.Assert(this.IsLocked);

            if (_recursionCount == 0)
            {
                ReleaseCore();
                return;
            }

            _recursionCount--;
        }

        // we use this to de-synchronize threads if interlocked operations fail
        // we will pick a random number in exponentially expanding range and spin that many times
        private static unsafe void CollisionBackoff(int collisions)
        {
            Debug.Assert(collisions > 0);

            // no need for much randomness here, we will just hash the stack address + s_contentionCount.
            uint rand = ((uint)&collisions + (uint)s_contentionCount) * 2654435769u;
            uint spins = rand >> (byte)(32 - Math.Min(collisions, MaxExponentialBackoffBits));
            Thread.SpinWait((int)spins);
        }

        // same idea as in CollisionBackoff, but with guaranteed minimum wait
        private static unsafe void IterationBackoff(int iteration)
        {
            Debug.Assert(iteration > 0 && iteration < MaxExponentialBackoffBits);

            uint rand = ((uint)&iteration + (uint)s_contentionCount) * 2654435769u;
            // set the highmost bit to ensure minimum number of spins is exponentialy increasing
            // it basically gurantees that we spin at least 1, 2, 4, 8, 16, times, and so on
            rand |= (1u << 31);
            uint spins = rand >> (byte)(32 - iteration);
            Thread.SpinWait((int)spins);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal bool TryEnterSlow(int timeoutMs, ThreadId currentThreadId)
        {
            Debug.Assert(timeoutMs >= -1);

            if (!currentThreadId.IsInitialized)
            {
                // The thread info hasn't been initialized yet for this thread, and the fast path hasn't been tried yet. After
                // initializing the thread info, try the fast path first.
                currentThreadId.InitializeForCurrentThread();
                Debug.Assert(_owningThreadId != currentThreadId.Id);
                if (this.TryLock())
                {
                    Debug.Assert(!new ThreadId(_owningThreadId).IsInitialized);
                    Debug.Assert(_recursionCount == 0);
                    _owningThreadId = currentThreadId.Id;
                    return true;
                }
            }
            else if (_owningThreadId == currentThreadId.Id)
            {
                Debug.Assert(this.IsLocked);

                uint newRecursionCount = _recursionCount + 1;
                if (newRecursionCount != 0)
                {
                    _recursionCount = newRecursionCount;
                    return true;
                }

                throw new LockRecursionException(SR.Lock_Enter_LockRecursionException);
            }

            if (timeoutMs == 0)
            {
                return false;
            }

            if (_spinCount == SpinCountNotInitialized)
            {
                LazyInit();
                _spinCount = s_minSpinCount;
            }

            bool hasWaited = false;
            long contentionTrackingStartedTicks = 0;
            // we will retry after waking up
            while (true)
            {
                int iteration = 1;

                // We will count when we failed to change the state of the lock and increase pauses
                // so that bursts of activity are better tolerated. This should not happen often.
                int collisions = 0;

                // We will track the changes of ownership while we are trying to acquire the lock.
                var oldOwner = _owningThreadId;
                uint ownerChanged = 0;

                int iterationLimit = _spinCount >> SpinCountScaleShift;
                // inner loop where we try acquiring the lock or registering as a waiter
                while (true)
                {
                    //
                    // Try to grab the lock.  We may take the lock here even if there are existing waiters.  This creates the possibility
                    // of starvation of waiters, but it also prevents lock convoys and preempted waiters from destroying perf.
                    // However, if we do not see _wakeWatchDog cleared for long enough, we go into YieldToWaiters mode to ensure some
                    // waiter progress.
                    //
                    uint oldState = _state;
                    bool canAcquire = ((oldState & Locked) == Unlocked) &&
                        (hasWaited || ((oldState & YieldToWaiters) == 0));

                    if (canAcquire)
                    {
                        uint newState = oldState | Locked;
                        if (hasWaited)
                            newState = (newState - WaiterCountIncrement) & ~(WaiterWoken | YieldToWaiters);

                        if (Interlocked.CompareExchange(ref _state, newState, oldState) == oldState)
                        {
                            // GOT THE LOCK!!
                            Debug.Assert((_state | Locked) != 0);
                            Debug.Assert(_owningThreadId == 0);
                            Debug.Assert(_recursionCount == 0);
                            _owningThreadId = currentThreadId.Id;

                            if (hasWaited)
                                _wakeWatchDog = 0;

                            // now we can estimate how busy the lock is and adjust spinning accordingly
                            short spinLimit = _spinCount;
                            if (ownerChanged != 0)
                            {
                                // The lock has changed ownership while we were trying to acquire it.
                                // It is a signal that we might want to spin less next time.
                                // Pursuing a lock that is being "stolen" by other threads is inefficient
                                // due to cache misses and unnecessary sharing of state that keeps invalidating.
                                if (spinLimit > s_minSpinCount)
                                {
                                    _spinCount = (short)(spinLimit - 1);
                                }
                            }
                            else if (spinLimit < s_maxSpinCount &&
                                iteration >= (spinLimit >> SpinCountScaleShift))
                            {
                                // we used all of allowed iterations, but the lock does not look very contested,
                                // we can allow a bit more spinning.
                                //
                                // NB: if we acquired the lock while registering a waiter, and owner did not change it still counts.
                                //     (however iteration does not grow beyond the iterationLimit)
                                _spinCount = (short)(spinLimit + 1);
                            }

                            if (contentionTrackingStartedTicks != 0)
                                LogContentionEnd(contentionTrackingStartedTicks);

                            return true;
                        }
                    }

                    var newOwner = _owningThreadId;
                    if (newOwner != 0 && newOwner != oldOwner)
                    {
                        if (oldOwner != 0)
                            ownerChanged++;

                        oldOwner = newOwner;
                    }

                    if (iteration < iterationLimit)
                    {
                        // We failed to acquire the lock and want to retry after a pause.
                        // Ideally we will retry right when the lock becomes free, but we cannot know when that will happen.
                        // We will use a pause that doubles up on every iteration. It will not be more than 2x worse
                        // than the ideal guess, while minimizing the number of retries.
                        // We will allow pauses up to 64~128 spinwaits.
                        IterationBackoff(Math.Min(iteration, 6));
                        iteration++;
                        continue;
                    }
                    else if (!canAcquire)
                    {
                        // make sure we have the event before committing to wait on it
                        if (_waitEvent == null)
                            CreateWaitEvent();

                        //
                        // We reached our spin limit, and need to wait.  Increment the waiter count.
                        // Note that we do not do any overflow checking on this increment.  In order to overflow,
                        // we'd need to have about 1 billion waiting threads, which is inconceivable anytime in the
                        // forseeable future.
                        //
                        uint newState = oldState + WaiterCountIncrement;
                        if (hasWaited)
                            newState = (newState - WaiterCountIncrement) & ~WaiterWoken;

                        if (Interlocked.CompareExchange(ref _state, newState, oldState) == oldState)
                            break;
                    }

                    CollisionBackoff(++collisions);
                }

                //
                // Now we wait.
                //

                TimeoutTracker timeoutTracker = TimeoutTracker.Start(timeoutMs);

                // Lock was not acquired and a waiter was registered. All following paths need to unregister the waiter, including
                // exceptional paths.
                try
                {
                    Interlocked.Increment(ref s_contentionCount);

                    if (contentionTrackingStartedTicks == 0)
                        contentionTrackingStartedTicks = LogContentionStart();

                    Debug.Assert(_state >= WaiterCountIncrement);
                    bool waitSucceeded = _waitEvent!.WaitOne(timeoutMs);
                    Debug.Assert(_state >= WaiterCountIncrement);

                    if (!waitSucceeded)
                        break;
                }
                catch
                {
                    // waiting failed
                    UnregisterWaiter(contentionTrackingStartedTicks);
                    throw;
                }

                // we did not time out and will try acquiring the lock again.
                timeoutMs = timeoutTracker.Remaining;
                hasWaited = true;
            }

            // We timed out.  We're not going to wait again.
            UnregisterWaiter(contentionTrackingStartedTicks);
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void UnregisterWaiter(long contentionTrackingStartedTicks)
        {
            int collisions = 0;
            while (true)
            {
                uint oldState = _state;
                Debug.Assert(oldState >= WaiterCountIncrement);

                uint newState = oldState - WaiterCountIncrement;

                // We could not have consumed a wake, or the wait would've succeeded.
                // If we are the last waiter though, we will clear WaiterWoken and YieldToWaiters
                // just so that lock would not look like contended.
                if (newState < WaiterCountIncrement)
                    newState &= ~(WaiterWoken | YieldToWaiters);

                if (Interlocked.CompareExchange(ref _state, newState, oldState) == oldState)
                {
                    if (contentionTrackingStartedTicks != 0)
                        LogContentionEnd(contentionTrackingStartedTicks);

                    return;
                }

                CollisionBackoff(++collisions);
            }
        }

        internal struct TimeoutTracker
        {
            private int _start;
            private int _timeout;

            public static TimeoutTracker Start(int timeout)
            {
                TimeoutTracker tracker = default;
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe void CreateWaitEvent()
        {
            Debug.Assert(!IsHeldByCurrentThread);

            var newWaitEvent = new AutoResetEvent(false);
            if (Interlocked.CompareExchange(ref _waitEvent, newWaitEvent, null) == null)
            {
                // Also check NativeRuntimeEventSource.Log.IsEnabled() to enable trimming
                if (StaticsInitComplete() && NativeRuntimeEventSource.Log.IsEnabled())
                {
                    if (NativeRuntimeEventSource.Log.IsEnabled(
                                EventLevel.Informational,
                                NativeRuntimeEventSource.Keywords.ContentionKeyword))
                    {
                        NativeRuntimeEventSource.Log.ContentionLockCreated(this);
                    }
                }
            }
            else
            {
                newWaitEvent.Dispose();
            }
        }

        private long LogContentionStart()
        {
            Debug.Assert(!IsHeldByCurrentThread);

            // Also check NativeRuntimeEventSource.Log.IsEnabled() to enable trimming
            if (StaticsInitComplete() && NativeRuntimeEventSource.Log.IsEnabled())
            {
                if (NativeRuntimeEventSource.Log.IsEnabled(
                            EventLevel.Informational,
                            NativeRuntimeEventSource.Keywords.ContentionKeyword))
                {
                    NativeRuntimeEventSource.Log.ContentionStart(this);

                    return Stopwatch.GetTimestamp();
                }
            }

            return 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogContentionEnd(long contentionTrackingStartedTicks)
        {
            Debug.Assert(IsHeldByCurrentThread);

            double waitDurationNs =
                (Stopwatch.GetTimestamp() - contentionTrackingStartedTicks) * 1_000_000_000.0 / Stopwatch.Frequency;

            try
            {
                // Also check NativeRuntimeEventSource.Log.IsEnabled() to enable trimming
                if (NativeRuntimeEventSource.Log.IsEnabled())
                {
                    NativeRuntimeEventSource.Log.ContentionStop(waitDurationNs);
                }
            }
            catch
            {
                // We are throwing. The acquire failed and we should not leave the lock locked.
                this.Exit();
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReleaseCore()
        {
            Debug.Assert(_recursionCount == 0);
            _owningThreadId = 0;
            uint origState = Interlocked.Decrement(ref _state);
            if ((int)origState < (int)WaiterCountIncrement) // true if have no waiters or WaiterWoken is set
            {
                return;
            }

            //
            // We have waiters; take the slow path.
            //
            AwakeWaiterIfNeeded();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AwakeWaiterIfNeeded()
        {
            int collisions = 0;
            while (true)
            {
                uint oldState = _state;
                if ((int)oldState >= (int)WaiterCountIncrement) // false if WaiterWoken is set
                {
                    // there are waiters, and nobody has woken one.
                    uint newState = oldState | WaiterWoken;

                    short lastWakeTicks = _wakeWatchDog;
                    if (lastWakeTicks != 0 && (short)Environment.TickCount - lastWakeTicks > WaiterWatchdogTicks)
                    {
                        newState |= YieldToWaiters;
                    }

                    if (Interlocked.CompareExchange(ref _state, newState, oldState) == oldState)
                    {
                        _waitEvent!.Set();
                        if (lastWakeTicks == 0)
                        {
                            // nonzero timestamp of the last wake
                            _wakeWatchDog = (short)(Environment.TickCount | 1);
                        }

                        return;
                    }
                }
                else
                {
                    // no need to wake a waiter.
                    return;
                }

                CollisionBackoff(++collisions);
            }
        }

        /// <summary>
        /// <code>true</code> if the lock is held by the calling thread, <code>false</code> otherwise.
        /// </summary>
        public bool IsHeldByCurrentThread
        {
            get
            {
                var owningThreadId = new ThreadId(_owningThreadId);
                bool isHeld = owningThreadId.IsInitialized && owningThreadId.Id == ThreadId.Current_NoInitialize.Id;
                Debug.Assert(!isHeld || this.IsLocked);
                return isHeld;
            }
        }

        internal static long ContentionCount => s_contentionCount;
        internal void Dispose() => _waitEvent?.Dispose();

        internal nint LockIdForEvents
        {
            get
            {
                Debug.Assert(_waitEvent != null);
                return _waitEvent.SafeWaitHandle.DangerousGetHandle();
            }
        }

        internal unsafe nint ObjectIdForEvents
        {
            get
            {
                Lock lockObj = this;
                return *(nint*)Unsafe.AsPointer(ref lockObj);
            }
        }

        internal ulong OwningThreadId => _owningThreadId;

        // Lock starts with MinSpinCount and may self-adjust up to the MaxSpinCount
        // Setting MaxSpinCount <= MinSpinCount will effectively disable adaptive spin adjustment
        private static short DetermineMaxSpinCount()
        {
            var count = AppContextConfigHelper.GetInt16Config(
                "System.Threading.Lock.MaxSpinCount",
                "DOTNET_Lock_MaxSpinCount",
                DefaultMaxSpinCount,
                allowNegative: false);

            return count >= short.MaxValue >> SpinCountScaleShift ?
                DefaultMaxSpinCount :
                count;
        }

        private static short DetermineMinSpinCount()
        {
            var count = AppContextConfigHelper.GetInt16Config(
                "System.Threading.Lock.MinSpinCount",
                "DOTNET_Lock_MinSpinCount",
                DefaultMinSpinCount,
                allowNegative: false);

            return count >= short.MaxValue >> SpinCountScaleShift ?
                DefaultMaxSpinCount :
                count;
        }
    }
}
