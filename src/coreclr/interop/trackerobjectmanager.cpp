// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "comwrappers.hpp"
#include <interoplibimports.h>

using OBJECTHANDLE = InteropLib::OBJECTHANDLE;
using RuntimeCallContext = InteropLibImports::RuntimeCallContext;

namespace
{

    //29a71c6a-3c42-4416-a39d-e2825a07a773
    const GUID IID_IReferenceTrackerHost = { 0x29a71c6a, 0x3c42, 0x4416, { 0xa3, 0x9d, 0xe2, 0x82, 0x5a, 0x7, 0xa7, 0x73} };

    //3cf184b4-7ccb-4dda-8455-7e6ce99a3298
    const GUID IID_IReferenceTrackerManager = { 0x3cf184b4, 0x7ccb, 0x4dda, { 0x84, 0x55, 0x7e, 0x6c, 0xe9, 0x9a, 0x32, 0x98} };

    //04b3486c-4687-4229-8d14-505ab584dd88
    const GUID IID_IFindReferenceTargetsCallback = { 0x04b3486c, 0x4687, 0x4229, { 0x8d, 0x14, 0x50, 0x5a, 0xb5, 0x84, 0xdd, 0x88} };

    // In order to minimize the impact of a constructor running on module load,
    // the HostServices class should have no instance fields.
    class HostServices : public IReferenceTrackerHost
    {
    public: // IReferenceTrackerHost
        STDMETHOD(DisconnectUnusedReferenceSources)(_In_ DWORD dwFlags);
        STDMETHOD(ReleaseDisconnectedReferenceSources)();
        STDMETHOD(NotifyEndOfReferenceTrackingOnThread)();
        STDMETHOD(GetTrackerTarget)(_In_ IUnknown* obj, _Outptr_ IReferenceTrackerTarget** ppNewReference);
        STDMETHOD(AddMemoryPressure)(_In_ UINT64 bytesAllocated);
        STDMETHOD(RemoveMemoryPressure)(_In_ UINT64 bytesAllocated);

    public: // IUnknown
        // Lifetime maintained by stack - we don't care about ref counts
        STDMETHOD_(ULONG, AddRef)() { return 1; }
        STDMETHOD_(ULONG, Release)() { return 1; }

