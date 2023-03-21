// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading;

// <summary>
// This class provides a way for browser threads to asynchronously wait for a sempahore
// from JS, without using the threadpool.  It is used to implement threadpool workers.
// </summary>
internal sealed partial class LowLevelJSSemaphore : IDisposable
{
    // TODO: implement some of the managed stuff from LowLevelLifoSemaphore
    private IntPtr lifo_semaphore;
    private CacheLineSeparatedCounts _separated;

    private readonly int _maximumSignalCount;
    private readonly int _spinCount;
    private readonly Action _onWait;

    // private const int SpinSleep0Threshold = 10;

    internal LowLevelJSSemaphore(int initialSignalCount, int maximumSignalCount, int spinCount, Action onWait)
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

        Create(maximumSignalCount);
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern IntPtr InitInternal();

#pragma warning disable IDE0060
    private void Create(int maximumSignalCount)
    {
        lifo_semaphore = InitInternal();
    }
#pragma warning restore IDE0060

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void DeleteInternal(IntPtr semaphore);

    public void Dispose()
    {
        DeleteInternal(lifo_semaphore);
        lifo_semaphore = IntPtr.Zero;
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void ReleaseInternal(IntPtr semaphore, int count);

    internal void Release(int additionalCount)
    {
        ReleaseInternal(lifo_semaphore, additionalCount);
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern unsafe void PrepareWaitInternal(IntPtr semaphore,
                                                   int timeoutMs,
                                                          /*delegate* unmanaged<IntPtr, GCHandle, IntPtr, void> successCallback*/ void* successCallback,
                                                          /*delegate* unmanaged<IntPtr, GCHandle, IntPtr, void> timeoutCallback*/ void* timeoutCallback,
                                                   GCHandle handle,
                                                   IntPtr userData);

    private sealed record WaitEntry (LowLevelJSSemaphore Semaphore, Action<LowLevelJSSemaphore, object?> OnSuccess, Action<LowLevelJSSemaphore, object?> OnTimeout, object? State);

    internal void PrepareWait(int timeout_ms, Action<LowLevelJSSemaphore, object?> onSuccess, Action<LowLevelJSSemaphore, object?> onTimeout, object? state)
    {
        WaitEntry entry = new (this, onSuccess, onTimeout, state);
        GCHandle gchandle = GCHandle.Alloc (entry);
        unsafe {
            delegate* unmanaged<IntPtr, GCHandle, IntPtr, void> successCallback = &SuccessCallback;
            delegate* unmanaged<IntPtr, GCHandle, IntPtr, void> timeoutCallback = &TimeoutCallback;
            PrepareWaitInternal (lifo_semaphore, timeout_ms, successCallback, timeoutCallback, gchandle, IntPtr.Zero);
        }
    }

    [UnmanagedCallersOnly]
    private static void SuccessCallback(IntPtr lifo_semaphore, GCHandle gchandle, IntPtr user_data)
    {
        WaitEntry entry = (WaitEntry)gchandle.Target!;
        gchandle.Free();
        entry.OnSuccess(entry.Semaphore, entry.State);
    }

    [UnmanagedCallersOnly]
    private static void TimeoutCallback(IntPtr lifo_semaphore, GCHandle gchandle, IntPtr user_data)
    {
        WaitEntry entry = (WaitEntry)gchandle.Target!;
        gchandle.Free();
        entry.OnTimeout(entry.Semaphore, entry.State);
    }

#region Counts
    private struct Counts : IEquatable<Counts>
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
    private struct CacheLineSeparatedCounts
    {
        private readonly Internal.PaddingFor32 _pad1;
        public Counts _counts;
        private readonly Internal.PaddingFor32 _pad2;
    }
#endregion

}
