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
    /// <summary>
    /// Provides a mutual exclusion lock primitive where a thread trying to acquire the lock waits in a loop
    /// repeatedly checking until the lock becomes available.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Spin locks can be used for leaf-level locks where the object allocation implied by using a <see
    /// cref="Monitor"/>, in size or due to garbage collection pressure, is overly
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
    public partial struct SpinLock
    {

        // The thread tracking disabled mask
        private const int LOCK_ID_DISABLE_MASK = unchecked((int)0x80000000);        // 1000 0000 0000 0000 0000 0000 0000 0000

        // The lock is held by some thread, but we don't know which
        private const int LOCK_ANONYMOUS_OWNED = 0x1;                               // 0000 0000 0000 0000 0000 0000 0000 0001

        // If the thread is unowned if:
        // m_owner zero and the thread tracking is enabled
        // m_owner & LOCK_ANONYMOUS_OWNED = zero and the thread tracking is disabled
        private const int LOCK_UNOWNED = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpinLock"/>
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
        /// Initializes a new instance of the <see cref="SpinLock"/>
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
        /// <exception cref="LockRecursionException">
        /// Thread ownership tracking is enabled, and the current thread has already acquired this lock.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The <paramref name="lockTaken"/> argument must be initialized to false prior to calling Enter.
        /// </exception>
        public void Enter(ref bool lockTaken)

        {
            EnterCore(ref lockTaken);
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
        /// <exception cref="LockRecursionException">
        /// Thread ownership tracking is enabled, and the current thread has already acquired this lock.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The <paramref name="lockTaken"/> argument must be initialized to false prior to calling TryEnter.
        /// </exception>
        public void TryEnter(ref bool lockTaken)

        {
            TryEnterCore(ref lockTaken);
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
        /// <param name="timeout">A <see cref="TimeSpan"/> that represents the number of milliseconds
        /// to wait, or a <see cref="TimeSpan"/> that represents -1 milliseconds to wait indefinitely.
        /// </param>
        /// <param name="lockTaken">True if the lock is acquired; otherwise, false. <paramref
        /// name="lockTaken"/> must be initialized to false prior to calling this method.</param>
        /// <exception cref="LockRecursionException">
        /// Thread ownership tracking is enabled, and the current thread has already acquired this lock.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The <paramref name="lockTaken"/> argument must be initialized to false prior to calling TryEnter.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is a negative
        /// number other than -1 milliseconds, which represents an infinite time-out -or- timeout is greater
        /// than <see cref="int.MaxValue"/> milliseconds.
        /// </exception>
        public void TryEnter(TimeSpan timeout, ref bool lockTaken)
        {
            // Validate the timeout
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
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
        /// cref="Timeout.Infinite"/> (-1) to wait indefinitely.</param>
        /// <param name="lockTaken">True if the lock is acquired; otherwise, false. <paramref
        /// name="lockTaken"/> must be initialized to false prior to calling this method.</param>
        /// <exception cref="LockRecursionException">
        /// Thread ownership tracking is enabled, and the current thread has already acquired this lock.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The <paramref name="lockTaken"/> argument must be initialized to false prior to calling TryEnter.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is
        /// a negative number other than -1, which represents an infinite time-out.</exception>
        public void TryEnter(int millisecondsTimeout, ref bool lockTaken)

        {
            TryEnterCore(millisecondsTimeout, ref lockTaken);
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
            ExitCore();
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
            ExitCore(useMemoryBarrier);
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
        /// <exception cref="InvalidOperationException">
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
                return (_owner & (~LOCK_ID_DISABLE_MASK)) == Environment.CurrentManagedThreadId;
            }
        }

        /// <summary>Gets whether thread ownership tracking is enabled for this instance.</summary>
        public bool IsThreadOwnerTrackingEnabled => (_owner & LOCK_ID_DISABLE_MASK) == 0;

        /// <summary>
        /// internal class used by debug type proxy attribute to display the owner thread ID
        /// </summary>
        internal sealed class SystemThreading_SpinLockDebugView
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

    }
}
#pragma warning restore 0420
