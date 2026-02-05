// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "comwrappers.hpp"
#include <interoplibimports.h>

using OBJECTHANDLE = InteropLib::OBJECTHANDLE;
using RuntimeCallContext = InteropLibImports::RuntimeCallContext;

namespace
{
    // 04b3486c-4687-4229-8d14-505ab584dd88
    const GUID IID_IFindReferenceTargetsCallback = { 0x04b3486c, 0x4687, 0x4229, { 0x8d, 0x14, 0x50, 0x5a, 0xb5, 0x84, 0xdd, 0x88} };

    VolatilePtr<IReferenceTrackerManager> s_TrackerManager; // The one and only Tracker Manager instance
    Volatile<bool> s_HasTrackingStarted = false;

    // Indicates if walking the external objects is needed.
    // (i.e. Have any IReferenceTracker instances been found?)
    bool ShouldWalkExternalObjects()
    {
        return (s_TrackerManager != nullptr);
    }

    // Callback implementation of IFindReferenceTargetsCallback
    class FindDependentWrappersCallback final : public IFindReferenceTargetsCallback
    {
        OBJECTHANDLE _sourceHandle;
        RuntimeCallContext* _runtimeCallCxt;

    public:
        FindDependentWrappersCallback(_In_ OBJECTHANDLE sourceHandle, _In_ RuntimeCallContext* runtimeCallCxt)
            : _sourceHandle{ sourceHandle }
            , _runtimeCallCxt{ runtimeCallCxt }
        {
            _ASSERTE(_sourceHandle != nullptr && runtimeCallCxt != nullptr);
        }

        STDMETHOD(FoundTrackerTarget)(_In_ IReferenceTrackerTarget* target)
        {
            HRESULT hr;

            if (target == nullptr)
                return E_POINTER;

            ManagedObjectWrapper* mow = ManagedObjectWrapper::MapFromIUnknown(target);

            // Not a target we implemented or wrapper is marked to be destroyed.
            if (mow == nullptr || mow->IsMarkedToDestroy())
                return S_OK;

            // Notify the runtime a reference path was found.
            RETURN_IF_FAILED(InteropLibImports::FoundReferencePath(
                _runtimeCallCxt,
                _sourceHandle,
                mow->GetTarget()));

            return S_OK;
        }

        // Lifetime maintained by stack - we don't care about ref counts
        STDMETHOD_(ULONG, AddRef)() { return 1; }
        STDMETHOD_(ULONG, Release)() { return 1; }

        STDMETHOD(QueryInterface)(
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject)
        {
            if (ppvObject == nullptr)
                return E_POINTER;

            if (IsEqualIID(riid, IID_IFindReferenceTargetsCallback))
            {
                *ppvObject = static_cast<IFindReferenceTargetsCallback*>(this);
            }
            else if (IsEqualIID(riid, IID_IUnknown))
            {
                *ppvObject = static_cast<IUnknown*>(this);
            }
            else
            {
                *ppvObject = nullptr;
                return E_NOINTERFACE;
            }

            (void)AddRef();
            return S_OK;
        }
    };

    HRESULT WalkExternalTrackerObjects(_In_ RuntimeCallContext* cxt)
    {
        _ASSERTE(cxt != nullptr);

        bool walkFailed = false;
        HRESULT hr = S_OK;

        IReferenceTracker* trackerTarget = nullptr;
        OBJECTHANDLE proxyObject = nullptr;
        while (InteropLibImports::IteratorNext(cxt, (void**)&trackerTarget, &proxyObject))
        {
            if (trackerTarget == nullptr)
                continue;

            // Ask the tracker instance to find all reference targets.
            FindDependentWrappersCallback cb{ proxyObject, cxt };
            hr = trackerTarget->FindTrackerTargets(&cb);
            if (FAILED(hr))
                break;
        }

        if (FAILED(hr))
        {
            // Remember the fact that we've failed and stop walking
            walkFailed = true;
            InteropLibImports::SetGlobalPeggingState(true);
        }

        _ASSERTE(s_TrackerManager != nullptr);
        (void)s_TrackerManager->FindTrackerTargetsCompleted(walkFailed ? TRUE : FALSE);

        return hr;
    }
}

