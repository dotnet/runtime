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
        private const int SBLK_RECLEVEL_SHIFT = 16;               // shift right this much to get recursion level

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int* GetHeaderPtr(MethodTable** ppMethodTable)
        {
            // The header is 4 bytes before m_pEEType field on all architectures
            return (int*)ppMethodTable - 1;
        }

        /// <summary>
        /// Returns the hash code assigned to the object.  If no hash code has yet been assigned,
        /// it assigns one in a thread-safe way.
        /// </summary>
        public static unsafe int GetHashCode(object o)
        {
            if (o == null)
                return 0;

            fixed (MethodTable** ppMethodTable = &o.GetMethodTableRef())
            {
                int* pHeader = GetHeaderPtr(ppMethodTable);
                int bits = *pHeader;
                int hashOrIndex = bits & MASK_HASHCODE_INDEX;
                if ((bits & BIT_SBLK_IS_HASHCODE) != 0)
                {
                    // Found the hash code in the header
                    Debug.Assert(hashOrIndex != 0);
                    return hashOrIndex;
                }

                if ((bits & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX) != 0)
                {
                    // Look up the hash code in the SyncTable
                    int hashCode = SyncTable.GetHashCode(hashOrIndex);
                    if (hashCode != 0)
                    {
                        return hashCode;
                    }
                }

                // The hash code has not yet been set.  Assign some value.
                return AssignHashCode(o, pHeader);
            }
        }

        /// <summary>
        /// If a hash code has been assigned to the object, it is returned. Otherwise zero is
        /// returned.
        /// </summary>
        public static unsafe int TryGetHashCode(object o)
        {
            if (o == null)
                return 0;

            fixed (MethodTable** ppMethodTable = &o.GetMethodTableRef())
            {
                int* pHeader = GetHeaderPtr(ppMethodTable);
                int bits = *pHeader;
                int hashOrIndex = bits & MASK_HASHCODE_INDEX;
                if ((bits & BIT_SBLK_IS_HASHCODE) != 0)
                {
                    // Found the hash code in the header
                    Debug.Assert(hashOrIndex != 0);
                    return hashOrIndex;
                }

                if ((bits & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX) != 0)
                {
                    // Look up the hash code in the SyncTable
                    return SyncTable.GetHashCode(hashOrIndex);
                }

                // The hash code has not yet been set.
                return 0;
            }
        }

        /// <summary>
        /// Assigns a hash code to the object in a thread-safe way.
        /// </summary>
        private static unsafe int AssignHashCode(object o, int* pHeader)
        {
            int newHash = RuntimeHelpers.GetNewHashCode() & MASK_HASHCODE_INDEX;
            // Never use the zero hash code.  SyncTable treats the zero value as "not assigned".
            if (newHash == 0)
            {
                newHash = 1;
            }

            while (true)
            {
                int oldBits = *pHeader;

                // if have hashcode, just return it
                if ((oldBits & BIT_SBLK_IS_HASHCODE) != 0)
                {
                    // Found the hash code in the header
                    int h = oldBits & MASK_HASHCODE_INDEX;
                    Debug.Assert(h != 0);
                    return h;
                }

                // if have something else, break, we need a syncblock.
                if ((oldBits & MASK_HASHCODE_INDEX) != 0)
                {
                    break;
                }

                // there is nothing - try set hashcode inline
                Debug.Assert((oldBits & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX) == 0);
                int newBits = BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | BIT_SBLK_IS_HASHCODE | oldBits | newHash;
                if (Interlocked.CompareExchange(pHeader, newBits, oldBits) == oldBits)
                {
                    return newHash;
                }

                // contention, try again
            }

            if (!GetSyncEntryIndex(*pHeader, out int syncIndex))
            {
                // Assign a new sync entry
                syncIndex = SyncTable.AssignEntry(o, pHeader);
            }

            // Set the hash code in SyncTable. This call will resolve the potential race.
            return SyncTable.SetHashCode(syncIndex, newHash);
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
            fixed (MethodTable** ppMethodTable = &o.GetMethodTableRef())
            {
                int* pHeader = GetHeaderPtr(ppMethodTable);
                if (GetSyncEntryIndex(*pHeader, out int syncIndex))
                {
                    return syncIndex;
                }

                // Assign a new sync entry
                return SyncTable.AssignEntry(o, pHeader);
            }
        }

        /// <summary>
        /// Sets the sync entry index in a thread-safe way.
        /// </summary>
        public static unsafe void SetSyncEntryIndex(int* pHeader, int syncIndex)
        {
            // Holding this lock implies there is at most one thread setting the sync entry index at
            // any given time.  We also require that the sync entry index has not been already set.
            Debug.Assert(SyncTable.s_lock.IsHeldByCurrentThread);
            int oldBits, newBits;

            do
            {
                oldBits = *pHeader;
                // we should not have a sync index yet.
                Debug.Assert(!HasSyncEntryIndex(oldBits));

                if ((oldBits & BIT_SBLK_IS_HASHCODE) != 0)
                {
                    // set the hash code in the sync entry
                    SyncTable.MoveHashCodeToNewEntry(syncIndex, oldBits & MASK_HASHCODE_INDEX);
                    // reset the lock info, in case we have set it in the previous iteration
                    SyncTable.MoveThinLockToNewEntry(syncIndex, 0, 0);
                }
                else
                {
                    // set the lock info
                    SyncTable.MoveThinLockToNewEntry(
                        syncIndex,
                        oldBits & SBLK_MASK_LOCK_THREADID,
                        (uint)((oldBits & SBLK_MASK_LOCK_RECLEVEL) >> SBLK_RECLEVEL_SHIFT));
                }

                // Store the sync entry index
                newBits = oldBits & ~(BIT_SBLK_IS_HASHCODE | MASK_HASHCODE_INDEX);
                newBits |= syncIndex | BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX;
            }
            while (Interlocked.CompareExchange(pHeader, newBits, oldBits) != oldBits);
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
        // The common cases (free lock, fat lock) are handled inline. A thread id that doesn't fit,
        // recursive acquisition, contention and lost races are rarer and less performance sensitive,
        // so they are handled out of line in AcquireUncommon to keep this inlined fast path small.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool TryAcquireThinLock(object obj, int millisecondsTimeout = 0)
        {
            ArgumentNullException.ThrowIfNull(obj);

            // for an object used in locking there are two common cases:
            // - header bits are unused or
            // - there is a sync entry
            int oldBits;
            fixed (MethodTable** ppMethodTable = &obj.GetMethodTableRef())
            {
                int* pHeader = GetHeaderPtr(ppMethodTable);
                oldBits = *pHeader;

                // Common case: the header is clean. Take it by storing our thread id with a single CAS.
                if (oldBits == 0)
                {
                    // Thread IDs are allocated sequentially starting from 1 and recycled, so it's
                    // unusual to have a thread ID that doesn't fit in the thin-lock field.
                    // Check here rather than at entry to keep the hot path as tight as possible.
                    // The uninitialized 0 id is also ruled out by this check.
                    int currentThreadID = ManagedThreadId.CurrentManagedThreadIdUnchecked;
                    if ((uint)(currentThreadID - 1) < (uint)SBLK_MASK_LOCK_THREADID)
                    {
                        if (Interlocked.CompareExchange(pHeader, currentThreadID, oldBits) == oldBits)
                        {
                            return true;
                        }
                    }
                }
            }

            // Before checking uncommon cases, check if the lock is fat (or becoming fat).
            // This is another common case.
            int resultOrIndex = 0;
            if ((oldBits & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX) == 0)
            {
                resultOrIndex = TryAcquireUncommon(obj, millisecondsTimeout == 0);

                // If we acquired (or recursively re-acquired) the thin lock, we are done,
                // regardless of the timeout.
                if (resultOrIndex == -1)
                {
                    return true;
                }

                // With no timeout, a Failure (lock owned by someone else) is definitive.
                if (resultOrIndex == 0 && millisecondsTimeout == 0)
                {
                    return false;
                }
            }

            Lock lck = resultOrIndex == 0 ?
                ObjectHeader.GetLockObject(obj) :
                SyncTable.GetLockObject(resultOrIndex);

            return lck.TryEnter_Outlined(millisecondsTimeout);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void AcquireThinLock(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            // for an object used in locking there are two common cases:
            // - header bits are unused or
            // - there is a sync entry
            int oldBits;
            fixed (MethodTable** ppMethodTable = &obj.GetMethodTableRef())
            {
                int* pHeader = GetHeaderPtr(ppMethodTable);
                oldBits = *pHeader;

                // Common case: the header is clean. Take it by storing our thread id with a single CAS.
                if (oldBits == 0)
                {
                    // Thread IDs are allocated sequentially starting from 1 and recycled, so it's
                    // unusual to have a thread ID that doesn't fit in the thin-lock field.
                    // Check here rather than at entry to keep the hot path as tight as possible.
                    // The uninitialized 0 id is also ruled out by this check.
                    int currentThreadID = ManagedThreadId.CurrentManagedThreadIdUnchecked;
                    if ((uint)(currentThreadID - 1) < (uint)SBLK_MASK_LOCK_THREADID)
                    {
                        if (Interlocked.CompareExchange(pHeader, currentThreadID, oldBits) == oldBits)
                        {
                            return;
                        }
                    }
                }
            }

            // Before checking uncommon cases, check if the lock is fat (or becoming fat).
            // This is another common case.
            int resultOrIndex = 0;
            if ((oldBits & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX) != 0 ||
                (resultOrIndex = TryAcquireUncommon(obj, false)) != -1)
            {
                Lock lck = resultOrIndex == 0 ?
                    ObjectHeader.GetLockObject(obj) :
                    SyncTable.GetLockObject(resultOrIndex);

                lck.TryEnter_Outlined(Timeout.Infinite);
            }
        }

        // handling uncommon cases here - recursive lock, contention, retries
        // -1 - success
        // 0 - failed
        // syncIndex - retry with the Lock
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe int TryAcquireUncommon(object obj, bool isOneShot)
        {
            // A one-shot acquire does not spin while the lock is owned by another thread, but it still
            // retries the rare CAS failures below where the lock is not owned by somebody else: a caller
            // that knows the lock is unowned expects a one-shot acquire to succeed.
            // Lock.IsSingleProcessor is lazily initialized at the first contended acquire; until then it
            // is false and we assume a multicore machine.
            int retries = isOneShot || Lock.IsSingleProcessor ? 0 : 16;

            int currentThreadID = ManagedThreadId.Current;
            // A thin lock can only store a thread id that fits in the thread-id field.
            // This check is deferred to here (rather than at entry) because it is unusual to be true.
            if ((uint)currentThreadID > (uint)SBLK_MASK_LOCK_THREADID)
                return GetSyncIndex(obj);

            // retry when the lock is owned by somebody else.
            // this loop will spinwait between iterations.
            for (int i = 0; i <= retries; i++)
            {
                fixed (MethodTable** ppMethodTable = &obj.GetMethodTableRef())
                {
                    int* pHeader = GetHeaderPtr(ppMethodTable);

                    // rare retries when lock is not owned by somebody else.
                    // these do not count as iterations and do not spinwait.
                    while (true)
                    {
                        int oldBits = *pHeader;

                        // If has a hash code or syncblock,
                        // we cannot use a thin-lock.
                        if ((oldBits & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX) != 0)
                        {
                            return SyncTable.AssignEntry(obj, pHeader);
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
                                    return -1;
                                }

                                // rare contention on owned lock,
                                // perhaps hashcode was installed or finalization bits were touched.
                                // we still own the lock though and may be able to increment, try again
                                continue;
                            }
                            else
                            {
                                // overflow, need to transition to a fat Lock
                                return SyncTable.AssignEntry(obj, pHeader);
                            }
                        }
                        // If no one owns the lock, try acquiring it.
                        else if ((oldBits & SBLK_MASK_LOCK_THREADID) == 0)
                        {
                            int newBits = oldBits | currentThreadID;
                            if (Interlocked.CompareExchange(pHeader, newBits, oldBits) == oldBits)
                            {
                                return -1;
                            }

                            // Someone else touched the bits. Try again.
                            continue;
                        }
                        else
                        {
                            // Owned by somebody else. Now we spinwait and retry.
                            break;
                        }
                    }
                }

                // lock is thin, but owned by somebody else.
                // spin a bit before retrying (1 spinwait is roughly 35 nsec)
                // the object is not pinned here
                Thread.SpinWaitInternal(i);
            }

            // the lock is thin, but owned by somebody else
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Release(object obj)
        {
            Debug.Assert(obj != null);

            Lock fatLock;
            fixed (MethodTable** ppMethodTable = &obj.GetMethodTableRef())
            {
                int* pHeader = GetHeaderPtr(ppMethodTable);

                // We may need to retry if we own the lock but the CAS races with another thread
                // touching unrelated header bits.
                while (true)
                {
                    int oldBits = *pHeader;
                    // is the lock thin?
                    if ((oldBits & (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX)) == 0)
                    {
                        int currentThreadID = ManagedThreadId.CurrentManagedThreadIdUnchecked;
                        // transform uninitialized ID into -1, so it will not match any possible lock owner
                        currentThreadID |= (currentThreadID - 1) >> 31;

                        // do we own the thin lock?
                        if ((oldBits & SBLK_MASK_LOCK_THREADID) == currentThreadID)
                        {
                            // decrement count or release entirely.
                            int newBits = (oldBits & SBLK_MASK_LOCK_RECLEVEL) != 0 ?
                                oldBits - SBLK_LOCK_RECLEVEL_INC :
                                oldBits & ~SBLK_MASK_LOCK_THREADID;

                            if (Interlocked.CompareExchange(pHeader, newBits, oldBits) == oldBits)
                            {
                                return;
                            }

                            // rare contention on owned lock,
                            // we still own the lock, try again
                            continue;
                        }
                    }

                    if (!GetSyncEntryIndex(oldBits, out int syncIndex))
                    {
                        // someone else owns or noone.
                        throw new SynchronizationLockException();
                    }

                    // Get the fat lock. Must be done while still pinning the obj.
                    fatLock = SyncTable.GetLockObject(syncIndex);
                    break;
                }
            }

            fatLock.Exit();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsAcquired(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            int currentThreadID = ManagedThreadId.CurrentManagedThreadIdUnchecked;
            // transform uninitialized ID into -1, so it will not match any possible lock owner
            currentThreadID |= (currentThreadID - 1) >> 31;

            fixed (MethodTable** ppMethodTable = &obj.GetMethodTableRef())
            {
                int* pHeader = GetHeaderPtr(ppMethodTable);
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
