// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef _SYNCBLK_INL_
#define _SYNCBLK_INL_

#ifndef DACCESS_COMPILE

FORCEINLINE bool AwareLock::SpinWaitAndBackOffBeforeOperation(DWORD *spinCountRef)
{
    CONTRACTL{
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(spinCountRef != nullptr);
    DWORD &spinCount = *spinCountRef;
    _ASSERTE(g_SystemInfo.dwNumberOfProcessors != 1);

    if (spinCount > g_SpinConstants.dwMaximumDuration)
    {
        return false;
    }

    for (DWORD i = 0; i < spinCount; i++)
    {
        YieldProcessor();
    }

    spinCount *= g_SpinConstants.dwBackoffFactor;
    return true;
}

FORCEINLINE bool AwareLock::EnterHelper(Thread* pCurThread, bool checkRecursiveCase)
{
    CONTRACTL{
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    LONG state = m_MonitorHeld.LoadWithoutBarrier();
    if (state == 0)
    {
        if (InterlockedCompareExchangeAcquire((LONG*)&m_MonitorHeld, 1, 0) == 0)
        {
            m_HoldingThread = pCurThread;
            m_Recursion = 1;
            pCurThread->IncLockCount();
            return true;
        }
    }
    else if (checkRecursiveCase && GetOwningThread() == pCurThread) /* monitor is held, but it could be a recursive case */
    {
        m_Recursion++;
        return true;
    }
    return false;
}

FORCEINLINE AwareLock::EnterHelperResult ObjHeader::EnterObjMonitorHelper(Thread* pCurThread)
{
    CONTRACTL{
        SO_TOLERANT;
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
        DWORD tid = pCurThread->GetThreadId();
        if (tid > SBLK_MASK_LOCK_THREADID)
        {
            return AwareLock::EnterHelperResult_UseSlowPath;
        }

        LONG newValue = oldValue | tid;
        if (InterlockedCompareExchangeAcquire((LONG*)&m_SyncBlockValue, newValue, oldValue) == oldValue)
        {
            pCurThread->IncLockCount();
            return AwareLock::EnterHelperResult_Entered;
        }

        return AwareLock::EnterHelperResult_Contention;
    }

    if (oldValue & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX)
    {
        // If we have a hash code already, we need to create a sync block
        if (oldValue & BIT_SBLK_IS_HASHCODE)
        {
            return AwareLock::EnterHelperResult_UseSlowPath;
        }

        SyncBlock *syncBlock = g_pSyncTable[oldValue & MASK_SYNCBLOCKINDEX].m_SyncBlock;
        _ASSERTE(syncBlock != NULL);
        if (syncBlock->m_Monitor.EnterHelper(pCurThread, true /* checkRecursiveCase */))
        {
            return AwareLock::EnterHelperResult_Entered;
        }

        return AwareLock::EnterHelperResult_Contention;
    }

    // The header is transitioning - treat this as if the lock was taken
    if (oldValue & BIT_SBLK_SPIN_LOCK)
    {
        return AwareLock::EnterHelperResult_Contention;
    }

    // Here we know we have the "thin lock" layout, but the lock is not free.
    // It could still be the recursion case - compare the thread id to check
    if (pCurThread->GetThreadId() != (DWORD)(oldValue & SBLK_MASK_LOCK_THREADID))
    {
        return AwareLock::EnterHelperResult_Contention;
    }

    // Ok, the thread id matches, it's the recursion case.
    // Bump up the recursion level and check for overflow
    LONG newValue = oldValue + SBLK_LOCK_RECLEVEL_INC;

    if ((newValue & SBLK_MASK_LOCK_RECLEVEL) == 0)
    {
        return AwareLock::EnterHelperResult_UseSlowPath;
    }

    if (InterlockedCompareExchangeAcquire((LONG*)&m_SyncBlockValue, newValue, oldValue) == oldValue)
    {
        return AwareLock::EnterHelperResult_Entered;
    }

    // Use the slow path instead of spinning. The compare-exchange above would not fail often, and it's not worth forcing the
    // spin loop that typically follows the call to this function to check the recursive case, so just bail to the slow path.
    return AwareLock::EnterHelperResult_UseSlowPath;
}

// Helper encapsulating the core logic for releasing monitor. Returns what kind of 
// follow up action is necessary. This is FORCEINLINE to make it provide a very efficient implementation.
FORCEINLINE AwareLock::LeaveHelperAction AwareLock::LeaveHelper(Thread* pCurThread)
{
    CONTRACTL {
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    if (m_HoldingThread != pCurThread)
        return AwareLock::LeaveHelperAction_Error;

    _ASSERTE((size_t)m_MonitorHeld & 1);
    _ASSERTE(m_Recursion >= 1);

#if defined(_DEBUG) && defined(TRACK_SYNC) && !defined(CROSSGEN_COMPILE)
    // The best place to grab this is from the ECall frame
    Frame   *pFrame = pCurThread->GetFrame();
    int      caller = (pFrame && pFrame != FRAME_TOP ? (int) pFrame->GetReturnAddress() : -1);
    pCurThread->m_pTrackSync->LeaveSync(caller, this);
#endif

    if (--m_Recursion == 0)
    {
        m_HoldingThread->DecLockCount();
        m_HoldingThread = NULL;

        // Clear lock bit.
        LONG state = InterlockedDecrementRelease((LONG*)&m_MonitorHeld);

        // If wait count is non-zero on successful clear, we must signal the event.
        if (state & ~1)
        {
            return AwareLock::LeaveHelperAction_Signal;
        }
    }
    return AwareLock::LeaveHelperAction_None;
}

// Helper encapsulating the core logic for releasing monitor. Returns what kind of 
// follow up action is necessary. This is FORCEINLINE to make it provide a very efficient implementation.
FORCEINLINE AwareLock::LeaveHelperAction ObjHeader::LeaveObjMonitorHelper(Thread* pCurThread)
{
    CONTRACTL {
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    DWORD syncBlockValue = m_SyncBlockValue.LoadWithoutBarrier();

    if ((syncBlockValue & (BIT_SBLK_SPIN_LOCK + BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX)) == 0)
    {
        if ((syncBlockValue & SBLK_MASK_LOCK_THREADID) != pCurThread->GetThreadId())
        {
            // This thread does not own the lock.
            return AwareLock::LeaveHelperAction_Error;                
        }

        if (!(syncBlockValue & SBLK_MASK_LOCK_RECLEVEL))
        {
            // We are leaving the lock
            DWORD newValue = (syncBlockValue & (~SBLK_MASK_LOCK_THREADID));
            if (InterlockedCompareExchangeRelease((LONG*)&m_SyncBlockValue, newValue, syncBlockValue) != (LONG)syncBlockValue)
            {
                return AwareLock::LeaveHelperAction_Yield;
            }
            pCurThread->DecLockCount();
        }
        else
        {
            // recursion and ThinLock
            DWORD newValue = syncBlockValue - SBLK_LOCK_RECLEVEL_INC;
            if (InterlockedCompareExchangeRelease((LONG*)&m_SyncBlockValue, newValue, syncBlockValue) != (LONG)syncBlockValue)
            {
                return AwareLock::LeaveHelperAction_Yield;
            }
        }

        return AwareLock::LeaveHelperAction_None;
    }

    if ((syncBlockValue & (BIT_SBLK_SPIN_LOCK + BIT_SBLK_IS_HASHCODE)) == 0)
    {
        _ASSERTE((syncBlockValue & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX) != 0);
        SyncBlock *syncBlock = g_pSyncTable[syncBlockValue & MASK_SYNCBLOCKINDEX].m_SyncBlock;
        _ASSERTE(syncBlock != NULL);
        return syncBlock->m_Monitor.LeaveHelper(pCurThread);
    }

    if (syncBlockValue & BIT_SBLK_SPIN_LOCK)
    {
        return AwareLock::LeaveHelperAction_Contention;        
    }

    // This thread does not own the lock.
    return AwareLock::LeaveHelperAction_Error;
}

#endif // DACCESS_COMPILE

// Provide access to the object associated with this awarelock, so client can
// protect it.
inline OBJECTREF AwareLock::GetOwningObject()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    // gcc on mac needs these intermediate casts to avoid some ambiuous overloading in the DAC case
    PTR_SyncTableEntry table = SyncTableEntry::GetSyncTableEntry();
    return (OBJECTREF)(Object*)(PTR_Object)table[(m_dwSyncIndex & ~SyncBlock::SyncBlockPrecious)].m_Object;
}

#endif  // _SYNCBLK_INL_
