// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

using Internal.Runtime;

namespace System.Threading
{
    /// <summary>
    /// Manipulates the object header located 4 bytes before each object's MethodTable pointer
    /// in the managed heap.
    /// </summary>
    /// <remarks>
    /// Do not store managed pointers (ref int) to the object header in locals or parameters
    /// as they may be incorrectly updated during garbage collection.
    /// </remarks>
    internal static class ObjectHeader
    {
        // The following three header bits are reserved for the GC engine:
        //   BIT_SBLK_UNUSED        = 0x80000000
        //   BIT_SBLK_FINALIZER_RUN = 0x40000000
        //   BIT_SBLK_GC_RESERVE    = 0x20000000
        //
        // All other bits may be used to store runtime data: hash code, sync entry index, etc.
        // Here we use the same bit layout as in CLR: if bit 26 (BIT_SBLK_IS_HASHCODE) is set,
        // all the lower bits 0..25 store the hash code, otherwise they store either the sync
        // entry index (indicated by BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX) or thin lock data.
        private const int IS_HASHCODE_BIT_NUMBER = 26;
        private const int IS_HASH_OR_SYNCBLKINDEX_BIT_NUMBER = 27;
        private const int BIT_SBLK_IS_HASHCODE = 1 << IS_HASHCODE_BIT_NUMBER;
        internal const int MASK_HASHCODE_INDEX = BIT_SBLK_IS_HASHCODE - 1;
        private const int BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX = 1 << IS_HASH_OR_SYNCBLKINDEX_BIT_NUMBER;

