// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal;

namespace System.Threading
{
    /// <summary>
    /// A LIFO semaphore.
    /// Waits on this semaphore are uninterruptible.
    /// </summary>
    internal sealed partial class LowLevelLifoSemaphore
    {
        // The spin count is chosen to be in the range of typical thread wake latency and some additional overhead,
        // all assuming a single spin is calibrated to around 35 nanoseconds.
        // The thread wake latency commonly measures at 2-10 microsecond (year 2026) and unlikely to drastically change.
        private const int DefaultSemaphoreSpinCountLimit = 256;
        // The cooldown roughly serves as detection that the thread did not spend time being blocked.
        // If it woke in under 4 microseconds, it was likely a fast/trivial wake without blocking.
        private const int DefaultWakeCooldown = 4;

        private CacheLineSeparatedCounts _separated;

        private readonly short _maxSpinCount;
        private readonly short _threadWakeCooldownUsec;
        private readonly Action _onWait;

        // When we need to block threads we use a linked list of per-thread blockers.
        // When we need to wake a worker, we pop the topmost blocker and release it.
        private sealed class LifoBlockerNode
        {
            internal LifoBlockerNode? _next;
            internal LowLevelThreadBlocker _blocker = new LowLevelThreadBlocker();

            ~LifoBlockerNode()
            {
                _blocker.Dispose();
            }
        }

        [ThreadStatic]
        private static LifoBlockerNode? t_blockerNode;

        private readonly LowLevelLock _blockerStackLock = new LowLevelLock();
        private LifoBlockerNode? _blockerStack;

        // Sometimes due to races we may see nonzero waiter count, but no blockers to wake.
        // That happens if threads that added themselves to waiter count, have not yet blocked themselves.
        // In such case we increment _racingUnblocks and the waiter will simply
        // decrement the counter and return without blocking.
        private int _racingUnblocks;

        // If a _blockerStackLock is locked by other thread, like someone is inserting itself into blocker list,
        // we cannot proceed with a wake, but we do not want to wait while releasing, thus we do it in two-stages:
        // - we register an intent to wake, then
        // - try waking and if _blockerStackLock is locked the waking becomes
        //   a responsibility of the thread that holds the lock.
        // The main goal here is that the threads who release other threads do not get themselves blocked as
        // the releasers are the hot threads that do the actual work (as opposed to threads who are parking/unparking).
        private int _pendingWake;

        public LowLevelLifoSemaphore(Action onWait)
        {
            _separated = default;
            _onWait = onWait;

            _maxSpinCount = AppContextConfigHelper.GetInt16ComPlusOrDotNetConfig(
                "System.Threading.ThreadPool.UnfairSemaphoreSpinLimit",
                "ThreadPool_UnfairSemaphoreSpinLimit",
                DefaultSemaphoreSpinCountLimit,
                false);

            _threadWakeCooldownUsec = AppContextConfigHelper.GetInt16ComPlusOrDotNetConfig(
                "System.Threading.ThreadPool.UnfairSemaphoreWakeCooldown",
                "ThreadPool_UnfairSemaphoreWakeCooldown",
                DefaultWakeCooldown,
                false);
        }

        public bool Wait(int timeoutMs)
        {
            Debug.Assert(timeoutMs >= -1);

            // Try one-shot acquire first
            Counts counts = _separated._counts;
            if (counts.SignalCount != 0)
            {
                Counts newCounts = counts;
                newCounts.DecrementSignalCount();
                Counts countsBeforeUpdate = _separated._counts.InterlockedCompareExchange(newCounts, counts);
                if (countsBeforeUpdate == counts)
                {
                    // we've consumed a signal
                    return true;
                }
            }

            RuntimeFeature.ThrowIfMultithreadingIsNotSupported();

            return WaitSlow(timeoutMs);
        }

        private bool WaitSlow(int timeoutMs)
        {
            int spinsRemaining = Environment.IsSingleProcessor ? 0 : _maxSpinCount;

            uint iteration = 0;
            while (spinsRemaining > 0)
            {
                spinsRemaining -= Backoff.Exponential(iteration++);

                Counts counts = _separated._counts;
                if (counts.SignalCount != 0)
                {
                    Counts newCounts = counts;
                    newCounts.DecrementSignalCount();
                    Counts countsBeforeUpdate = _separated._counts.InterlockedCompareExchange(newCounts, counts);
                    if (countsBeforeUpdate == counts)
                    {
                        // we've consumed a signal
                        return true;
                    }
                }
            }

            return WaitNoSpin(timeoutMs);
        }

