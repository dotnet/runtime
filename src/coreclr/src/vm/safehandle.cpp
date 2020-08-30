// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

/*============================================================
**
** Class:  SafeHandle
**
**
** Purpose: The unmanaged implementation of the SafeHandle
**          class
**
===========================================================*/

#include "common.h"
#include "vars.hpp"
#include "object.h"
#include "excep.h"
#include "frames.h"
#include "eecontract.h"
#include "typestring.h"

WORD SafeHandle::s_IsInvalidHandleMethodSlot = MethodTable::NO_SLOT;
WORD SafeHandle::s_ReleaseHandleMethodSlot = MethodTable::NO_SLOT;

void SafeHandle::Init()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    // For reliability purposes, we need to eliminate all possible failure
    // points before making a call to a CER method. IsInvalidHandle, and
    // ReleaseHandle methods are critical calls that are already prepared (code:
    // PrepareCriticalFinalizerObject). As a performance optimization, we are
    // calling these methods through a fast macro that assumes the method slot
    // has been already cached. Since figuring out the method slot for these 2
    // methods involves calling .GetMethod which can fail, we are doing this
    // eagerly here, Otherwise we will have to do it at the time of the call,
    // and this could be at risk if .GetMethod failed.
    MethodDesc* pMD = CoreLibBinder::GetMethod(METHOD__SAFE_HANDLE__GET_IS_INVALID);
    s_IsInvalidHandleMethodSlot = pMD->GetSlot();

    pMD = CoreLibBinder::GetMethod(METHOD__SAFE_HANDLE__RELEASE_HANDLE);
    s_ReleaseHandleMethodSlot = pMD->GetSlot();
}

// These AddRef and Release methods (and supporting functions) also exist with equivalent
// code in SafeHandle.cs.  Those implementations are the primary ones used by most code
// and exposed publicly; the implementations here are only for use by the runtime, without
// having to call out to the managed implementations.

void SafeHandle::AddRef()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INSTANCE_CHECK;
    } CONTRACTL_END;

    // Cannot use "this" after Release, which toggles the GC mode.
    SAFEHANDLEREF sh(this);

    _ASSERTE(sh->IsFullyInitialized());

    // See comments in SafeHandle.cs

    INT32 oldState, newState;
    do {

        oldState = sh->m_state;

        if (oldState & SH_State_Closed)
            COMPlusThrow(kObjectDisposedException, IDS_EE_SAFEHANDLECLOSED);

        newState = oldState + SH_RefCountOne;

    } while (InterlockedCompareExchange((LONG*)&sh->m_state, newState, oldState) != oldState);
}

void SafeHandle::Release(bool fDispose)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INSTANCE_CHECK;
    } CONTRACTL_END;

    // Cannot use "this" after RunReleaseMethod, which toggles the GC mode.
    SAFEHANDLEREF sh(this);

    _ASSERTE(sh->IsFullyInitialized());

    // See comments in SafeHandle.cs

    bool fPerformRelease = false;

    INT32 oldState, newState;
    do {

        oldState = sh->m_state;
        if (fDispose && (oldState & SH_State_Disposed))
            return;

        if ((oldState & SH_State_RefCount) == 0)
            COMPlusThrow(kObjectDisposedException, IDS_EE_SAFEHANDLECLOSED);

        fPerformRelease = ((oldState & (SH_State_RefCount | SH_State_Closed)) == SH_RefCountOne) && m_ownsHandle;

        if (fPerformRelease)
        {
            GCPROTECT_BEGIN(sh);

            CLR_BOOL fIsInvalid = FALSE;

            DECLARE_ARGHOLDER_ARRAY(args, 1);
            args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(sh);

            PREPARE_SIMPLE_VIRTUAL_CALLSITE_USING_SLOT(s_IsInvalidHandleMethodSlot, sh);

            CRITICAL_CALLSITE;
            CALL_MANAGED_METHOD(fIsInvalid, CLR_BOOL, args);

            if (fIsInvalid)
            {
                fPerformRelease = false;
            }

            GCPROTECT_END();
        }

        newState = (oldState - SH_RefCountOne) |
                   ((oldState & SH_State_RefCount) == SH_RefCountOne ? SH_State_Closed : 0) |
                   (fDispose ? SH_State_Disposed : 0);

    } while (InterlockedCompareExchange((LONG*)&sh->m_state, newState, oldState) != oldState);

    if (fPerformRelease)
        RunReleaseMethod((SafeHandle*) OBJECTREFToObject(sh));
}

void SafeHandle::SetHandle(LPVOID handle)
{
    CONTRACTL {
        THROWS;
        MODE_COOPERATIVE;
        INSTANCE_CHECK;
    } CONTRACTL_END;

    _ASSERTE(IsFullyInitialized());

    // The SafeHandle's handle field can only be set it if the SafeHandle isn't
    // closed or disposed and its ref count is 1.
    if (m_state != (LONG)SH_RefCountOne)
        COMPlusThrow(kObjectDisposedException, IDS_EE_SAFEHANDLECANNOTSETHANDLE);

    m_handle = handle;
}

void AcquireSafeHandle(SAFEHANDLEREF* s)
{
    WRAPPER_NO_CONTRACT;
    GCX_COOP();
    _ASSERTE(s != NULL && *s != NULL);
    (*s)->AddRef();
}

void ReleaseSafeHandle(SAFEHANDLEREF* s)
{
    WRAPPER_NO_CONTRACT;
    GCX_COOP();
    _ASSERTE(s != NULL && *s != NULL);
    (*s)->Release(false);
}


// This could theoretically be an instance method, but we'd need to
// somehow GC protect the this pointer or never dereference any
// field within the object.  It's a lot simpler if we simply make
// this method static.
void SafeHandle::RunReleaseMethod(SafeHandle* psh)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    SAFEHANDLEREF sh(psh);
    _ASSERTE(sh != NULL);
    _ASSERTE(sh->m_ownsHandle);
    _ASSERTE(sh->IsFullyInitialized());

    GCPROTECT_BEGIN(sh);

    // Save last error from P/Invoke in case the implementation of ReleaseHandle
    // trashes it (important because this ReleaseHandle could occur implicitly
    // as part of unmarshaling another P/Invoke).
    Thread *pThread = GetThread();
    DWORD dwSavedError = pThread->m_dwLastError;

    CLR_BOOL fReleaseHandle = FALSE;

    DECLARE_ARGHOLDER_ARRAY(args, 1);
    args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(sh);

    PREPARE_SIMPLE_VIRTUAL_CALLSITE_USING_SLOT(s_ReleaseHandleMethodSlot, sh);

    CRITICAL_CALLSITE;
    CALL_MANAGED_METHOD(fReleaseHandle, CLR_BOOL, args);

    pThread->m_dwLastError = dwSavedError;

    GCPROTECT_END();
}
