// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "platform.h"
#include <interoplib.h>
#include <interoplibabi.h>
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
        HRESULT MarkComActivated(_In_ IUnknown* wrapperMaybe) noexcept
        {
            ManagedObjectWrapper* wrapper = ManagedObjectWrapper::MapFromIUnknownWithQueryInterface(wrapperMaybe);
            if (wrapper == nullptr)
                return E_INVALIDARG;

            wrapper->SetFlag(CreateComInterfaceFlagsEx::IsComActivated);
            return S_OK;
        }

        void GetIUnknownImpl(
            _Out_ void** fpQueryInterface,
            _Out_ void** fpAddRef,
            _Out_ void** fpRelease) noexcept
        {
            ManagedObjectWrapper::GetIUnknownImpl(fpQueryInterface, fpAddRef, fpRelease);
        }

        void const* GetTaggedCurrentVersionImpl() noexcept
        {
            return ManagedObjectWrapper::GetTaggedCurrentVersionImpl();
        }

        HRESULT BeginExternalObjectReferenceTracking(_In_ RuntimeCallContext* cxt) noexcept
        {
            return TrackerObjectManager::BeginReferenceTracking(cxt);
        }

        HRESULT EndExternalObjectReferenceTracking() noexcept
        {
            return TrackerObjectManager::EndReferenceTracking();
        }

        HRESULT DetachNonPromotedObjects(_In_ RuntimeCallContext* cxt) noexcept
        {
            return TrackerObjectManager::DetachNonPromotedObjects(cxt);
        }

        void const* GetIReferenceTrackerTargetVftbl() noexcept
        {
            return ManagedObjectWrapper::GetIReferenceTrackerTargetImpl();
        }

        bool HasReferenceTrackerManager() noexcept
        {
            return TrackerObjectManager::HasReferenceTrackerManager();
        }

        bool TryRegisterReferenceTrackerManager(_In_ void* manager) noexcept
        {
            return TrackerObjectManager::TryRegisterReferenceTrackerManager((IReferenceTrackerManager*)manager);
        }

        bool IsRooted(InteropLib::ABI::ManagedObjectWrapperLayout* mow) noexcept
        {
            return reinterpret_cast<ManagedObjectWrapper*>(mow)->IsRooted();
        }
    }

#endif // FEATURE_COMWRAPPERS
}
