// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    internal static partial class ObjectHeader
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
        private static unsafe int* GetHeaderPtr(nint* ppMethodTable)
        {
            // The header is 4 bytes before MT pointer on all architectures
            return (int*)ppMethodTable - 1;
        }

        internal static Lock GetLockObject(object obj)
        {
            IntPtr lockHandle = GetLockHandleIfExists(obj);
            if (lockHandle != 0)
            {
                Lock lockObj = GCHandle<Lock>.FromIntPtr(lockHandle).Target;
                GC.KeepAlive(obj);
                return lockObj;
            }

            return GetLockObjectFallback(obj);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static Lock GetLockObjectFallback(object obj)
            {
#pragma warning disable CS9216 // A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
                object lockObj = new Lock();
#pragma warning restore CS9216
                GetOrCreateLockObject(ObjectHandleOnStack.Create(ref obj), ObjectHandleOnStack.Create(ref lockObj));
                return (Lock)lockObj!;
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr GetLockHandleIfExists(object obj);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ObjectHeader_GetOrCreateLockObject")]
        private static partial void GetOrCreateLockObject(ObjectHandleOnStack obj, ObjectHandleOnStack lockObj);

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
        // so they are handled out of line in TryAcquireUncommon to keep this inlined fast path small.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool TryAcquireThinLock(object obj, int millisecondsTimeout = 0)
        {
            ArgumentNullException.ThrowIfNull(obj);

            // for an object used in locking there are two common cases:
            // - header bits are unused or
            // - there is a sync entry
            int oldBits;
            fixed (nint* ppMethodTable = &obj.GetMethodTableRef())
            {
                int* pHeader = GetHeaderPtr(ppMethodTable);
                oldBits = *pHeader;

                // Common case: the header is clean. Take it by storing our thread id with a single CAS.
                if (oldBits == 0)
                {
                    // Thread IDs are allocated sequentially starting from 1 and recycled, so it's
                    // unusual to have a thread ID that doesn't fit in the thin-lock field.
                    // Check here rather than at entry to keep the hot path as tight as possible.
                    int currentThreadID = ManagedThreadId.Current;
                    if ((uint)currentThreadID <= (uint)SBLK_MASK_LOCK_THREADID)
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
            if ((oldBits & (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | BIT_SBLK_SPIN_LOCK)) == 0)
            {
                HeaderLockResult result = TryAcquireUncommon(obj, millisecondsTimeout == 0);

                // If we acquired (or recursively re-acquired) the thin lock, we are done,
                // regardless of the timeout.
                if (result == HeaderLockResult.Success)
                {
                    return true;
                }

                // With no timeout, a Failure (lock owned by someone else) is definitive.
                if (result == HeaderLockResult.Failure && millisecondsTimeout == 0)
                {
                    return false;
                }
            }

            Lock lck = GetLockObject(obj);
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
            fixed (nint* ppMethodTable = &obj.GetMethodTableRef())
            {
                int* pHeader = GetHeaderPtr(ppMethodTable);
                oldBits = *pHeader;

                // Common case: the header is clean. Take it by storing our thread id with a single CAS.
                if (oldBits == 0)
                {
                    // Thread IDs are allocated sequentially starting from 1 and recycled, so it's
                    // unusual to have a thread ID that doesn't fit in the thin-lock field.
                    // Check here rather than at entry to keep the hot path as tight as possible.
                    // If the id doesn't fit, we fall through and call TryAcquireUncommon outside the
                    // fixed block to avoid keeping the object pinned while potentially spinning.
                    int currentThreadID = ManagedThreadId.Current;
                    if ((uint)currentThreadID <= (uint)SBLK_MASK_LOCK_THREADID)
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
            if ((oldBits & (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | BIT_SBLK_SPIN_LOCK)) != 0 ||
                TryAcquireUncommon(obj, false) != HeaderLockResult.Success)
            {
                Lock lck = GetLockObject(obj);
                lck.Enter();
            }
        }

        // handling uncommon cases here - recursive lock, contention, retries
        // The public entry point spins by default (e.g. a blocking Monitor.Enter). Callers that want a
        // single attempt (e.g. Monitor.TryEnter) pass isOneShot: true to succeed only if the lock is
        // currently unowned.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe HeaderLockResult TryAcquireUncommon(object obj, bool isOneShot)
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
                return HeaderLockResult.UseSlowPath;

            // retry when the lock is owned by somebody else.
            // this loop will spinwait between iterations.
            for (int i = 0; i <= retries; i++)
            {
                fixed (nint* ppMethodTable = &obj.GetMethodTableRef())
                {
                    int* pHeader = GetHeaderPtr(ppMethodTable);

                    // rare retries when lock is not owned by somebody else.
                    // these do not count as iterations and do not spinwait.
                    while (true)
                    {
                        int oldBits = *pHeader;

                        // If has a hash code, syncblock, or is in the process of upgrading,
                        // we cannot use a thin-lock.
                        if ((oldBits & (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | BIT_SBLK_SPIN_LOCK)) != 0)
                        {
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
                Thread.SpinWait(i);
            }

            // the lock is thin, but owned by somebody else
            return HeaderLockResult.Failure;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Release(object obj)
        {
            Debug.Assert(obj != null);

            fixed (nint* ppMethodTable = &obj.GetMethodTableRef())
            {
                int* pHeader = GetHeaderPtr(ppMethodTable);

                // We may need to retry if we own the lock but the CAS races with another thread
                // touching unrelated header bits.
                while (true)
                {
                    int oldBits = *pHeader;
                    // is the lock thin?
                    if ((oldBits & (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | BIT_SBLK_SPIN_LOCK)) == 0)
                    {
                        // In CoreCLR, the managed ID is set by the runtime, thus we do not
                        // call CurrentManagedThreadIdUnchecked like we do in NativeAOT
                        int currentThreadID = ManagedThreadId.Current;

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

                            // rare contention on owned thin lock,
                            // we still own the lock, try again
                            continue;
                        }
                    }

                    // do slow path.
                    break;
                }
            }

            // This is a case when we have:
            // * a fat lock - the most likely case by far, or
            // * we don't own the lock and need to throw and it is ok if the lock gets inflated.
            // Let the slow path handle this.
            GetLockObject(obj).Exit();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsAcquired(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            fixed (nint* ppMethodTable = &obj.GetMethodTableRef())
            {
                int* pHeader = GetHeaderPtr(ppMethodTable);

                // If the spin lock is set, the header may be transitioning to a sync block.
                // In that case, fall back to the slow path to determine ownership.
                int oldBits = *pHeader;

                // If no hash code or syncblock, the lock state is determined by the header.
                if ((oldBits & (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | BIT_SBLK_SPIN_LOCK)) == 0)
                {
                    // do we own the thin lock?
                    return (oldBits & SBLK_MASK_LOCK_THREADID) == ManagedThreadId.Current;
                }
            }

            // Has a hash code or syncblock - let the slow path determine ownership.
            // Done outside the fixed block to avoid pinning the object across the call.
            return GetLockObject(obj).IsHeldByCurrentThread;
        }
    }
}
