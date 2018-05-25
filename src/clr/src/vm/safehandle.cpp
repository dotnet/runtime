// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
#include "mdaassistants.h"
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
    MethodDesc* pMD = MscorlibBinder::GetMethod(METHOD__SAFE_HANDLE__GET_IS_INVALID);
    s_IsInvalidHandleMethodSlot = pMD->GetSlot();
    
    pMD = MscorlibBinder::GetMethod(METHOD__SAFE_HANDLE__RELEASE_HANDLE);
    s_ReleaseHandleMethodSlot = pMD->GetSlot();
}

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

    // To prevent handle recycling security attacks we must enforce the
    // following invariant: we cannot successfully AddRef a handle on which
    // we've committed to the process of releasing.

    // We ensure this by never AddRef'ing a handle that is marked closed and
    // never marking a handle as closed while the ref count is non-zero. For
    // this to be thread safe we must perform inspection/updates of the two
    // values as a single atomic operation. We achieve this by storing them both
    // in a single aligned DWORD and modifying the entire state via interlocked
    // compare exchange operations.

    // Additionally we have to deal with the problem of the Dispose operation.
    // We must assume that this operation is directly exposed to untrusted
    // callers and that malicious callers will try and use what is basically a
    // Release call to decrement the ref count to zero and free the handle while
    // it's still in use (the other way a handle recycling attack can be
    // mounted). We combat this by allowing only one Dispose to operate against
    // a given safe handle (which balances the creation operation given that
    // Dispose suppresses finalization). We record the fact that a Dispose has
    // been requested in the same state field as the ref count and closed state.

    // So the state field ends up looking like this:
    //
    //  31                                                        2  1   0
    // +-----------------------------------------------------------+---+---+
    // |                           Ref count                       | D | C |
    // +-----------------------------------------------------------+---+---+
    // 
    // Where D = 1 means a Dispose has been performed and C = 1 means the
    // underlying handle has (or will be shortly) released.

    // Might have to perform the following steps multiple times due to
    // interference from other AddRef's and Release's.
    INT32 oldState, newState;
    do {

        // First step is to read the current handle state. We use this as a
        // basis to decide whether an AddRef is legal and, if so, to propose an
        // update predicated on the initial state (a conditional write).
        oldState = sh->m_state;

        // Check for closed state.
        if (oldState & SH_State_Closed)
            COMPlusThrow(kObjectDisposedException, IDS_EE_SAFEHANDLECLOSED);

        // Not closed, let's propose an update (to the ref count, just add
        // SH_RefCountOne to the state to effectively add 1 to the ref count).
        // Continue doing this until the update succeeds (because nobody
        // modifies the state field between the read and write operations) or
        // the state moves to closed.
        newState = oldState + SH_RefCountOne;

    } while (InterlockedCompareExchange((LONG*)&sh->m_state, newState, oldState) != oldState);

    // If we got here we managed to update the ref count while the state
    // remained non closed. So we're done.
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

    // See AddRef above for the design of the synchronization here. Basically we
    // will try to decrement the current ref count and, if that would take us to
    // zero refs, set the closed state on the handle as well.
    bool fPerformRelease = false;

    // Might have to perform the following steps multiple times due to
    // interference from other AddRef's and Release's.
    INT32 oldState, newState;
    do {

        // First step is to read the current handle state. We use this cached
        // value to predicate any modification we might decide to make to the
        // state).
        oldState = sh->m_state;

        // If this is a Dispose operation we have additional requirements (to
        // ensure that Dispose happens at most once as the comments in AddRef
        // detail). We must check that the dispose bit is not set in the old
        // state and, in the case of successful state update, leave the disposed
        // bit set. Silently do nothing if Dispose has already been called
        // (because we advertise that as a semantic of Dispose).
        if (fDispose && (oldState & SH_State_Disposed))
            return;

        // We should never see a ref count of zero (that would imply we have
        // unbalanced AddRef and Releases). (We might see a closed state before
        // hitting zero though -- that can happen if SetHandleAsInvalid is
        // used).
        if ((oldState & SH_State_RefCount) == 0)
            COMPlusThrow(kObjectDisposedException, IDS_EE_SAFEHANDLECLOSED);

        // If we're proposing a decrement to zero and the handle is not closed
        // and we own the handle then we need to release the handle upon a
        // successful state update.
        fPerformRelease = ((oldState & (SH_State_RefCount | SH_State_Closed)) == SH_RefCountOne) && m_ownsHandle;

        // If so we need to check whether the handle is currently invalid by
        // asking the SafeHandle subclass. We must do this *before*
        // transitioning the handle to closed, however, since setting the closed
        // state will cause IsInvalid to always return true.
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

        // Attempt the update to the new state, fail and retry if the initial
        // state has been modified in the meantime. Decrement the ref count by
        // substracting SH_RefCountOne from the state then OR in the bits for
        // Dispose (if that's the reason for the Release) and closed (if the
        // initial ref count was 1).
        newState = (oldState - SH_RefCountOne) |
                   ((oldState & SH_State_RefCount) == SH_RefCountOne ? SH_State_Closed : 0) |
                   (fDispose ? SH_State_Disposed : 0);

    } while (InterlockedCompareExchange((LONG*)&sh->m_state, newState, oldState) != oldState);

    // If we get here we successfully decremented the ref count. Additionally we
    // may have decremented it to zero and set the handle state as closed. In
    // this case (providng we own the handle) we will call the ReleaseHandle
    // method on the SafeHandle subclass.
    if (fPerformRelease)
        RunReleaseMethod((SafeHandle*) OBJECTREFToObject(sh));
}

