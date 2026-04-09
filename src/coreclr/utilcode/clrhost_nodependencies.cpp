// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

#include "stdafx.h"

#include "clrhost.h"
#include "utilcode.h"
#include "ex.h"
#include "clrnt.h"
#include "contract.h"

#if defined(_DEBUG_IMPL) && defined(ENABLE_CONTRACTS_IMPL)

thread_local ClrDebugState* t_pClrDebugState;

// Fls callback to deallocate ClrDebugState when our FLS block goes away.
void FreeClrDebugState(LPVOID pTlsData)
{
    ClrDebugState *pClrDebugState = (ClrDebugState*)pTlsData;

    // Make sure the ClrDebugState was initialized by a compatible version of
    // utilcode.lib. If it was initialized by an older version, we just let it leak.
    if (pClrDebugState && (pClrDebugState->ViolationMask() & CanFreeMe) && !(pClrDebugState->ViolationMask() & BadDebugState))
    {
        // Since "!(pClrDebugState->m_violationmask & BadDebugState)", we know we have
        // a valid m_pLockData
        _ASSERTE(pClrDebugState->GetDbgStateLockData() != NULL);
        HeapFree(GetProcessHeap(), 0, pClrDebugState->GetDbgStateLockData());

        HeapFree(GetProcessHeap(), 0, pClrDebugState);
    }
}

//=============================================================================================
// Used to initialize the per-thread ClrDebugState. This is called once per thread (with
// possible exceptions for OOM scenarios.)
//
// No matter what, this function will not return NULL. If it can't do its job because of OOM reasons,
// it will return a pointer to &gBadClrDebugState which effectively disables contracts for
// this thread.
//=============================================================================================
ClrDebugState *CLRInitDebugState()
{
    // This is our global "bad" debug state that thread use when they OOM on CLRInitDebugState.
    // We really only need to initialize it once but initializing each time is convenient
    // and has low perf impact.
    static ClrDebugState gBadClrDebugState;
    gBadClrDebugState.ViolationMaskSet( AllViolation );
    gBadClrDebugState.SetOkToThrow();

    ClrDebugState *pNewClrDebugState = NULL;
    ClrDebugState *pClrDebugState = NULL;
    DbgStateLockData *pNewLockData = NULL;

    // Yuck. We cannot call the hosted allocator for ClrDebugState (it is impossible to maintain a guarantee
    // that none of code paths, many of them called conditionally, don't themselves trigger a ClrDebugState creation.)
    // We have to call the OS directly for this.
    pNewClrDebugState = (ClrDebugState*)HeapAlloc(GetProcessHeap(), 0, sizeof(ClrDebugState));
    if (pNewClrDebugState != NULL)
    {
        // Only allocate a DbgStateLockData if its owning ClrDebugState was successfully allocated
        pNewLockData  = (DbgStateLockData *)HeapAlloc(GetProcessHeap(), 0, sizeof(DbgStateLockData));
    }

    if ((pNewClrDebugState != NULL) && (pNewLockData != NULL))
    {
        // Both allocations succeeded, so initialize the structures, and have
        // pNewClrDebugState point to pNewLockData.  If either of the allocations
        // failed, we'll use gBadClrDebugState for this thread, and free whichever of
        // pNewClrDebugState or pNewLockData actually did get allocated (if either did).
        // (See code in this function below, outside this block.)

        pNewClrDebugState->SetStartingValues();
        pNewClrDebugState->ViolationMaskSet( CanFreeMe );
        _ASSERTE(!(pNewClrDebugState->ViolationMask() & BadDebugState));

        pNewLockData->SetStartingValues();
        pNewClrDebugState->SetDbgStateLockData(pNewLockData);
    }


    // This is getting really diseased. All the one-time host init stuff inside the ClrFlsStuff could actually
    // have caused mscorwks contracts to be executed since the last time we actually checked to see if the ClrDebugState
    // needed creating.
    //
    // So we must make one last check to see if the ClrDebugState still needs creating.
    //
    ClrDebugState *pTmp = t_pClrDebugState;
    if (pTmp != NULL)
    {
        // Recursive call set up ClrDebugState for us
        pClrDebugState = pTmp;
    }
    else if ((pNewClrDebugState != NULL) && (pNewLockData != NULL))
    {
        // Normal case: our new ClrDebugState will be the one we just allocated.
        // Note that we require BOTH the ClrDebugState and the DbgStateLockData
        // structures to have been successfully allocated for contracts to be
        // enabled for this thread.
        _ASSERTE(!(pNewClrDebugState->ViolationMask() & BadDebugState));
        _ASSERTE(pNewClrDebugState->GetDbgStateLockData() == pNewLockData);
        pClrDebugState = pNewClrDebugState;
    }
    else
    {
        // OOM case: HeapAlloc of newClrDebugState failed.
        pClrDebugState = &gBadClrDebugState;
    }

    _ASSERTE(pClrDebugState != NULL);

    t_pClrDebugState = pClrDebugState;

    // The ClrDebugState we allocated above made it into FLS iff
    //      the DbgStateLockData we allocated above made it into
    //      the FLS's ClrDebugState::m_pLockData
    // These debug-only checks enforce this invariant

    if (pClrDebugState != NULL)
    {
        // If we're here, then typically pClrDebugState is what's in FLS.  However,
        // it's possible that pClrDebugState is gBadClrDebugState, and FLS is NULL
        // (if the last ClrFlsSetValue() failed).  Either way, our checks below
        // are valid ones to make.

        if (pClrDebugState == pNewClrDebugState)
        {
            // ClrDebugState we allocated above made it into FLS, so DbgStateLockData
            // must be there, too
            _ASSERTE(pNewLockData != NULL);
            _ASSERTE(pClrDebugState->GetDbgStateLockData() == pNewLockData);
        }
        else
        {
            // ClrDebugState we allocated above did NOT make it into FLS,
            // so the DbgStateLockData we allocated must not be there, either
            _ASSERTE(pClrDebugState->GetDbgStateLockData() == NULL || pClrDebugState->GetDbgStateLockData() != pNewLockData);
        }
    }

    // One more invariant:  Because of ordering & conditions around the HeapAllocs above,
    // we'll never have a DbgStateLockData without a ClrDebugState
    _ASSERTE((pNewLockData == NULL) || (pNewClrDebugState != NULL));

    if (pNewClrDebugState != NULL && pClrDebugState != pNewClrDebugState)
    {
        // We allocated a ClrDebugState which didn't make it into FLS, so free it.
        HeapFree(GetProcessHeap(), 0, pNewClrDebugState);
        if (pNewLockData != NULL)
        {
            // We also allocated a DbgStateLockData that didn't make it into FLS, so
            // free it, too.  (Remember, we asserted above that we can only have
            // this unused DbgStateLockData if we had an unused ClrDebugState
            // as well (which we just freed).)
            HeapFree(GetProcessHeap(), 0, pNewLockData);
        }
    }

    return pClrDebugState;
} // CLRInitDebugState

