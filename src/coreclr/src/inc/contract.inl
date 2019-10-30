// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ---------------------------------------------------------------------------
// Contract.inl
//

// ! I am the owner for issues in the contract *infrastructure*, not for every 
// ! CONTRACT_VIOLATION dialog that comes up. If you interrupt my work for a routine
// ! CONTRACT_VIOLATION, you will become the new owner of this file.
// ---------------------------------------------------------------------------

#ifndef CONTRACT_INL_
#define CONTRACT_INL_

#include "contract.h"
#include <string.h>

#ifndef _countof
#define _countof(x) (sizeof(x)/sizeof(x[0]))
#endif

#ifdef ENABLE_CONTRACTS_IMPL

inline void BaseContract::DoChecks(UINT testmask, __in_z const char *szFunction, __in_z const char *szFile, int lineNum)
{
    STATIC_CONTRACT_DEBUG_ONLY;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    // Cache the pointer to our ClrDebugState if it's not already cached.
    // Derived types could set up this ptr before calling BaseContract::DoChecks if they have access to the Thread ptr
    if (m_pClrDebugState == NULL)
    {
        m_pClrDebugState = GetClrDebugState();
    }

    // Save the incoming contents for restoration in the destructor
    m_IncomingClrDebugState = *m_pClrDebugState;

    m_testmask = testmask;  // Save the testmask for destructor

    // Setup the new stack record.
    m_contractStackRecord.m_szFunction = szFunction;
    m_contractStackRecord.m_szFile     = szFile;
    m_contractStackRecord.m_lineNum    = lineNum;
    m_contractStackRecord.m_testmask   = testmask;
    m_contractStackRecord.m_construct  = "CONTRACT";

    // Link the new ContractStackRecord into the chain for this thread.
    m_pClrDebugState->LinkContractStackTrace( &m_contractStackRecord );

    if (testmask & DEBUG_ONLY_Yes)
    {
        m_pClrDebugState->SetDebugOnly();
    }

    switch (testmask & FAULT_Mask)
    {
        case FAULT_Forbid:
            m_pClrDebugState->ViolationMaskReset( FaultViolation|FaultNotFatal );
            m_pClrDebugState->SetFaultForbid();
            break;

        case FAULT_Inject:
            if (m_pClrDebugState->IsFaultForbid() &&
                !(m_pClrDebugState->ViolationMask() & (FaultViolation|FaultNotFatal|BadDebugState)))
            {
                CONTRACT_ASSERT("INJECT_FAULT called in a FAULTFORBID region.",
                                BaseContract::FAULT_Forbid,
                                BaseContract::FAULT_Mask,
                                m_contractStackRecord.m_szFunction,
                                m_contractStackRecord.m_szFile,
                                m_contractStackRecord.m_lineNum);
            }
            break;

        case FAULT_Disabled:
            // Nothing
            break;

        default:
            UNREACHABLE();
    }

    switch (testmask & THROWS_Mask)
    {
        case THROWS_Yes:
            m_pClrDebugState->CheckOkayToThrow(m_contractStackRecord.m_szFunction,
                                               m_contractStackRecord.m_szFile,
                                               m_contractStackRecord.m_lineNum);
            break;

        case THROWS_No:
            m_pClrDebugState->ViolationMaskReset( ThrowsViolation );
            m_pClrDebugState->ResetOkToThrow();
            break;

        case THROWS_Disabled:
            // Nothing
            break;

        default:
            UNREACHABLE();
    }

    // LOADS_TYPE check
    switch (testmask & LOADS_TYPE_Mask)
    {
        case LOADS_TYPE_Disabled:
            // Nothing
            break;

        default:
            {
                UINT newTypeLoadLevel = ((testmask & LOADS_TYPE_Mask) >> LOADS_TYPE_Shift) - 1;
                if (newTypeLoadLevel > m_pClrDebugState->GetMaxLoadTypeLevel())
                {
                    if (!((LoadsTypeViolation|BadDebugState) & m_pClrDebugState->ViolationMask()))
                    {
                        CONTRACT_ASSERT("A function tried to load a type past the current level limit.",
                                        (m_pClrDebugState->GetMaxLoadTypeLevel() + 1) << LOADS_TYPE_Shift,
                                        Contract::LOADS_TYPE_Mask,
                                        m_contractStackRecord.m_szFunction,
                                        m_contractStackRecord.m_szFile,
                                        m_contractStackRecord.m_lineNum
                                        );
                    }
                }
                m_pClrDebugState->SetMaxLoadTypeLevel(newTypeLoadLevel);
                m_pClrDebugState->ViolationMaskReset(LoadsTypeViolation);
                
            }
            break;
    }

    if (testmask & CAN_RETAKE_LOCK_No)
    {
        m_pClrDebugState->OnEnterCannotRetakeLockFunction();
        m_pClrDebugState->ResetOkToRetakeLock();
    }

    switch (testmask & CAN_TAKE_LOCK_Mask)
    {
        case CAN_TAKE_LOCK_Yes: 
            m_pClrDebugState->CheckOkayToLock(m_contractStackRecord.m_szFunction,
                                              m_contractStackRecord.m_szFile,
                                              m_contractStackRecord.m_lineNum);
            break;

        case CAN_TAKE_LOCK_No:
            m_pClrDebugState->ViolationMaskReset(TakesLockViolation);
            m_pClrDebugState->ResetOkToLock();
            break;

        case CAN_TAKE_LOCK_Disabled:
            // Nothing
            break;

        default:
            UNREACHABLE();
    }

}

