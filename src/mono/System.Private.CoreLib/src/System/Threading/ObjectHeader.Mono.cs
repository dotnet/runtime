// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    // A union that contains either an uninflated lockword, or a pointer to a synchronization struct
    // </summary>
    [StructLayout(LayoutKind.Explicit)]
    private struct LockWord
    {
#region Keep in sync with monitor.h

        [FieldOffset(0)]
        private IntPtr _lock_word;
        [FieldOffset(0)]
        private unsafe MonoThreadsSync* _sync;

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

        public unsafe MonoThreadsSync* GetInflatedLock()
        {
            LockWord lw;
            lw._sync = default;
            lw._lock_word = _lock_word & ~StatusMask;
            return lw._sync;
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
            unsafe { res._sync = default; }
            res._lock_word = _lock_word + (1 << NestShift);
            return res;
        }

        public LockWord DecrementNest()
        {
            LockWord res;
            unsafe { res._sync = default; }
            res._lock_word = _lock_word - (1 << NestShift);
            return res;
        }

        public int GetOwner() => (int)(_lock_word >> OwnerShift);

        public static LockWord NewThinHash(int hash)
        {
            LockWord res;
            unsafe { res._sync = default; }
            res._lock_word = (((IntPtr)(uint)hash) << HashShift) | (IntPtr)Status.HasHash;
            return res;
        }

        public static unsafe LockWord NewInflated(MonoThreadsSync* sync)
        {
            LockWord res;
            res._lock_word = default;
            res._sync = sync;
            res._lock_word |= (IntPtr)Status.Inflated;
            return res;
        }

        public static LockWord NewFlat(int owner)
        {
            LockWord res;
            unsafe { res._sync = default; }
            res._lock_word = ((IntPtr)(uint)owner) << OwnerShift;
            return res;
        }

        public static LockWord FromObjectHeader(ref Header header)
        {
            LockWord lw;
            unsafe { lw._sync = default; }
            lw._lock_word = header.synchronization;
            return lw;
        }

        public IntPtr AsIntPtr => _lock_word;
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
                unsafe {
                    hash = lw.GetInflatedLock()->hash_code;
                    return true;
                }
            } else {
                hash = lw.FlatHash;
                return true;
            }
        }
        return false;
    }

#if false // WIP
    public static bool TryEnterFast(object? o, ref bool lockTaken)
    {
        if (lockTaken || o == null)
            return false;

        LockWord lw = GetLockWord (ref o);
        if (lw.IsFree)
        {
            int owner = Thread.CurrentThread.GetSmallId();
            LockWord nlw = LockWord.NewFlat(owner);
            if (LockWordCompareExchnage (ref o, nlw, lw) == lw.AsIntPtr)
            {
                lockTaken = true;
                return true;
            } else {
                // someone acquired it in the meantime or put in a hash
                Inflate(o);
                return TryEnterInflated(o, ref lockTaken);
            }
        }
        else if (lw.IsInflated)
            return TryEnterInflated(o, ref lockTaken);
        else if (lw.IsFlat)
        {
            int owner = Thread.CurrentThread.GetSmallId();
            if (lw.GetOwner() == owner)
            {
                if (lw.IsMaxNest)
                {
                    InflateOwned(o);
                    return TryEnterInflated(o, ref lockTaken);
                }
                else
                {
                    LockWord nlw = lw.IncrementNest();
                    IntPtr prev = LockWordCompareExchange(ref o, nlw, lw);
                    if (prev != lw.AsIntPtr)
                    {
                        // Someone else inflated it in the meantime
                        return TryEnterInflated(o, ref lockTaken);
                    }
                    lockTaken = true;
                    return true;
                }
            }
        }
        Debug.Assert (lw.HasHash);
        Inflate(o);
        return TryEnterInflated(o, ref lockTaken);
    }
#endif

}
