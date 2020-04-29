// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Implementation: WeakReferenceNative.cpp
**
**
===========================================================*/

#include "common.h"

#include "gchandleutilities.h"
#include "weakreferencenative.h"
#include "typestring.h"
#include "typeparse.h"
#include "threadsuspend.h"

//************************************************************************

// We use several special values of the handle to track extra state without increasing the instance size.
const LPVOID specialWeakReferenceHandles[3] = { 0, 0, 0 };

// SPECIAL_HANDLE_SPINLOCK is used to implement spinlock that protects against races between setting the target and finalization
#define SPECIAL_HANDLE_SPINLOCK ((OBJECTHANDLE)(&specialWeakReferenceHandles[0]))

// SPECIAL_HANDLE_FINALIZED is used to track the original type of the handle so that IsTrackResurrection keeps working on finalized
// objects for backward compatibility.
#define SPECIAL_HANDLE_FINALIZED_SHORT  ((OBJECTHANDLE)(&specialWeakReferenceHandles[1]))
#define SPECIAL_HANDLE_FINALIZED_LONG   ((OBJECTHANDLE)(&specialWeakReferenceHandles[2]))

#define IS_SPECIAL_HANDLE(h) ((size_t)(h) - (size_t)(&specialWeakReferenceHandles) < sizeof(specialWeakReferenceHandles))

FORCEINLINE OBJECTHANDLE AcquireWeakHandleSpinLock(WEAKREFERENCEREF pThis);
FORCEINLINE void ReleaseWeakHandleSpinLock(WEAKREFERENCEREF pThis, OBJECTHANDLE newHandle);

struct WeakHandleSpinLockHolder
{
    OBJECTHANDLE RawHandle;
    OBJECTHANDLE Handle;
    WEAKREFERENCEREF* pWeakReference;

    WeakHandleSpinLockHolder(OBJECTHANDLE rawHandle, WEAKREFERENCEREF* weakReference)
        : RawHandle(rawHandle), Handle(rawHandle), pWeakReference(weakReference)
    {
        STATIC_CONTRACT_LEAF;
    }

    ~WeakHandleSpinLockHolder()
    {
        WRAPPER_NO_CONTRACT;
        ReleaseWeakHandleSpinLock(*pWeakReference, RawHandle);
    }

private:
    WeakHandleSpinLockHolder(const WeakHandleSpinLockHolder& other);
    WeakHandleSpinLockHolder& operator=(const WeakHandleSpinLockHolder& other);
};

//************************************************************************

//
// Spinlock implemented by overloading the WeakReference::m_Handle field that protects against races between setting
// the target and finalization
//

NOINLINE OBJECTHANDLE AcquireWeakHandleSpinLockSpin(WEAKREFERENCEREF pThis)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    DWORD dwSwitchCount = 0;
    YieldProcessorNormalizationInfo normalizationInfo;

    //
    // Boilerplate spinning logic stolen from other locks
    //
    for (;;)
    {
        if (g_SystemInfo.dwNumberOfProcessors > 1)
        {
            DWORD spincount = g_SpinConstants.dwInitialDuration;

            for (;;)
            {
                YieldProcessorNormalizedForPreSkylakeCount(normalizationInfo, spincount);

                OBJECTHANDLE handle = InterlockedExchangeT(&pThis->m_Handle, SPECIAL_HANDLE_SPINLOCK);
                if (handle != SPECIAL_HANDLE_SPINLOCK)
                    return handle;

                spincount *= g_SpinConstants.dwBackoffFactor;
                if (spincount > g_SpinConstants.dwMaximumDuration)
                {
                    break;
                }
            }
        }

        __SwitchToThread(0, ++dwSwitchCount);

        OBJECTHANDLE handle = InterlockedExchangeT(&pThis->m_Handle, SPECIAL_HANDLE_SPINLOCK);
        if (handle != SPECIAL_HANDLE_SPINLOCK)
            return handle;
    }
}

FORCEINLINE OBJECTHANDLE AcquireWeakHandleSpinLock(WEAKREFERENCEREF pThis)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    OBJECTHANDLE handle = InterlockedExchangeT(&pThis->m_Handle, SPECIAL_HANDLE_SPINLOCK);
    if (handle != SPECIAL_HANDLE_SPINLOCK)
        return handle;
    return AcquireWeakHandleSpinLockSpin(pThis);
}

