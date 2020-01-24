// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"

// Interop library header
#include <interoplibimports.h>

#include "interoplibinterface.h"

namespace InteropLibImports
{
    void* MemAlloc(_In_ size_t sizeInBytes)
    {
        STANDARD_VM_CONTRACT;

        _ASSERTE(0 != sizeInBytes);
        return ::malloc(sizeInBytes);
    }

    void MemFree(_In_ void* mem)
    {
        STANDARD_VM_CONTRACT;

        ::free(mem);
    }
}

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

    // Switch to COOP mode to check if the object already
    // has a wrapper in its syncblock.
    {
        GCX_COOP();

        struct
        {
            OBJECTREF implRef;
            OBJECTREF instRef;
        } gc;
        ::ZeroMemory(&gc, sizeof(gc));

        GCPROTECT_BEGIN(gc);

        gc.implRef = ObjectToOBJECTREF(*comWrappersImpl.m_ppObject);
        gc.instRef = ObjectToOBJECTREF(*instance.m_ppObject);
        _ASSERTE(gc.implRef != NULL && gc.instRef != NULL);


        // If it does, then return the
        // existing wrapper. If the object doesn't, then compute the
        // VTables for the type.
        GCPROTECT_END();
    }

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

    const HandleType implHandleType{ HNDTYPE_STRONG };
    OBJECTHANDLE implHandle;

    BEGIN_QCALL;

    // Enter cooperative mode to create the handle and store it
    // for future use in the reference tracker host scenario.
    {
        GCX_COOP();

        OBJECTREF implRef = NULL;
        GCPROTECT_BEGIN(implRef);

        implRef = ObjectToOBJECTREF(*comWrappersImpl.m_ppObject);
        _ASSERTE(implRef != NULL);

        implHandle = GetAppDomain()->CreateTypedHandle(implRef, implHandleType);

        if (!InteropLib::RegisterReferenceTrackerHostCallback(implHandle))
        {
            DestroyHandleCommon(implHandle, implHandleType);
            COMPlusThrow(kInvalidOperationException, IDS_EE_RESET_REFERENCETRACKERHOST_CALLBACKS);
        }

        GCPROTECT_END();
    }

    END_QCALL;
}

#endif // FEATURE_COMINTEROP
