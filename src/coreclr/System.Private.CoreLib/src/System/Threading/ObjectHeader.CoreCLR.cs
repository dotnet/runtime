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

        public enum HeaderLockResult
        {
            Success = 0,
            Failure = 1,
            UseSlowPath = 2
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int* GetHeaderPtr(byte* ppObjectData)
        {
            // The header is the 4 bytes before a pointer-sized chunk before the object data pointer.
            return (int*)(ppObjectData - sizeof(void*) - sizeof(int));
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

        // Try acquiring the thin-lock.
        // The public entry point spins by default (e.g. a blocking Monitor.Enter). Callers that want a
        // single attempt (e.g. Monitor.TryEnter) pass isOneShot: true to succeed only if the lock is
        // currently unowned.
        public static HeaderLockResult AcquireThinLock(object obj, bool isOneShot = false)
        {
            ArgumentNullException.ThrowIfNull(obj);

            HeaderLockResult result = Acquire(obj);
            if (result == HeaderLockResult.Failure && !isOneShot)
            {
                return TryAcquireThinLockSpin(obj);
            }
            return result;
        }

        // Try acquiring the thin-lock in a single attempt.
        // The common cases (free lock, fat lock) are handled inline. Recursive acquisition and
        // contention are rarer and less performance sensitive, so they are handled out of line in
        // AcquireUncommon to keep this inlined fast path small.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe HeaderLockResult Acquire(object obj)
        {
            int currentThreadID = ManagedThreadId.Current;

            fixed (byte* pObjectData = &obj.GetRawData())
            {
                int* pHeader = GetHeaderPtr(pObjectData);
                int oldBits = *pHeader;

                // Common case: the header has no hashcode/syncblock index, is not spin-locked and the
                // lock is free (no owning thread id and no recursion level). Take it by storing our
                // thread id with a single CAS.
                if ((oldBits & (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | BIT_SBLK_SPIN_LOCK | SBLK_MASK_LOCK_THREADID | SBLK_MASK_LOCK_RECLEVEL)) == 0)
                {
                    // Thread IDs are allocated sequentially starting from 1 and recycled, so it's
                    // unusual to have a thread ID that doesn't fit in the thin-lock field.
                    // Check here rather than at entry to keep the hot path as tight as possible.
                    // The uninitialized 0 id is also ruled out by this check.
                    if ((uint)(currentThreadID - 1) >= (uint)SBLK_MASK_LOCK_THREADID)
                    {
                        return HeaderLockResult.UseSlowPath;
                    }

                    if (Interlocked.CompareExchange(pHeader, oldBits | currentThreadID, oldBits) == oldBits)
                    {
                        return HeaderLockResult.Success;
                    }
                }

                // Another common case: the header has a hashcode or syncblock index, or another thread
                // is upgrading it (spin lock). We cannot use a thin-lock.
                if ((oldBits & (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | BIT_SBLK_SPIN_LOCK)) != 0)
                {
                    return HeaderLockResult.UseSlowPath;
                }

                // Everything else - a recursive acquire, contention or a lost race - is uncommon and
                // handled out of line.
                return AcquireUncommon(pHeader, oldBits, currentThreadID);
            }
        }

        // Handles the uncommon thin-lock acquire cases: recursive acquisition and contention.
        // 'oldBits' is the header value that prevented the inline fast path in Acquire from
        // completing. Kept out of line so the common path stays small.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe HeaderLockResult AcquireUncommon(int* pHeader, int oldBits, int currentThreadID)
        {
            // Thread IDs that don't fit in the thin-lock field cannot use thin locks. This check is
            // deferred to here (rather than at entry) because it is unusual to be true.
            if ((uint)(currentThreadID - 1) >= (uint)SBLK_MASK_LOCK_THREADID)
            {
                return HeaderLockResult.UseSlowPath;
            }

            // We have the thin-lock layout but the lock is not free.
            // If we do not already own it, the lock is held by somebody else.
            if ((oldBits & SBLK_MASK_LOCK_THREADID) != currentThreadID)
            {
                return HeaderLockResult.Failure;
            }

            // We own the lock: this is a recursive acquire. Increment the recursion level and check
            // for overflow.
            int newBits = oldBits + SBLK_LOCK_RECLEVEL_INC;
            if ((newBits & SBLK_MASK_LOCK_RECLEVEL) == 0)
            {
                // overflow, need to transition to a fat Lock
                return HeaderLockResult.UseSlowPath;
            }

            if (Interlocked.CompareExchange(pHeader, newBits, oldBits) == oldBits)
            {
                return HeaderLockResult.Success;
            }

            // Someone else touched the header bits. Let the caller retry/spin or inflate.
            return HeaderLockResult.Failure;
        }

        private static unsafe HeaderLockResult TryAcquireThinLockSpin(object obj)
        {
            int currentThreadID = ManagedThreadId.Current;

            // A thin lock can only store a thread id that is initialized (non-zero) and fits in the
            // thread-id field. A single unsigned comparison rules out both the uninitialized 0 id and
            // ids that are too large; such threads must use the slow/fat path.
            if ((uint)(currentThreadID - 1) >= (uint)SBLK_MASK_LOCK_THREADID)
                return HeaderLockResult.UseSlowPath;

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
                            return HeaderLockResult.UseSlowPath;
                        }
                        // If we already own the lock, try incrementing recursion level.
                        else if ((oldBits & SBLK_MASK_LOCK_THREADID) == currentThreadID)
                        {
                            // try incrementing recursion level, check for overflow
                            int newBits = oldBits + SBLK_LOCK_RECLEVEL_INC;
                            if ((newBits & SBLK_MASK_LOCK_RECLEVEL) != 0)
                            {
                                if (Interlocked.CompareExchange(pHeader, newBits, oldBits) == oldBits)
                                {
                                    return HeaderLockResult.Success;
                                }

                                // rare contention on owned lock,
                                // perhaps hashcode was installed or finalization bits were touched.
                                // we still own the lock though and may be able to increment, try again
                                continue;
                            }
                            else
                            {
                                // overflow, need to transition to a fat Lock
                                return HeaderLockResult.UseSlowPath;
                            }
                        }
                        // If no one owns the lock, try acquiring it.
                        else if ((oldBits & SBLK_MASK_LOCK_THREADID) == 0)
                        {
                            int newBits = oldBits | currentThreadID;
                            if (Interlocked.CompareExchange(pHeader, newBits, oldBits) == oldBits)
                            {
                                return HeaderLockResult.Success;
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
            return HeaderLockResult.Failure;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe HeaderLockResult Release(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            int currentThreadID = ManagedThreadId.CurrentManagedThreadIdUnchecked;
            // transform uninitialized ID into -1, so it will not match any possible lock owner
            currentThreadID |= (currentThreadID - 1) >> 31;

            fixed (byte* pObjectData = &obj.GetRawData())
            {
                int* pHeader = GetHeaderPtr(pObjectData);

                // We may need to retry if we own the lock but the CAS races with another thread
                // touching unrelated header bits. GC should not set its bits while we are here, but
                // finalization might. We still own the lock in that case, so retrying lets us release
                // it without needlessly inflating it to a fat lock.
                while (true)
                {
                    int oldBits = *pHeader;

                    int newBits;

                    // Common case: the lock is thin, owned by us and held only once.
                    // A single masked comparison verifies in one shot that the spinlock bit is clear,
                    // there is no hashcode/syncblock index, the recursion level is 0 and we own the lock.
                    // The comparison is only meaningful when the current thread id fits in the thread-id
                    // field; ids that don't fit (including the uninitialized -1 sentinel) can never own a
                    // thin lock and are routed to the checks below. This guard is independent of the header
                    // load, so it can be evaluated while the header is fetched from memory.
                    if ((currentThreadID & ~SBLK_MASK_LOCK_THREADID) == 0 &&
                        (oldBits & (BIT_SBLK_SPIN_LOCK | BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | SBLK_MASK_LOCK_RECLEVEL | SBLK_MASK_LOCK_THREADID)) == currentThreadID)
                    {
                        // Release entirely. Since the recursion level is 0, clearing the owning
                        // thread id is the same as subtracting it from the header.
                        newBits = oldBits - currentThreadID;
                    }
                    // Otherwise, if the lock is still thin
                    else if ((oldBits & (BIT_SBLK_SPIN_LOCK | BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX)) == 0)
                    {
                        // If we own the lock it must be held recursively (the single-hold case is handled above),
                        // so just decrement the recursion level.
                        if ((oldBits & SBLK_MASK_LOCK_THREADID) == currentThreadID)
                        {
                            newBits = oldBits - SBLK_LOCK_RECLEVEL_INC;
                        }
                        else
                        {
                            return HeaderLockResult.Failure;
                        }
                    }
                    else
                    {
                        // Has a hashcode/syncblock index, or another thread is upgrading it (spin lock).
                        return HeaderLockResult.UseSlowPath;
                    }

                    if (Interlocked.CompareExchange(pHeader, newBits, oldBits) == oldBits)
                    {
                        return HeaderLockResult.Success;
                    }

                    // rare contention on owned lock,
                    // we still own the lock, try again
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe HeaderLockResult IsAcquired(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            fixed (byte* pObjectData = &obj.GetRawData())
            {
                int* pHeader = GetHeaderPtr(pObjectData);

                // Ignore the spinlock here.
                // Either we'll read the thin-lock data in the header or we'll have a sync block.
                // In either case, the two will be consistent.
                int oldBits = *pHeader;

                // If has a hash code or syncblock, we cannot determine the lock state from the header
                // use the slow path.
                if ((oldBits & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX) != 0)
                {
                    return HeaderLockResult.UseSlowPath;
                }

                // if we own the lock
                if ((oldBits & SBLK_MASK_LOCK_THREADID) == ManagedThreadId.Current)
                {
                    return HeaderLockResult.Success;
                }

                // someone else owns or no one.
                return HeaderLockResult.Failure;
            }
        }
    }
}
