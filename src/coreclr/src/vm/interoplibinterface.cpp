// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "interoplibinterface.h"

// Interop library exports/imports
#include <interoplib.h>
#include <interoplibimports.h>

#ifdef FEATURE_COMINTEROP

void QCALLTYPE ComWrappersNative::GetIUnknownImpl(
        _Out_ void** fpQueryInterface,
        _Out_ void** fpAddRef,
        _Out_ void** fpRelease)
{
    QCALL_CONTRACT;

    _ASSERTE(fpQueryInterface != nullptr);
    _ASSERTE(fpAddRef != nullptr);
    _ASSERTE(fpRelease != nullptr);

    BEGIN_QCALL;

    InteropLib::GetIUnknownImpl(fpQueryInterface, fpAddRef, fpRelease);

    END_QCALL;
}

void* QCALLTYPE ComWrappersNative::GetOrCreateComInterfaceForObject(
    _In_ QCall::ObjectHandleOnStack comWrappersImpl,
    _In_ QCall::ObjectHandleOnStack instance,
    _In_ INT32 flags)
{
    QCALL_CONTRACT;

    void* wrapper = nullptr;

    BEGIN_QCALL;

    END_QCALL;

    return wrapper;
}

void QCALLTYPE ComWrappersNative::GetOrCreateObjectForComInstance(
    _In_ QCall::ObjectHandleOnStack comWrappersImpl,
    _In_ void* externalComObject,
    _In_ INT32 flags,
    _Inout_ QCall::ObjectHandleOnStack retValue)
{
    QCALL_CONTRACT;

    _ASSERTE(externalComObject != nullptr);

    BEGIN_QCALL;

    END_QCALL;
}

void QCALLTYPE ComWrappersNative::RegisterForReferenceTrackerHost(
    _In_ QCall::ObjectHandleOnStack comWrappersImpl)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    END_QCALL;
}

#endif // FEATURE_COMINTEROP
