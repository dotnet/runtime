// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _SYNCBLK_INL_
#define _SYNCBLK_INL_

FORCEINLINE ObjHeader::HeaderLockResult ObjHeader::AcquireHeaderThinLock(DWORD tid)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    LONG oldValue = m_SyncBlockValue.LoadWithoutBarrier();

    if ((oldValue & (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX +
                     BIT_SBLK_SPIN_LOCK +
                     SBLK_MASK_LOCK_THREADID +
                     SBLK_MASK_LOCK_RECLEVEL)) == 0)
    {
        if (tid > SBLK_MASK_LOCK_THREADID)
        {
            return HeaderLockResult::UseSlowPath;
        }

        LONG newValue = oldValue | tid;
#if defined(TARGET_WINDOWS) && defined(TARGET_ARM64)
        if (FastInterlockedCompareExchangeAcquire((LONG*)&m_SyncBlockValue, newValue, oldValue) == oldValue)
#else
        if (InterlockedCompareExchangeAcquire((LONG*)&m_SyncBlockValue, newValue, oldValue) == oldValue)
#endif
        {
            return HeaderLockResult::Success;
        }

        return HeaderLockResult::Failure;
    }

    if (oldValue & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX)
    {
        return HeaderLockResult::UseSlowPath;
    }

    // The header is transitioning - use the slow path
    if (oldValue & BIT_SBLK_SPIN_LOCK)
    {
        return HeaderLockResult::UseSlowPath;
    }

    // Here we know we have the "thin lock" layout, but the lock is not free.
    // It could still be the recursion case - compare the thread id to check
    if (tid != (DWORD)(oldValue & SBLK_MASK_LOCK_THREADID))
    {
        return HeaderLockResult::Failure;
    }

    // Ok, the thread id matches, it's the recursion case.
    // Bump up the recursion level and check for overflow
    LONG newValue = oldValue + SBLK_LOCK_RECLEVEL_INC;

    if ((newValue & SBLK_MASK_LOCK_RECLEVEL) == 0)
    {
        return HeaderLockResult::UseSlowPath;
    }

#if defined(TARGET_WINDOWS) && defined(TARGET_ARM64)
    if (FastInterlockedCompareExchangeAcquire((LONG*)&m_SyncBlockValue, newValue, oldValue) == oldValue)
#else
    if (InterlockedCompareExchangeAcquire((LONG*)&m_SyncBlockValue, newValue, oldValue) == oldValue)
#endif
    {
        return HeaderLockResult::Success;
    }

    // We failed to acquire the lock in one shot.
    // If we only have one processor, don't waste time spinning.
    if (g_SystemInfo.dwNumberOfProcessors == 1)
    {
        return HeaderLockResult::UseSlowPath;
    }

    return AcquireHeaderThinLockWithSpin(tid);
}

inline ObjHeader::HeaderLockResult ObjHeader::AcquireHeaderThinLockWithSpin(DWORD tid)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(tid <= SBLK_MASK_LOCK_THREADID);
    } CONTRACTL_END;

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
    // Exponential back-off with a limit:
    //   0, 1, 2, 4, 8, 8, 8, 8, 8, 8, 8, . . . .
    //
    // Linear back-off
    //   0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, . . . .
    //
    // In this case these strategies are close in terms of average and worst case latency, so we will prefer linear
    // back-off as it favors micro-contention scenario, which we expect.
    //

    for (int i = 0; i < 16; ++i)
    {
        LONG oldValue = m_SyncBlockValue.LoadWithoutBarrier();

        if ((oldValue & (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX +
                        BIT_SBLK_SPIN_LOCK +
                        SBLK_MASK_LOCK_THREADID +
                        SBLK_MASK_LOCK_RECLEVEL)) == 0)
        {
            LONG newValue = oldValue | tid;
#if defined(TARGET_WINDOWS) && defined(TARGET_ARM64)
            if (FastInterlockedCompareExchangeAcquire((LONG*)&m_SyncBlockValue, newValue, oldValue) == oldValue)
#else
            if (InterlockedCompareExchangeAcquire((LONG*)&m_SyncBlockValue, newValue, oldValue) == oldValue)
#endif
            {
                return HeaderLockResult::Success;
            }

            // Someone else just beat us to the lock.
            // Try again.
            YieldProcessorNormalized(i);
            continue;
        }

        if (oldValue & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX)
        {
            return HeaderLockResult::UseSlowPath;
        }

        // The header is transitioning - use the slow path
        if (oldValue & BIT_SBLK_SPIN_LOCK)
        {
            return HeaderLockResult::UseSlowPath;
        }

        // Here we know we have the "thin lock" layout, but the lock is not free.
        // It could still be the recursion case - compare the thread id to check
        if (tid != (DWORD)(oldValue & SBLK_MASK_LOCK_THREADID))
        {
            // Someone else owns the lock.
            // Try again.
            YieldProcessorNormalized(i);
            continue;
        }

        // Ok, the thread id matches, it's the recursion case.
        // Bump up the recursion level and check for overflow
        LONG newValue = oldValue + SBLK_LOCK_RECLEVEL_INC;

        if ((newValue & SBLK_MASK_LOCK_RECLEVEL) == 0)
        {
            return HeaderLockResult::UseSlowPath;
        }

#if defined(TARGET_WINDOWS) && defined(TARGET_ARM64)
        if (FastInterlockedCompareExchangeAcquire((LONG*)&m_SyncBlockValue, newValue, oldValue) == oldValue)
#else
        if (InterlockedCompareExchangeAcquire((LONG*)&m_SyncBlockValue, newValue, oldValue) == oldValue)
#endif
        {
            return HeaderLockResult::Success;
        }

        // Something touched one of the other bits in the header (like the finalizer bits).
        // Try again.
        YieldProcessorNormalized(i);
    }

    // We failed to acquire the lock after spinning.
    // Use the slow path to wait.
    return HeaderLockResult::UseSlowPath;
}

