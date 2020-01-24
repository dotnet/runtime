// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"

// Interop library header
#include <interoplibimports.h>

#include "interoplibinterface.h"

// void ____()
// {
//     CONTRACTL
//     {
//         THROWS;
//         //NOTHROW;
//         MODE_COOPERATIVE;
//         // MODE_PREEMPTIVE;
//         // MODE_ANY;
//         // PRECONDITION(pSrc != NULL);
//     }
//     CONTRACTL_END;
// }

namespace
{
    void* CallComputeVTables(
        _In_ OBJECTREF impl,
        _In_ OBJECTREF instance,
        _In_ INT32 flags,
        _Out_ DWORD* vtableCount)
    {
        CONTRACTL
        {
            THROWS;
            MODE_COOPERATIVE;
            PRECONDITION(impl != NULL);
            PRECONDITION(instance != NULL);
            PRECONDITION(vtableCount != NULL);
        }
        CONTRACTL_END;

        void* vtables = NULL;

        PREPARE_NONVIRTUAL_CALLSITE(METHOD__COMWRAPPERS__COMPUTE_VTABLES);
        DECLARE_ARGHOLDER_ARRAY(args, 4);
        args[ARGNUM_0]  = OBJECTREF_TO_ARGHOLDER(impl);
        args[ARGNUM_1]  = OBJECTREF_TO_ARGHOLDER(instance);
        args[ARGNUM_2]  = DWORD_TO_ARGHOLDER(flags);
        args[ARGNUM_3]  = PTR_TO_ARGHOLDER(vtableCount);
        CALL_MANAGED_METHOD(vtables, void*, args);

        return vtables;
    }

    OBJECTREF CallGetObject(
        _In_ OBJECTREF impl,
        _In_ IUnknown* externalComObject,
        _In_ INT32 flags)
    {
        CONTRACTL
        {
            THROWS;
            MODE_COOPERATIVE;
            PRECONDITION(impl != NULL);
            PRECONDITION(externalComObject != NULL);
        }
        CONTRACTL_END;

        OBJECTREF retObjRef = NULL;

        PREPARE_NONVIRTUAL_CALLSITE(METHOD__COMWRAPPERS__CREATE_OBJECT);
        DECLARE_ARGHOLDER_ARRAY(args, 3);
        args[ARGNUM_0]  = OBJECTREF_TO_ARGHOLDER(impl);
        args[ARGNUM_1]  = PTR_TO_ARGHOLDER(externalComObject);
        args[ARGNUM_2]  = DWORD_TO_ARGHOLDER(flags);
        CALL_MANAGED_METHOD(retObjRef, OBJECTREF, args);

        return retObjRef;
    }
}

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

    HRESULT GetOrCreateTrackerTargetForExternal(
        _In_ InteropLib::OBJECTHANDLE impl,
        _In_ IUnknown* externalComObject,
        _In_ INT32 externalObjectFlags,
        _In_ INT32 trackerTargetFlags,
        _Outptr_ IUnknown** trackerTarget) noexcept
    {
        CONTRACTL
        {
            NOTHROWS;
            MODE_ANY;
            PRECONDITION(impl != NULL);
            PRECONDITION(externalComObject != NULL);
            PRECONDITION(trackerTarget != NULL);
        }
        CONTRACTL_END;

        ::OBJECTHANDLE implHandle = static_cast<::OBJECTHANDLE>(impl);

        {
            GCX_COOP();

            struct
            {
                OBJECTREF implRef;
                OBJECTREF newObjRef;
            } gc;
            ::ZeroMemory(&gc, sizeof(gc));
            GCPROTECT_BEGIN(gc);

            gc.implRef = ObjectFromHandle(implHandle);
            _ASSERTE(gc.implRef != NULL);

            //
            // Get wrapper for external object
            //

            //
            // Get wrapper for managed object
            //

            GCPROTECT_END();
        }

        return S_OK;
    }
}

#ifdef FEATURE_COMINTEROP

void QCALLTYPE ComWrappersNative::GetIUnknownImpl(
        _Out_ void** fpQueryInterface,
        _Out_ void** fpAddRef,
        _Out_ void** fpRelease)
{
    QCALL_CONTRACT;

    _ASSERTE(fpQueryInterface != NULL);
    _ASSERTE(fpAddRef != NULL);
    _ASSERTE(fpRelease != NULL);

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

    void* wrapper = NULL;

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
        _ASSERTE(gc.implRef != NULL);

        //
        // Check the objects SyncBlock for an existing COM object
        //

        gc.instRef = ObjectToOBJECTREF(*instance.m_ppObject);
        _ASSERTE(gc.instRef != NULL);

        //
        // Compute VTables for the new existing COM object
        //

        DWORD vtableCount;
        void* vtables = CallComputeVTables(gc.implRef, gc.instRef, flags, &vtableCount);

        //
        // 
        //

        GCPROTECT_END();
    }

    END_QCALL;

    return wrapper;
}

void QCALLTYPE ComWrappersNative::GetOrCreateObjectForComInstance(
    _In_ QCall::ObjectHandleOnStack comWrappersImpl,
    _In_ void* ext,
    _In_ INT32 flags,
    _Inout_ QCall::ObjectHandleOnStack retValue)
{
    QCALL_CONTRACT;

    _ASSERTE(ext != NULL);

    BEGIN_QCALL;

    HRESULT hr;
    IUnknown* externalComObject = reinterpret_cast<IUnknown*>(ext);

    // Determine the true identity of the object
    SafeComHolder<IUnknown> identity = NULL;
    hr = externalComObject->QueryInterface(IID_IUnknown, &identity);
    _ASSERTE(hr == S_OK);

    // Switch to COOP mode in order to check if the the InteropLib already
    // has an object for the external COM object or to create a new one.
    {
        GCX_COOP();

        struct
        {
            OBJECTREF implRef;
            OBJECTREF newObjRef;
        } gc;
        ::ZeroMemory(&gc, sizeof(gc));
        GCPROTECT_BEGIN(gc);

        gc.implRef = ObjectToOBJECTREF(*comWrappersImpl.m_ppObject);
        _ASSERTE(gc.implRef != NULL);

        gc.newObjectRef = CallGetObject(gc.implRef, identity, flags);

        // Set the return value
        retValue.Set(gc.newObjectRef);

        GCPROTECT_END();
    }

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
