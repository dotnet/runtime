// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_COMWRAPPERS

// Runtime headers
#include "common.h"
#include "simplerwlock.hpp"
#include "rcwrefcache.h"
#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
#include "olecontexthelpers.h"
#endif
#include "finalizerthread.h"

// Interop library header
#include <interoplibabi.h>
#include <interoplibimports.h>

#include "interoplibinterface.h"

using CreateObjectFlags = InteropLib::Com::CreateObjectFlags;

namespace
{
    int CallICustomQueryInterface(
        _In_ OBJECTREF* implPROTECTED,
        _In_ REFGUID iid,
        _Outptr_result_maybenull_ void** ppObject)
    {
        CONTRACTL
        {
            THROWS;
            MODE_COOPERATIVE;
            PRECONDITION(implPROTECTED != NULL);
            PRECONDITION(ppObject != NULL);
        }
        CONTRACTL_END;

        int result;

        PREPARE_NONVIRTUAL_CALLSITE(METHOD__COMWRAPPERS__CALL_ICUSTOMQUERYINTERFACE);
        DECLARE_ARGHOLDER_ARRAY(args, 3);
        args[ARGNUM_0]  = OBJECTREF_TO_ARGHOLDER(*implPROTECTED);
        args[ARGNUM_1]  = PTR_TO_ARGHOLDER(&iid);
        args[ARGNUM_2]  = PTR_TO_ARGHOLDER(ppObject);
        CALL_MANAGED_METHOD(result, int, args);

        return result;
    }

    BOOL g_isGlobalPeggingOn = TRUE;
}

extern "C" void QCALLTYPE ComWrappers_GetIUnknownImpl(
        _Out_ void** fpQueryInterface,
        _Out_ void** fpAddRef,
        _Out_ void** fpRelease)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;

    _ASSERTE(fpQueryInterface != NULL);
    _ASSERTE(fpAddRef != NULL);
    _ASSERTE(fpRelease != NULL);

    InteropLib::Com::GetIUnknownImpl(fpQueryInterface, fpAddRef, fpRelease);
}

void ComWrappersNative::MarkWrapperAsComActivated(_In_ IUnknown* wrapperMaybe)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(wrapperMaybe != NULL);
    }
    CONTRACTL_END;

    {
        GCX_PREEMP();
        // The IUnknown may or may not represent a wrapper, so E_INVALIDARG is okay here.
        HRESULT hr = InteropLib::Com::MarkComActivated(wrapperMaybe);
        _ASSERTE(SUCCEEDED(hr) || hr == E_INVALIDARG);
    }
}

bool GlobalComWrappersForMarshalling::TryGetOrCreateComInterfaceForObject(
    _In_ OBJECTREF instance,
    _Outptr_ void** wrapperRaw)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    void* wrapper;

    GCPROTECT_BEGIN(instance);

    PREPARE_NONVIRTUAL_CALLSITE(METHOD__COMWRAPPERS__GET_OR_CREATE_COM_INTERFACE_FOR_OBJECT_WITH_GLOBAL_MARSHALLING_INSTANCE);
    DECLARE_ARGHOLDER_ARRAY(args, 1);
    args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(instance);
    CALL_MANAGED_METHOD(wrapper, void*, args);

    GCPROTECT_END();

    *wrapperRaw = wrapper;

    return wrapper != nullptr;
}

bool GlobalComWrappersForMarshalling::TryGetOrCreateObjectForComInstance(
    _In_ IUnknown* externalComObject,
    _In_ INT32 objFromComIPFlags,
    _Out_ OBJECTREF* objRef)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // TrackerObject support and unwrapping matches the built-in semantics that the global marshalling scenario mimics.
    int flags = CreateObjectFlags::CreateObjectFlags_TrackerObject | CreateObjectFlags::CreateObjectFlags_Unwrap;
    if ((objFromComIPFlags & ObjFromComIP::UNIQUE_OBJECT) != 0)
        flags |= CreateObjectFlags::CreateObjectFlags_UniqueInstance;

    OBJECTREF obj = NULL;
    GCPROTECT_BEGIN(obj);

    PREPARE_NONVIRTUAL_CALLSITE(METHOD__COMWRAPPERS__GET_OR_CREATE_OBJECT_FOR_COM_INSTANCE_WITH_GLOBAL_MARSHALLING_INSTANCE);
    DECLARE_ARGHOLDER_ARRAY(args, 2);
    args[ARGNUM_0] = PTR_TO_ARGHOLDER(externalComObject);
    args[ARGNUM_1] = DWORD_TO_ARGHOLDER(flags);
    CALL_MANAGED_METHOD_RETREF(obj, OBJECTREF, args);

    GCPROTECT_END();

    *objRef = obj;
    return obj != NULL;
}

