// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "platform.h"
#include <interoplib.h>
#include <interoplibimports.h>

#ifdef FEATURE_COMWRAPPERS
#include "comwrappers.hpp"
#endif // FEATURE_COMWRAPPERS

using OBJECTHANDLE = InteropLib::OBJECTHANDLE;
using RuntimeCallContext = InteropLibImports::RuntimeCallContext;

namespace InteropLib
{
#ifdef FEATURE_COMWRAPPERS
    // Exposed COM related API
    namespace Com
    {
        HRESULT CreateWrapperForObject(
            _In_ OBJECTHANDLE instance,
            _In_ INT32 vtableCount,
            _In_ void* vtablesRaw,
            _In_ enum CreateComInterfaceFlags flags,
            _Outptr_ IUnknown** wrapper) noexcept
        {
            _ASSERTE(instance != nullptr && wrapper != nullptr);

            // Validate the supplied vtable data is valid with a
            // reasonable count.
            if ((vtablesRaw == nullptr && vtableCount != 0) || vtableCount < 0)
                return E_INVALIDARG;

            HRESULT hr;

            // Convert input to appropriate types.
            auto vtables = static_cast<ABI::ComInterfaceEntry*>(vtablesRaw);

            ManagedObjectWrapper* mow;
            RETURN_IF_FAILED(ManagedObjectWrapper::Create(flags, instance, vtableCount, vtables, &mow));

            *wrapper = static_cast<IUnknown*>(mow->As(IID_IUnknown));
            return S_OK;
        }

        void DestroyWrapperForObject(_In_ void* wrapperMaybe) noexcept
        {
            ManagedObjectWrapper* wrapper = ManagedObjectWrapper::MapFromIUnknown(static_cast<IUnknown*>(wrapperMaybe));

            // A caller should not be destroying a wrapper without knowing if the wrapper is valid.
            _ASSERTE(wrapper != nullptr);

            ManagedObjectWrapper::Destroy(wrapper);
        }

        HRESULT IsActiveWrapper(_In_ IUnknown* wrapperMaybe) noexcept
        {
            ManagedObjectWrapper* wrapper = ManagedObjectWrapper::MapFromIUnknown(wrapperMaybe);
            if (wrapper == nullptr)
                return E_INVALIDARG;

            ULONG count = wrapper->IsActiveAddRef();
            if (count == 1 || wrapper->Target == nullptr)
            {
                // The wrapper isn't active.
                (void)wrapper->Release();
                return S_FALSE;
            }

            return S_OK;
        }

        HRESULT ReactivateWrapper(_In_ IUnknown* wrapperMaybe, _In_ OBJECTHANDLE handle) noexcept
        {
            ManagedObjectWrapper* wrapper = ManagedObjectWrapper::MapFromIUnknown(wrapperMaybe);
            if (wrapper == nullptr || handle == nullptr)
                return E_INVALIDARG;

            // Take an AddRef() as an indication of ownership.
            (void)wrapper->AddRef();

            // If setting this object handle fails, then the race
            // was lost and we will cleanup the handle.
            if (!wrapper->TrySetObjectHandle(handle))
                InteropLibImports::DeleteObjectInstanceHandle(handle);

            return S_OK;
        }

        HRESULT GetObjectForWrapper(_In_ IUnknown* wrapper, _Outptr_result_maybenull_ OBJECTHANDLE* object) noexcept
        {
            if (object == nullptr)
                return E_POINTER;

            *object = nullptr;

            HRESULT hr = IsActiveWrapper(wrapper);
            if (hr != S_OK)
                return hr;

            ManagedObjectWrapper *mow = ManagedObjectWrapper::MapFromIUnknown(wrapper);
            _ASSERTE(mow != nullptr);

            *object = mow->Target;
            return S_OK;
        }

        HRESULT MarkComActivated(_In_ IUnknown* wrapperMaybe) noexcept
        {
            ManagedObjectWrapper* wrapper = ManagedObjectWrapper::MapFromIUnknown(wrapperMaybe);
            if (wrapper == nullptr)
                return E_INVALIDARG;

            wrapper->SetFlag(CreateComInterfaceFlagsEx::IsComActivated);
            return S_OK;
        }

        HRESULT IsComActivated(_In_ IUnknown* wrapperMaybe) noexcept
        {
            ManagedObjectWrapper* wrapper = ManagedObjectWrapper::MapFromIUnknown(wrapperMaybe);
            if (wrapper == nullptr)
                return E_INVALIDARG;

            return wrapper->IsSet(CreateComInterfaceFlagsEx::IsComActivated) ? S_OK : S_FALSE;
        }

        HRESULT CreateWrapperForExternal(
            _In_ IUnknown* external,
            _In_ enum CreateObjectFlags flags,
            _In_ size_t contextSize,
            _Out_ ExternalWrapperResult* result) noexcept
        {
            _ASSERTE(external != nullptr && result != nullptr);

            HRESULT hr;

            NativeObjectWrapperContext* wrapperContext;
            RETURN_IF_FAILED(NativeObjectWrapperContext::Create(external, flags, contextSize, &wrapperContext));

            result->Context = wrapperContext->GetRuntimeContext();
            result->FromTrackerRuntime = (wrapperContext->GetReferenceTracker() != nullptr);
            return S_OK;
        }

        void DestroyWrapperForExternal(_In_ void* contextMaybe) noexcept
        {
            NativeObjectWrapperContext* context = NativeObjectWrapperContext::MapFromRuntimeContext(contextMaybe);

            // A caller should not be destroying a context without knowing if the context is valid.
            _ASSERTE(context != nullptr);

            // Check if the tracker object manager should be informed prior to being destroyed.
            IReferenceTracker* trackerMaybe = context->GetReferenceTracker();
            if (trackerMaybe != nullptr)
            {
                // We only call this during a GC so ignore the failure as
                // there is no way we can handle it at this point.
                HRESULT hr = TrackerObjectManager::BeforeWrapperDestroyed(trackerMaybe);
                _ASSERTE(SUCCEEDED(hr));
                (void)hr;
            }

            NativeObjectWrapperContext::Destroy(context);
        }

        void SeparateWrapperFromTrackerRuntime(_In_ void* contextMaybe) noexcept
        {
            NativeObjectWrapperContext* context = NativeObjectWrapperContext::MapFromRuntimeContext(contextMaybe);

            // A caller should not be separating a context without knowing if the context is valid.
            _ASSERTE(context != nullptr);

            context->DisconnectTracker();
        }

        void GetIUnknownImpl(
            _Out_ void** fpQueryInterface,
            _Out_ void** fpAddRef,
            _Out_ void** fpRelease) noexcept
        {
            ManagedObjectWrapper::GetIUnknownImpl(fpQueryInterface, fpAddRef, fpRelease);
        }

        HRESULT BeginExternalObjectReferenceTracking(_In_ RuntimeCallContext* cxt) noexcept
        {
            return TrackerObjectManager::BeginReferenceTracking(cxt);
        }

        HRESULT EndExternalObjectReferenceTracking() noexcept
        {
            return TrackerObjectManager::EndReferenceTracking();
        }
    }

#endif // FEATURE_COMWRAPPERS
}