FORCEINLINE void ReleaseWeakHandleSpinLock(WEAKREFERENCEREF pThis, OBJECTHANDLE newHandle)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(newHandle != SPECIAL_HANDLE_SPINLOCK);
    pThis->m_Handle = newHandle;
}

//************************************************************************

MethodTable *pWeakReferenceMT = NULL;
MethodTable *pWeakReferenceOfTCanonMT = NULL;

//************************************************************************

FCIMPL3(void, WeakReferenceNative::Create, WeakReferenceObject * pThisUNSAFE, Object * pTargetUNSAFE, CLR_BOOL trackResurrection)
{
    FCALL_CONTRACT;

    struct
    {
        WEAKREFERENCEREF pThis;
        OBJECTREF pTarget;
    } gc;

    gc.pThis = WEAKREFERENCEREF(pThisUNSAFE);
    gc.pTarget = OBJECTREF(pTargetUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    if (gc.pThis == NULL)
        COMPlusThrow(kNullReferenceException);

    if (pWeakReferenceMT == NULL)
        pWeakReferenceMT = MscorlibBinder::GetClass(CLASS__WEAKREFERENCE);

    _ASSERTE(gc.pThis->GetMethodTable()->CanCastToClass(pWeakReferenceMT));

    // Create the handle.

    gc.pThis->m_Handle = GetAppDomain()->CreateTypedHandle(gc.pTarget,
        trackResurrection ? HNDTYPE_WEAK_LONG : HNDTYPE_WEAK_SHORT);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL3(void, WeakReferenceOfTNative::Create, WeakReferenceObject * pThisUNSAFE, Object * pTargetUNSAFE, CLR_BOOL trackResurrection)
{
    FCALL_CONTRACT;

    struct
    {
        WEAKREFERENCEREF pThis;
        OBJECTREF pTarget;
    } gc;

    gc.pThis = WEAKREFERENCEREF(pThisUNSAFE);
    gc.pTarget = OBJECTREF(pTargetUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    if (gc.pThis == NULL)
        COMPlusThrow(kNullReferenceException);

    if (pWeakReferenceOfTCanonMT == NULL)
        pWeakReferenceOfTCanonMT = gc.pThis->GetMethodTable()->GetCanonicalMethodTable();

    _ASSERTE(gc.pThis->GetMethodTable()->GetCanonicalMethodTable() == pWeakReferenceOfTCanonMT);

    // Create the handle.

    gc.pThis->m_Handle = GetAppDomain()->CreateTypedHandle(gc.pTarget,
        trackResurrection ? HNDTYPE_WEAK_LONG : HNDTYPE_WEAK_SHORT);


    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

//************************************************************************

// This entrypoint is also used for direct finalization by the GC. Note that we cannot depend on the runtime being suspended
// when this is called because of background GC. Background GC is going to call this method while user managed code is running.
void FinalizeWeakReference(Object * obj)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    WEAKREFERENCEREF pThis((WeakReferenceObject *)(obj));

    // The suspension state of the runtime must be prevented from changing while in this function in order for this to be safe.
    OBJECTHANDLE handle = ThreadSuspend::SysIsSuspended() ? pThis->m_Handle.LoadWithoutBarrier() : AcquireWeakHandleSpinLock(pThis);
    OBJECTHANDLE handleToDestroy = NULL;

    // Check for not yet constructed or already finalized handle
    if ((handle != NULL) && !IS_SPECIAL_HANDLE(handle))
    {
        handleToDestroy = handle;

        // Cache the old handle value
        HandleType handleType = GCHandleUtilities::GetGCHandleManager()->HandleFetchType(handleToDestroy);
        _ASSERTE(handleType == HNDTYPE_WEAK_LONG || handleType == HNDTYPE_WEAK_SHORT);

        handle = (handleType == HNDTYPE_WEAK_LONG) ?
            SPECIAL_HANDLE_FINALIZED_LONG : SPECIAL_HANDLE_FINALIZED_SHORT;
    }

    // Release the spin lock
    // This is necessary even when the spin lock is not acquired
    // (i.e. When ThreadSuspend::SysIsSuspended() == true)
    // so that the new handle value is set.
    ReleaseWeakHandleSpinLock(pThis, handle);

    if (handleToDestroy != NULL)
    {
        DestroyTypedHandle(handleToDestroy);
    }
}

FCIMPL1(void, WeakReferenceNative::Finalize, WeakReferenceObject * pThis)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_NOPOLL();

    if (pThis == NULL)
    {
        FCUnique(0x1);
        COMPlusThrow(kNullReferenceException);
    }

    FinalizeWeakReference(pThis);

    HELPER_METHOD_FRAME_END_POLL();
}
FCIMPLEND

FCIMPL1(void, WeakReferenceOfTNative::Finalize, WeakReferenceObject * pThis)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_NOPOLL();

    if (pThis == NULL)
        COMPlusThrow(kNullReferenceException);

    FinalizeWeakReference(pThis);

    HELPER_METHOD_FRAME_END_POLL();
}
FCIMPLEND

//************************************************************************

#include <optsmallperfcritical.h>

static FORCEINLINE OBJECTREF GetWeakReferenceTarget(WEAKREFERENCEREF pThis)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    OBJECTHANDLE rawHandle = pThis->m_Handle.LoadWithoutBarrier();
    OBJECTHANDLE handle = rawHandle;

    if (handle == NULL)
        return NULL;

    // Try a speculative lock-free read first
    if (rawHandle != SPECIAL_HANDLE_SPINLOCK)
    {
        //
        // There is a theoretic chance that the speculative lock-free read may AV while reading the value
        // of freed handle if the handle table decides to release the memory that the handle lives in.
        // It is not exploitable security issue because of we will fail fast on the AV. It is denial of service only.
        // Non-malicious user code will never hit.
        //
        // We had this theoretical bug in there since forever. Fixing it by always taking the lock would
        // degrade the performance critical weak handle getter several times. The right fix may be
        // to ensure that handle table memory is released only if the runtime is suspended.
        //
        Object * pSpeculativeTarget = VolatileLoad((Object **)(handle));

        //
        // We want to ensure that the handle was still alive when we fetched the target,
        // so we double check m_handle here. Note that the reading of the handle
        // value has to take memory barrier for this to work, but reading of m_handle does not.
        //
        if (rawHandle == pThis->m_Handle.LoadWithoutBarrier())
        {
            return OBJECTREF(pSpeculativeTarget);
        }
    }


    rawHandle = AcquireWeakHandleSpinLock(pThis);
    GCX_NOTRIGGER();

    handle = rawHandle;
    OBJECTREF pTarget = OBJECTREF(*(Object **)(handle));

    ReleaseWeakHandleSpinLock(pThis, rawHandle);

    return pTarget;
}