extern "C" void* QCALLTYPE ComWrappers_AllocateRefCountedHandle(_In_ QCall::ObjectHandleOnStack obj)
{
    QCALL_CONTRACT;

    void* handle = NULL;

    BEGIN_QCALL;

    {
        GCX_COOP();

        handle = GetAppDomain()->CreateTypedHandle(obj.Get(), HNDTYPE_REFCOUNTED);
    }

    END_QCALL;

    return handle;
}

extern "C" void const* QCALLTYPE ComWrappers_GetIReferenceTrackerTargetVftbl()
{
    QCALL_CONTRACT_NO_GC_TRANSITION;

    void const* vftbl = NULL;

    return InteropLib::Com::GetIReferenceTrackerTargetVftbl();
}

extern "C" void const* QCALLTYPE ComWrappers_GetTaggedImpl()
{
    QCALL_CONTRACT_NO_GC_TRANSITION;

    return InteropLib::Com::GetTaggedCurrentVersionImpl();
}

extern "C" CLR_BOOL QCALLTYPE TrackerObjectManager_HasReferenceTrackerManager()
{
    QCALL_CONTRACT_NO_GC_TRANSITION;

    return InteropLib::Com::HasReferenceTrackerManager();
}

extern "C" CLR_BOOL QCALLTYPE TrackerObjectManager_TryRegisterReferenceTrackerManager(_In_ void* manager)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;

    return InteropLib::Com::TryRegisterReferenceTrackerManager(manager);
}

OBJECTHANDLE GCHandleSetObject::Iterator::Current() const
{
    LIMITED_METHOD_CONTRACT;
    return _currentEntry->_value;
}

bool GCHandleSetObject::Iterator::MoveNext()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;

        // Should only be called during a GC suspension
        PRECONDITION(Debug_IsLockedViaThreadSuspension());
    }
    CONTRACTL_END;

    if (_currentEntry != NULL)
    {
        _currentEntry = _currentEntry->_next;
    }

    if (_currentEntry == NULL)
    {
        // Certain buckets might be empty, so loop until we find
        // one with an entry.
        while (++_currentIndex != (int32_t)_buckets->GetNumComponents())
        {
            _currentEntry = (HANDLESETENTRYREF)_buckets->GetAt(_currentIndex);
            if (_currentEntry != NULL)
            {
                return true;
            }
        }

        return false;
    }

    return true;
}

