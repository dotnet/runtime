// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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
    public sealed partial class Lock
    {
        // Lock participates in class construction and must not have a static constructor.
        internal const int UninitializedThreadId = 0;

        private int _owningThreadId; // cDAC depends on exact name of this field

        private uint _state; // cDAC depends on this field name and bit 0 indicating a held lock
        private uint _recursionCount; // cDAC depends on exact name of this field

        /// <summary>
        /// Initializes a new instance of the <see cref="Lock"/> class.
        /// </summary>
        public Lock() => Initialize();


#if NATIVEAOT // The method needs to be public in NativeAOT so that other private libraries can access it
        public Lock(bool useTrivialWaits)
#else
        internal Lock(bool useTrivialWaits)
#endif
            : this()
        {
            InitializeUseTrivialWaits(useTrivialWaits);
        }

        internal int OwningManagedThreadId => (int)_owningThreadId;

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
            int currentThreadId = TryEnter_Inlined(timeoutMs: -1);
            Debug.Assert(currentThreadId != UninitializedThreadId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int EnterAndGetCurrentThreadId()
        {
            int currentThreadId = TryEnter_Inlined(timeoutMs: -1);
            Debug.Assert(currentThreadId != UninitializedThreadId);
            Debug.Assert(currentThreadId == _owningThreadId);
            return currentThreadId;
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
        public Scope EnterScope() => new Scope(this, EnterAndGetCurrentThreadId());

        /// <summary>
        /// A disposable structure that is returned by <see cref="EnterScope()"/>, which when disposed, exits the lock.
        /// </summary>
        public ref struct Scope
        {
            private Lock? _lockObj;
            private int _currentThreadId;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Scope(Lock lockObj, int currentThreadId)
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
                if (lockObj is not null)
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
        public bool TryEnter() => TryEnter_Inlined(timeoutMs: 0) != UninitializedThreadId;

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
        internal bool TryEnter_Outlined(int timeoutMs) => TryEnter_Inlined(timeoutMs) != UninitializedThreadId;

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
            var owningThreadId = _owningThreadId;
            if (owningThreadId == UninitializedThreadId || owningThreadId != ManagedThreadId.CurrentManagedThreadIdUnchecked)
            {
                ThrowHelper.ThrowSynchronizationLockException_LockExit();
            }

            ExitImpl();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void Exit(int currentThreadId)
        {
            Debug.Assert(currentThreadId != UninitializedThreadId);
            Debug.Assert(currentThreadId == ManagedThreadId.CurrentManagedThreadIdUnchecked);

            if (_owningThreadId != currentThreadId)
            {
                ThrowHelper.ThrowSynchronizationLockException_LockExit();
            }

            ExitImpl();
        }

        internal void Reenter(uint previousRecursionCount)
        {
            Debug.Assert(!IsHeldByCurrentThread);

#if FEATURE_WASM_MANAGED_THREADS
            if (Thread.ThrowOnBlockingWaitOnJSInteropThread || Thread.WarnOnBlockingWaitOnJSInteropThread)
            {
                // A timed condition wait must restore ownership even if another thread still holds the lock.
                while (!TryEnter(int.MaxValue))
                {
                }
            }
            else
#endif
            {
                Enter();
            }
            _recursionCount = previousRecursionCount;
        }

        /// <summary>
        /// <code>true</code> if the lock is held by the calling thread, <code>false</code> otherwise.
        /// </summary>
        public bool IsHeldByCurrentThread => IsHeldByCurrentThreadCore;

        internal int OwningThreadId => _owningThreadId;

#if CORECLR
        [System.Runtime.InteropServices.UnmanagedCallersOnly]
        private static unsafe void InitializeForMonitor(Lock* pLock, int managedThreadId, uint recursionCount, Exception* pException)
        {
            try
            {
                pLock->InitializeForMonitor(managedThreadId, recursionCount);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }
#endif

        internal bool Wait(int millisecondsTimeout)
        {
#pragma warning disable CS9216 // A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
            return Wait(millisecondsTimeout, this);
#pragma warning restore CS9216 // A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
        }
    }
}
