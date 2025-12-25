// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Threading
{
    /// <summary>
    /// A LIFO semaphore.
    /// Waits on this semaphore are uninterruptible.
    /// </summary>
    internal sealed partial class LowLevelLifoSemaphore : IDisposable
    {
        private CacheLineSeparatedCounts _separated;

        private readonly int _maximumSignalCount;
        private readonly uint _spinCount;
        private readonly Action _onWait;

        public LowLevelLifoSemaphore(int maximumSignalCount, uint spinCount, Action onWait)
        {
            Debug.Assert(maximumSignalCount > 0);
            Debug.Assert(maximumSignalCount <= short.MaxValue);
            Debug.Assert(spinCount >= 0);

            _separated = default;
            _maximumSignalCount = maximumSignalCount;
            _spinCount = spinCount;
            _onWait = onWait;

            Create(maximumSignalCount);
        }

        public bool Wait(int timeoutMs)
        {
            Debug.Assert(timeoutMs >= -1);

#if FEATURE_WASM_MANAGED_THREADS
            Thread.AssureBlockingPossible();
#endif

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

            return WaitSlow(timeoutMs);
        }

        private bool WaitSlow(int timeoutMs)
        {
            // Now spin briefly with exponential backoff.
            // We use random exponential backoff because:
            // - we do not know how soon a signal appears, but with exponential backoff we will not be more than 2x off the ideal guess
            // - it gives mild preference to the most recent spinners. We want LIFO here so that hot(er) threads keep running.
            // - it is possible that spinning workers prevent non-pool threads from submitting more work to the pool,
            //   so we want some workers to sleep earlier than others.
            uint spinCount = Environment.IsSingleProcessor ? 0 : _spinCount;
            for (uint iteration = 0; iteration < spinCount; iteration++)
            {
                Backoff.Exponential(iteration);

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

            // Now we will try registering as a waiter and wait.
            // If signaled before that, we have to acquire as this can be the last thread that could take that signal.
            // The difference with spinning above is that we are not waiting for a signal. We should immediately succeed
            // unless a lot of threads are trying to update the counts. Thus we use a different attempt counter.
            uint collisionCount = 0;
            while (true)
            {
                Counts counts = _separated._counts;
                Counts newCounts = counts;
                if (counts.SignalCount != 0)
                {
                    newCounts.DecrementSignalCount();
                }
                else
                {
                    newCounts.IncrementWaiterCount();
                }

                Counts countsBeforeUpdate = _separated._counts.InterlockedCompareExchange(newCounts, counts);
                if (countsBeforeUpdate == counts)
                {
                    return counts.SignalCount != 0 || WaitForSignal(timeoutMs);
                }

                Backoff.Exponential(collisionCount++);
            }
        }

        private bool WaitForSignal(int timeoutMs)
        {
            Debug.Assert(timeoutMs > 0 || timeoutMs == -1);

            _onWait();

            while (true)
            {
                int startWaitTicks = timeoutMs != -1 ? Environment.TickCount : 0;
                if (timeoutMs == 0 || !WaitCore(timeoutMs))
                {
                    // Unregister the waiter. The wait subsystem used above guarantees that a thread that wakes due to a timeout does
                    // not observe a signal to the object being waited upon.
                    _separated._counts.InterlockedDecrementWaiterCount();
                    return false;
                }
                int endWaitTicks = timeoutMs != -1 ? Environment.TickCount : 0;

                uint collisionCount = 0;
                while (true)
                {
                    Counts counts = _separated._counts;
                    Counts newCounts = counts;

                    Debug.Assert(counts.WaiterCount != 0);
                    Debug.Assert(counts.CountOfWaitersSignaledToWake != 0);

                    newCounts.DecrementCountOfWaitersSignaledToWake();
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

                        // we've consumed a wake, but there was no signal, we will wait again.
                        break;
                    }

                    // collision, try again.
                    Backoff.Exponential(collisionCount++);
                }

                // we will wait again, reduce timeout
                if (timeoutMs != -1)
                {
                    int waitMs = endWaitTicks - startWaitTicks;
                    if (waitMs >= 0 && waitMs < timeoutMs)
                        timeoutMs -= waitMs;
                    else
                        timeoutMs = 0;
                }
            }
        }

        public void Signal()
        {
            // Increment signal count. This enables one-shot acquire.
            Counts counts = _separated._counts.InterlockedIncrementSignalCount();

            // Now check if waiters need to be woken
            uint collisionCount = 0;
            while (true)
            {
                // Determine how many waiters to wake.
                // The number of wakes should not be more than the signal count, not more than waiter count and discount any pending wakes.
                int countOfWaitersToWake = (int)Math.Min(counts.SignalCount, counts.WaiterCount) - counts.CountOfWaitersSignaledToWake;
                if (countOfWaitersToWake <= 0)
                {
                    // No waiters to wake. This is the most common case.
                    return;
                }

                Counts newCounts = counts;
                newCounts.AddCountOfWaitersSignaledToWake((uint)countOfWaitersToWake);
                Counts countsBeforeUpdate = _separated._counts.InterlockedCompareExchange(newCounts, counts);
                if (countsBeforeUpdate == counts)
                {
                    Debug.Assert(_maximumSignalCount - counts.SignalCount >= 1);
                    if (countOfWaitersToWake > 0)
                        ReleaseCore(countOfWaitersToWake);
                    return;
                }

                // collision, try again.
                Backoff.Exponential(collisionCount++);

                counts = _separated._counts;
            }
        }

        private struct Counts : IEquatable<Counts>
        {
            private const byte SignalCountShift = 0;
            private const byte WaiterCountShift = 16;
            private const byte CountOfWaitersSignaledToWakeShift = 32;

            private ulong _data;

            private Counts(ulong data) => _data = data;

            private ushort GetUInt16Value(byte shift) => (ushort)(_data >> shift);
            private void SetUInt16Value(ushort value, byte shift) =>
                _data = (_data & ~((ulong)ushort.MaxValue << shift)) | ((ulong)value << shift);

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

        [StructLayout(LayoutKind.Sequential)]
        private struct CacheLineSeparatedCounts
        {
            private readonly Internal.PaddingFor32 _pad1;
            public Counts _counts;
            private readonly Internal.PaddingFor32 _pad2;
        }
    }
}
