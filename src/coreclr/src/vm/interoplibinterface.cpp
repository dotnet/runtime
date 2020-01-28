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
    const HandleType InstanceHandleType{ HNDTYPE_STRONG };

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

        struct
        {
            OBJECTREF implRef;
            OBJECTREF instRef;
        } gc;
        ::ZeroMemory(&gc, sizeof(gc));
        GCPROTECT_BEGIN(gc);

        gc.implRef = impl;
        gc.instRef = instance;

        PREPARE_NONVIRTUAL_CALLSITE(METHOD__COMWRAPPERS__COMPUTE_VTABLES);
        DECLARE_ARGHOLDER_ARRAY(args, 4);
        args[ARGNUM_0]  = OBJECTREF_TO_ARGHOLDER(gc.implRef);
        args[ARGNUM_1]  = OBJECTREF_TO_ARGHOLDER(gc.instRef);
        args[ARGNUM_2]  = DWORD_TO_ARGHOLDER(flags);
        args[ARGNUM_3]  = PTR_TO_ARGHOLDER(vtableCount);
        CALL_MANAGED_METHOD(vtables, void*, args);

        GCPROTECT_END();

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

        OBJECTREF retObjRef;

        struct
        {
            OBJECTREF implRef;
        } gc;
        ::ZeroMemory(&gc, sizeof(gc));
        GCPROTECT_BEGIN(gc);

        gc.implRef = impl;

        PREPARE_NONVIRTUAL_CALLSITE(METHOD__COMWRAPPERS__CREATE_OBJECT);
        DECLARE_ARGHOLDER_ARRAY(args, 3);
        args[ARGNUM_0]  = OBJECTREF_TO_ARGHOLDER(gc.implRef);
        args[ARGNUM_1]  = PTR_TO_ARGHOLDER(externalComObject);
        args[ARGNUM_2]  = DWORD_TO_ARGHOLDER(flags);
        CALL_MANAGED_METHOD(retObjRef, OBJECTREF, args);

        GCPROTECT_END();

        return retObjRef;
    }
}

namespace InteropLibImports
{
    void* MemAlloc(_In_ size_t sizeInBytes, _In_ AllocScenario scenario)
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_ANY;
            PRECONDITION(sizeInBytes != 0);
        }
        CONTRACTL_END;

        return ::malloc(sizeInBytes);
    }

    void MemFree(_In_ void* mem, _In_ AllocScenario scenario)
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_ANY;
            PRECONDITION(mem != NULL);
        }
        CONTRACTL_END;

        ::free(mem);
    }

    void DeleteObjectInstanceHandle(_In_ InteropLib::OBJECTHANDLE handle)
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_PREEMPTIVE;
            PRECONDITION(handle != NULL);
        }
        CONTRACTL_END;

        ::OBJECTHANDLE objectHandle = static_cast<::OBJECTHANDLE>(handle);
        DestroyHandleCommon(objectHandle, InstanceHandleType);
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
            NOTHROW;
            MODE_PREEMPTIVE;
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

void* QCALLTYPE ComWrappersNative::GetOrCreateComInterfaceForObject(
    _In_ QCall::ObjectHandleOnStack comWrappersImpl,
    _In_ QCall::ObjectHandleOnStack instance,
    _In_ INT32 flags)
{
    QCALL_CONTRACT;

    HRESULT hr;

    SafeComHolder<IUnknown> newWrapper;
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

        gc.instRef = ObjectToOBJECTREF(*instance.m_ppObject);
        _ASSERTE(gc.instRef != NULL);

        // Check the object's SyncBlock for a managed object wrapper.
        SyncBlock* syncBlock = gc.instRef->GetSyncBlock();
        InteropSyncBlockInfo* interopInfo = syncBlock->GetInteropInfo();

        // Query the associated InteropSyncBlockInfo for an existing managed object wrapper.
        if (!interopInfo->TryGetManagedObjectComWrapper(&wrapper))
        {
            // Get the supplied COM Wrappers implementation to request VTable computation.
            gc.implRef = ObjectToOBJECTREF(*comWrappersImpl.m_ppObject);
            _ASSERTE(gc.implRef != NULL);

            // Compute VTables for the new existing COM object
            //
            // N.B. Calling to compute the associated VTables is perhaps early since no lock
            // is taken. However, a key assumption here is that the returned memory will be
            // idempotent for the same object.
            DWORD vtableCount;
            void* vtables = CallComputeVTables(gc.implRef, gc.instRef, flags, &vtableCount);

            // Re-query the associated InteropSyncBlockInfo for an existing managed object wrapper.
            if (!interopInfo->TryGetManagedObjectComWrapper(&wrapper))
            {
                OBJECTHANDLE instHandle = GetAppDomain()->CreateTypedHandle(gc.instRef, InstanceHandleType);

                // Call the InteropLib and create the associated managed object wrapper.
                hr = InteropLib::CreateComInterfaceForObject(instHandle, vtableCount, vtables, flags, &newWrapper);
                if (FAILED(hr))
                {
                    DestroyHandleCommon(instHandle, InstanceHandleType);
                    COMPlusThrowHR(hr);
                }
                _ASSERTE(!newWrapper.IsNull());

                // Try setting the newly created managed object wrapper on the InteropSyncBlockInfo.
                if (!interopInfo->TrySetManagedObjectComWrapper(newWrapper))
                {
                    // If the managed object wrapper couldn't be set, then
                    // it should be possible to get the current one.
                    if (!interopInfo->TryGetManagedObjectComWrapper(&wrapper))
                        UNREACHABLE();
                }
            }
        }

        // Determine what to return.
        if (!newWrapper.IsNull())
        {
            // A new managed object wrapper was created, remove the object from the holder.
            // No AddRef() here since the wrapper should be created with a reference.
            wrapper = newWrapper.Extract();
        }
        else
        {
            // An existing wrapper should have an AddRef() performed.
            _ASSERTE(wrapper != NULL);
            (void)static_cast<IUnknown *>(wrapper)->AddRef();
        }

        GCPROTECT_END();
    }

    END_QCALL;

    _ASSERTE(wrapper != NULL);
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
    SafeComHolder<IUnknown> identity;
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

        gc.newObjRef = CallGetObject(gc.implRef, identity, flags);

        // Set the return value
        retValue.Set(gc.newObjRef);

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

#endif // FEATURE_COMINTEROP
