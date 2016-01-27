// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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



FCIMPL3(VOID, DependentHandle::nInitialize, Object *_primary, Object *_secondary, OBJECTHANDLE *outHandle)
{
    FCALL_CONTRACT;

    _ASSERTE(outHandle != NULL && *outHandle == NULL);  // Multiple initializations disallowed 

    OBJECTREF primary(_primary);
    OBJECTREF secondary(_secondary);

    HELPER_METHOD_FRAME_BEGIN_NOPOLL();
    
    // Create the handle.
    *outHandle = GetAppDomain()->CreateDependentHandle(primary, secondary);

    HELPER_METHOD_FRAME_END_POLL();

}
FCIMPLEND



FCIMPL1(VOID, DependentHandle::nFree, OBJECTHANDLE handle)
{
    FCALL_CONTRACT;

    _ASSERTE(handle != NULL);
    
    HELPER_METHOD_FRAME_BEGIN_0();

    DestroyDependentHandle(handle);

    HELPER_METHOD_FRAME_END();

}
FCIMPLEND



FCIMPL2(VOID, DependentHandle::nGetPrimary, OBJECTHANDLE handle, Object **outPrimary)
{
    FCALL_CONTRACT;
    _ASSERTE(handle != NULL && outPrimary != NULL);
    *outPrimary = OBJECTREFToObject(ObjectFromHandle(handle));
}
FCIMPLEND



FCIMPL3(VOID, DependentHandle::nGetPrimaryAndSecondary, OBJECTHANDLE handle, Object **outPrimary, Object **outSecondary)
{
    FCALL_CONTRACT;
    _ASSERTE(handle != NULL && outPrimary != NULL && outSecondary != NULL);
    *outPrimary = OBJECTREFToObject(ObjectFromHandle(handle));
    *outSecondary = OBJECTREFToObject(GetDependentHandleSecondary(handle));
}
FCIMPLEND

