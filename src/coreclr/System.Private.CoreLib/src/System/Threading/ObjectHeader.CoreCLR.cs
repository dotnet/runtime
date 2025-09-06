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

        // This lock is only taken when we need to modify the index value in m_SyncBlockValue.
        // It should not be taken if the object already has a real syncblock index.
        // In this managed side, we skip the fast path while this spinlock is in use.
        // We'll sync up in the slow path.
        private const int BIT_SBLK_SPIN_LOCK = 0x10000000;

        // if BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX is clear, the rest of the header dword is laid out as follows:
        // - lower sixteen bits (bits 0 thru 15) is thread id used for the thin locks
        //   value is zero if no thread is holding the lock
        // - following six bits (bits 16 thru 21) is recursion level used for the thin locks
        //   value is zero if lock is not taken or only taken once by the same thread
        private const int SBLK_MASK_LOCK_THREADID = 0x0000FFFF;   // special value of 0 + 65535 thread ids
        private const int SBLK_MASK_LOCK_RECLEVEL = 0x003F0000;   // 64 recursion levels
        private const int SBLK_LOCK_RECLEVEL_INC = 0x00010000;    // each level is this much higher than the previous one

        // These must match the values in syncblk.h
        public enum AcquireHeaderResult
        {
            Success = 0,
            Contention = 1,
            UseSlowPath = 2
        };

        // These must match the values in syncblk.h
        public enum ReleaseHeaderResult
        {
            Success = 0,
            UseSlowPath = 1,
            Error = 2
        };

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern AcquireHeaderResult AcquireInternal(object obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern ReleaseHeaderResult Release(object obj);

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

        // Try acquiring the thin-lock
        public static unsafe AcquireHeaderResult TryAcquireThinLock(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            AcquireHeaderResult result = AcquireInternal(obj);
            if (result == AcquireHeaderResult.Contention)
            {
                return TryAcquireThinLockSpin(obj);
            }
            return result;
        }

        private static unsafe AcquireHeaderResult TryAcquireThinLockSpin(object obj)
        {
            int currentThreadID = (int)Lock.ThreadId.Current_NoInitialize.Id;

            // does thread ID fit?
            if (currentThreadID > SBLK_MASK_LOCK_THREADID)
                return AcquireHeaderResult.UseSlowPath;

            int retries = Lock.IsSingleProcessor ? 0 : 16;

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

                        // If has a hash code, syncblock, or is in the process of upgrading,
                        // we cannot use a thin-lock.
                        if ((oldBits & (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | BIT_SBLK_SPIN_LOCK)) != 0)
                        {
                            // Need to use a thick-lock.
                            return AcquireHeaderResult.UseSlowPath;
                        }
                        // If we already own the lock, try incrementing recursion level.
                        else if ((oldBits & SBLK_MASK_LOCK_THREADID) == currentThreadID)
                        {
                            // try incrementing recursion level, check for overflow
                            int newBits = oldBits + SBLK_LOCK_RECLEVEL_INC;
                            if ((newBits & SBLK_MASK_LOCK_RECLEVEL) != 0)
                            {
                                if (Interlocked.CompareExchange(ref *pHeader, newBits, oldBits) == oldBits)
                                {
                                    return AcquireHeaderResult.Success;
                                }

                                // rare contention on owned lock,
                                // perhaps hashcode was installed or finalization bits were touched.
                                // we still own the lock though and may be able to increment, try again
                                continue;
                            }
                            else
                            {
                                // overflow, need to transition to a fat Lock
                                return AcquireHeaderResult.UseSlowPath;
                            }
                        }
                        // If no one owns the lock, try acquiring it.
                        else if ((oldBits & SBLK_MASK_LOCK_THREADID) == 0)
                        {
                            int newBits = oldBits | currentThreadID;
                            if (Interlocked.CompareExchange(ref *pHeader, newBits, oldBits) == oldBits)
                            {
                                return AcquireHeaderResult.Success;
                            }

                            // rare contention on lock.
                            // Try again in case the finalization bits were touched.
                            continue;
                        }
                        else
                        {
                            // Owned by somebody else. Now we spinwait and retry.
                            break;
                        }
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
            return AcquireHeaderResult.Contention;
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

                // Ignore the spinlock here.
                // Either we'll read the thin-lock data in the header or read from the syncblock.
                // In either case, the two will be consistent.
                int oldBits = *pHeader;

                // if we own the lock
                if ((oldBits & SBLK_MASK_LOCK_THREADID) == currentThreadID &&
                   (oldBits & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX) == 0)
                {
                    return true;
                }

                if (HasSyncEntryIndex(oldBits))
                {
                    return Monitor.GetLockObject(obj).IsHeldByCurrentThread;
                }

                // someone else owns or noone.
                return false;
            }
        }
    }
}
