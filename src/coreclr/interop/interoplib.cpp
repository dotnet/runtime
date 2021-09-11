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
            auto vtables = static_cast<::ABI::ComInterfaceEntry*>(vtablesRaw);

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

        HRESULT IsWrapperRooted(_In_ IUnknown* wrapperMaybe) noexcept
        {
            ManagedObjectWrapper* wrapper = ManagedObjectWrapper::MapFromIUnknown(wrapperMaybe);
            if (wrapper == nullptr)
                return E_INVALIDARG;

            return wrapper->IsRooted() ? S_OK : S_FALSE;
        }

        HRESULT GetObjectForWrapper(_In_ IUnknown* wrapper, _Outptr_result_maybenull_ OBJECTHANDLE* object) noexcept
        {
            _ASSERTE(wrapper != nullptr && object != nullptr);
            *object = nullptr;

            // Attempt to get the managed object wrapper.
            ManagedObjectWrapper *mow = ManagedObjectWrapper::MapFromIUnknown(wrapper);
            if (mow == nullptr)
                return E_INVALIDARG;

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

        HRESULT DetermineIdentityAndInnerForExternal(
            _In_ IUnknown* external,
            _In_ enum CreateObjectFlags flags,
            _Outptr_ IUnknown** identity,
            _Inout_ IUnknown** innerMaybe) noexcept
        {
            _ASSERTE(external != nullptr && identity != nullptr && innerMaybe != nullptr);

            IUnknown* checkForIdentity = external;

            // Check if the flags indicate we are creating
            // an object for an external IReferenceTracker instance
            // that we are aggregating with.
            bool refTrackerInnerScenario = (flags & CreateObjectFlags_TrackerObject)
                && (flags & CreateObjectFlags_Aggregated);

            ComHolder<IReferenceTracker> trackerObject;
            if (refTrackerInnerScenario)
            {
                // We are checking the supplied external value
                // for IReferenceTracker since in .NET 5 this could
                // actually be the inner and we want the true identity
                // not the inner . This is a trick since the only way
                // to get identity from an inner is through a non-IUnknown
                // interface QI. Once we have the IReferenceTracker
                // instance we can be sure the QI for IUnknown will really
                // be the true identity.
                HRESULT hr = external->QueryInterface(IID_IReferenceTracker, (void**)&trackerObject);
                if (SUCCEEDED(hr))
                    checkForIdentity = trackerObject.p;
            }

            HRESULT hr;

            IUnknown* identityLocal;
            RETURN_IF_FAILED(checkForIdentity->QueryInterface(IID_IUnknown, (void **)&identityLocal));

            // Set the inner if scenario dictates an update.
            if (*innerMaybe == nullptr          // User didn't supply inner - .NET 5 API scenario sanity check.
                && checkForIdentity != external // Target of check was changed - .NET 5 API scenario sanity check.
                && external != identityLocal    // The supplied object doesn't match the computed identity.
                && refTrackerInnerScenario)     // The appropriate flags were set.
            {
                *innerMaybe = external;
            }

            *identity = identityLocal;
            return S_OK;
        }

        HRESULT CreateWrapperForExternal(
            _In_ IUnknown* external,
            _In_opt_ IUnknown* inner,
            _In_ enum CreateObjectFlags flags,
            _In_ size_t contextSize,
            _Out_ ExternalWrapperResult* result) noexcept
        {
            _ASSERTE(external != nullptr && result != nullptr);

            HRESULT hr;

            NativeObjectWrapperContext* wrapperContext;
            RETURN_IF_FAILED(NativeObjectWrapperContext::Create(external, inner, flags, contextSize, &wrapperContext));

            result->Context = wrapperContext->GetRuntimeContext();
            result->FromTrackerRuntime = (wrapperContext->GetReferenceTracker() != nullptr);
            result->ManagedObjectWrapper = (ManagedObjectWrapper::MapFromIUnknown(external) != nullptr);
            return S_OK;
        }

        void NotifyWrapperForExternalIsBeingCollected(_In_ void* contextMaybe) noexcept
         {
             NativeObjectWrapperContext* context = NativeObjectWrapperContext::MapFromRuntimeContext(contextMaybe);

             // A caller should not be destroying a context without knowing if the context is valid.
             _ASSERTE(context != nullptr);

            // Check if the tracker object manager should be informed of collection.
            IReferenceTracker* trackerMaybe = context->GetReferenceTracker();
            if (trackerMaybe != nullptr)
            {
                // We only call this during a GC so ignore the failure as
                // there is no way we can handle it at this point.
                HRESULT hr = TrackerObjectManager::BeforeWrapperFinalized(trackerMaybe);
                _ASSERTE(SUCCEEDED(hr));
                (void)hr;
            }
        }

        void DestroyWrapperForExternal(_In_ void* contextMaybe, _In_ bool notifyIsBeingCollected) noexcept
        {
            NativeObjectWrapperContext* context = NativeObjectWrapperContext::MapFromRuntimeContext(contextMaybe);

            // A caller should not be destroying a context without knowing if the context is valid.
            _ASSERTE(context != nullptr);

            if (notifyIsBeingCollected)
                NotifyWrapperForExternalIsBeingCollected(contextMaybe);

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