FCIMPL1(Object *, WeakReferenceNative::GetTarget, WeakReferenceObject * pThisUNSAFE)
{
    FCALL_CONTRACT;

    WEAKREFERENCEREF pThis(pThisUNSAFE);
    if (pThis == NULL)
    {
        FCUnique(0x1);
        FCThrow(kNullReferenceException);
    }

    OBJECTREF pTarget = GetWeakReferenceTarget(pThis);
    FC_GC_POLL_AND_RETURN_OBJREF(pTarget);
}
FCIMPLEND

FCIMPL1(Object *, WeakReferenceOfTNative::GetTarget, WeakReferenceObject * pThisUNSAFE)
{
    FCALL_CONTRACT;

    WEAKREFERENCEREF pThis(pThisUNSAFE);
    if (pThis == NULL)
    {
        FCThrow(kNullReferenceException);
    }

    OBJECTREF pTarget = GetWeakReferenceTarget(pThis);

    FC_GC_POLL_AND_RETURN_OBJREF(pTarget);
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, WeakReferenceNative::IsAlive, WeakReferenceObject * pThisUNSAFE)
{
    FCALL_CONTRACT;

    WEAKREFERENCEREF pThis(pThisUNSAFE);
    if (pThis == NULL)
    {
        FCThrow(kNullReferenceException);
    }

    BOOL fRet = GetWeakReferenceTarget(pThis) != NULL;

    FC_GC_POLL_RET();

    FC_RETURN_BOOL(fRet);
}
FCIMPLEND

#include <optdefault.h>

//************************************************************************

#include <optsmallperfcritical.h>