#endif //defined(_DEBUG_IMPL) && defined(ENABLE_CONTRACTS_IMPL)

#ifdef _DEBUG

// This is a DEBUG routing to verify that a memory region complies with executable requirements
BOOL DbgIsExecutable(LPVOID lpMem, SIZE_T length)
{
#if defined(TARGET_UNIX)
    // No NX support on PAL
    return TRUE;
#else // !(TARGET_UNIX)
    BYTE *regionStart = (BYTE*) ALIGN_DOWN((BYTE*)lpMem, GetOsPageSize());
    BYTE *regionEnd = (BYTE*) ALIGN_UP((BYTE*)lpMem+length, GetOsPageSize());
    _ASSERTE(length > 0);
    _ASSERTE(regionStart < regionEnd);

    while(regionStart < regionEnd)
    {
        MEMORY_BASIC_INFORMATION mbi;

        SIZE_T cbBytes = ClrVirtualQuery(regionStart, &mbi, sizeof(mbi));
        _ASSERTE(cbBytes);

        // The pages must have EXECUTE set
        if(!(mbi.Protect & (PAGE_EXECUTE | PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY)))
            return FALSE;

        _ASSERTE((BYTE*)mbi.BaseAddress + mbi.RegionSize > regionStart);
        regionStart = (BYTE*)mbi.BaseAddress + mbi.RegionSize;
    }

    return TRUE;
#endif // TARGET_UNIX
}

#endif //_DEBUG