namespace InteropLibImports
{
    bool HasValidTarget(_In_ InteropLib::OBJECTHANDLE handle) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(handle != NULL);
        }
        CONTRACTL_END;

        ::OBJECTHANDLE objectHandle = static_cast<::OBJECTHANDLE>(handle);

        // A valid target is one that is not null.
        bool isNotNull = ObjectHandleIsNull(objectHandle) == FALSE;
        return isNotNull;
    }

    void DestroyHandle(_In_ InteropLib::OBJECTHANDLE handle) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(handle != NULL);
        }
        CONTRACTL_END;
        ::OBJECTHANDLE objectHandle = static_cast<::OBJECTHANDLE>(handle);

        DestroyRefcountedHandle(objectHandle);
    }

    bool GetGlobalPeggingState() noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        return (VolatileLoad(&g_isGlobalPeggingOn) != FALSE);
    }

    bool IsObjectPromoted(_In_ InteropLib::OBJECTHANDLE handle) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        if (handle == nullptr)
            return false;

        ::OBJECTHANDLE objectHandle = static_cast<::OBJECTHANDLE>(handle);

        OBJECTREF obj = ObjectFromHandle(objectHandle);

        if (obj == nullptr)
            return false;

        return GCHeapUtilities::GetGCHeap()->IsPromoted(OBJECTREFToObject(obj));
    }

    void SetGlobalPeggingState(_In_ bool state) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        BOOL newState = state ? TRUE : FALSE;
        VolatileStore(&g_isGlobalPeggingOn, newState);
    }

    TryInvokeICustomQueryInterfaceResult TryInvokeICustomQueryInterface(
        _In_ InteropLib::OBJECTHANDLE handle,
        _In_ REFGUID iid,
        _Outptr_result_maybenull_ void** obj) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_ANY;
            PRECONDITION(handle != NULL);
            PRECONDITION(obj != NULL);
        }
        CONTRACTL_END;

        *obj = NULL;

        // If this is a GC thread, then someone is trying to query for something
        // at a time when we can't run managed code.
        if (IsGCThread())
            return TryInvokeICustomQueryInterfaceResult::OnGCThread;

        // Ideally the BEGIN_EXTERNAL_ENTRYPOINT/END_EXTERNAL_ENTRYPOINT pairs
        // would be used here. However, this code path can be entered from within
        // and from outside the runtime.
        MAKE_CURRENT_THREAD_AVAILABLE_EX(GetThreadNULLOk());
        if (CURRENT_THREAD == NULL)
        {
            CURRENT_THREAD = SetupThreadNoThrow();

            // If we failed to set up a new thread, we are going to indicate
            // there was a general failure to invoke instead of failing fast.
            if (CURRENT_THREAD == NULL)
                return TryInvokeICustomQueryInterfaceResult::FailedToInvoke;
        }

        HRESULT hr;
        auto result = TryInvokeICustomQueryInterfaceResult::FailedToInvoke;
        EX_TRY_THREAD(CURRENT_THREAD)
        {
            // Switch to Cooperative mode since object references
            // are being manipulated.
            GCX_COOP();

            struct
            {
                OBJECTREF objRef;
            } gc;
            gc.objRef = NULL;
            GCPROTECT_BEGIN(gc);

            // Get the target of the external object's reference.
            ::OBJECTHANDLE objectHandle = static_cast<::OBJECTHANDLE>(handle);
            gc.objRef = ObjectFromHandle(objectHandle);

            result = (TryInvokeICustomQueryInterfaceResult)CallICustomQueryInterface(&gc.objRef, iid, obj);

            GCPROTECT_END();
        }
        EX_CATCH_HRESULT(hr);

        // Assert valid value.
        _ASSERTE(TryInvokeICustomQueryInterfaceResult::Min <= result
            && result <= TryInvokeICustomQueryInterfaceResult::Max);

        return result;
    }

    struct RuntimeCallContext
    {
        RCWRefCache* RefCache;
        GCHandleSetObject::Iterator _iterator;
    };

    bool IteratorNext(
        _In_ RuntimeCallContext* runtimeContext,
        _Outptr_result_maybenull_ void** referenceTracker,
        _Outptr_result_maybenull_ InteropLib::OBJECTHANDLE* proxyHandle) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            PRECONDITION(runtimeContext != NULL);
            PRECONDITION(referenceTracker != NULL);
            PRECONDITION(proxyHandle != NULL);

            // Should only be called during a GC suspension
            PRECONDITION(Debug_IsLockedViaThreadSuspension());
        }
        CONTRACTL_END;

        if (!runtimeContext->_iterator.MoveNext())
        {
            *referenceTracker = NULL;
            *proxyHandle = NULL;
            return false;
        }
        OBJECTHANDLE nativeObjectWrapperHandle = runtimeContext->_iterator.Current();
        REFTRACKEROBJECTWRAPPERREF nativeObjectWrapper = (REFTRACKEROBJECTWRAPPERREF)ObjectFromHandle(nativeObjectWrapperHandle);

        if (nativeObjectWrapper == NULL)
        {
            *referenceTracker = NULL;
            *proxyHandle = NULL;
            return true;
        }

        *referenceTracker = dac_cast<PTR_VOID>(nativeObjectWrapper->GetTrackerObject());
        *proxyHandle = static_cast<InteropLib::OBJECTHANDLE>(nativeObjectWrapper->GetProxyHandle());
        return true;
    }

    HRESULT FoundReferencePath(
        _In_ RuntimeCallContext* runtimeContext,
        _In_ InteropLib::OBJECTHANDLE sourceHandle,
        _In_ InteropLib::OBJECTHANDLE targetHandle) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            PRECONDITION(runtimeContext != NULL);
            PRECONDITION(sourceHandle != NULL);
            PRECONDITION(targetHandle != NULL);

            // Should only be called during a GC suspension
            PRECONDITION(Debug_IsLockedViaThreadSuspension());
        }
        CONTRACTL_END;

        // Get the external object's managed wrapper
        ::OBJECTHANDLE srcHandle = static_cast<::OBJECTHANDLE>(sourceHandle);
        OBJECTREF source = ObjectFromHandle(srcHandle);

        // Get the target of the external object's reference.
        ::OBJECTHANDLE tgtHandle = static_cast<::OBJECTHANDLE>(targetHandle);
        MOWHOLDERREF holder = (MOWHOLDERREF)ObjectFromHandle(tgtHandle);

        // Return if the holder has been collected
        if (holder == NULL)
        {
            return S_FALSE;
        }

        OBJECTREF target = holder->_wrappedObject;

        // Return if these are the same object.
        if (source == target)
        {
            return S_FALSE;
        }

        STRESS_LOG2(LF_INTEROP, LL_INFO1000, "Found reference path: 0x%p => 0x%p\n",
            OBJECTREFToObject(source),
            OBJECTREFToObject(target));
        return runtimeContext->RefCache->AddReferenceFromObjectToObject(source, target);
    }
}