FCIMPL2(void, WeakReferenceNative::SetTarget, WeakReferenceObject * pThisUNSAFE, Object * pTargetUNSAFE)
{
    FCALL_CONTRACT;

    WEAKREFERENCEREF pThis(pThisUNSAFE);
    OBJECTREF pTarget(pTargetUNSAFE);

    if (pThis == NULL)
    {
        FCUnique(0x1);
        FCThrowVoid(kNullReferenceException);
    }

    {
        OBJECTHANDLE handle = AcquireWeakHandleSpinLock(pThis);
        if (handle == NULL || IS_SPECIAL_HANDLE(handle))
        {
            ReleaseWeakHandleSpinLock(pThis, handle);
            FCThrowResVoid(kInvalidOperationException, W("InvalidOperation_HandleIsNotInitialized"));
        }

        // Switch to no-trigger after the handle was validate. FCThrow triggers.
        GCX_NOTRIGGER();

        StoreObjectInHandle(handle, pTarget);

        ReleaseWeakHandleSpinLock(pThis, handle);
    }

    FC_GC_POLL();
    return;
}
FCIMPLEND

FCIMPL2(void, WeakReferenceOfTNative::SetTarget, WeakReferenceObject * pThisUNSAFE, Object * pTargetUNSAFE)
{
    FCALL_CONTRACT;

    WEAKREFERENCEREF pThis(pThisUNSAFE);
    OBJECTREF pTarget(pTargetUNSAFE);

    if (pThis == NULL)
    {
        FCThrowVoid(kNullReferenceException);
    }

    {
        OBJECTHANDLE handle = AcquireWeakHandleSpinLock(pThis);
        if (handle == NULL || IS_SPECIAL_HANDLE(handle))
        {
            ReleaseWeakHandleSpinLock(pThis, handle);
            FCThrowResVoid(kInvalidOperationException, W("InvalidOperation_HandleIsNotInitialized"));
        }

        // Switch to no-trigger after the handle was validate. FCThrow triggers.
        GCX_NOTRIGGER();
        StoreObjectInHandle(handle, pTarget);

        ReleaseWeakHandleSpinLock(pThis, handle);
    }

    FC_GC_POLL();
    return;
}
FCIMPLEND

#include <optdefault.h>

//************************************************************************

FCIMPL1(FC_BOOL_RET, WeakReferenceNative::IsTrackResurrection, WeakReferenceObject * pThisUNSAFE)
{
    FCALL_CONTRACT;

    WEAKREFERENCEREF pThis(pThisUNSAFE);

    if (pThis == NULL)
    {
        FCUnique(0x1);
        FCThrow(kNullReferenceException);
    }

    BOOL trackResurrection = FALSE;
    OBJECTHANDLE handle = AcquireWeakHandleSpinLock(pThis);
    {
        GCX_NOTRIGGER();

        if (handle == NULL)
        {
            trackResurrection = FALSE;
        }
        else
        if (IS_SPECIAL_HANDLE(handle))
        {
            trackResurrection = (handle == SPECIAL_HANDLE_FINALIZED_LONG);
        }
        else
        {
            trackResurrection = GCHandleUtilities::GetGCHandleManager()->HandleFetchType(handle) == HNDTYPE_WEAK_LONG;
        }

        ReleaseWeakHandleSpinLock(pThis, handle);
    }

    FC_GC_POLL_RET();
    FC_RETURN_BOOL(trackResurrection);
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, WeakReferenceOfTNative::IsTrackResurrection, WeakReferenceObject * pThisUNSAFE)
{
    FCALL_CONTRACT;

    WEAKREFERENCEREF pThis(pThisUNSAFE);

    if (pThis == NULL)
    {
        FCThrow(kNullReferenceException);
    }

    BOOL trackResurrection = FALSE;
    OBJECTHANDLE handle = AcquireWeakHandleSpinLock(pThis);
    {
        GCX_NOTRIGGER();

        if (handle == NULL)
        {
            trackResurrection = FALSE;
        }
        else
        if (IS_SPECIAL_HANDLE(handle))
        {
            trackResurrection = (handle == SPECIAL_HANDLE_FINALIZED_LONG);
        }
        else
        {
            trackResurrection = GCHandleUtilities::GetGCHandleManager()->HandleFetchType(handle) == HNDTYPE_WEAK_LONG;
        }

        ReleaseWeakHandleSpinLock(pThis, handle);
    }

    FC_GC_POLL_RET();
    FC_RETURN_BOOL(trackResurrection);
}
FCIMPLEND

//************************************************************************