void SafeHandle::Dispose()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INSTANCE_CHECK;
    } CONTRACTL_END;

    // You can't use the "this" pointer after the call to Release because
    // Release may trigger a GC.
    SAFEHANDLEREF sh(this);

    _ASSERTE(sh->IsFullyInitialized());

    GCPROTECT_BEGIN(sh);
    sh->Release(true);
    // Suppress finalization on this object (we may be racing here but the
    // operation below is idempotent and a dispose should never race a
    // finalization).
    GCHeapUtilities::GetGCHeap()->SetFinalizationRun(OBJECTREFToObject(sh));
    GCPROTECT_END();
}

void SafeHandle::SetHandle(LPVOID handle)
{
    CONTRACTL {
        THROWS;
        MODE_COOPERATIVE;
        INSTANCE_CHECK;
        SO_TOLERANT;
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

    if (!fReleaseHandle) {
#ifdef MDA_SUPPORTED
        MDA_TRIGGER_ASSISTANT(ReleaseHandleFailed, ReportViolation(sh->GetTypeHandle(), sh->m_handle));
#endif
    }

    pThread->m_dwLastError = dwSavedError;

    GCPROTECT_END();
}

FCIMPL1(void, SafeHandle::DisposeNative, SafeHandle* refThisUNSAFE)
{
    FCALL_CONTRACT;

    SAFEHANDLEREF sh(refThisUNSAFE);
    if (sh == NULL)
        FCThrowVoid(kNullReferenceException);

    HELPER_METHOD_FRAME_BEGIN_1(sh);
    _ASSERTE(sh->IsFullyInitialized());
    sh->Dispose();
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL1(void, SafeHandle::Finalize, SafeHandle* refThisUNSAFE)
{
    FCALL_CONTRACT;

    SAFEHANDLEREF sh(refThisUNSAFE);
    _ASSERTE(sh != NULL);

    HELPER_METHOD_FRAME_BEGIN_1(sh);

    if (sh->IsFullyInitialized())
        sh->Dispose();

    // By the time we get here we better have gotten rid of any handle resources
    // we own (unless we were force finalized during shutdown).

    // It's possible to have a critical finalizer reference a
    // safehandle that ends up calling DangerousRelease *after* this finalizer
    // is run.  In that case we assert since the state is not closed. 
//    _ASSERTE(!sh->IsFullyInitialized() || (sh->m_state & SH_State_Closed) || g_fEEShutDown);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL1(void, SafeHandle::SetHandleAsInvalid, SafeHandle* refThisUNSAFE)
{
    FCALL_CONTRACT;

    SAFEHANDLEREF sh(refThisUNSAFE);
    _ASSERTE(sh != NULL);

    // Attempt to set closed state (low order bit of the m_state field).
    // Might have to attempt these repeatedly, if the operation suffers
    // interference from an AddRef or Release.
    INT32 oldState, newState;
    do {

        // First step is to read the current handle state so we can predicate a
        // state update on it.
        oldState = sh->m_state;

        // New state has the same ref count but is now closed. Attempt to write
        // this new state but fail if the state was updated in the meantime.
        newState = oldState | SH_State_Closed;

    } while (InterlockedCompareExchange((LONG*)&sh->m_state, newState, oldState) != oldState);

    GCHeapUtilities::GetGCHeap()->SetFinalizationRun(OBJECTREFToObject(sh));
}
FCIMPLEND

FCIMPL2(void, SafeHandle::DangerousAddRef, SafeHandle* refThisUNSAFE, CLR_BOOL *pfSuccess)
{
    FCALL_CONTRACT;

    SAFEHANDLEREF sh(refThisUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_1(sh);

    if (pfSuccess == NULL)
        COMPlusThrow(kNullReferenceException);

    sh->AddRef();
    *pfSuccess = TRUE;

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL1(void, SafeHandle::DangerousRelease, SafeHandle* refThisUNSAFE)
{
    FCALL_CONTRACT;

    SAFEHANDLEREF sh(refThisUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_1(sh);

    sh->Release(FALSE);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL1(void, CriticalHandle::FireCustomerDebugProbe, CriticalHandle* refThisUNSAFE)
{
    FCALL_CONTRACT;

    CRITICALHANDLEREF ch(refThisUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_1(ch);

#ifdef MDA_SUPPORTED
    MDA_TRIGGER_ASSISTANT(ReleaseHandleFailed, ReportViolation(ch->GetTypeHandle(), ch->m_handle));
#else
    FCUnique(0x53);
#endif

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
