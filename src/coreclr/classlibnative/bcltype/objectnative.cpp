// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: ObjectNative.cpp
//

//
// Purpose: Native methods on System.Object
//
//

#include "common.h"

#include "objectnative.h"
#include "excep.h"
#include "vars.hpp"
#include "field.h"
#include "object.h"
#include "comsynchronizable.h"
#include "eeconfig.h"

extern "C" INT32 QCALLTYPE ObjectNative_GetHashCodeSlow(QCall::ObjectHandleOnStack objHandle)
{
    QCALL_CONTRACT;

    INT32 idx = 0;

    BEGIN_QCALL;

    GCX_COOP();

    _ASSERTE(objHandle.Get() != NULL);
    idx = objHandle.Get()->GetHashCodeEx();

    END_QCALL;

    return idx;
}

FCIMPL1(INT32, ObjectNative::TryGetHashCode, Object* obj)
{
    FCALL_CONTRACT;

    if (obj == NULL)
        return 0;

    OBJECTREF objRef = ObjectToOBJECTREF(obj);
    DWORD bits = objRef->GetHeader()->GetBits();
    if (bits & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX)
    {
        if (bits & BIT_SBLK_IS_HASHCODE)
        {
            // Common case: the object already has a hash code
            return bits & MASK_HASHCODE;
        }
        else
        {
            // We have a sync block index. This means if we already have a hash code,
            // it is in the sync block, otherwise we will return 0, which means "not set".
            SyncBlock *psb = objRef->PassiveGetSyncBlock();
            if (psb != NULL)
                return psb->GetHashCode();
        }
    }
    return 0;
}
FCIMPLEND

FCIMPL2(FC_BOOL_RET, ObjectNative::ContentEquals, Object *pThisRef, Object *pCompareRef)
{
    FCALL_CONTRACT;

    // Should be ensured by caller
    _ASSERTE(pThisRef != NULL);
    _ASSERTE(pCompareRef != NULL);
    _ASSERTE(pThisRef->GetMethodTable() == pCompareRef->GetMethodTable());

    MethodTable *pThisMT = pThisRef->GetMethodTable();

    // Compare the contents
    BOOL ret = memcmp(
        pThisRef->GetData(),
        pCompareRef->GetData(),
        pThisMT->GetNumInstanceFieldBytes()) == 0;

    FC_RETURN_BOOL(ret);
}
FCIMPLEND

extern "C" void QCALLTYPE ObjectNative_AllocateUninitializedClone(QCall::ObjectHandleOnStack objHandle)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    OBJECTREF refClone = objHandle.Get();
    _ASSERTE(refClone != NULL); // Should be handled at managed side
    MethodTable* pMT = refClone->GetMethodTable();

    // assert that String has overloaded the Clone() method
    _ASSERTE(pMT != g_pStringClass);

    if (pMT->IsArray())
    {
        objHandle.Set(DupArrayForCloning((BASEARRAYREF)refClone));
    }
    else
    {
        // We don't need to call the <cinit> because we know
        //  that it has been called....(It was called before this was created)
        objHandle.Set(AllocateObject(pMT));
    }

    END_QCALL;
}

extern "C" BOOL QCALLTYPE Monitor_Wait(QCall::ObjectHandleOnStack pThis, INT32 Timeout)
{
    QCALL_CONTRACT;

    BOOL retVal = FALSE;

    BEGIN_QCALL;

    GCX_COOP();

     // Arguments validated on managed side
    _ASSERTE(pThis.Get() != NULL);
    _ASSERTE(Timeout >= INFINITE_TIMEOUT);

    retVal = pThis.Get()->Wait(Timeout);

    END_QCALL;

    return retVal;
}

extern "C" void QCALLTYPE Monitor_Pulse(QCall::ObjectHandleOnStack pThis)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    _ASSERTE(pThis.Get() != NULL);
    pThis.Get()->Pulse();

    END_QCALL;
}

extern "C" void QCALLTYPE Monitor_PulseAll(QCall::ObjectHandleOnStack pThis)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    _ASSERTE(pThis.Get() != NULL);
    pThis.Get()->PulseAll();

    END_QCALL;
}

