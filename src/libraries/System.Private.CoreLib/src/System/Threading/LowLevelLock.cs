// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    /// <summary>
    /// A lightweight non-recursive mutex. Waits on this lock are uninterruptible (from Thread.Interrupt(), which is supported
    /// in some runtimes). That is the main reason this lock type would be used over interruptible locks, such as in a
    /// low-level-infrastructure component that was historically not susceptible to a pending interrupt, and for compatibility
    /// reasons, to ensure that it still would not be susceptible after porting that component to managed code.
    /// </summary>
    internal sealed class LowLevelLock : IDisposable
    {
        private const uint SpinCount = 4;

        private const uint LockedMask = 1;
        private const uint WaiterCountIncrement = 2;
        private const uint WaiterWoken = 1u << 31;

        // Layout:
        //   - Bit 0: 1 if the lock is locked, 0 otherwise
        //   - Sign bit: Indicates whether a thread has been signaled, but has not yet been released from the wait. See SignalWaiter.
        //   - Remaining bits: Number of threads waiting to acquire a lock
        private uint _state;

#if DEBUG
        private Thread? _ownerThread;
#endif

        private LowLevelThreadBlocker _blocker;

        public LowLevelLock()
        {
            _blocker = new LowLevelThreadBlocker();
        }

        ~LowLevelLock() => Dispose();

        public void Dispose()
        {
            VerifyIsNotLockedByAnyThread();

            _blocker.Dispose();
            GC.SuppressFinalize(this);
        }

#if DEBUG
        public bool IsLocked
        {
            get
            {
                bool isLocked = _ownerThread == Thread.CurrentThread;
                Debug.Assert(!isLocked || (_state & LockedMask) != 0);
                return isLocked;
            }
        }
#endif

        [Conditional("DEBUG")]
        public void VerifyIsLocked()
        {
#if DEBUG
            Debug.Assert(_ownerThread == Thread.CurrentThread);
#endif
            Debug.Assert((_state & LockedMask) != 0);
        }

#pragma warning disable CA1822
        [Conditional("DEBUG")]
        public void VerifyIsNotLocked()
        {
#if DEBUG
            Debug.Assert(_ownerThread != Thread.CurrentThread);
#endif
        }

        [Conditional("DEBUG")]
        private void VerifyIsNotLockedByAnyThread()
        {
#if DEBUG
            Debug.Assert(_ownerThread == null);
#endif
        }
#pragma warning restore CA1822

        [Conditional("DEBUG")]
        private void ResetOwnerThread()
        {
            VerifyIsLocked();
#if DEBUG
            _ownerThread = null;
#endif
        }

        [Conditional("DEBUG")]
        private void SetOwnerThreadToCurrent()
        {
            VerifyIsNotLockedByAnyThread();
#if DEBUG
            _ownerThread = Thread.CurrentThread;
#endif
        }

        public bool TryAcquire()
        {
            VerifyIsNotLocked();

            uint state = _state;
            bool acquired = (state & LockedMask) == 0 &&
                Interlocked.CompareExchange(ref _state, state + LockedMask, state) == state;
            if (acquired)
            {
                SetOwnerThreadToCurrent();
            }

            return acquired;
        }

        public void Acquire()
        {
            if (TryAcquire())
                return;

            SpinAndAcquire();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SpinAndAcquire()
        {
            VerifyIsNotLocked();

            uint spinCount = Environment.IsSingleProcessor ? 0: SpinCount;
            for (uint i = 0; i < spinCount; i++)
            {
                Backoff.Exponential(i);
                if (TryAcquire())
                {
                    return;
                }
            }

            WaitAndAcquire();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void WaitAndAcquire()
        {
            VerifyIsNotLocked();

            RuntimeFeature.ThrowIfMultithreadingIsNotSupported();

            // Atomically either register this thread as a waiter or acquire the lock.
            uint collisions = 0;
            while (true)
            {
                uint state = _state;
                uint newState = (state & LockedMask) == 0 ?
                    state + LockedMask :
                    state + WaiterCountIncrement;

                if (Interlocked.CompareExchange(ref _state, newState, state) == state)
                {
                    if ((state & LockedMask) == 0)
                    {
                        Debug.Assert((_state & LockedMask) != 0);
                        SetOwnerThreadToCurrent();
                        return;
                    }

                    // registered as a waiter
                    break;
                }

                Backoff.Exponential(collisions++);
            }

            // Wait until woken, repeatedly until the lock can be acquired by this thread.
            while (true)
            {
                _blocker.Wait();

                collisions = 0;
                while (true)
                {
                    uint state = _state;

                    Debug.Assert(state >= WaiterCountIncrement);
                    Debug.Assert((state & WaiterWoken) != 0);

                    uint newState = state & ~WaiterWoken;
                    if ((state & LockedMask) == 0)
                    {
                        newState -= WaiterCountIncrement;
                        newState += LockedMask;
                    }

                    if (Interlocked.CompareExchange(ref _state, newState, state) == state)
                    {
                        if ((state & LockedMask) == 0)
                        {
                            Debug.Assert((_state & LockedMask) != 0);
                            SetOwnerThreadToCurrent();
                            return;
                        }

                        // we consumed the wake signal, but the lock was still held; wait again
                        break;
                    }

                    Backoff.Exponential(collisions++);
                }
            }
        }

        public void Release()
        {
            Debug.Assert((_state & LockedMask) != 0);
            ResetOwnerThread();

            // NB: if waiter is woken the state is negative
            if ((int)Interlocked.Decrement(ref _state) > 0)
            {
                SignalWaiter();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SignalWaiter()
        {
            if ((Interlocked.Or(ref _state, WaiterWoken) & WaiterWoken) == 0)
            {
                _blocker.WakeOne();
            }
        }
    }
}
