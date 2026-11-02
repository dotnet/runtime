// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma warning disable 0420

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// A spin lock is a mutual exclusion lock primitive where a thread trying to acquire the lock waits in a loop ("spins")
// repeatedly checking until the lock becomes available. As the thread remains active performing a non-useful task,
// the use of such a lock is a kind of busy waiting and consumes CPU resources without performing real work.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    public partial struct SpinLock
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

        // Waiters mask if the thread tracking is disabled
        private const int WAITERS_MASK = ~(LOCK_ID_DISABLE_MASK | 1);               // 0111 1111 1111 1111 1111 1111 1111 1110

        // The Thread tacking is disabled and the lock bit is set, used in Enter fast path to make sure the id is disabled and lock is available
        private const int ID_DISABLED_AND_ANONYMOUS_OWNED = unchecked((int)0x80000001); // 1000 0000 0000 0000 0000 0000 0000 0001

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

        private void EnterCore(ref bool lockTaken)
        {
            // Try to keep the code and branching in this method as small as possible in order to inline the method
            int observedOwner = _owner;
            if (lockTaken || // invalid parameter
                (observedOwner & ID_DISABLED_AND_ANONYMOUS_OWNED) != LOCK_ID_DISABLE_MASK || // thread tracking is enabled or the lock is already acquired
                CompareExchange(ref _owner, observedOwner | LOCK_ANONYMOUS_OWNED, observedOwner, ref lockTaken) != observedOwner) // acquiring the lock failed
                ContinueTryEnter(Timeout.Infinite, ref lockTaken); // Then try the slow path if any of the above conditions is met
        }

        private void TryEnterCore(ref bool lockTaken)
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

        private void TryEnterCore(int millisecondsTimeout, ref bool lockTaken)
        {
            int observedOwner = _owner;
            if (millisecondsTimeout < -1 || // invalid parameter
                lockTaken || // invalid parameter
                (observedOwner & ID_DISABLED_AND_ANONYMOUS_OWNED) != LOCK_ID_DISABLE_MASK ||  // thread tracking is enabled or the lock is already acquired
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

            long startTime = 0;
            if (millisecondsTimeout != Timeout.Infinite && millisecondsTimeout != 0)
            {
                startTime = Environment.TickCount64;
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
            // ***Step 1, take the lock or update the waiters

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
            else // failed to acquire the lock, then try to update the waiters. If the waiters count reached the maximum, just break the loop to avoid overflow
            {
                if ((observedOwner & WAITERS_MASK) != MAXIMUM_WAITERS)
                {
                    // This can still overflow, but maybe there will never be that many waiters
                    turn = (Interlocked.Add(ref _owner, 2) & WAITERS_MASK) >> 1;
                }
            }

            // lock acquired failed and waiters updated

            // *** Step 2, Spinning and Yielding
            SpinWait spinner = default;
            if (turn > Environment.ProcessorCount)
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
            SpinWait spinner = default;
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
        private void ContinueTryEnterWithThreadTracking(int millisecondsTimeout, long startTime, ref bool lockTaken)
        {
            Debug.Assert(IsThreadOwnerTrackingEnabled);

            const int LockUnowned = 0;
            // We are using thread IDs to mark ownership. Snap the thread ID and check for recursion.
            // We also must or the ID enablement bit, to ensure we propagate when we CAS it in.
            int newOwner = Environment.CurrentManagedThreadId;
            if (_owner == newOwner)
            {
                // We don't allow lock recursion.
                throw new LockRecursionException(SR.SpinLock_TryEnter_LockRecursionException);
            }

            SpinWait spinner = default;

            // Loop until the lock has been successfully acquired or, if specified, the timeout expires.
            while (true)
            {
                // We failed to get the lock, either from the fast route or the last iteration
                // and the timeout hasn't expired; spin once and try again.
                spinner.SpinOnce();

                // Test before trying to CAS, to avoid acquiring the line exclusively unnecessarily.

                if (_owner == LockUnowned)
                {
                    if (CompareExchange(ref _owner, newOwner, LockUnowned, ref lockTaken) == LockUnowned)
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
            }
        }

        private void ExitCore()
        {
            // This is the fast path for the thread tracking is disabled, otherwise go to the slow path
            if ((_owner & LOCK_ID_DISABLE_MASK) == 0)
                ExitSlowPath(true);
            else
                Interlocked.Decrement(ref _owner);
        }

        private void ExitCore(bool useMemoryBarrier)
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
                ThrowHelper.ThrowSynchronizationLockException_LockExit();
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

    }
}
#pragma warning restore 0420
