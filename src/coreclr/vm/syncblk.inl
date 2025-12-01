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

    // Use the slow path instead of spinning. The compare-exchange above would not fail often, and it's not worth forcing the
    // spin loop that typically follows the call to this function to check the recursive case, so just bail to the slow path.
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

#endif // _SYNCBLK_INL_