bool TrackerObjectManager::HasReferenceTrackerManager()
{
    return s_TrackerManager != nullptr;
}

bool TrackerObjectManager::TryRegisterReferenceTrackerManager(_In_ IReferenceTrackerManager* manager)
{
    _ASSERTE(manager != nullptr);
    return InterlockedCompareExchangePointer((void**)&s_TrackerManager, manager, nullptr) == nullptr;
}

HRESULT TrackerObjectManager::BeforeWrapperFinalized(_In_ IReferenceTracker* obj)
{
    _ASSERTE(obj != nullptr);

    HRESULT hr;

    // Notify tracker runtime that we are about to finalize a wrapper
    // (same timing as short weak handle) for this object.
    // They need this information to disconnect weak refs and stop firing events,
    // so that they can avoid resurrecting the object.
    RETURN_IF_FAILED(obj->DisconnectFromTrackerSource());

    return S_OK;
}

HRESULT TrackerObjectManager::BeginReferenceTracking(_In_ RuntimeCallContext* cxt)
{
    _ASSERTE(cxt != nullptr);

    if (!ShouldWalkExternalObjects())
        return S_FALSE;

    HRESULT hr;

    _ASSERTE(s_HasTrackingStarted == false);
    _ASSERTE(InteropLibImports::GetGlobalPeggingState());

    s_HasTrackingStarted = true;

    // Let the tracker runtime know we are about to walk external objects so that
    // they can lock their reference cache. Note that the tracker runtime doesn't need to
    // unpeg all external objects at this point and they can do the pegging/unpegging.
    // in FindTrackerTargetsCompleted.
    _ASSERTE(s_TrackerManager != nullptr);
    RETURN_IF_FAILED(s_TrackerManager->ReferenceTrackingStarted());

    // From this point, the tracker runtime decides whether a target
    // should be pegged or not as the global pegging flag is now off.
    InteropLibImports::SetGlobalPeggingState(false);

    // Time to walk the external objects
    RETURN_IF_FAILED(WalkExternalTrackerObjects(cxt));

    return S_OK;
}

HRESULT TrackerObjectManager::EndReferenceTracking()
{
    if (s_HasTrackingStarted != true
        || !ShouldWalkExternalObjects())
        return S_FALSE;

    HRESULT hr;

    // Let the tracker runtime know the external object walk is done and they need to:
    // 1. Unpeg all managed object wrappers (mow) if the (mow) needs to be unpegged
    //       (i.e. when the (mow) is only reachable by other external tracker objects).
    // 2. Peg all mows if the mow needs to be pegged (i.e. when the above condition is not true)
    // 3. Unlock reference cache when they are done.
    _ASSERTE(s_TrackerManager != nullptr);
    hr = s_TrackerManager->ReferenceTrackingCompleted();
    _ASSERTE(SUCCEEDED(hr));

    InteropLibImports::SetGlobalPeggingState(true);
    s_HasTrackingStarted = false;

    return hr;
}

HRESULT TrackerObjectManager::DetachNonPromotedObjects(_In_ RuntimeCallContext* cxt)
{
    _ASSERTE(cxt != nullptr);

    HRESULT hr;
    IReferenceTracker* trackerTarget = nullptr;
    OBJECTHANDLE proxyObject = NULL;
    while (InteropLibImports::IteratorNext(cxt, (void**)&trackerTarget, &proxyObject))
    {
        if (trackerTarget == nullptr)
            continue;

        if (proxyObject == nullptr)
            continue;

        if (!InteropLibImports::IsObjectPromoted(proxyObject))
        {
            RETURN_IF_FAILED(BeforeWrapperFinalized(trackerTarget));
        }
    }

    return S_OK;
}