FORCEINLINE BOOL BaseContract::CheckFaultInjection()
{
    // ??? use m_tag to see if we should trigger an injection
    return FALSE;
}

inline BOOL ClrDebugState::CheckOkayToThrowNoAssert()
{
    if (!IsOkToThrow() && !(m_violationmask & (ThrowsViolation|BadDebugState)))
    {
        return FALSE;
    }
    return TRUE;
}

inline void ClrDebugState::CheckOkayToThrow(__in_z const char *szFunction, __in_z const char *szFile, int lineNum)
{
    STATIC_CONTRACT_DEBUG_ONLY;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    if (!CheckOkayToThrowNoAssert())
    {
        CONTRACT_ASSERT("THROWS called in a NOTHROW region.",
                        BaseContract::THROWS_No,
                        BaseContract::THROWS_Mask,
                        szFunction,
                        szFile,
                        lineNum);
    }
}

inline BOOL ClrDebugState::CheckOkayToLockNoAssert()
{
    STATIC_CONTRACT_DEBUG_ONLY;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    if (!IsOkToLock() && !(m_violationmask & (TakesLockViolation|BadDebugState)))
    {
        return FALSE;
    }
    return TRUE;
}

inline void ClrDebugState::CheckOkayToLock(__in_z const char *szFunction, __in_z const char *szFile, int lineNum)
{
    STATIC_CONTRACT_DEBUG_ONLY;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    if (!CheckOkayToLockNoAssert())
    {

        CONTRACT_ASSERT("CAN_TAKE_LOCK called in a CANNOT_TAKE_LOCK region.",
                        BaseContract::CAN_TAKE_LOCK_No,
                        BaseContract::CAN_TAKE_LOCK_Mask,
                        szFunction,
                        szFile,
                        lineNum);
                        
    }
}


inline void ClrDebugState::LockTaken(DbgStateLockType dbgStateLockType,
                                     UINT cTakes, 
                                     void * pvLock, 
                                     __in_z const char * szFunction, 
                                     __in_z const char * szFile, 
                                     int lineNum)
{
    STATIC_CONTRACT_DEBUG_ONLY;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    if ((m_violationmask & BadDebugState) != 0)
    {
        return;
    }

    // Assert if we're taking a lock in a CANNOT_TAKE_LOCK scope.  Even if this asserts, we'll
    // continue to the following lines to track the lock
    CheckOkayToLock(szFunction, szFile, lineNum);

    _ASSERTE(GetDbgStateLockData() != NULL);

    if (!IsOkToRetakeLock())
    {
        if (m_LockState.IsLockRetaken(pvLock))
        {
            CONTRACT_ASSERT("You cannot take a lock which is already being held in a CANNOT_RETAKE_LOCK scope.",
                     BaseContract::CAN_RETAKE_LOCK_No,
                     BaseContract::CAN_RETAKE_LOCK_No,
                     szFunction,
                     szFile,
                     lineNum);
        }
    }

    GetDbgStateLockData()->LockTaken(dbgStateLockType, cTakes, pvLock, szFunction, szFile, lineNum);
}

