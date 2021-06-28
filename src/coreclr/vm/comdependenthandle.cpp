// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: COMDependentHandle.cpp
//

//
// FCall's for the DependentHandle class
//
// Handle functions require cooperative mode, making these fcalls poor candidates for QCall conversion.
//


#include "common.h"
#include "comdependenthandle.h"

FCIMPL2(OBJECTHANDLE, DependentHandle::InternalInitialize, Object *_target, Object *_dependent)
{
    FCALL_CONTRACT;

    OBJECTREF target(_target);
    OBJECTREF dependent(_dependent);
    OBJECTHANDLE result = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();

    // Create the handle.
    result = GetAppDomain()->CreateDependentHandle(target, dependent);

    HELPER_METHOD_FRAME_END_POLL();

    return result;
}
FCIMPLEND

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

FCIMPL1(VOID, DependentHandle::InternalFree, OBJECTHANDLE handle)
{
    FCALL_CONTRACT;

    _ASSERTE(handle != NULL);

    HELPER_METHOD_FRAME_BEGIN_0();

    DestroyDependentHandle(handle);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