        // if BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX is clear, the rest of the header dword is laid out as follows:
        // - lower sixteen bits (bits 0 thru 15) is thread id used for the thin locks
        //   value is zero if no thread is holding the lock
        // - following six bits (bits 16 thru 21) is recursion level used for the thin locks
        //   value is zero if lock is not taken or only taken once by the same thread
        private const int SBLK_MASK_LOCK_THREADID = 0x0000FFFF;   // special value of 0 + 65535 thread ids
        private const int SBLK_MASK_LOCK_RECLEVEL = 0x003F0000;   // 64 recursion levels
        private const int SBLK_LOCK_RECLEVEL_INC = 0x00010000;    // each level is this much higher than the previous one

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int* GetHeaderPtr(byte* ppObjectData)
        {
            // The header is the 4 bytes before a pointer-sized chunk before the object data pointer.
            return (int*)(ppObjectData - sizeof(void*) - sizeof(int));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasSyncEntryIndex(int header)
        {
            return (header & (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | BIT_SBLK_IS_HASHCODE)) == BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX;
        }

        /// <summary>
        /// Extracts the sync entry index or the hash code from the header value.  Returns true
        /// if the header value stores the sync entry index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetSyncEntryIndex(int header, out int index)
        {
            index = header & MASK_HASHCODE_INDEX;
            return HasSyncEntryIndex(header);
        }

        /// <summary>
        /// Returns the Monitor synchronization object assigned to this object.  If no synchronization
        /// object has yet been assigned, it assigns one in a thread-safe way.
        /// </summary>
        public static unsafe Lock GetLockObject(object o)
        {
            return SyncTable.GetLockObject(GetSyncIndex(o));
        }

        private static unsafe int GetSyncIndex(object o)
        {
            fixed (byte* pObjectData = &o.GetRawData())
            {
                int* pHeader = GetHeaderPtr(pObjectData);
                if (GetSyncEntryIndex(*pHeader, out int syncIndex))
                {
                    return syncIndex;
                }

                // Assign a new sync entry
                return SyncTable.AssignEntry(o, pHeader);
            }
        }

        //
        // A few words about spinning choices:
        //
        // Most locks are not contentious. In fact most locks provide exclusive access safety, but in reality are used by
        // one thread. And then there are locks that do see multiple threads, but the accesses are short and not overlapping.
        // Thin lock is an optimization for such scenarios.
        //
        // If we see a thin lock held by other thread for longer than ~5 microseconds, we will "inflate" the lock
        // and let the adaptive spinning in the fat Lock sort it out whether we have a contentious lock or a long-held lock.
        //
        // Another thing to consider is that SpinWait(1) is calibrated to about 35-50 nanoseconds.
        // It can take much longer only if nop/pause takes much longer, which it should not, as that would be getting
        // close to the RAM latency.
        //
        // Considering that taking and releasing the lock takes 2 CAS instructions + some overhead, we can estimate shortest
        // time the lock can be held to be in hundreds of nanoseconds. Thus it is unlikely to see more than
        // 8-10 threads contending for the lock without inflating it. Therefore we can expect to acquire a thin lock in
        // under 16 tries.
        //
        // As for the backoff strategy we have two choices:
        // Exponential back-off with a lmit:
        //   0, 1, 2, 4, 8, 8, 8, 8, 8, 8, 8, . . . .
        //
        // Linear back-off
        //   0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, . . . .
        //
        // In this case these strategies are close in terms of average and worst case latency, so we will prefer linear
        // back-off as it favors micro-contention scenario, which we expect.
        //

        // Returs:
        // -1 - success
        // 0 - failed
        // syncIndex - retry with the Lock
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int Acquire(object obj, int currentThreadID)
        {
            return TryAcquire(obj, currentThreadID, oneShot: false);
        }

        // -1 - success
        // 0 - failed
        // syncIndex - retry with the Lock
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int TryAcquire(object obj, int currentThreadID, bool oneShot = true)
        {
            ArgumentNullException.ThrowIfNull(obj);

            // if thread ID is uninitialized or too big, we do "uncommon" part.
            if ((uint)(currentThreadID - 1) <= (uint)SBLK_MASK_LOCK_THREADID)
            {
                // for an object used in locking there are two common cases:
                // - header bits are unused or
                // - there is a sync entry
                fixed (byte* pObjectData = &obj.GetRawData())
                {
                    int* pHeader = GetHeaderPtr(pObjectData);
                    int oldBits = *pHeader;
                    // if unused for anything, try setting our thread id
                    // N.B. hashcode, thread ID and sync index are never 0, and hashcode is largest of all
                    if ((oldBits & MASK_HASHCODE_INDEX) == 0)
                    {
                        if (Interlocked.CompareExchange(ref *pHeader, oldBits | currentThreadID, oldBits) == oldBits)
                        {
                            return -1;
                        }
                    }
                    else if (GetSyncEntryIndex(oldBits, out int syncIndex))
                    {
                        if (SyncTable.GetLockObject(syncIndex).TryEnterOneShot(currentThreadID))
                        {
                            return -1;
                        }

                        // has sync entry -> slow path
                        return syncIndex;
                    }
                }
            }

            return TryAcquireUncommon(obj, currentThreadID, oneShot);
        }

        // handling uncommon cases here - recursive lock, contention, retries
        // -1 - success
        // 0 - failed
        // syncIndex - retry with the Lock
        private static unsafe int TryAcquireUncommon(object obj, int currentThreadID, bool oneShot)
        {
            if (currentThreadID == 0)
                currentThreadID = Environment.CurrentManagedThreadId;

            // does thread ID fit?
            if (currentThreadID > SBLK_MASK_LOCK_THREADID)
                return GetSyncIndex(obj);

            // Lock.IsSingleProcessor gets a value that is lazy-initialized at the first contended acquire.
            // Until then it is false and we assume we have multicore machine.
            int retries = oneShot || Lock.IsSingleProcessor ? 0 : 16;

            // retry when the lock is owned by somebody else.
            // this loop will spinwait between iterations.
            for (int i = 0; i <= retries; i++)
            {
                fixed (byte* pObjectData = &obj.GetRawData())
                {
                    int* pHeader = GetHeaderPtr(pObjectData);

                    // rare retries when lock is not owned by somebody else.
                    // these do not count as iterations and do not spinwait.
                    while (true)
                    {
                        int oldBits = *pHeader;

                        // if unused for anything, try setting our thread id
                        // N.B. hashcode, thread ID and sync index are never 0, and hashcode is largest of all
                        if ((oldBits & MASK_HASHCODE_INDEX) == 0)
                        {
                            int newBits = oldBits | currentThreadID;
                            if (Interlocked.CompareExchange(ref *pHeader, newBits, oldBits) == oldBits)
                            {
                                return -1;
                            }

                            // contention on a lock that noone owned,
                            // but we do not know if there is an owner yet, so try again
                            continue;
                        }

                        // has sync entry -> slow path
                        if (GetSyncEntryIndex(oldBits, out int syncIndex))
                        {
                            return syncIndex;
                        }

                        // has hashcode -> slow path
                        if ((oldBits & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX) != 0)
                        {
                            return SyncTable.AssignEntry(obj, pHeader);
                        }

                        // we own the lock already
                        if ((oldBits & SBLK_MASK_LOCK_THREADID) == currentThreadID)
                        {
                            // try incrementing recursion level, check for overflow
                            int newBits = oldBits + SBLK_LOCK_RECLEVEL_INC;
                            if ((newBits & SBLK_MASK_LOCK_RECLEVEL) != 0)
                            {
                                if (Interlocked.CompareExchange(ref *pHeader, newBits, oldBits) == oldBits)
                                {
                                    return -1;
                                }

                                // rare contention on owned lock,
                                // perhaps hashcode was installed or finalization bits were touched.
                                // we still own the lock though and may be able to increment, try again
                                continue;
                            }
                            else
                            {
                                // overflow, transition to a fat Lock
                                return SyncTable.AssignEntry(obj, pHeader);
                            }
                        }

                        // someone else owns.
                        break;
                    }
                }

                if (retries != 0)
                {
                    // spin a bit before retrying (1 spinwait is roughly 35 nsec)
                    // the object is not pinned here
                    Thread.SpinWait(i);
                }
            }

            // owned by somebody else
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Release(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            int currentThreadID = (int)Lock.ThreadId.Current_NoInitialize.Id;
            // transform uninitialized ID into -1, so it will not match any possible lock owner
            currentThreadID |= (currentThreadID - 1) >> 31;

            Lock fatLock;
            fixed (byte* pObjectData = &obj.GetRawData())
            {
                int* pHeader = GetHeaderPtr(pObjectData);
                while (true)
                {
                    int oldBits = *pHeader;

                    // if we own the lock
                    if ((oldBits & SBLK_MASK_LOCK_THREADID) == currentThreadID &&
                        (oldBits & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX) == 0)
                    {
                        // decrement count or release entirely.
                        int newBits = (oldBits & SBLK_MASK_LOCK_RECLEVEL) != 0 ?
                            oldBits - SBLK_LOCK_RECLEVEL_INC :
                            oldBits & ~SBLK_MASK_LOCK_THREADID;

                        if (Interlocked.CompareExchange(ref *pHeader, newBits, oldBits) == oldBits)
                        {
                            return;
                        }

                        // rare contention on owned lock,
                        // we still own the lock, try again
                        continue;
                    }

                    if (!GetSyncEntryIndex(oldBits, out int syncIndex))
                    {
                        // someone else owns or noone.
                        throw new SynchronizationLockException();
                    }

                    fatLock = SyncTable.GetLockObject(syncIndex);
                    break;
                }
            }

            fatLock.Exit(currentThreadID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsAcquired(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            int currentThreadID = (int)Lock.ThreadId.Current_NoInitialize.Id;
            // transform uninitialized ID into -1, so it will not match any possible lock owner
            currentThreadID |= (currentThreadID - 1) >> 31;

            fixed (byte* pObjectData = &obj.GetRawData())
            {
                int* pHeader = GetHeaderPtr(pObjectData);
                int oldBits = *pHeader;

                // if we own the lock
                if ((oldBits & SBLK_MASK_LOCK_THREADID) == currentThreadID &&
                   (oldBits & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX) == 0)
                {
                    return true;
                }

                if (GetSyncEntryIndex(oldBits, out int syncIndex))
                {
                    return SyncTable.GetLockObject(syncIndex).GetIsHeldByCurrentThread(currentThreadID);
                }

                // someone else owns or noone.
                return false;
            }
        }
    }
}