inline void ClrDebugState::LockReleased(DbgStateLockType dbgStateLockType, UINT cReleases, void * pvLock)
{
    STATIC_CONTRACT_DEBUG_ONLY;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    if ((m_violationmask & BadDebugState) != 0)
    {
        return;
    }

    _ASSERTE(GetDbgStateLockData() != NULL);

    if (!IsOkToRetakeLock())
    {
        // It is very suspicious to release any locks being hold at the time this function was 
        // called in a CANNOT_RETAKE_LOCK scope
        _ASSERTE(m_LockState.IsSafeToRelease(cReleases));
    }

    GetDbgStateLockData()->LockReleased(dbgStateLockType, cReleases, pvLock);
}

inline UINT ClrDebugState::GetLockCount(DbgStateLockType dbgStateLockType)
{
    STATIC_CONTRACT_DEBUG_ONLY;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    if ((m_violationmask & BadDebugState) != 0)
    {
        return 0;
    }

    _ASSERTE(GetDbgStateLockData() != NULL);
    return GetDbgStateLockData()->GetLockCount(dbgStateLockType);
}

inline UINT ClrDebugState::GetCombinedLockCount()
{
    STATIC_CONTRACT_DEBUG_ONLY;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    if ((m_violationmask & BadDebugState) != 0)
    {
        return 0;
    }

    _ASSERTE(GetDbgStateLockData() != NULL);
    return GetDbgStateLockData()->GetCombinedLockCount();
}

inline void DbgStateLockData::LockTaken(DbgStateLockType dbgStateLockType,
                                        UINT cTakes,      // # times we're taking this lock (usually 1)
                                        void * pvLock, 
                                        __in_z const char * szFunction, 
                                        __in_z const char * szFile, 
                                        int lineNum)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    // Technically the lock's already been taken before we're called, but it's
    // handy to have this contract here at the leaf end of the call chain, as it
    // ensures SCAN will enforce that no use of the LOCK_TAKEN macros occurs
    // in a CANNOT_TAKE_LOCK scope (as LOCK_TAKEN macros just call this function).
    STATIC_CONTRACT_CAN_TAKE_LOCK;

    // Valid enum?
    _ASSERTE(UINT(dbgStateLockType) < kDbgStateLockType_Count);

    UINT cCombinedLocks = GetCombinedLockCount();

    // Are we exceeding the threshold for what we can store in m_rgTakenLockInfos?
    // If so, assert a warning, but we'll deal with it.
    if ((cCombinedLocks <= _countof(m_rgTakenLockInfos)) &&
        (cCombinedLocks + cTakes > _countof(m_rgTakenLockInfos)))
    {
        // Actually, for now we are NOT asserting until I can dedicate more time
        // to this.  Some class loader code paths legally hold many simultaneous
        // locks (>10).  Need to do further analysis on reasonable value to set
        // for kMaxAllowedSimultaneousLocks.  Since lock order checking is turned
        // off for the moment anyway, exceeding kMaxAllowedSimultaneousLocks
        // has no consequences for now anyway.
    }

    m_rgcLocksTaken[dbgStateLockType] += cTakes;

    // Remember as many of these new entrances in m_rgTakenLockInfos as we can
    for (UINT i = cCombinedLocks;
         i < min (_countof(m_rgTakenLockInfos), cCombinedLocks + cTakes);
         i++)
    {
        m_rgTakenLockInfos[i].m_pvLock = pvLock;
        m_rgTakenLockInfos[i].m_szFile = szFile;
        m_rgTakenLockInfos[i].m_lineNum = lineNum;
    }
}

inline void DbgStateLockData::LockReleased(DbgStateLockType dbgStateLockType, UINT cReleases, void * pvLock)
{
    // Valid enum?
    _ASSERTE(UINT(dbgStateLockType) < kDbgStateLockType_Count);

    if (cReleases > m_rgcLocksTaken[dbgStateLockType])
    {
        _ASSERTE(!"Releasing lock(s) that were never taken");
        cReleases = m_rgcLocksTaken[dbgStateLockType];
    }

    UINT cCombinedLocks = GetCombinedLockCount();

    // If lock count is within range of our m_rgTakenLockInfos buffer size, then
    // make sure we're releasing locks in reverse order of how we took them
    for (UINT i = cCombinedLocks - cReleases;
         i < min (_countof(m_rgTakenLockInfos), cCombinedLocks);
         i++)
    {
        if (m_rgTakenLockInfos[i].m_pvLock != pvLock)
        {
            // Ok, I lied.  We're not really checking that we're releasing locks in reverse
            // order, because sometimes we legally release them out of order.  (The loader
            // does this intentionally in a few places.) We should consider whether those
            // places can be changed, or whether we can add some kind of macro to declare
            // that we're releasing out of order, and that it's ok & intentional.  At that
            // point, we can place a nice ASSERTE right here.  Until then, do nothing.
        }

        // We may be clearing out the wrong entry in m_rgTakenLockInfos here, if the locks
        // were released out of order.  However, it will eventually correct itself once all
        // the out-of-order locks have been released.  And our count
        // (i.e., m_rgcLocksTaken[dbgStateLockType]) will always be accurate
        memset(&(m_rgTakenLockInfos[i]), 
               0,
               sizeof(m_rgTakenLockInfos[i]));
    }

    m_rgcLocksTaken[dbgStateLockType] -= cReleases;
}

