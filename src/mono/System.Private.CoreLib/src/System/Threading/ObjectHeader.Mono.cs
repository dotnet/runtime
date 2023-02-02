// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading;

/// <summary>
/// Manipulates the object header located in the first few words of each object in the managed heap.
/// </summary>
internal static class ObjectHeader
{
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct Header
    {
#region Keep in sync with src/native/public/mono/metadata/details/object-types.h
        public void* vtable;
        public IntPtr synchronization; // really a LockWord
#endregion // keep in sync with src/native/public/mono/metadata/details/object-types.h
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct MonoThreadsSync
    {
#region Keep in sync with monitor.h
        // FIXME: volatile?
        public uint status;
        public uint nest;
        public int hash_code;
        // Note: more fields after here
#endregion // keep in sync with monitor.h
    }

    private static class SyncBlock
    {
        public static ref int HashCode(ref MonoThreadsSync mon) => ref mon.hash_code;
        public static ref uint Status (ref MonoThreadsSync mon) => ref mon.status;

        // only call if current thread owns the lock
        public static void IncrementNest (ref MonoThreadsSync mon)
        {
            mon.nest++;
        }
    }

    // <summary>
    // Manipulate the MonoThreadSync:status field
    // </summary>
    private static class MonitorStatus
    {
#region Keep in sync with monitor.h
        private const uint OwnerMask = 0x0000ffffu;
        private const uint EntryCountMask = 0xffff0000u;
        //private const uint EntryCountWaiters = 0x80000000u;
        //private const uint EntryCountZero = 0x7fff0000u;
        //private const int EntryCountShift = 16;
#endregion // keep in sync with monitor.h

        public static int GetOwner(uint status) => (int)(status & OwnerMask);
        public static uint SetOwner (uint status, int owner)
        {
            return (status & EntryCountMask) | (uint)owner;
        }
    }

    // <summary>
    // A union that contains either an uninflated lockword, or a pointer to a synchronization struct
    // </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct LockWord
    {
#region Keep in sync with monitor.h

        private IntPtr _lock_word;

        private const int StatusBits = 2;

        private const int NestBits = 8;

        private const IntPtr StatusMask = (1 << StatusBits) - 1;

        private const IntPtr NestMask = ((1 << NestBits) - 1) << StatusBits;

        private const int HashShift = StatusBits;

        private const int NestShift = StatusBits;

        private const int OwnerShift = StatusBits + NestBits;

        [Flags]
        private enum Status
        {
            Flat = 0,
            HasHash = 1,
            Inflated = 2,
        }
#endregion // Keep in sync with monitor.h

        public bool IsInflated => (_lock_word & (IntPtr)Status.Inflated) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref MonoThreadsSync GetInflatedLock()
        {
            unsafe
            {
                IntPtr ptr = _lock_word & ~StatusMask;
                return ref Unsafe.AsRef<MonoThreadsSync>((void*)ptr);
            }
        }

        public bool HasHash => (_lock_word & (IntPtr)Status.HasHash) != 0;

        public bool IsFree => _lock_word == IntPtr.Zero;

        public bool IsFlat => (_lock_word & StatusMask) == (IntPtr)Status.Flat;

        public bool IsNested => (_lock_word & NestMask) == NestMask;

        public int FlatHash => (int)(_lock_word >>> HashShift);

        public int FlatNest
        {
            get
            {
                if (IsFree)
                    return 0;
                /* Inword nest count starts from 0 */
                return 1 + (int)((_lock_word & NestMask) >>> NestShift);
            }
        }

        public bool IsNestMax => (_lock_word & NestMask) == NestMask;

        public LockWord IncrementNest()
        {
            LockWord res;
            res._lock_word = _lock_word + (1 << NestShift);
            return res;
        }

        public LockWord DecrementNest()
        {
            LockWord res;
            res._lock_word = _lock_word - (1 << NestShift);
            return res;
        }

        public int GetOwner() => (int)(_lock_word >>> OwnerShift);

        public static LockWord NewThinHash(int hash)
        {
            LockWord res;
            res._lock_word = (((IntPtr)(uint)hash) << HashShift) | (IntPtr)Status.HasHash;
            return res;
        }

        public static unsafe LockWord NewInflated(MonoThreadsSync* sync)
        {
            IntPtr ptr = (IntPtr)(void*)sync;
            ptr |= (IntPtr)Status.Inflated;
            LockWord res;
            res._lock_word = ptr;
            return res;
        }

        public static LockWord NewFlat(int owner)
        {
            LockWord res;
            res._lock_word = ((IntPtr)(uint)owner) << OwnerShift;
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LockWord FromObjectHeader(ref Header header)
        {
            LockWord lw;
            lw._lock_word = header.synchronization;
            return lw;
        }

        public IntPtr AsIntPtr => _lock_word;

        internal void SetFromIntPtr (IntPtr new_lw)
        {
            _lock_word = new_lw;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe ref Header ObjectHeaderUNSAFE(ref object obj)
    {
        Header** hptr = (Header**)Unsafe.AsPointer(ref obj);
        ref Header h = ref Unsafe.AsRef<Header>(*hptr);
        return ref h;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static LockWord GetLockWord(ref object obj)
    {
        LockWord lw;
        unsafe
        {
            ref Header h = ref ObjectHeaderUNSAFE(ref obj);
            lw = LockWord.FromObjectHeader(ref h);
        }
        GC.KeepAlive(obj);
        return lw;
    }

    private static IntPtr LockWordCompareExchange (ref object obj, LockWord nlw, LockWord expected)
    {
        ref Header h = ref ObjectHeaderUNSAFE(ref obj);
        IntPtr result = Interlocked.CompareExchange (ref h.synchronization, nlw.AsIntPtr, expected.AsIntPtr);
        GC.KeepAlive (obj);
        return result;
    }

    /// <summary>
    /// Tries to get the hash code from the object if it is
    /// already known and return true. Otherwise returns false.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetHashCode(object? o, out int hash)
    {
        hash = 0;
        if (o == null)
            return true;

        LockWord lw = GetLockWord (ref o);
        if (lw.HasHash) {
            if (lw.IsInflated) {
                ref MonoThreadsSync mon = ref lw.GetInflatedLock();
                ref int hashRef = ref SyncBlock.HashCode(ref mon);
                hash = hashRef;
                return false;
            } else {
                hash = lw.FlatHash;
                return true;
            }
        }
        GC.KeepAlive (o);
        return false;
    }

    private static bool TryEnterInflatedFast(object o)
    {
        LockWord lw = GetLockWord (ref o);
        int small_id = Thread.CurrentThread.GetSmallId();
        ref MonoThreadsSync mon = ref lw.GetInflatedLock();
        while (true)
        {
            uint old_status = SyncBlock.Status (ref mon);
            if (MonitorStatus.GetOwner(old_status) == 0)
            {
                uint new_status = MonitorStatus.SetOwner(old_status, small_id);
                uint prev_status = Interlocked.CompareExchange (ref SyncBlock.Status (ref mon), new_status, old_status);
                if (prev_status == old_status)
                {
                    return true;
                }
                // someone else changed the status, go around the loop again
                continue;
            }
            if (MonitorStatus.GetOwner(old_status) == small_id)
            {
                // we own it
                SyncBlock.IncrementNest (ref mon);
                return true;
            }
            else
            {
                // someone else owns it, fall back to slow path
                return false;
            }
        }
    }

    // returns false if we should fall back to the slow path
    // returns true if the lock was taken
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryEnterFast(object? o)
    {
        Debug.Assert (o != null);
        LockWord lw = GetLockWord (ref o);
        if (lw.IsFree)
        {
            int owner = Thread.CurrentThread.GetSmallId();
            LockWord nlw = LockWord.NewFlat(owner);
            if (LockWordCompareExchange (ref o, nlw, lw) == lw.AsIntPtr)
            {
                return true;
            } else {
                return false;
            }
        }
        else if (lw.IsInflated)
        {
            return TryEnterInflatedFast(o);
        }
        else if (lw.IsFlat)
        {
            int owner = Thread.CurrentThread.GetSmallId();
            if (lw.GetOwner() == owner)
            {
                if (lw.IsNestMax)
                {
                    // too much recursive locking, need to inflate
                    return false;
                } else {
                    LockWord nlw = lw.IncrementNest();
                    if (LockWordCompareExchange (ref o, nlw, lw) == lw.AsIntPtr)
                    {
                        return true;
                    }
                    else
                    {
                        // someone else inflated it in the meantime, fall back to slow path
                        return false;
                    }
                }
            }
            // there's contention, go to slow path
            return false;
        }
        Debug.Assert (lw.HasHash);
        return false;
    }

    // true if obj is owned by the current thread
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEntered(object obj)
    {
        LockWord lw = GetLockWord(ref obj);

        if (lw.IsFlat)
        {
            return lw.GetOwner() == Thread.CurrentThread.GetSmallId();
        }
        else if (lw.IsInflated)
        {
            return MonitorStatus.GetOwner(lw.GetInflatedLock().status) == Thread.CurrentThread.GetSmallId();
        }
        return false;
    }

    // true if obj is owned by any thread
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasOwner(object obj)
    {
        LockWord lw = GetLockWord(ref obj);

        if (lw.IsFlat)
            return !lw.IsFree;
        else if (lw.IsInflated)
        {
            return MonitorStatus.GetOwner(lw.GetInflatedLock().status) != 0;
        }

        return false;
    }

}