namespace
{
    MethodTable* s_pManagedObjectWrapperHolderMT  = NULL;
}

bool ComWrappersNative::IsManagedObjectComWrapper(_In_ OBJECTREF managedObjectWrapperHolderRef, _Out_ bool* pIsRooted)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (managedObjectWrapperHolderRef->GetGCSafeMethodTable() != s_pManagedObjectWrapperHolderMT )
    {
        return false;
    }

    MOWHOLDERREF holder = (MOWHOLDERREF)managedObjectWrapperHolderRef;

    *pIsRooted = InteropLib::Com::IsRooted(holder->_wrapper);

    return true;
}

namespace
{
    OBJECTHANDLE NativeObjectWrapperCacheHandle = NULL;
    RCWRefCache* pAppDomainRCWRefCache = NULL;
}

void ComWrappersNative::OnFullGCStarted()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_COOPERATIVE;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // If no cache exists, then there is nothing to do here.
    if (NativeObjectWrapperCacheHandle == NULL)
        return;

    HANDLESETREF handleSet = (HANDLESETREF)ObjectFromHandle(NativeObjectWrapperCacheHandle);
    if (handleSet == NULL)
        return;

    STRESS_LOG0(LF_INTEROP, LL_INFO10000, "Begin Reference Tracking\n");
    RCWRefCache* refCache = pAppDomainRCWRefCache;

    // Reset the ref cache
    refCache->ResetDependentHandles();

    // Create a call context for the InteropLib.
    InteropLibImports::RuntimeCallContext cxt{refCache, GCHandleSetObject::Iterator{ handleSet }};
    (void)InteropLib::Com::BeginExternalObjectReferenceTracking(&cxt);

    // Shrink cache and clear unused handles.
    refCache->ShrinkDependentHandles();
}

void ComWrappersNative::OnFullGCFinished()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_COOPERATIVE;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (NativeObjectWrapperCacheHandle == NULL || ObjectFromHandle(NativeObjectWrapperCacheHandle) == NULL)
        return;

    (void)InteropLib::Com::EndExternalObjectReferenceTracking();
    STRESS_LOG0(LF_INTEROP, LL_INFO10000, "End Reference Tracking\n");
}

void ComWrappersNative::OnAfterGCScanRoots()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_COOPERATIVE;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (NativeObjectWrapperCacheHandle == NULL)
        return;

    HANDLESETREF handleSet = (HANDLESETREF)ObjectFromHandle(NativeObjectWrapperCacheHandle);
    if (handleSet == NULL)
        return;

    STRESS_LOG0(LF_INTEROP, LL_INFO10000, "Detach Non-promoted object from the Reference Tracker\n");

    // Create a call context for the InteropLib.
    InteropLibImports::RuntimeCallContext cxt{pAppDomainRCWRefCache, GCHandleSetObject::Iterator{ handleSet }};

    (void)InteropLib::Com::DetachNonPromotedObjects(&cxt);
}

extern "C" void QCALLTYPE ComWrappers_RegisterIsRootedCallback()
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    // Grab the method table for the objects we inspect in our ref-counted handle callback.
    s_pManagedObjectWrapperHolderMT  = CoreLibBinder::GetClass(CLASS__MANAGED_OBJECT_WRAPPER_HOLDER);

    END_QCALL;
}

extern "C" void QCALLTYPE TrackerObjectManager_RegisterNativeObjectWrapperCache(_In_ QCall::ObjectHandleOnStack cache)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();
    NativeObjectWrapperCacheHandle = GetAppDomain()->CreateHandle(cache.Get());
    // Fetch the RCWRefCache here so we don't try to allocate it during GC.
    pAppDomainRCWRefCache = GetAppDomain()->GetRCWRefCache();

    END_QCALL;
}

extern "C" CLR_BOOL QCALLTYPE TrackerObjectManager_IsGlobalPeggingEnabled()
{
    QCALL_CONTRACT_NO_GC_TRANSITION;

    return InteropLibImports::GetGlobalPeggingState();
}

// Some of our "Untracked" COM objects may be owned by static globals
// in client code. We need to ensure that we don't try executing a managed
// method for the first time when the process is shutting down.
// Therefore, we need to provide unmanaged implementations of AddRef and Release.
namespace
{
    int STDMETHODCALLTYPE Untracked_AddRefRelease(void*)
    {
        return 1;
    }
}

extern "C" void* QCALLTYPE ComWrappers_GetUntrackedAddRefRelease()
{
    QCALL_CONTRACT_NO_GC_TRANSITION;

    return (void*)Untracked_AddRefRelease;
}

#endif // FEATURE_COMWRAPPERS