inline void DbgStateLockData::SetStartingValues()
{
    memset(this, 0, sizeof(*this));
}

inline UINT DbgStateLockData::GetLockCount(DbgStateLockType dbgStateLockType)
{
    _ASSERTE(UINT(dbgStateLockType) < kDbgStateLockType_Count);
    return m_rgcLocksTaken[dbgStateLockType]; 
}

inline UINT DbgStateLockData::GetCombinedLockCount()
{
    // If this fires, the set of lock types must have changed.  You'll need to
    // fix the sum below to include all lock types
    _ASSERTE(kDbgStateLockType_Count == 3);

    return m_rgcLocksTaken[0] + m_rgcLocksTaken[1] + m_rgcLocksTaken[2];
}

inline void DbgStateLockState::SetStartingValues()
{
    m_cLocksEnteringCannotRetakeLock = 0;
    m_pLockData = NULL;     // Will get filled in by CLRInitDebugState()
}

// We set a marker to record the number of locks that have been taken when 
// CANNOT_RETAKE_LOCK contract is constructed.
inline void DbgStateLockState::OnEnterCannotRetakeLockFunction()
{
    m_cLocksEnteringCannotRetakeLock = m_pLockData->GetCombinedLockCount();
}

inline BOOL DbgStateLockState::IsLockRetaken(void * pvLock)
{
    // m_cLocksEnteringCannotRetakeLock must be in valid range
    _ASSERTE(m_cLocksEnteringCannotRetakeLock <= m_pLockData->GetCombinedLockCount());

    // m_cLocksEnteringCannotRetakeLock records the number of locks that were taken
    // when CANNOT_RETAKE_LOCK contract was constructed.
    for (UINT i = 0; 
        i < min(_countof(m_pLockData->m_rgTakenLockInfos), m_cLocksEnteringCannotRetakeLock); 
        ++i)
    {
        if (m_pLockData->m_rgTakenLockInfos[i].m_pvLock == pvLock)
        {
            return TRUE;
        }
    }
    return FALSE;
}

inline BOOL DbgStateLockState::IsSafeToRelease(UINT cReleases)
{
    return m_cLocksEnteringCannotRetakeLock <= (m_pLockData->GetCombinedLockCount() - cReleases);
}

inline void DbgStateLockState::SetDbgStateLockData(DbgStateLockData * pDbgStateLockData)
{
    m_pLockData = pDbgStateLockData;
}

inline DbgStateLockData * DbgStateLockState::GetDbgStateLockData()
{
    return m_pLockData;
}

