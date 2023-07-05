// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

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
        private const int SpinCount = 8;
        private const int SpinSleep0Threshold = 4;

        private const int LockedMask = 1;
        private const int WaiterCountIncrement = 2;

        private static readonly Func<object, bool> s_spinWaitTryAcquireCallback = SpinWaitTryAcquireCallback;

        // Layout:
        //   - Bit 0: 1 if the lock is locked, 0 otherwise
        //   - Remaining bits: Number of threads waiting to acquire a lock
        private int _state;

#if DEBUG
        private Thread? _ownerThread;
#endif

        // Indicates whether a thread has been signaled, but has not yet been released from the wait. See SignalWaiter. Reads
        // and writes must occur while _monitor is locked.
        private bool _isAnyWaitingThreadSignaled;

        private LowLevelSpinWaiter _spinWaiter;
        private LowLevelMonitor _monitor;

        public LowLevelLock()
        {
            _spinWaiter = default(LowLevelSpinWaiter);
            _monitor.Initialize();
        }

        ~LowLevelLock() => Dispose();

        public void Dispose()
        {
            VerifyIsNotLockedByAnyThread();

            _monitor.Dispose();
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

            // A common case is that there are no waiters, so hope for that and try to acquire the lock
            int state = Interlocked.CompareExchange(ref _state, LockedMask, 0);
            if (state == 0 || TryAcquire_NoFastPath(state))
            {
                SetOwnerThreadToCurrent();
                return true;
            }
            return false;
        }

        private bool TryAcquire_NoFastPath(int state)
        {
            // The lock may be available, but there may be waiters. This thread could acquire the lock in that case. Acquiring
            // the lock means that if this thread is repeatedly acquiring and releasing the lock, it could permanently starve
            // waiters. Waiting instead in the same situation would deterministically create a lock convoy. Here, we opt for
            // acquiring the lock to prevent a deterministic lock convoy in that situation, and rely on the system's
            // waiting/waking implementation to mitigate starvation, even in cases where there are enough logical processors to
            // accommodate all threads.
            return (state & LockedMask) == 0 && Interlocked.CompareExchange(ref _state, state + LockedMask, state) == state;
        }

        private static bool SpinWaitTryAcquireCallback(object state)
        {
            var thisRef = (LowLevelLock)state;
            return thisRef.TryAcquire_NoFastPath(thisRef._state);
        }

        public void Acquire()
        {
            if (!TryAcquire())
            {
                WaitAndAcquire();
            }
        }

        private void WaitAndAcquire()
        {
            VerifyIsNotLocked();

            // Spin a bit to see if the lock becomes available, before forcing the thread into a wait state
            if (_spinWaiter.SpinWaitForCondition(s_spinWaitTryAcquireCallback, this, SpinCount, SpinSleep0Threshold))
            {
                Debug.Assert((_state & LockedMask) != 0);
                SetOwnerThreadToCurrent();
                return;
            }

            _monitor.Acquire();

            // Register this thread as a waiter by incrementing the waiter count. Incrementing the waiter count and waiting on
            // the monitor need to appear atomic to SignalWaiter so that its signal won't be lost.
            int state = Interlocked.Add(ref _state, WaiterCountIncrement);

            // Wait on the monitor until signaled, repeatedly until the lock can be acquired by this thread
            while (true)
            {
                // The lock may have been released before the waiter count was incremented above, so try to acquire the lock
                // with the new state before waiting
                if ((state & LockedMask) == 0 &&
                    Interlocked.CompareExchange(ref _state, state + (LockedMask - WaiterCountIncrement), state) == state)
                {
                    break;
                }

                _monitor.Wait();

                // Indicate to SignalWaiter that the signaled thread has woken up
                _isAnyWaitingThreadSignaled = false;

                state = _state;
                Debug.Assert((uint)state >= WaiterCountIncrement);
            }

            _monitor.Release();

            Debug.Assert((_state & LockedMask) != 0);
            SetOwnerThreadToCurrent();
        }

        public void Release()
        {
            Debug.Assert((_state & LockedMask) != 0);
            ResetOwnerThread();

            if (Interlocked.Decrement(ref _state) != 0)
            {
                SignalWaiter();
            }
        }

        private void SignalWaiter()
        {
            // Since the lock was already released by the caller, there are no guarantees on the state at this point. For
            // instance, if there was only one thread waiting before the lock was released, then after the lock was released,
            // another thread may have acquired and released the lock, and signaled the waiter, before the first thread arrives
            // here. The monitor's lock is used to synchronize changes to the waiter count, so acquire the monitor and recheck
            // the waiter count before signaling.
            _monitor.Acquire();

            // Keep track of whether a thread has been signaled but has not yet been released from the wait.
            // _isAnyWaitingThreadSignaled is set to false when a signaled thread wakes up. Since threads can preempt waiting
            // threads and acquire the lock (see TryAcquire), it allows for example, one thread to acquire and release the lock
            // multiple times while there are multiple waiting threads. In such a case, we don't want that thread to signal a
            // waiter every time it releases the lock, as that will cause unnecessary context switches with more and more
            // signaled threads waking up, finding that the lock is still locked, and going right back into a wait state. So,
            // signal only one waiting thread at a time.
            if ((uint)_state >= WaiterCountIncrement && !_isAnyWaitingThreadSignaled)
            {
                _isAnyWaitingThreadSignaled = true;
                _monitor.Signal_Release();
                return;
            }

            _monitor.Release();
        }
    }
}