// Helper encapsulating the core logic for releasing monitor. Returns what kind of
// follow up action is necessary. This is FORCEINLINE to make it provide a very efficient implementation.
FORCEINLINE ObjHeader::HeaderLockResult ObjHeader::ReleaseHeaderThinLock(DWORD tid)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    DWORD syncBlockValue = m_SyncBlockValue.LoadWithoutBarrier();

    if ((syncBlockValue & (BIT_SBLK_SPIN_LOCK + BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX)) == 0)
    {
        if (tid > SBLK_MASK_LOCK_THREADID)
        {
            return HeaderLockResult::UseSlowPath;
        }

        if ((syncBlockValue & SBLK_MASK_LOCK_THREADID) != tid)
        {
            // This thread does not own the lock.
            return HeaderLockResult::Failure;
        }

        if (!(syncBlockValue & SBLK_MASK_LOCK_RECLEVEL))
        {
            // We are leaving the lock
            DWORD newValue = (syncBlockValue & (~SBLK_MASK_LOCK_THREADID));

#if defined(TARGET_WINDOWS) && defined(TARGET_ARM64)
            if (FastInterlockedCompareExchangeRelease((LONG*)&m_SyncBlockValue, newValue, syncBlockValue) == (LONG)syncBlockValue)
#else
            if (InterlockedCompareExchangeRelease((LONG*)&m_SyncBlockValue, newValue, syncBlockValue) == (LONG)syncBlockValue)
#endif
            {
                return HeaderLockResult::Success;
            }

            return HeaderLockResult::UseSlowPath;
        }
        else
        {
            // recursion and ThinLock
            DWORD newValue = syncBlockValue - SBLK_LOCK_RECLEVEL_INC;
#if defined(TARGET_WINDOWS) && defined(TARGET_ARM64)
            if (FastInterlockedCompareExchangeRelease((LONG*)&m_SyncBlockValue, newValue, syncBlockValue) == (LONG)syncBlockValue)
#else
            if (InterlockedCompareExchangeRelease((LONG*)&m_SyncBlockValue, newValue, syncBlockValue) == (LONG)syncBlockValue)
#endif
            {
                return HeaderLockResult::Success;
            }

            return HeaderLockResult::UseSlowPath;
        }
    }

    return HeaderLockResult::UseSlowPath;
}

FORCEINLINE ObjHeader::HeaderLockResult ObjHeader::IsHeaderThinLockOwnedByThread(DWORD tid)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    DWORD syncBlockValue = m_SyncBlockValue.LoadWithoutBarrier();

    // We ignore the header spinlock here.
    // Either we'll read the thin-lock data in the header or we'll have a sync block.
    // In either case, the two will be consistent.
    if ((syncBlockValue & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX) == 0)
    {
        if ((syncBlockValue & SBLK_MASK_LOCK_THREADID) != tid)
        {
            // This thread does not own the lock.
            return HeaderLockResult::Failure;
        }

        return HeaderLockResult::Success;
    }

    // If has a hash code or syncblock, we cannot determine the lock state from the header.
    return HeaderLockResult::UseSlowPath;
}

#endif // _SYNCBLK_INL_