        STDMETHOD(QueryInterface)(
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject)
        {
            if (ppvObject == nullptr)
                return E_POINTER;

            if (IsEqualIID(riid, IID_IReferenceTrackerHost))
            {
                *ppvObject = static_cast<IReferenceTrackerHost*>(this);
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

    // Global instance of host services.
    HostServices g_HostServicesInstance;

    // Defined in windows.ui.xaml.hosting.referencetracker.h.
    enum XAML_REFERENCETRACKER_DISCONNECT
    {
        // Indicates the disconnect is during a suspend and a GC can be trigger.
        XAML_REFERENCETRACKER_DISCONNECT_SUSPEND = 0x00000001
    };

    STDMETHODIMP HostServices::DisconnectUnusedReferenceSources(_In_ DWORD flags)
    {
        InteropLibImports::GcRequest type = InteropLibImports::GcRequest::Default;

        // Request a "stop the world" GC when a suspend is occurring.
        if (flags & XAML_REFERENCETRACKER_DISCONNECT_SUSPEND)
            type = InteropLibImports::GcRequest::FullBlocking;

        return InteropLibImports::RequestGarbageCollectionForExternal(type);
    }

    STDMETHODIMP HostServices::ReleaseDisconnectedReferenceSources()
    {
        return InteropLibImports::WaitForRuntimeFinalizerForExternal();
    }

    STDMETHODIMP HostServices::NotifyEndOfReferenceTrackingOnThread()
    {
        return InteropLibImports::ReleaseExternalObjectsFromCurrentThread();
    }

    // Creates a proxy object (managed object wrapper) that points to the given IUnknown.
    // The proxy represents the following:
    //   1. Has a managed reference pointing to the external object
    //      and therefore forms a cycle that can be resolved by GC.
    //   2. Forwards data binding requests.
    //
    // For example:
    //
    // Grid <---- NoCW             Grid <-------- NoCW
    // | ^                         |              ^
    // | |             Becomes     |              |
    // v |                         v              |
    // Rectangle                  Rectangle ----->Proxy
    //
    // Arguments
    //   obj        - An IUnknown* where a NoCW points to (Grid, in this case)
    //                    Notes:
    //                    1. We can either create a new NoCW or get back an old one from the cache.
    //                    2. This obj could be a regular tracker runtime object for data binding.
    //  ppNewReference  - The IReferenceTrackerTarget* for the proxy created
    //                    The tracker runtime will call IReferenceTrackerTarget to establish a reference.
    //
    STDMETHODIMP HostServices::GetTrackerTarget(_In_ IUnknown* obj, _Outptr_ IReferenceTrackerTarget** ppNewReference)
    {
        if (obj == nullptr || ppNewReference == nullptr)
            return E_INVALIDARG;

        HRESULT hr;

        // QI for IUnknown to get the identity unknown
        ComHolder<IUnknown> identity;
        RETURN_IF_FAILED(obj->QueryInterface(IID_IUnknown, (void**)&identity));

        // Get or create an existing implementation for this external.
        ComHolder<IUnknown> target;
        RETURN_IF_FAILED(InteropLibImports::GetOrCreateTrackerTargetForExternal(
            identity,
            InteropLib::Com::CreateObjectFlags_TrackerObject,
            InteropLib::Com::CreateComInterfaceFlags_TrackerSupport,
            (void**)&target));

        return target->QueryInterface(IID_IReferenceTrackerTarget, (void**)ppNewReference);
    }

    STDMETHODIMP HostServices::AddMemoryPressure(_In_ UINT64 bytesAllocated)
    {
        return InteropLibImports::AddMemoryPressureForExternal(bytesAllocated);
    }

    STDMETHODIMP HostServices::RemoveMemoryPressure(_In_ UINT64 bytesAllocated)
    {
        return InteropLibImports::RemoveMemoryPressureForExternal(bytesAllocated);
    }

    VolatilePtr<IReferenceTrackerManager> s_TrackerManager; // The one and only Tracker Manager instance
    Volatile<BOOL> s_HasTrackingStarted = FALSE;

    // Indicates if walking the external objects is needed.
    // (i.e. Have any IReferenceTracker instances been found?)
    bool ShouldWalkExternalObjects()
    {
        return (s_TrackerManager != nullptr);
    }

    // Callback implementation of IFindReferenceTargetsCallback
    class FindDependentWrappersCallback : public IFindReferenceTargetsCallback
    {
        NativeObjectWrapperContext* _nowCxt;
        RuntimeCallContext* _runtimeCallCxt;

    public:
        FindDependentWrappersCallback(_In_ NativeObjectWrapperContext* nowCxt, _In_ RuntimeCallContext* runtimeCallCxt)
            : _nowCxt{ nowCxt }
            , _runtimeCallCxt{ runtimeCallCxt }
        {
            _ASSERTE(_nowCxt != nullptr && runtimeCallCxt != nullptr);
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
                _nowCxt->GetRuntimeContext(),
                mow->Target));

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

        BOOL walkFailed = FALSE;
        HRESULT hr;

        void* extObjContext = nullptr;
        while (S_OK == (hr = InteropLibImports::IteratorNext(cxt, &extObjContext)))
        {
            _ASSERTE(extObjContext != nullptr);

            NativeObjectWrapperContext* nowc = NativeObjectWrapperContext::MapFromRuntimeContext(extObjContext);

            // Check if the object is a tracker object.
            IReferenceTracker* trackerMaybe = nowc->GetReferenceTracker();
            if (trackerMaybe == nullptr)
                continue;

            // Ask the tracker instance to find all reference targets.
            FindDependentWrappersCallback cb{ nowc, cxt };
            hr = trackerMaybe->FindTrackerTargets(&cb);
            if (FAILED(hr))
                break;
        }

        if (FAILED(hr))
        {
            // Remember the fact that we've failed and stop walking
            walkFailed = TRUE;
            InteropLibImports::SetGlobalPeggingState(true);
        }

        _ASSERTE(s_TrackerManager != nullptr);
        (void)s_TrackerManager->FindTrackerTargetsCompleted(walkFailed);

        return hr;
    }
}

HRESULT TrackerObjectManager::OnIReferenceTrackerFound(_In_ IReferenceTracker* obj)
{
    _ASSERTE(obj != nullptr);
    if (s_TrackerManager != nullptr)
        return S_OK;

    // Retrieve IReferenceTrackerManager
    HRESULT hr;
    ComHolder<IReferenceTrackerManager> trackerManager;
    RETURN_IF_FAILED(obj->GetReferenceTrackerManager(&trackerManager));

    ComHolder<IReferenceTrackerHost> hostServices;
    RETURN_IF_FAILED(g_HostServicesInstance.QueryInterface(IID_IReferenceTrackerHost, (void**)&hostServices));

    // Attempt to set the tracker instance.
    if (InterlockedCompareExchangePointer((void**)&s_TrackerManager, trackerManager.p, nullptr) == nullptr)
    {
        (void)trackerManager.Detach(); // Ownership has been transfered
        RETURN_IF_FAILED(s_TrackerManager->SetReferenceTrackerHost(hostServices));
    }

    return S_OK;
}

HRESULT TrackerObjectManager::AfterWrapperCreated(_In_ IReferenceTracker* obj)
{
    _ASSERTE(obj != nullptr);

    HRESULT hr;

    // Notify tracker runtime that we've created a new wrapper for this object.
    // To avoid surprises, we should notify them before we fire the first AddRefFromTrackerSource.
    RETURN_IF_FAILED(obj->ConnectFromTrackerSource());

    // Send out AddRefFromTrackerSource callbacks to notify tracker runtime we've done AddRef()
    // for certain interfaces. We should do this *after* we made a AddRef() because we should never
    // be in a state where report refs > actual refs
    RETURN_IF_FAILED(obj->AddRefFromTrackerSource()); // IUnknown
    RETURN_IF_FAILED(obj->AddRefFromTrackerSource()); // IReferenceTracker

    return S_OK;
}

HRESULT TrackerObjectManager::BeforeWrapperDestroyed(_In_ IReferenceTracker* obj)
{
    _ASSERTE(obj != nullptr);

    HRESULT hr;

    // Notify tracker runtime that we are about to destroy a wrapper
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

    _ASSERTE(s_HasTrackingStarted == FALSE);
    _ASSERTE(InteropLibImports::GetGlobalPeggingState());

    s_HasTrackingStarted = TRUE;

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
    if (s_HasTrackingStarted != TRUE
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
    s_HasTrackingStarted = FALSE;

    return hr;
}
