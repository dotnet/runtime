// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: COMDependentHandle.cpp
//

//
// FCalls and QCalls for the DependentHandle class
//


#include "common.h"
#include "comdependenthandle.h"

FCIMPL3(OBJECTHANDLE, DependentHandle::InternalAlloc, CLR_BOOL isDeferFinalize, Object *target, Object *dependent)
{
    FCALL_CONTRACT;

    // Use slow path if profiler is tracking GC
    if (CORProfilerTrackGC())
        return NULL;

    return GetAppDomain()->GetHandleStore()->CreateDependentHandle(
        isDeferFinalize ? HNDTYPE_DEPENDENT_DEFER_FINALIZE : HNDTYPE_DEPENDENT,
        target, dependent);
}
FCIMPLEND

extern "C" OBJECTHANDLE QCALLTYPE DependentHandle_InternalAllocWithGCTransition(BOOL isDeferFinalize, QCall::ObjectHandleOnStack target, QCall::ObjectHandleOnStack dependent)
{
    QCALL_CONTRACT;

    OBJECTHANDLE result = NULL;

    BEGIN_QCALL;

    GCX_COOP();
    result = isDeferFinalize ?
             GetAppDomain()->CreateDependentHandleDeferFinalize(target.Get(), dependent.Get()) :
             GetAppDomain()->CreateDependentHandle(target.Get(), dependent.Get());

    END_QCALL;

    return result;
}

FCIMPL1(Object*, DependentHandle::InternalGetTarget, OBJECTHANDLE handle)
{
    FCALL_CONTRACT;
    FCUnique(0x54);

    _ASSERTE(handle != NULL);

    return OBJECTREFToObject(ObjectFromHandle(handle));
}
FCIMPLEND

FCIMPL1(Object*, DependentHandle::InternalGetDependent, OBJECTHANDLE handle)
{
    FCALL_CONTRACT;

    _ASSERTE(handle != NULL);

    OBJECTREF target = ObjectFromHandle(handle);

    IGCHandleManager *mgr = GCHandleUtilities::GetGCHandleManager();

    // The dependent is tracked only if target is non-null
    return (target != NULL) ? mgr->GetDependentHandleSecondary(handle) : NULL;
}
FCIMPLEND

FCIMPL2(Object*, DependentHandle::InternalGetTargetAndDependent, OBJECTHANDLE handle, Object **outDependent)
{
    FCALL_CONTRACT;

    _ASSERTE(handle != NULL && outDependent != NULL);

    OBJECTREF target = ObjectFromHandle(handle);
    IGCHandleManager *mgr = GCHandleUtilities::GetGCHandleManager();

    // The dependent is tracked only if target is non-null
    *outDependent = (target != NULL) ? mgr->GetDependentHandleSecondary(handle) : NULL;

    return OBJECTREFToObject(target);
}
FCIMPLEND

FCIMPL1(VOID, DependentHandle::InternalSetTargetToNull, OBJECTHANDLE handle)
{
    FCALL_CONTRACT;

    _ASSERTE(handle != NULL);

    IGCHandleManager *mgr = GCHandleUtilities::GetGCHandleManager();
    mgr->StoreObjectInHandle(handle, NULL);
}
FCIMPLEND

FCIMPL3(VOID, DependentHandle::InternalSetDependent, CLR_BOOL isDeferFinalize, OBJECTHANDLE handle, Object *_dependent)
{
    FCALL_CONTRACT;

    _ASSERTE(handle != NULL);

    IGCHandleManager *mgr = GCHandleUtilities::GetGCHandleManager();
    mgr->SetDependentHandleSecondary(isDeferFinalize ? HNDTYPE_DEPENDENT_DEFER_FINALIZE : HNDTYPE_DEPENDENT, handle, _dependent);
}
FCIMPLEND

FCIMPL2(FC_BOOL_RET, DependentHandle::InternalFree, CLR_BOOL isDeferFinalize, OBJECTHANDLE handle)
{
    FCALL_CONTRACT;

    _ASSERTE(handle != NULL);

    // Use slow path if profiler is tracking GC
    if (CORProfilerTrackGC())
        FC_RETURN_BOOL(false);

    if (isDeferFinalize)
    {
        DestroyDependentDeferFinalizeHandle(handle);
    }
    else
    {
        DestroyDependentHandle(handle);
    }

    FC_RETURN_BOOL(true);
}
FCIMPLEND

extern "C" void QCALLTYPE DependentHandle_InternalFreeWithGCTransition(BOOL isDeferFinalize, OBJECTHANDLE handle)
{
    QCALL_CONTRACT;

    _ASSERTE(handle != NULL);

    BEGIN_QCALL;

    GCX_COOP();
    if (isDeferFinalize)
    {
        DestroyDependentDeferFinalizeHandle(handle);
    }
    else
    {
        DestroyDependentHandle(handle);
    }

    END_QCALL;
}