        public bool WaitNoSpin(int timeoutMs)
        {
            if (timeoutMs == 0)
                return false;

            Counts counts = _separated._counts.InterlockedIncrementWaiterCount();

            // If there are pending signals, we may end in a condition that requires
            // waking a waiter.
            // Perhaps the current thread will be such waiter, but we should still
            // go through wait/wake routine (vs. just claiming the signal) as the
            // caller wants to park the thread.
            MaybeWakeWaiter(counts);

            return WaitAsWaiter(timeoutMs);
        }

        // If we have signals and have waiters, we need to make sure at least one is waking.
        // We wake one waiter at a time. If it finds work it will ask for workers and that can wake more waiters
        // if other workers do not consume the additional signals.
        // It is generally unusual to have > 1 signal. That only happens when the count of desired workers had a forced change.
        // In any case, we would prefer that extra signals be consumed by active workers, but must guarantee that signals
        // are consumed eventually thus we release waiters one by one.
        private static bool HasWaitersToWake(Counts counts) =>
            counts.CountOfWaitersSignaledToWake == 0 &&
            counts.SignalCount > 0 &&
            counts.WaiterCount > 0;

        private void MaybeWakeWaiter(Counts counts)
        {
            if (!HasWaitersToWake(counts))
            {
                // No waiters to wake. This is the most common case.
                return;
            }

            MaybeWakeWaiterSlow(counts);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void MaybeWakeWaiterSlow(Counts counts)
        {
            Debug.Assert(HasWaitersToWake(counts));

            uint collisionCount = 0;
            do
            {
                Counts newCounts = counts;
                newCounts.AddCountOfWaitersSignaledToWake(1);
                Debug.Assert(newCounts.CountOfWaitersSignaledToWake == 1);
                Counts countsBeforeUpdate = _separated._counts.InterlockedCompareExchange(newCounts, counts);
                if (countsBeforeUpdate == counts)
                {
                    WakeOne();
                    break;
                }

                if (!HasWaitersToWake(countsBeforeUpdate))
                    break;

                // CAS collision, but still have waiters to wake, try again.
                Backoff.Exponential(collisionCount++);
                counts = _separated._counts;
            }
            while (HasWaitersToWake(counts));
        }

        private bool WaitAsWaiter(int timeoutMs)
        {
            Debug.Assert(timeoutMs > 0 || timeoutMs == -1);

            _onWait();

            while (true)
            {
                long waitStartTick = Stopwatch.GetTimestamp();

                // In the context of this semaphore the purpose of timeoutMs is just to age out
                // workers that have not been woken for very long time.
                // We do not need to reduce the timeout after spurious wakes as that will only result
                // in workers that are woken spuriously to eventually exit and be replaced by new workers.
                if (!Block(timeoutMs))
                {
                    // Unregister the waiter, but do not decrement wake count, the thread did not observe a wake.
                    _separated._counts.InterlockedDecrementWaiterCount();
                    return false;
                }

                // The thread could not obtain work for quite a while. We will require a 4 usec
                // cooldown before reintroducing the thread. The sleep/wake transition typically
                // takes care of the wait, but the blocker has fast wake paths and the underlying
                // OS API may have trivial/spinning wake paths as well and fast wakeups can happen
                // and are hard to avoid completely.
                // So, if a fast wake happened when parking was desired, we hold up the thread a bit
                // before releasing.
                long cooldown = Stopwatch.Frequency * _threadWakeCooldownUsec / 1000000;
                while (Stopwatch.GetTimestamp() - waitStartTick < cooldown)
                {
                    Thread.UninterruptibleSleep0();
                    Thread.SpinWait(1);
                }

                uint collisionCount = 0;
                while (true)
                {
                    Counts counts = _separated._counts;
                    Counts newCounts = counts;

                    Debug.Assert(counts.WaiterCount != 0);

                    // we consumed a wake, decrement the count
                    Debug.Assert(counts.CountOfWaitersSignaledToWake == 1);
                    newCounts.DecrementCountOfWaitersSignaledToWake();

                    // If there is a signal, try claiming it and stop waiting.
                    if (newCounts.SignalCount != 0)
                    {
                        newCounts.DecrementSignalCount();
                        newCounts.DecrementWaiterCount();
                    }

                    Counts countsBeforeUpdate = _separated._counts.InterlockedCompareExchange(newCounts, counts);
                    if (countsBeforeUpdate == counts)
                    {
                        if (counts.SignalCount != 0)
                        {
                            // success
                            return true;
                        }

                        // We've consumed a wake, but there was no signal.
                        // The semaphore is unfair and spurious/stolen wakes can happen.
                        // We will have to wait again.
                        break;
                    }

                    // CAS collision, try again.
                    Backoff.Exponential(collisionCount++);
                }
            }
        }

        public void Signal()
        {
            // Increment signal count. This enables one-shot acquire.
            Counts counts = _separated._counts.InterlockedIncrementSignalCount();
            MaybeWakeWaiter(counts);
        }

        private bool Block(int timeoutMs)
        {
            Debug.Assert(timeoutMs >= -1);

            LifoBlockerNode? blockerNode = t_blockerNode;
            if (blockerNode == null)
            {
                try
                {
                    t_blockerNode = blockerNode = new LifoBlockerNode();
                }
                catch (OutOfMemoryException)
                {
                    // Treat OOM as a timeout.
                    // The thread will try to exit.
                    return false;
                }
            }

            _blockerStackLock.Acquire();
            if (_racingUnblocks != 0)
            {
                Debug.Assert(_blockerStack == null);
                Debug.Assert(_racingUnblocks > 0);
                _racingUnblocks--;
                blockerNode = null;
            }
            else
            {
                blockerNode._next = _blockerStack;
                _blockerStack = blockerNode;
            }

            _blockerStackLock.Release();

            // LowLevelLock release is a full fence thus ordinary read of _pendingWake is ok
            if (_pendingWake > 0)
                WakeOneCore();

            if (blockerNode != null)
            {
                // Suppress the wake-up preemption that the OS would normally apply
                // when this thread is unblocked. The semaphore is used to park workers.
                // A transient wake-up advantage provides no benefit here and can result in
                // the woken thread preempting already-working threads. On Windows this
                // disables SetThreadPriorityBoost. On Linux this switches the thread to
                // SCHED_BATCH before the wait, which sched(7) documents as applying "a small
                // scheduling penalty with respect to wakeup behavior", then restores to the
                // default SCHED_OTHER after the wait. These are best-effort; failures are
                // asserted in debug builds but otherwise ignored.
                WakePreemptionScope wakePreemptionScope = SuppressWakePreemption();

                try
                {
                    while (!blockerNode._blocker.TimedWait(timeoutMs))
                    {
                        if (TryRemove(blockerNode))
                        {
                            return false;
                        }

                        // We timed out, but our waiter is already popped. Someone is waking
                        // our blocker. This is a very rare case.
                        // We can't leave or the wake could be lost, so let's wait again.
                        // The blocker is likely woken already, but give it some extra time,
                        // just so we do not keep coming here again.
                        timeoutMs = 10;
                    }
                }
                finally
                {
                    RestoreWakePreemption(wakePreemptionScope);
                }
            }

            return true;
        }

        private void WakeOne()
        {
            // Use Interlocked. This assignment must happen before trying to acquire the _blockerStackLock
            int origWake = Interlocked.Exchange(ref _pendingWake, 1);
            Debug.Assert(origWake == 0);
            WakeOneCore();
        }

        // Turn any pending wakes into actual thread wakes, but use TryAcquire to acquire the _blockerStackLock.
        // If someone acquires _blockerStackLock, it becomes its responsibility to check for pending wakes after
        // releasing and call here if needed.
        private void WakeOneCore()
        {
            while (true)
            {
                if (!_blockerStackLock.TryAcquire())
                {
                    // The lock holder will pick up _pendingWake on release.
                    // NOTE: both setting _pendingWake and releasing the lock are done via full fence atomic
                    // operations, thus the holder is guaranteed to observe the wake that it is blocking.
                    return;
                }

                if (_pendingWake == 0)
                {
                    _blockerStackLock.Release();

                    // Loop: handle any wakes that arrived while we were holding the lock
                    //       it is highly unlikely, but not impossible.
                    //
                    // LowLevelLock release is a full fence thus ordinary read of _pendingWake is ok
                    if (_pendingWake != 0)
                        continue;

                    // no pending wakes
                    return;
                }

                // We use only one pending wake at a time and this is the only place when we clear it.
                // We are also holding the _blockerStackLock and whoever we are unparking cannot acknowledge
                // the wake while we are holding the lock.
                // Until the wake is acknowledged _pendingWake cannot be changed by any thread except the current.
                // Therefore we can use an ordinary --
                Debug.Assert(_pendingWake == 1);
                _pendingWake--;

                LifoBlockerNode? top = _blockerStack;
                if (top != null)
                {
                    _blockerStack = top._next;
                    top._next = null;
                }
                else
                {
                    _racingUnblocks++;
                    Debug.Assert(_racingUnblocks != ushort.MaxValue);
                }

                // No new wakes can be pended while we are holding the lock for the purpose of
                // clearing an existing pending wake.
                // Thus we do not check _pendingWake after releasing the lock in this case.
                Debug.Assert(_pendingWake == 0);
                _blockerStackLock.Release();

                if (top != null)
                {
                    top._blocker.WakeOne();
                }

                return;
            }
        }

        // Used when waiter times out
        private bool TryRemove(LifoBlockerNode node)
        {
            bool removed = false;
            _blockerStackLock.Acquire();

            LifoBlockerNode? current = _blockerStack;
            if (current == node)
            {
                _blockerStack = node._next;
                node._next = null;
                removed = true;
            }
            else
            {
                while (current != null)
                {
                    if (current._next == node)
                    {
                        current._next = node._next;
                        node._next = null;
                        removed = true;
                        break;
                    }

                    current = current._next;
                }
            }

            _blockerStackLock.Release();

            // LowLevelLock release is a full fence thus ordinary read of _pendingWake is ok
            if (_pendingWake > 0)
                WakeOneCore();

            return removed;
        }

        private struct Counts : IEquatable<Counts>
        {
            private const byte SignalCountShift = 0;
            private const byte WaiterCountShift = 16;
            private const byte CountOfWaitersSignaledToWakeShift = 32;

            private ulong _data;

            private Counts(ulong data) => _data = data;

            private ushort GetUInt16Value(byte shift) => (ushort)(_data >> shift);

            public ushort SignalCount
            {
                get => GetUInt16Value(SignalCountShift);
            }

            public Counts InterlockedIncrementSignalCount()
            {
                var countsAfterUpdate = new Counts(Interlocked.Add(ref _data, 1ul << SignalCountShift));
                Debug.Assert(countsAfterUpdate.SignalCount != ushort.MaxValue); // overflow check
                return countsAfterUpdate;
            }

            public void DecrementSignalCount()
            {
                Debug.Assert(SignalCount != 0);
                _data -= (ulong)1 << SignalCountShift;
            }

            public ushort WaiterCount
            {
                get => GetUInt16Value(WaiterCountShift);
            }

            public void DecrementWaiterCount()
            {
                Debug.Assert(WaiterCount != 0);
                _data -= (ulong)1 << WaiterCountShift;
            }

            public void IncrementWaiterCount()
            {
                _data += (ulong)1 << WaiterCountShift;
                Debug.Assert(WaiterCount != 0);
            }

            public void InterlockedDecrementWaiterCount()
            {
                var countsAfterUpdate = new Counts(Interlocked.Add(ref _data, unchecked((ulong)-1) << WaiterCountShift));
                Debug.Assert(countsAfterUpdate.WaiterCount != ushort.MaxValue); // underflow check
            }

            public Counts InterlockedIncrementWaiterCount()
            {
                var countsAfterUpdate = new Counts(Interlocked.Add(ref _data, unchecked((ulong)1) << WaiterCountShift));
                Debug.Assert(countsAfterUpdate.WaiterCount != ushort.MaxValue); // overflow check
                return countsAfterUpdate;
            }

            public ushort CountOfWaitersSignaledToWake
            {
                get => GetUInt16Value(CountOfWaitersSignaledToWakeShift);
            }

            public void AddCountOfWaitersSignaledToWake(uint value)
            {
                _data += (ulong)value << CountOfWaitersSignaledToWakeShift;
                var countsAfterUpdate = new Counts(_data);
                Debug.Assert(countsAfterUpdate.CountOfWaitersSignaledToWake != ushort.MaxValue); // overflow check
            }

            public void DecrementCountOfWaitersSignaledToWake()
            {
                Debug.Assert(CountOfWaitersSignaledToWake != 0);
                _data -= (ulong)1 << CountOfWaitersSignaledToWakeShift;
            }

            public Counts InterlockedCompareExchange(Counts newCounts, Counts oldCounts) =>
                new Counts(Interlocked.CompareExchange(ref _data, newCounts._data, oldCounts._data));

            public static bool operator ==(Counts lhs, Counts rhs) => lhs.Equals(rhs);
            public static bool operator !=(Counts lhs, Counts rhs) => !lhs.Equals(rhs);

            public override bool Equals([NotNullWhen(true)] object? obj) => obj is Counts other && Equals(other);
            public bool Equals(Counts other) => _data == other._data;
            public override int GetHashCode() => (int)_data + (int)(_data >> 32);
        }

        [StructLayout(LayoutKind.Explicit, Size = 2 * PaddingHelpers.CACHE_LINE_SIZE)]
        private struct CacheLineSeparatedCounts
        {
            [FieldOffset(PaddingHelpers.CACHE_LINE_SIZE)]
            public Counts _counts;
        }
    }
}
