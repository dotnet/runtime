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

FCIMPL2(OBJECTHANDLE, DependentHandle::InternalAlloc, Object *target, Object *dependent)
{
    FCALL_CONTRACT;

    // Use slow path if profiler is tracking GC
    if (CORProfilerTrackGC())
        return NULL;

    return GetAppDomain()->GetHandleStore()->CreateDependentHandle(target, dependent);
}
FCIMPLEND

extern "C" OBJECTHANDLE QCALLTYPE DependentHandle_InternalAllocWithGCTransition(QCall::ObjectHandleOnStack target, QCall::ObjectHandleOnStack dependent)
{
    QCALL_CONTRACT;

    OBJECTHANDLE result = NULL;

    BEGIN_QCALL;

    GCX_COOP();
    result = GetAppDomain()->CreateDependentHandle(target.Get(), dependent.Get());

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

FCIMPL2(VOID, DependentHandle::InternalSetDependent, OBJECTHANDLE handle, Object *_dependent)
{
    FCALL_CONTRACT;

    _ASSERTE(handle != NULL);

    IGCHandleManager *mgr = GCHandleUtilities::GetGCHandleManager();
    mgr->SetDependentHandleSecondary(handle, _dependent);
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, DependentHandle::InternalFree, OBJECTHANDLE handle)
{
    FCALL_CONTRACT;

    _ASSERTE(handle != NULL);

    // Use slow path if profiler is tracking GC
    if (CORProfilerTrackGC())
        FC_RETURN_BOOL(false);

    DestroyDependentHandle(handle);
    FC_RETURN_BOOL(true);
}
FCIMPLEND

extern "C" void QCALLTYPE DependentHandle_InternalFreeWithGCTransition(OBJECTHANDLE handle)
{
    QCALL_CONTRACT;

    _ASSERTE(handle != NULL);

    BEGIN_QCALL;

    GCX_COOP();
    DestroyDependentHandle(handle);

    END_QCALL;
}
