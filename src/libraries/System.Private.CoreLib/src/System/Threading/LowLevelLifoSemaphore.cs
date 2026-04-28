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
        private const int DefaultSemaphoreSpinCountLimit = 256;

        private CacheLineSeparatedCounts _separated;

        private readonly int _maximumSignalCount;
        private readonly int _maxSpinCount;
        private readonly Action _onWait;

        // When we need to block threads we use a linked list of thread blockers.
        // When we need to wake a worker, we pop the topmost blocker and release it.
        private sealed class LifoWaitNode : LowLevelThreadBlocker
        {
            internal LifoWaitNode? _next;
        }

        private readonly LowLevelLock _stackLock = new LowLevelLock();
        private LifoWaitNode? _blockerStack;

        // Sometimes due to races we may see nonzero waiter count, but no blockers to wake.
        // That happens if a thread that added itself to waiter count, has not yet blocked itself.
        // In such case we increment _pendingSignals and the waiter will simply
        // decrement the counter and return without blocking.
        private int _pendingSignals;

        [ThreadStatic]
        private static LifoWaitNode? t_blocker;

        public LowLevelLifoSemaphore(int maximumSignalCount, Action onWait)
        {
            Debug.Assert(maximumSignalCount > 0);
            Debug.Assert(maximumSignalCount <= short.MaxValue);

            _separated = default;
            _maximumSignalCount = maximumSignalCount;
            _onWait = onWait;

            _maxSpinCount = AppContextConfigHelper.GetInt32ComPlusOrDotNetConfig(
                "System.Threading.ThreadPool.UnfairSemaphoreSpinLimit",
                "ThreadPool_UnfairSemaphoreSpinLimit",
                DefaultSemaphoreSpinCountLimit,
                false);

            // Do not accept unreasonably huge _maxSpinCount value to prevent overflows.
            // Also, 1+ minute spins do not make sense.
            if (_maxSpinCount > 1000000)
                _maxSpinCount = DefaultSemaphoreSpinCountLimit;
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
            Counts counts = _separated._counts.InterlockedIncrementWaiterCount();

            // If there are pending signals, we may end in a condition that requires
            // waking a waiter.
            // Perhaps the current thread will be such waiter, but we should still
            // go through wait/wake routine (vs. just claiming the signal) as the
            // caller wants to park the thread.
            MaybeWakeWaiters(counts);

            return WaitAsWaiter(timeoutMs);
        }

        private void MaybeWakeWaiters(Counts counts)
        {
            // Check if waiters need to be woken
            uint collisionCount = 0;
            while (true)
            {
                // Determine how many waiters we can wake.
                // The number of wakes should not be more than the signal count, not more than waiter count and discount any pending wakes.
                int countOfWaitersToWake = (int)Math.Min(counts.SignalCount, counts.WaiterCount) - counts.CountOfWaitersSignaledToWake;
                if (countOfWaitersToWake <= 0)
                {
                    // No waiters to wake. This is the most common case.
                    break;
                }

                // Wake one waiter. If it finds work it will ask for workers and that can wake more waiters if spinners
                // do not consume the additional signals.
                // NB: It is rare to have > 1 signal. That only happens when the count of desired workers had a forced change.
                // We would prefer that extra signals be consumed by spinners thus we release waiters one by one.
                countOfWaitersToWake = 1;
                if (counts.CountOfWaitersSignaledToWake > 0)
                {
                    // A waiter is already waking up.
                    break;
                }

                Counts newCounts = counts;
                newCounts.AddCountOfWaitersSignaledToWake((uint)countOfWaitersToWake);
                Counts countsBeforeUpdate = _separated._counts.InterlockedCompareExchange(newCounts, counts);
                if (countsBeforeUpdate == counts)
                {
                    Debug.Assert(_maximumSignalCount - counts.SignalCount >= 1);
                    if (countOfWaitersToWake > 0)
                    {
                        ReleaseCore(countOfWaitersToWake);
                    }

                    break;
                }

                // collision, try again.
                Backoff.Exponential(collisionCount++);

                counts = _separated._counts;
            }
        }

        private bool WaitAsWaiter(int timeoutMs)
        {
            Debug.Assert(timeoutMs >= -1);

            while (true)
            {
                long waitStartTick = Stopwatch.GetTimestamp();

                // Allow anyone who wants to run to go ahead.
                // (before trying to block and possibly taking a fast wake path)
                Thread.UninterruptibleSleep0();

                if (timeoutMs == 0 || !Block(timeoutMs))
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
                long cooldown = Stopwatch.Frequency * 4 / 1000000;
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
                    Debug.Assert(counts.CountOfWaitersSignaledToWake != 0);
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
            MaybeWakeWaiters(counts);
        }

        private bool Block(int timeoutMs)
        {
            Debug.Assert(timeoutMs >= -1);

            LifoWaitNode? blocker = t_blocker;
            if (blocker == null)
            {
                t_blocker = blocker = new LifoWaitNode();
            }

            _stackLock.Acquire();
            if (_pendingSignals != 0)
            {
                Debug.Assert(_blockerStack == null);
                Debug.Assert(_pendingSignals > 0);
                _pendingSignals--;
                blocker = null;
            }
            else
            {
                blocker._next = _blockerStack;
                _blockerStack = blocker;
            }

            _stackLock.Release();

            // lock release has a full fence thus ordinary read of _pendingWakes is ok
            if (_pendingWakes > 0)
                WakeOneCore();

            if (blocker != null)
            {
                _onWait();
                while (!blocker.TimedWait(timeoutMs))
                {
                    if (TryRemove(blocker))
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

            return true;
        }

        private void ReleaseCore(int count)
        {
            Debug.Assert(count > 0);

            for (int i = 0; i < count; i++)
            {
                WakeOne();
            }
        }

        private int _pendingWakes;

        private void WakeOne()
        {
            Interlocked.Increment(ref _pendingWakes);
            WakeOneCore();
        }

        private void WakeOneCore()
        {
            while (true)
            {
                if (!_stackLock.TryAcquire())
                    return; // lock holder will pick up _pendingWakes on their exit

                if (Interlocked.Decrement(ref _pendingWakes) < 0)
                {
                    // No work claimed - restore and bail
                    Interlocked.Increment(ref _pendingWakes);
                    _stackLock.Release();
                    return;
                }

                LifoWaitNode? top = _blockerStack;
                if (top != null)
                {
                    _blockerStack = top._next;
                    top._next = null;
                }
                else
                {
                    _pendingSignals++;
                    Debug.Assert(_pendingSignals != ushort.MaxValue);
                }

                _stackLock.Release();
                if (top != null)
                {
                    top.WakeOne();
                }

                // lock release has a full fence thus ordinary read of _pendingWakes is ok
                if (_pendingWakes <= 0)
                    return;

                // Loop: handle any wakes that arrived while we were working
            }
        }

        // Used when waiter times out
        private bool TryRemove(LifoWaitNode node)
        {
            bool removed = false;
            _stackLock.Acquire();

            LifoWaitNode? current = _blockerStack;
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

            _stackLock.Release();

            // lock release has a full fence thus ordinary read of _pendingWakes is ok
            if (_pendingWakes > 0)
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
