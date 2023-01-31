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
        public IntPtr vtable;
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
    [StructLayout(LayoutKind.Explicit)]
    private struct LockWord
    {
#region Keep in sync with monitor.h

        [FieldOffset(0)]
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

        public int FlatHash => (int)(_lock_word >> HashShift);

        public int FlatNest
        {
            get
            {
                if (IsFree)
                    return 0;
                /* Inword nest count starts from 0 */
                return 1 + (int)((_lock_word & NestMask) >> NestShift);
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

        public int GetOwner() => (int)(_lock_word >> OwnerShift);

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

    private static LockWord GetLockWord(ref object obj)
    {
        ref Header h = ref Unsafe.As<object, Header>(ref obj);
        LockWord lw = LockWord.FromObjectHeader(ref h);
        GC.KeepAlive(obj);
        return lw;
    }

    private static IntPtr LockWordCompareExchange (ref object obj, LockWord nlw, LockWord expected)
    {
        ref Header h = ref Unsafe.As<object, Header>(ref obj);
        return Interlocked.CompareExchange (ref h.synchronization, nlw.AsIntPtr, expected.AsIntPtr);
    }

    /// <summary>
    /// Tries to get the hash code from the object if it is
    /// already known and return true. Otherwise returns false.
    /// </summary>
    public static bool TryGetHashCode(object? o, out int hash)
    {
        hash = 0;
        if (o == null)
            return true;

        LockWord lw = GetLockWord (ref o);
        if (lw.HasHash) {
            if (lw.IsInflated) {
                hash = lw.GetInflatedLock().hash_code;
                return true;
            } else {
                hash = lw.FlatHash;
                return true;
            }
        }
        return false;
    }

#if false // need to implement alloc_mon/discard_mon
    private static void Inflate(object o)
    {
        unsafe
        {
            MonoThreadsSync *mon = alloc_mon();
            LockWord nlw = LockWord.NewInflated (mon);
            LockWord old_lw = GetLockWord (ref o);
            while (true)
            {
                if (old_lw.IsInflated)
                    break;
                else if (old_lw.HasHash)
                {
                    mon->hash_code = old_lw.FlatHash;
                    mon->status = SyncSetOwner (mon->status, 0);
                    nlw = nlw.SetHasHash();
                    
                }
                else if (old_lw.IsFree)
                {
                    mon->status = SyncSetOwner (mon->status, old_lw.GetOwner());
                    mon->nest = old_lw.FlatNest;
                }
                Interlocked.MemoryBarrier();
                IntPtr prev = LockWordCompareExchange(ref o, nlw, old_lw);
                if (prev == old_lw.AsIntPtr)
                    return; // success
                old_lw.SetFromIntPtr(prev); // go around one more time
            }
            // someone else inflated it first
            discard_mon (mon);
        }
    }
#endif

    private static bool TryEnterInflatedFast(object o, ref bool lockTaken)
    {
        LockWord lw = GetLockWord (ref o);
        int small_id = Thread.CurrentThread.GetSmallId();
        ref MonoThreadsSync mon = ref lw.GetInflatedLock();
        while (true)
        {
            uint old_status = mon.status;
            if (MonitorStatus.GetOwner(old_status) == 0)
            {
                uint new_status = MonitorStatus.SetOwner(old_status, small_id);
                uint prev_status = Interlocked.CompareExchange (ref mon.status, new_status, old_status);
                if (prev_status == old_status)
                {
                    lockTaken = true;
                    return true;
                }
                // someone else changed the status, go around the loop again
                continue;
            }
            if (MonitorStatus.GetOwner(old_status) == small_id)
            {
                // we own it
                mon.nest++;
                lockTaken = true;
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
    // returns true if we tried to enter
    // sets lockTaken to true if the lock was taken
    public static bool TryEnterFast(object? o, ref bool lockTaken)
    {
        if (lockTaken || o == null)
            return false;

        lockTaken = false;

        LockWord lw = GetLockWord (ref o);
        if (lw.IsFree)
        {
            int owner = Thread.CurrentThread.GetSmallId();
            LockWord nlw = LockWord.NewFlat(owner);
            if (LockWordCompareExchange (ref o, nlw, lw) == lw.AsIntPtr)
            {
                lockTaken = true;
                return true;
            } else {
                return false;
#if false
                // someone acquired it in the meantime or put in a hash
                Inflate(o);
                return TryEnterInflatedFast(o, ref lockTaken);
#endif
            }
        }
        else if (lw.IsInflated)
        {
            return TryEnterInflatedFast(o, ref lockTaken);
        }
        else if (lw.IsFlat)
        {
            return false;
#if false
            int owner = Thread.CurrentThread.GetSmallId();
            if (lw.GetOwner() == owner)
            {
                if (lw.IsMaxNest)
                {
                    InflateOwned(o);
                    return TryEnterInflatedFast(o, ref lockTaken);
                }
                else
                {
                    LockWord nlw = lw.IncrementNest();
                    IntPtr prev = LockWordCompareExchange(ref o, nlw, lw);
                    if (prev != lw.AsIntPtr)
                    {
                        // Someone else inflated it in the meantime
                        return TryEnterInflatedFast(o, ref lockTaken);
                    }
                    lockTaken = true;
                    return true;
                }
            }
            Inflate(o);
            return TryEnterInflatedFast(o, refLockTaken);
#endif
        }
        Debug.Assert (lw.HasHash);
        return false;
#if false
        Inflate(o);
        return TryEnterInflatedFast(o, ref lockTaken);
#endif
    }

    // true if obj is owned by the current thread
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
