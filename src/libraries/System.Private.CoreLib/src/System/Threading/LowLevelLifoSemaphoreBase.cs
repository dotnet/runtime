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
    internal abstract class LowLevelLifoSemaphoreBase
    {
        protected CacheLineSeparatedCounts _separated;

        protected readonly int _maximumSignalCount;
        protected readonly int _spinCount;
        protected readonly Action _onWait;

        public LowLevelLifoSemaphoreBase(int initialSignalCount, int maximumSignalCount, int spinCount, Action onWait)
        {
            Debug.Assert(initialSignalCount >= 0);
            Debug.Assert(initialSignalCount <= maximumSignalCount);
            Debug.Assert(maximumSignalCount > 0);
            Debug.Assert(spinCount >= 0);

            _separated = default;
            _separated._counts.SignalCount = (uint)initialSignalCount;
            _maximumSignalCount = maximumSignalCount;
            _spinCount = spinCount;
            _onWait = onWait;
        }

        protected abstract void ReleaseCore(int count);

        public void Release(int releaseCount)
        {
            Debug.Assert(releaseCount > 0);
            Debug.Assert(releaseCount <= _maximumSignalCount);

            int countOfWaitersToWake;
            Counts counts = _separated._counts;
            while (true)
            {
                Counts newCounts = counts;

                // Increase the signal count. The addition doesn't overflow because of the limit on the max signal count in constructor.
                newCounts.AddSignalCount((uint)releaseCount);

                // Determine how many waiters to wake, taking into account how many spinners and waiters there are and how many waiters
                // have previously been signaled to wake but have not yet woken
                countOfWaitersToWake =
                    (int)Math.Min(newCounts.SignalCount, (uint)counts.WaiterCount + counts.SpinnerCount) -
                    counts.SpinnerCount -
                    counts.CountOfWaitersSignaledToWake;
                if (countOfWaitersToWake > 0)
                {
                    // Ideally, limiting to a maximum of releaseCount would not be necessary and could be an assert instead, but since
                    // WaitForSignal() does not have enough information to tell whether a woken thread was signaled, and due to the cap
                    // below, it's possible for countOfWaitersSignaledToWake to be less than the number of threads that have actually
                    // been signaled to wake.
                    if (countOfWaitersToWake > releaseCount)
                    {
                        countOfWaitersToWake = releaseCount;
                    }

                    // Cap countOfWaitersSignaledToWake to its max value. It's ok to ignore some woken threads in this count, it just
                    // means some more threads will be woken next time. Typically, it won't reach the max anyway.
                    newCounts.AddUpToMaxCountOfWaitersSignaledToWake((uint)countOfWaitersToWake);
                }

                Counts countsBeforeUpdate = _separated._counts.InterlockedCompareExchange(newCounts, counts);
                if (countsBeforeUpdate == counts)
                {
                    Debug.Assert(releaseCount <= _maximumSignalCount - counts.SignalCount);
                    if (countOfWaitersToWake > 0)
                        ReleaseCore(countOfWaitersToWake);
                    return;
                }

                counts = countsBeforeUpdate;
            }
        }

        protected struct Counts : IEquatable<Counts>
        {
            private const byte SignalCountShift = 0;
            private const byte WaiterCountShift = 32;
            private const byte SpinnerCountShift = 48;
            private const byte CountOfWaitersSignaledToWakeShift = 56;

            private ulong _data;

            private Counts(ulong data) => _data = data;

            private uint GetUInt32Value(byte shift) => (uint)(_data >> shift);
            private void SetUInt32Value(uint value, byte shift) =>
                _data = (_data & ~((ulong)uint.MaxValue << shift)) | ((ulong)value << shift);
            private ushort GetUInt16Value(byte shift) => (ushort)(_data >> shift);
            private void SetUInt16Value(ushort value, byte shift) =>
                _data = (_data & ~((ulong)ushort.MaxValue << shift)) | ((ulong)value << shift);
            private byte GetByteValue(byte shift) => (byte)(_data >> shift);
            private void SetByteValue(byte value, byte shift) =>
                _data = (_data & ~((ulong)byte.MaxValue << shift)) | ((ulong)value << shift);

            public uint SignalCount
            {
                get => GetUInt32Value(SignalCountShift);
                set => SetUInt32Value(value, SignalCountShift);
            }

            public void AddSignalCount(uint value)
            {
                Debug.Assert(value <= uint.MaxValue - SignalCount);
                _data += (ulong)value << SignalCountShift;
            }

            public void IncrementSignalCount() => AddSignalCount(1);

            public void DecrementSignalCount()
            {
                Debug.Assert(SignalCount != 0);
                _data -= (ulong)1 << SignalCountShift;
            }

            public ushort WaiterCount
            {
                get => GetUInt16Value(WaiterCountShift);
                set => SetUInt16Value(value, WaiterCountShift);
            }

            public void IncrementWaiterCount()
            {
                Debug.Assert(WaiterCount < ushort.MaxValue);
                _data += (ulong)1 << WaiterCountShift;
            }

            public void DecrementWaiterCount()
            {
                Debug.Assert(WaiterCount != 0);
                _data -= (ulong)1 << WaiterCountShift;
            }

            public void InterlockedDecrementWaiterCount()
            {
                var countsAfterUpdate = new Counts(Interlocked.Add(ref _data, unchecked((ulong)-1) << WaiterCountShift));
                Debug.Assert(countsAfterUpdate.WaiterCount != ushort.MaxValue); // underflow check
            }

            public byte SpinnerCount
            {
                get => GetByteValue(SpinnerCountShift);
                set => SetByteValue(value, SpinnerCountShift);
            }

            public void IncrementSpinnerCount()
            {
                Debug.Assert(SpinnerCount < byte.MaxValue);
                _data += (ulong)1 << SpinnerCountShift;
            }

            public void DecrementSpinnerCount()
            {
                Debug.Assert(SpinnerCount != 0);
                _data -= (ulong)1 << SpinnerCountShift;
            }

            public byte CountOfWaitersSignaledToWake
            {
                get => GetByteValue(CountOfWaitersSignaledToWakeShift);
                set => SetByteValue(value, CountOfWaitersSignaledToWakeShift);
            }

            public void AddUpToMaxCountOfWaitersSignaledToWake(uint value)
            {
                uint availableCount = (uint)(byte.MaxValue - CountOfWaitersSignaledToWake);
                if (value > availableCount)
                {
                    value = availableCount;
                }
                _data += (ulong)value << CountOfWaitersSignaledToWakeShift;
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
        protected struct CacheLineSeparatedCounts
        {
            private readonly Internal.PaddingFor32 _pad1;
            public Counts _counts;
            private readonly Internal.PaddingFor32 _pad2;
        }
    }
}