FCIMPL1(FC_BOOL_RET, ObjectNative::IsLockHeld, Object* pThisUNSAFE)
{
    FCALL_CONTRACT;

    BOOL retVal;
    DWORD owningThreadId;
    DWORD acquisitionCount;

    //
    // If the lock is held, check if it's held by the current thread.
    //
    retVal = pThisUNSAFE->GetThreadOwningMonitorLock(&owningThreadId, &acquisitionCount);
    if (retVal)
        retVal = GetThread()->GetThreadId() == owningThreadId;

    FC_RETURN_BOOL(retVal);
}
FCIMPLEND

extern "C" INT64 QCALLTYPE Monitor_GetLockContentionCount()
{
    QCALL_CONTRACT;

    INT64 result = 0;

    BEGIN_QCALL;

    result = (INT64)Thread::GetTotalMonitorLockContentionCount();

    END_QCALL;
    return result;
}

//========================================================================
//
//      MONITOR HELPERS
//
//========================================================================

/*********************************************************************/
extern "C" void QCALLTYPE Monitor_Enter_Slowpath(QCall::ObjectHandleOnStack objHandle)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    objHandle.Get()->EnterObjMonitor();
    END_QCALL;
}

/*********************************************************************/
#include <optsmallperfcritical.h>

FCIMPL1(FC_BOOL_RET, ObjectNative::Monitor_TryEnter_FastPath, Object* obj)
{
    FCALL_CONTRACT;

    if (obj->TryEnterObjMonitorSpinHelper())
    {
        FC_RETURN_BOOL(TRUE);
    }
    else
    {
        FC_RETURN_BOOL(FALSE);
    }
}
FCIMPLEND

FCIMPL2(AwareLock::EnterHelperResult, ObjectNative::Monitor_TryEnter_FastPath_WithTimeout, Object* obj, INT32 timeOut)
{
    FCALL_CONTRACT;

    Thread* pCurThread = GetThread();

    if (pCurThread->CatchAtSafePoint())
    {
        return AwareLock::EnterHelperResult::UseSlowPath;
    }

    AwareLock::EnterHelperResult result = obj->EnterObjMonitorHelper(pCurThread);
    if (result == AwareLock::EnterHelperResult::Contention)
    {
        if (timeOut == 0)
        {
            return AwareLock::EnterHelperResult::Contention;
        }

        result = obj->EnterObjMonitorHelperSpin(pCurThread);
    }

    return result;
}
FCIMPLEND

#include <optdefault.h>

/*********************************************************************/
extern "C" INT32 QCALLTYPE Monitor_TryEnter_Slowpath(QCall::ObjectHandleOnStack objHandle, INT32 timeOut)
{
    QCALL_CONTRACT;

    BOOL result = FALSE;

    BEGIN_QCALL;

    GCX_COOP();

    if (timeOut < -1)
        COMPlusThrow(kArgumentOutOfRangeException);

    result = objHandle.Get()->TryEnterObjMonitor(timeOut);

    END_QCALL;

    return result;
}

/*********************************************************************/
extern "C" void QCALLTYPE Monitor_Exit_Slowpath(QCall::ObjectHandleOnStack objHandle, AwareLock::LeaveHelperAction exitBehavior)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    if (exitBehavior != AwareLock::LeaveHelperAction::Signal)
    {
        if (!objHandle.Get()->LeaveObjMonitor())
            COMPlusThrow(kSynchronizationLockException);
    }
    else
    {
        // Signal the event
        SyncBlock *psb = objHandle.Get()->PassiveGetSyncBlock();
        if (psb != NULL)
            psb->QuickGetMonitor()->Signal();
    }
    END_QCALL;
}

#include <optsmallperfcritical.h>
FCIMPL1(AwareLock::LeaveHelperAction, ObjectNative::Monitor_Exit_FastPath, Object* obj)
{
    FCALL_CONTRACT;

    // Handle the simple case without erecting helper frame
    return obj->LeaveObjMonitorHelper(GetThread());
}
FCIMPLEND
#include <optdefault.h>