inline
void CONTRACT_ASSERT(const char *szElaboration,
                     UINT  whichTest,        
                     UINT  whichTestMask,
                     const char *szFunction,
                     const char *szFile,
                     int   lineNum)
{
    if (CheckClrDebugState() && ( CheckClrDebugState()->ViolationMask() & BadDebugState))
    {
        _ASSERTE(!"Someone tried to assert a contract violation although the contracts were disabled in this thread due to"
                  " an OOM or a shim/mscorwks mismatch. You can probably safely ignore this assert - however, whoever"
                  " called CONTRACT_ASSERT was supposed to checked if the current violationmask had the BadDebugState set."
                  " Look up the stack, see who called CONTRACT_ASSERT and file a bug against the owner.");
        return;
    }

    // prevent recursion - we use the same mechanism as CHECK, so this will
    // also prevent mutual recursion involving ASSERT_CHECKs
    CHECK _check;
    if (_check.EnterAssert())
    {
        char Buf[512*20 + 2048 + 1024];

        sprintf_s(Buf,_countof(Buf), "CONTRACT VIOLATION by %s at \"%s\" @ %d\n\n%s\n", szFunction, szFile, lineNum, szElaboration);

        int count = 20;
        ContractStackRecord *pRec = CheckClrDebugState() ? CheckClrDebugState()->GetContractStackTrace() : NULL;
        BOOL foundconflict = FALSE;
        BOOL exceptionBuildingStack = FALSE;

        PAL_TRY_NAKED
        {
            while (pRec != NULL)
            {
                char tmpbuf[512];
                BOOL fshowconflict = FALSE;

                if (!foundconflict)
                {
                    if (whichTest == (pRec->m_testmask & whichTestMask))
                    {
                        foundconflict = TRUE;
                        fshowconflict = TRUE;
                    }
                }

                if (count != 0 || fshowconflict)
                {
                    if (count != 0)
                    {
                        count--;
                    }
                    else
                    {
                        // Show that some lines have been skipped
                        strcat_s(Buf, _countof(Buf), "\n                        ...");

                    }

                    sprintf_s(tmpbuf,_countof(tmpbuf),
                            "\n%s  %s in %s at \"%s\" @ %d",
                            fshowconflict ? "VIOLATED-->" : "                      ",
                            pRec->m_construct,
                            pRec->m_szFunction,
                            pRec->m_szFile,
                            pRec->m_lineNum
                            );
                
                    strcat_s(Buf, _countof(Buf), tmpbuf);
                }

                pRec = pRec->m_pNext;
            }
        }
        PAL_EXCEPT_NAKED(EXCEPTION_EXECUTE_HANDLER)
        {
            // We're done trying to walk the stack of contracts. We faulted trying to form the contract stack trace,
            // and that usually means that its corrupted. A common cause of this is having CONTRACTs in functions that
            // never return, but instead do a non-local goto.
            count = 0;
            exceptionBuildingStack = TRUE;
        }
        PAL_ENDTRY_NAKED;

        if (count == 0)
        {
            strcat_s(Buf,_countof(Buf), "\n                        ...");
        }

        if (exceptionBuildingStack)
        {
            strcat_s(Buf,_countof(Buf),
                   "\n"
                   "\nError forming contract stack. Any contract stack displayed above is correct,"
                   "\nbut it's most probably truncated. This is probably due to a CONTRACT in a"
                   "\nfunction that does a non-local goto. There are two bugs here:"
                   "\n"
                   "\n    1) the CONTRACT violation, and"
                   "\n    2) the CONTRACT in the function with the non-local goto."
                   "\n"
                   "\nPlease fix both bugs!"
                   "\n"
                   );
        }
        
        strcat_s(Buf,_countof(Buf), "\n\n");

        if (!foundconflict && count != 0)
        {
            if (whichTest == BaseContract::THROWS_No)
            {
                strcat_s(Buf,_countof(Buf), "You can't throw here because there is no handler on the stack.\n");
            }
            else
            {
                strcat_s(Buf,_countof(Buf), "We can't find the violated contract. Look for an old-style non-holder-based contract.\n");
            }
        }

        DbgAssertDialog((char *)szFile, lineNum, Buf);
        _check.LeaveAssert();
    }
}


FORCEINLINE BOOL BaseContract::EnforceContract()
{
    if (s_alwaysEnforceContracts)
        return TRUE;
    else 
        return CHECK::EnforceAssert();
}

inline void BaseContract::SetUnconditionalContractEnforcement(BOOL value)
{
    s_alwaysEnforceContracts = value;
}

inline UINT GetDbgStateCombinedLockCount()
{
    return GetClrDebugState()->GetCombinedLockCount();
}
inline UINT GetDbgStateLockCount(DbgStateLockType dbgStateLockType)
{
    return GetClrDebugState()->GetLockCount(dbgStateLockType);
}

#define ASSERT_NO_USER_LOCKS_HELD()   \
    _ASSERTE(GetDbgStateLockCount(kDbgStateLockType_User) == 0)
#define ASSERT_NO_HOST_BREAKABLE_CRSTS_HELD()   \
    _ASSERTE(GetDbgStateLockCount(kDbgStateLockType_HostBreakableCrst) == 0)
#define ASSERT_NO_EE_LOCKS_HELD()   \
    _ASSERTE(GetDbgStateLockCount(kDbgStateLockType_EE) == 0)

#else  // ENABLE_CONTRACTS_IMPL

#define ASSERT_NO_USER_LOCKS_HELD()
#define ASSERT_NO_HOST_BREAKABLE_CRSTS_HELD()
#define ASSERT_NO_EE_LOCKS_HELD()

#endif  // ENABLE_CONTRACTS_IMPL

#endif  // CONTRACT_INL_
