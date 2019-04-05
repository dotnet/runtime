// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#pragma warning disable 0420

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// A spin lock is a mutual exclusion lock primitive where a thread trying to acquire the lock waits in a loop ("spins")
// repeatedly checking until the lock becomes available. As the thread remains active performing a non-useful task,
// the use of such a lock is a kind of busy waiting and consumes CPU resources without performing real work. 
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

#nullable enable
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    /// <summary>
    /// Provides a mutual exclusion lock primitive where a thread trying to acquire the lock waits in a loop
    /// repeatedly checking until the lock becomes available.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Spin locks can be used for leaf-level locks where the object allocation implied by using a <see
    /// cref="System.Threading.Monitor"/>, in size or due to garbage collection pressure, is overly
    /// expensive. Avoiding blocking is another reason that a spin lock can be useful, however if you expect
    /// any significant amount of blocking, you are probably best not using spin locks due to excessive
    /// spinning. Spinning can be beneficial when locks are fine grained and large in number (for example, a
    /// lock per node in a linked list) as well as when lock hold times are always extremely short. In
    /// general, while holding a spin lock, one should avoid blocking, calling anything that itself may
    /// block, holding more than one spin lock at once, making dynamically dispatched calls (interface and
    /// virtuals), making statically dispatched calls into any code one doesn't own, or allocating memory.
    /// </para>
    /// <para>
    /// <see cref="SpinLock"/> should only be used when it's been determined that doing so will improve an
    /// application's performance. It's also important to note that <see cref="SpinLock"/> is a value type,
    /// for performance reasons. As such, one must be very careful not to accidentally copy a SpinLock
    /// instance, as the two instances (the original and the copy) would then be completely independent of
    /// one another, which would likely lead to erroneous behavior of the application. If a SpinLock instance
    /// must be passed around, it should be passed by reference rather than by value.
    /// </para>
    /// <para>
    /// Do not store <see cref="SpinLock"/> instances in readonly fields.
    /// </para>
    /// <para>
    /// All members of <see cref="SpinLock"/> are thread-safe and may be used from multiple threads
    /// concurrently.
    /// </para>
    /// </remarks>
    [DebuggerTypeProxy(typeof(SystemThreading_SpinLockDebugView))]
    [DebuggerDisplay("IsHeld = {IsHeld}")]
    public struct SpinLock
    {
        // The current ownership state is a single signed int. There are two modes:
        //
        //    1) Ownership tracking enabled: the high bit is 0, and the remaining bits
        //       store the managed thread ID of the current owner.  When the 31 low bits
        //       are 0, the lock is available.
        //    2) Performance mode: when the high bit is 1, lock availability is indicated by the low bit.  
        //       When the low bit is 1 -- the lock is held; 0 -- the lock is available.
        //
        // There are several masks and constants below for convenience.

        private volatile int _owner;

        // After how many yields, call Sleep(1)
        private const int SLEEP_ONE_FREQUENCY = 40;

        // After how many yields, check the timeout
        private const int TIMEOUT_CHECK_FREQUENCY = 10;

        // Thr thread tracking disabled mask
        private const int LOCK_ID_DISABLE_MASK = unchecked((int)0x80000000);        // 1000 0000 0000 0000 0000 0000 0000 0000

        //the lock is held by some thread, but we don't know which
        private const int LOCK_ANONYMOUS_OWNED = 0x1;                               // 0000 0000 0000 0000 0000 0000 0000 0001

        // Waiters mask if the thread tracking is disabled
        private const int WAITERS_MASK = ~(LOCK_ID_DISABLE_MASK | 1);               // 0111 1111 1111 1111 1111 1111 1111 1110

        // The Thread tacking is disabled and the lock bit is set, used in Enter fast path to make sure the id is disabled and lock is available
        private const int ID_DISABLED_AND_ANONYMOUS_OWNED = unchecked((int)0x80000001); // 1000 0000 0000 0000 0000 0000 0000 0001

        // If the thread is unowned if:
        // m_owner zero and the thread tracking is enabled
        // m_owner & LOCK_ANONYMOUS_OWNED = zero and the thread tracking is disabled
        private const int LOCK_UNOWNED = 0;

        // The maximum number of waiters (only used if the thread tracking is disabled)
        // The actual maximum waiters count is this number divided by two because each waiter increments the waiters count by 2
        // The waiters count is calculated by m_owner & WAITERS_MASK 01111....110
        private const int MAXIMUM_WAITERS = WAITERS_MASK;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CompareExchange(ref int location, int value, int comparand, ref bool success)
        {
            int result = Interlocked.CompareExchange(ref location, value, comparand);
            success = (result == comparand);
            return result;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Threading.SpinLock"/>
        /// structure with the option to track thread IDs to improve debugging.
        /// </summary>
        /// <remarks>
        /// The default constructor for <see cref="SpinLock"/> tracks thread ownership.
        /// </remarks>
        /// <param name="enableThreadOwnerTracking">Whether to capture and use thread IDs for debugging
        /// purposes.</param>
        public SpinLock(bool enableThreadOwnerTracking)
        {
            _owner = LOCK_UNOWNED;
            if (!enableThreadOwnerTracking)
            {
                _owner |= LOCK_ID_DISABLE_MASK;
                Debug.Assert(!IsThreadOwnerTrackingEnabled, "property should be false by now");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Threading.SpinLock"/>
        /// structure with the option to track thread IDs to improve debugging.
        /// </summary>
        /// <remarks>
        /// The default constructor for <see cref="SpinLock"/> tracks thread ownership.
        /// </remarks>
        /// <summary>
        /// Acquires the lock in a reliable manner, such that even if an exception occurs within the method
        /// call, <paramref name="lockTaken"/> can be examined reliably to determine whether the lock was
        /// acquired.
        /// </summary>
        /// <remarks>
        /// <see cref="SpinLock"/> is a non-reentrant lock, meaning that if a thread holds the lock, it is
        /// not allowed to enter the lock again. If thread ownership tracking is enabled (whether it's
        /// enabled is available through <see cref="IsThreadOwnerTrackingEnabled"/>), an exception will be
        /// thrown when a thread tries to re-enter a lock it already holds. However, if thread ownership
        /// tracking is disabled, attempting to enter a lock already held will result in deadlock.
        /// </remarks>
        /// <param name="lockTaken">True if the lock is acquired; otherwise, false. <paramref
        /// name="lockTaken"/> must be initialized to false prior to calling this method.</param>
        /// <exception cref="T:System.Threading.LockRecursionException">
        /// Thread ownership tracking is enabled, and the current thread has already acquired this lock.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The <paramref name="lockTaken"/> argument must be initialized to false prior to calling Enter.
        /// </exception>
        public void Enter(ref bool lockTaken)
        {
            // Try to keep the code and branching in this method as small as possible in order to inline the method
            int observedOwner = _owner;
            if (lockTaken || // invalid parameter
                (observedOwner & ID_DISABLED_AND_ANONYMOUS_OWNED) != LOCK_ID_DISABLE_MASK || // thread tracking is enabled or the lock is already acquired
                CompareExchange(ref _owner, observedOwner | LOCK_ANONYMOUS_OWNED, observedOwner, ref lockTaken) != observedOwner) //acquiring the lock failed
                ContinueTryEnter(Timeout.Infinite, ref lockTaken); // Then try the slow path if any of the above conditions is met
        }

        /// <summary>
        /// Attempts to acquire the lock in a reliable manner, such that even if an exception occurs within
        /// the method call, <paramref name="lockTaken"/> can be examined reliably to determine whether the
        /// lock was acquired.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="Enter"/>, TryEnter will not block waiting for the lock to be available. If the
        /// lock is not available when TryEnter is called, it will return immediately without any further
        /// spinning.
        /// </remarks>
        /// <param name="lockTaken">True if the lock is acquired; otherwise, false. <paramref
        /// name="lockTaken"/> must be initialized to false prior to calling this method.</param>
        /// <exception cref="T:System.Threading.LockRecursionException">
        /// Thread ownership tracking is enabled, and the current thread has already acquired this lock.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The <paramref name="lockTaken"/> argument must be initialized to false prior to calling TryEnter.
        /// </exception>
        public void TryEnter(ref bool lockTaken)
        {
            int observedOwner = _owner;
            if (((observedOwner & LOCK_ID_DISABLE_MASK) == 0) | lockTaken)
            {
                // Thread tracking enabled or invalid arg. Take slow path.
                ContinueTryEnter(0, ref lockTaken);
            }
            else if ((observedOwner & LOCK_ANONYMOUS_OWNED) != 0)
            {
                // Lock already held by someone
                lockTaken = false;
            }
            else
            {
                // Lock wasn't held; try to acquire it.
                CompareExchange(ref _owner, observedOwner | LOCK_ANONYMOUS_OWNED, observedOwner, ref lockTaken);
            }
        }

        /// <summary>
        /// Attempts to acquire the lock in a reliable manner, such that even if an exception occurs within
        /// the method call, <paramref name="lockTaken"/> can be examined reliably to determine whether the
        /// lock was acquired.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="Enter"/>, TryEnter will not block indefinitely waiting for the lock to be
        /// available. It will block until either the lock is available or until the <paramref
        /// name="timeout"/>
        /// has expired.
        /// </remarks>
        /// <param name="timeout">A <see cref="System.TimeSpan"/> that represents the number of milliseconds
        /// to wait, or a <see cref="System.TimeSpan"/> that represents -1 milliseconds to wait indefinitely.
        /// </param>
        /// <param name="lockTaken">True if the lock is acquired; otherwise, false. <paramref
        /// name="lockTaken"/> must be initialized to false prior to calling this method.</param>
        /// <exception cref="T:System.Threading.LockRecursionException">
        /// Thread ownership tracking is enabled, and the current thread has already acquired this lock.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The <paramref name="lockTaken"/> argument must be initialized to false prior to calling TryEnter.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout"/> is a negative
        /// number other than -1 milliseconds, which represents an infinite time-out -or- timeout is greater
        /// than <see cref="System.Int32.MaxValue"/> milliseconds.
        /// </exception>
        public void TryEnter(TimeSpan timeout, ref bool lockTaken)
        {
            // Validate the timeout
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(timeout), timeout, SR.SpinLock_TryEnter_ArgumentOutOfRange);
            }

            // Call reliable enter with the int-based timeout milliseconds
            TryEnter((int)timeout.TotalMilliseconds, ref lockTaken);
        }

        /// <summary>
        /// Attempts to acquire the lock in a reliable manner, such that even if an exception occurs within
        /// the method call, <paramref name="lockTaken"/> can be examined reliably to determine whether the
        /// lock was acquired.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="Enter"/>, TryEnter will not block indefinitely waiting for the lock to be
        /// available. It will block until either the lock is available or until the <paramref
        /// name="millisecondsTimeout"/> has expired.
        /// </remarks>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see
        /// cref="System.Threading.Timeout.Infinite"/> (-1) to wait indefinitely.</param>
        /// <param name="lockTaken">True if the lock is acquired; otherwise, false. <paramref
        /// name="lockTaken"/> must be initialized to false prior to calling this method.</param>
        /// <exception cref="T:System.Threading.LockRecursionException">
        /// Thread ownership tracking is enabled, and the current thread has already acquired this lock.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The <paramref name="lockTaken"/> argument must be initialized to false prior to calling TryEnter.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is
        /// a negative number other than -1, which represents an infinite time-out.</exception>
        public void TryEnter(int millisecondsTimeout, ref bool lockTaken)
        {
            int observedOwner = _owner;
            if (millisecondsTimeout < -1 || //invalid parameter
                lockTaken || //invalid parameter
                (observedOwner & ID_DISABLED_AND_ANONYMOUS_OWNED) != LOCK_ID_DISABLE_MASK ||  //thread tracking is enabled or the lock is already acquired
                CompareExchange(ref _owner, observedOwner | LOCK_ANONYMOUS_OWNED, observedOwner, ref lockTaken) != observedOwner) // acquiring the lock failed
                ContinueTryEnter(millisecondsTimeout, ref lockTaken); // The call the slow pth
        }

        /// <summary>
        /// Try acquire the lock with long path, this is usually called after the first path in Enter and
        /// TryEnter failed The reason for short path is to make it inline in the run time which improves the
        /// performance. This method assumed that the parameter are validated in Enter or TryEnter method.
        /// </summary>
        /// <param name="millisecondsTimeout">The timeout milliseconds</param>
        /// <param name="lockTaken">The lockTaken param</param>
        private void ContinueTryEnter(int millisecondsTimeout, ref bool lockTaken)
        {
            // The fast path doesn't throw any exception, so we have to validate the parameters here
            if (lockTaken)
            {
                lockTaken = false;
                throw new ArgumentException(SR.SpinLock_TryReliableEnter_ArgumentException);
            }

            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(millisecondsTimeout), millisecondsTimeout, SR.SpinLock_TryEnter_ArgumentOutOfRange);
            }

            uint startTime = 0;
            if (millisecondsTimeout != Timeout.Infinite && millisecondsTimeout != 0)
            {
                startTime = TimeoutHelper.GetTime();
            }

            if (IsThreadOwnerTrackingEnabled)
            {
                // Slow path for enabled thread tracking mode
                ContinueTryEnterWithThreadTracking(millisecondsTimeout, startTime, ref lockTaken);
                return;
            }

            // then thread tracking is disabled
            // In this case there are three ways to acquire the lock
            // 1- the first way the thread either tries to get the lock if it's free or updates the waiters, if the turn >= the processors count then go to 3 else go to 2
            // 2- In this step the waiter threads spins and tries to acquire the lock, the number of spin iterations and spin count is dependent on the thread turn
            // the late the thread arrives the more it spins and less frequent it check the lock availability
            // Also the spins count is increases each iteration
            // If the spins iterations finished and failed to acquire the lock, go to step 3
            // 3- This is the yielding step, there are two ways of yielding Thread.Yield and Sleep(1)
            // If the timeout is expired in after step 1, we need to decrement the waiters count before returning

            int observedOwner;
            int turn = int.MaxValue;
            //***Step 1, take the lock or update the waiters

            // try to acquire the lock directly if possible or update the waiters count
            observedOwner = _owner;
            if ((observedOwner & LOCK_ANONYMOUS_OWNED) == LOCK_UNOWNED)
            {
                if (CompareExchange(ref _owner, observedOwner | 1, observedOwner, ref lockTaken) == observedOwner)
                {
                    // Acquired lock
                    return;
                }

                if (millisecondsTimeout == 0)
                {
                    // Did not acquire lock in CompareExchange and timeout is 0 so fail fast
                    return;
                }
            }
            else if (millisecondsTimeout == 0)
            {
                // Did not acquire lock as owned and timeout is 0 so fail fast
                return;
            }
            else //failed to acquire the lock, then try to update the waiters. If the waiters count reached the maximum, just break the loop to avoid overflow
            {
                if ((observedOwner & WAITERS_MASK) != MAXIMUM_WAITERS)
                {
                    // This can still overflow, but maybe there will never be that many waiters
                    turn = (Interlocked.Add(ref _owner, 2) & WAITERS_MASK) >> 1;
                }
            }

            // lock acquired failed and waiters updated

            //*** Step 2, Spinning and Yielding
            var spinner = new SpinWait();
            if (turn > PlatformHelper.ProcessorCount)
            {
                spinner.Count = SpinWait.YieldThreshold;
            }
            while (true)
            {
                spinner.SpinOnce(SLEEP_ONE_FREQUENCY);

                observedOwner = _owner;
                if ((observedOwner & LOCK_ANONYMOUS_OWNED) == LOCK_UNOWNED)
                {
                    int newOwner = (observedOwner & WAITERS_MASK) == 0 ? // Gets the number of waiters, if zero
                           observedOwner | 1 // don't decrement it. just set the lock bit, it is zero because a previous call of Exit(false) which corrupted the waiters
                           : (observedOwner - 2) | 1; // otherwise decrement the waiters and set the lock bit
                    Debug.Assert((newOwner & WAITERS_MASK) >= 0);

                    if (CompareExchange(ref _owner, newOwner, observedOwner, ref lockTaken) == observedOwner)
                    {
                        return;
                    }
                }

                if (spinner.Count % TIMEOUT_CHECK_FREQUENCY == 0)
                {
                    // Check the timeout.
                    if (millisecondsTimeout != Timeout.Infinite && TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout) <= 0)
                    {
                        DecrementWaiters();
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// decrements the waiters, in case of the timeout is expired
        /// </summary>
        private void DecrementWaiters()
        {
            SpinWait spinner = new SpinWait();
            while (true)
            {
                int observedOwner = _owner;
                if ((observedOwner & WAITERS_MASK) == 0) return; // don't decrement the waiters if it's corrupted by previous call of Exit(false)
                if (Interlocked.CompareExchange(ref _owner, observedOwner - 2, observedOwner) == observedOwner)
                {
                    Debug.Assert(!IsThreadOwnerTrackingEnabled); // Make sure the waiters never be negative which will cause the thread tracking bit to be flipped
                    break;
                }
                spinner.SpinOnce();
            }
        }

        /// <summary>
        /// ContinueTryEnter for the thread tracking mode enabled
        /// </summary>
        private void ContinueTryEnterWithThreadTracking(int millisecondsTimeout, uint startTime, ref bool lockTaken)
        {
            Debug.Assert(IsThreadOwnerTrackingEnabled);

            int lockUnowned = 0;
            // We are using thread IDs to mark ownership. Snap the thread ID and check for recursion.
            // We also must or the ID enablement bit, to ensure we propagate when we CAS it in.
            int newOwner = Environment.CurrentManagedThreadId;
            if (_owner == newOwner)
            {
                // We don't allow lock recursion.
                throw new LockRecursionException(SR.SpinLock_TryEnter_LockRecursionException);
            }


            SpinWait spinner = new SpinWait();

            // Loop until the lock has been successfully acquired or, if specified, the timeout expires.
            do
            {
                // We failed to get the lock, either from the fast route or the last iteration
                // and the timeout hasn't expired; spin once and try again.
                spinner.SpinOnce();

                // Test before trying to CAS, to avoid acquiring the line exclusively unnecessarily.

                if (_owner == lockUnowned)
                {
                    if (CompareExchange(ref _owner, newOwner, lockUnowned, ref lockTaken) == lockUnowned)
                    {
                        return;
                    }
                }
                // Check the timeout.  We only RDTSC if the next spin will yield, to amortize the cost.
                if (millisecondsTimeout == 0 ||
                    (millisecondsTimeout != Timeout.Infinite && spinner.NextSpinWillYield &&
                    TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout) <= 0))
                {
                    return;
                }
            } while (true);
        }

        /// <summary>
        /// Releases the lock.
        /// </summary>
        /// <remarks>
        /// The default overload of <see cref="Exit()"/> provides the same behavior as if calling <see
        /// cref="Exit(bool)"/> using true as the argument, but Exit() could be slightly faster than Exit(true).
        /// </remarks>
        /// <exception cref="SynchronizationLockException">
        /// Thread ownership tracking is enabled, and the current thread is not the owner of this lock.
        /// </exception>
        public void Exit()
        {
            //This is the fast path for the thread tracking is disabled, otherwise go to the slow path
            if ((_owner & LOCK_ID_DISABLE_MASK) == 0)
                ExitSlowPath(true);
            else
                Interlocked.Decrement(ref _owner);
        }

        /// <summary>
        /// Releases the lock.
        /// </summary>
        /// <param name="useMemoryBarrier">
        /// A Boolean value that indicates whether a memory fence should be issued in order to immediately
        /// publish the exit operation to other threads.
        /// </param>
        /// <remarks>
        /// Calling <see cref="Exit(bool)"/> with the <paramref name="useMemoryBarrier"/> argument set to
        /// true will improve the fairness of the lock at the expense of some performance. The default <see
        /// cref="Enter"/>
        /// overload behaves as if specifying true for <paramref name="useMemoryBarrier"/>.
        /// </remarks>
        /// <exception cref="SynchronizationLockException">
        /// Thread ownership tracking is enabled, and the current thread is not the owner of this lock.
        /// </exception>
        public void Exit(bool useMemoryBarrier)
        {
            // This is the fast path for the thread tracking is disabled and not to use memory barrier, otherwise go to the slow path
            // The reason not to add else statement if the usememorybarrier is that it will add more branching in the code and will prevent
            // method inlining, so this is optimized for useMemoryBarrier=false and Exit() overload optimized for useMemoryBarrier=true.
            int tmpOwner = _owner;
            if ((tmpOwner & LOCK_ID_DISABLE_MASK) != 0 & !useMemoryBarrier)
            {
                _owner = tmpOwner & (~LOCK_ANONYMOUS_OWNED);
            }
            else
            {
                ExitSlowPath(useMemoryBarrier);
            }
        }

        /// <summary>
        /// The slow path for exit method if the fast path failed
        /// </summary>
        /// <param name="useMemoryBarrier">
        /// A Boolean value that indicates whether a memory fence should be issued in order to immediately
        /// publish the exit operation to other threads
        /// </param>
        private void ExitSlowPath(bool useMemoryBarrier)
        {
            bool threadTrackingEnabled = (_owner & LOCK_ID_DISABLE_MASK) == 0;
            if (threadTrackingEnabled && !IsHeldByCurrentThread)
            {
                throw new SynchronizationLockException(SR.SpinLock_Exit_SynchronizationLockException);
            }

            if (useMemoryBarrier)
            {
                if (threadTrackingEnabled)
                {
                    Interlocked.Exchange(ref _owner, LOCK_UNOWNED);
                }
                else
                {
                    Interlocked.Decrement(ref _owner);
                }
            }
            else
            {
                if (threadTrackingEnabled)
                {
                    _owner = LOCK_UNOWNED;
                }
                else
                {
                    int tmpOwner = _owner;
                    _owner = tmpOwner & (~LOCK_ANONYMOUS_OWNED);
                }
            }
        }

        /// <summary>
        /// Gets whether the lock is currently held by any thread.
        /// </summary>
        public bool IsHeld
        {
            get
            {
                if (IsThreadOwnerTrackingEnabled)
                    return _owner != LOCK_UNOWNED;

                return (_owner & LOCK_ANONYMOUS_OWNED) != LOCK_UNOWNED;
            }
        }

        /// <summary>
        /// Gets whether the lock is currently held by any thread.
        /// </summary>
        /// <summary>
        /// Gets whether the lock is held by the current thread.
        /// </summary>
        /// <remarks>
        /// If the lock was initialized to track owner threads, this will return whether the lock is acquired
        /// by the current thread. It is invalid to use this property when the lock was initialized to not
        /// track thread ownership.
        /// </remarks>
        /// <exception cref="T:System.InvalidOperationException">
        /// Thread ownership tracking is disabled.
        /// </exception>
        public bool IsHeldByCurrentThread
        {
            get
            {
                if (!IsThreadOwnerTrackingEnabled)
                {
                    throw new InvalidOperationException(SR.SpinLock_IsHeldByCurrentThread);
                }
                return ((_owner & (~LOCK_ID_DISABLE_MASK)) == Environment.CurrentManagedThreadId);
            }
        }

        /// <summary>Gets whether thread ownership tracking is enabled for this instance.</summary>
        public bool IsThreadOwnerTrackingEnabled => (_owner & LOCK_ID_DISABLE_MASK) == 0; 

        #region Debugger proxy class
        /// <summary>
        /// Internal class used by debug type proxy attribute to display the owner thread ID 
        /// </summary>
        internal class SystemThreading_SpinLockDebugView
        {
            // SpinLock object
            private SpinLock _spinLock;

            /// <summary>
            /// SystemThreading_SpinLockDebugView constructor
            /// </summary>
            /// <param name="spinLock">The SpinLock to be proxied.</param>
            public SystemThreading_SpinLockDebugView(SpinLock spinLock)
            {
                // Note that this makes a copy of the SpinLock (struct). It doesn't hold a reference to it.
                _spinLock = spinLock;
            }

            /// <summary>
            /// Checks if the lock is held by the current thread or not
            /// </summary>
            public bool? IsHeldByCurrentThread
            {
                get
                {
                    try
                    {
                        return _spinLock.IsHeldByCurrentThread;
                    }
                    catch (InvalidOperationException)
                    {
                        return null;
                    }
                }
            }

            /// <summary>
            /// Gets the current owner thread, zero if it is released
            /// </summary>
            public int? OwnerThreadID
            {
                get
                {
                    if (_spinLock.IsThreadOwnerTrackingEnabled)
                    {
                        return _spinLock._owner;
                    }
                    else
                    {
                        return null;
                    }
                }
            }


            /// <summary>
            ///  Gets whether the lock is currently held by any thread or not.
            /// </summary>
            public bool IsHeld => _spinLock.IsHeld;
        }
        #endregion

    }
}
#pragma warning restore 0420
