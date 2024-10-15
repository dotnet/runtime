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

    // This is similar to QCallHandler ObjectHandleOnStack, but with a getter that let's view the
    // object's header.  This does two things:
    //
    // 1. It gives us a way to pass around a reference to the object header
    //
    // 2. because mono uses conservative stack scanning, we ensure there's always some place on the
    // stack that stores a pointer to the object, thus pinning the object.
    private unsafe ref struct ObjectHeaderOnStack
    {
        private Header** _header;
        private ObjectHeaderOnStack(ref object o)
        {
            _header = (Header**)Unsafe.AsPointer(ref o);
        }
        public static ObjectHeaderOnStack Create(ref object o)
        {
            return new ObjectHeaderOnStack(ref o);
        }
        public ref Header Header => ref Unsafe.AsRef<Header>(*_header);

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
        public static int HashCode(ref MonoThreadsSync mon) => mon.hash_code;
        public static ref uint Status(ref MonoThreadsSync mon) => ref mon.status;

        // only call if current thread owns the lock
        public static void IncrementNest(ref MonoThreadsSync mon)
        {
            mon.nest++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryDecrementNest(ref MonoThreadsSync mon)
        {
            Debug.Assert(mon.nest > 0);
            if (mon.nest == 1)
            {
                // leave mon.nest == 1, the caller will set mon.owner to 0 to indicate the monitor
                // is unlocked.  nest will start from 1 for the next time it is entered.
                return false;
            }
            mon.nest--;
            return true;
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
        private const uint EntryCountWaiters = 0x80000000u;
        //private const uint EntryCountZero = 0x7fff0000u;
        //private const int EntryCountShift = 16;
#endregion // keep in sync with monitor.h

        public static int GetOwner(uint status) => (int)(status & OwnerMask);
        public static uint SetOwner(uint status, int owner)
        {
            return (status & EntryCountMask) | (uint)owner;
        }

        public static bool HaveWaiters(uint status) => (status & EntryCountWaiters) != 0;
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

        public bool IsNested => (_lock_word & NestMask) != 0;

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

        internal void SetFromIntPtr(IntPtr new_lw)
        {
            _lock_word = new_lw;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static LockWord GetLockWord(ObjectHeaderOnStack h)
    {
        return LockWord.FromObjectHeader(ref h.Header);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IntPtr LockWordCompareExchange(ObjectHeaderOnStack h, LockWord nlw, LockWord expected)
    {
        return Interlocked.CompareExchange(ref h.Header.synchronization, nlw.AsIntPtr, expected.AsIntPtr);
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

        ObjectHeaderOnStack h = ObjectHeaderOnStack.Create(ref o);
        LockWord lw = GetLockWord(h);
        if (lw.HasHash) {
            if (lw.IsInflated) {
                ref MonoThreadsSync mon = ref lw.GetInflatedLock();
                hash = SyncBlock.HashCode(ref mon);
                return true;
            } else {
                hash = lw.FlatHash;
                return true;
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryEnterInflatedFast(scoped ref MonoThreadsSync mon, int small_id)
    {

        uint old_status = SyncBlock.Status(ref mon);
        if (MonitorStatus.GetOwner(old_status) == 0)
        {
            uint new_status = MonitorStatus.SetOwner(old_status, small_id);
            uint prev_status = Interlocked.CompareExchange(ref SyncBlock.Status(ref mon), new_status, old_status);
            if (prev_status == old_status)
            {
                return true;
            }
            // someone else changed the status, fall back to the slow path
            return false;
        }
        if (MonitorStatus.GetOwner(old_status) == small_id)
        {
            // we own it
            SyncBlock.IncrementNest(ref mon);
            return true;
        }
        else
        {
            // someone else owns it, fall back to slow path
            return false;
        }
    }

    // returns false if we should fall back to the slow path
    // returns true if the lock was taken
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryEnterFast(object? o)
    {
        Debug.Assert(o != null);
        ObjectHeaderOnStack h = ObjectHeaderOnStack.Create(ref o);
        LockWord lw = GetLockWord(h);
        if (!lw.IsInflated && lw.HasHash)
            return false; // need to inflate, fall back to native
        int owner = Thread.CurrentThread.GetSmallId();
        if (lw.IsFree)
        {
            LockWord nlw = LockWord.NewFlat(owner);
            if (LockWordCompareExchange(h, nlw, lw) == lw.AsIntPtr)
            {
                return true;
            } else {
                return false;
            }
        }
        else if (lw.IsInflated)
        {
            return TryEnterInflatedFast(ref lw.GetInflatedLock(), owner);
        }
        else if (lw.IsFlat)
        {
            if (lw.GetOwner() == owner)
            {
                if (lw.IsNestMax)
                {
                    // too much recursive locking, need to inflate
                    return false;
                } else {
                    LockWord nlw = lw.IncrementNest();
                    if (LockWordCompareExchange(h, nlw, lw) == lw.AsIntPtr)
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
        Debug.Assert(lw.HasHash);
        return false;
    }

    // true if obj is owned by the current thread
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEntered(object obj)
    {
        ObjectHeaderOnStack h = ObjectHeaderOnStack.Create(ref obj);
        LockWord lw = GetLockWord(h);

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
        ObjectHeaderOnStack h = ObjectHeaderOnStack.Create(ref obj);
        LockWord lw = GetLockWord(h);

        if (lw.IsFlat)
            return !lw.IsFree;
        else if (lw.IsInflated)
        {
            return MonitorStatus.GetOwner(lw.GetInflatedLock().status) != 0;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryExitInflated(scoped ref MonoThreadsSync mon)
    {
        // if we're in a nested lock, decrement the count and we're done
        if (SyncBlock.TryDecrementNest(ref mon))
            return true;

        ref uint status = ref SyncBlock.Status(ref mon);
        uint old_status = status;
        // if there are waiters, fall back to the slow path to wake them
        if (MonitorStatus.HaveWaiters(old_status))
            return false;
        uint new_status = MonitorStatus.SetOwner(old_status, 0);
        uint prev_status = Interlocked.CompareExchange(ref status, new_status, old_status);
        if (prev_status == old_status)
            return true; // success, and there were no waiters, we're done
        else
            return false; // we need to retry, but maybe a waiter arrived, fall back to the slow path
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryExitFlat(ObjectHeaderOnStack h, LockWord lw)
    {

        Debug.Assert(!lw.IsInflated);
        // if the lock word is flat, there has been no contention
        LockWord nlw;
        if (lw.IsNested)
            nlw = lw.DecrementNest();
        else
            nlw = default;

        if (LockWordCompareExchange(h, nlw, lw) == lw.AsIntPtr)
            return true;
        // someone inflated the lock in the meantime, fall back to the slow path

        return false;
    }


    // checks that obj is locked by the current thread
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    #pragma warning disable IDE0060 // spurious "unused parameter" warning because we never use obj, just take aref to it.
    public static bool TryExitChecked(object obj)
    #pragma warning restore IDE0060
    {
        ObjectHeaderOnStack h = ObjectHeaderOnStack.Create(ref obj);
        LockWord lw = GetLockWord(h);
        bool owned = false;

        ref MonoThreadsSync mon = ref Unsafe.NullRef<MonoThreadsSync>();
        if (lw.IsFlat)
        {
            owned = (lw.GetOwner() == Thread.CurrentThread.GetSmallId());
        }
        else if (lw.IsInflated)
        {
            mon = ref lw.GetInflatedLock();
            owned = (MonitorStatus.GetOwner(mon.status) == Thread.CurrentThread.GetSmallId());
        }
        if (!owned)
            throw new SynchronizationLockException(SR.Arg_SynchronizationLockException);
        if (lw.IsInflated)
            return TryExitInflated(ref mon);
        else
            return TryExitFlat(h, lw);
    }
}
