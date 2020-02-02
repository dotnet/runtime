// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Runtime headers
#include <volatile.h>

#include "comwrappers.h"
#include <interoplibimports.h>

using OBJECTHANDLE = InteropLib::OBJECTHANDLE;

namespace
{
    const IID IID_IReferenceTrackerHost = __uuidof(IReferenceTrackerHost);
    const IID IID_IReferenceTrackerTarget = __uuidof(IReferenceTrackerTarget);
    const IID IID_IReferenceTracker = __uuidof(IReferenceTracker);
    const IID IID_IReferenceTrackerManager = __uuidof(IReferenceTrackerManager);
    const IID IID_IFindReferenceTargetsCallback = __uuidof(IFindReferenceTargetsCallback);

    // In order to minimize the impact of a constructor running on module load,
    // the HostServices class should have no instance fields.
    class HostServices : public IReferenceTrackerHost
    {
    public: // static
        static Volatile<OBJECTHANDLE> RuntimeImpl;

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

    // Runtime implementation for some host services.
    Volatile<OBJECTHANDLE> HostServices::RuntimeImpl;

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
        if (flags & XAML_REFERENCETRACKER_DISCONNECT_SUSPEND)
        {
        }

        return E_NOTIMPL; // [TODO]
    }

    STDMETHODIMP HostServices::ReleaseDisconnectedReferenceSources()
    {
        return E_NOTIMPL; // [TODO]
    }

    //
    // Release context-bound RCWs and Jupiter RCWs (which are free-threaded but context-bound)
    // in the current apartment
    //
    STDMETHODIMP HostServices::NotifyEndOfReferenceTrackingOnThread()
    {
        return E_NOTIMPL; // [TODO]
    }

    //
    // Creates a proxy object that points to the given RCW
    // The proxy
    // 1. Has a managed reference pointing to the RCW, and therefore forms a cycle that can be resolved by GC
    // 2. Forwards data binding requests
    // For example:
    //
    // Grid <---- RCW             Grid <-------- RCW
    // | ^                         |              ^
    // | |             Becomes     |              |
    // v |                         v              |
    // Rectangle                  Rectangle ----->Proxy
    //
    // Arguments
    //   obj        - The identity IUnknown* where a RCW points to (Grid, in this case)
    //                    Note that
    //                    1) we can either create a new RCW or get back an old one from cache
    //                    2) This obj could be a regular WinRT object (such as WinRT collection) for data binding
    //  ppNewReference  - The IReferenceTrackerTarget* for the proxy created
    //                    Jupiter will call IReferenceTrackerTarget to establish a jupiter reference
    //
    STDMETHODIMP HostServices::GetTrackerTarget(_In_ IUnknown* obj, _Outptr_ IReferenceTrackerTarget** ppNewReference)
    {
        if (obj == nullptr || ppNewReference == nullptr)
            return E_INVALIDARG;

        HRESULT hr;

        OBJECTHANDLE impl = HostServices::RuntimeImpl;
        if (impl == nullptr)
            return E_NOT_SET;

        // QI for IUnknown to get the identity unknown
        ComHolder<IUnknown> identity;
        RETURN_IF_FAILED(obj->QueryInterface(&identity));

        // Get or create an existing implementation for the this external.
        ComHolder<IUnknown> target;
        RETURN_IF_FAILED(GetOrCreateTrackerTargetForExternal(
            impl,
            identity,
            CreateRCWFlags::TrackerObject,
            CreateCCWFlags::TrackerSupport,
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
}

namespace
{
    // [TODO]
    Volatile<IReferenceTrackerManager*> s_TrackerManager; // The one and only Tracker Manager instance
    Volatile<BOOL> s_IsGCStarted = FALSE;

    //
    // Tells GC whether walking all the Jupiter RCW is necessary, which only should happen
    // if we have seen jupiter RCWs
    // [TODO]
    BOOL NeedToWalkRCWs()
    {
        return (s_TrackerManager.load() != nullptr);
    }

    //
    // Callback implementation of IFindReferenceTargetsCallback
    // [TODO]
    class FindDependentWrappersCallback : public IFindReferenceTargetsCallback
    {
    public:
        FindDependentWrappersCallback(_In_ RCWInstance* rcw, _In_ TrackerRCWEnum* rcwEnum)
            : _rcw{ rcw }
            , _rcwEnum{ rcwEnum }
        {
        }

        STDMETHOD(FoundTrackerTarget)(_In_ IReferenceTrackerTarget* target)
        {
            assert(target != nullptr);

            HRESULT hr;

            CCW* ccw = CCW::MapIUnknownToWrapper(target);

            // Not a target we implemented.
            if (ccw == nullptr)
                return S_OK;

            //
            // Skip dependent handle creation if RCW/CCW points to the same managed object
            //
            if (_rcw->GetObjectGCHandle() == ccw->GetObjectGCHandle())
                return S_OK;

            //
            // Jupiter might return CCWs with outstanding references that are either :
            // 1. Neutered - in this case it is unsafe to touch m_ppThis
            // 2. RefCounted handle NULLed out by GC
            //
            // Skip those to avoid crashes
            //
            if (!ccw->IsAlive())
                return S_OK;

            //
            // Add a reference from rcw -> ccw so that GC knows about this reference
            //
            RETURN_IF_FAILED(_rcwEnum->AddReferenceFromRCWToCCW(_rcw, ccw));

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

    private:
        RCWInstance* _rcw;
        TrackerRCWEnum* _rcwEnum;
    };

    //
    // Ask Jupiter all the CCWs referenced (through native code) by this RCW and build reference for RCW -> CCW
    // so that GC knows about this reference
    // [TODO]
    HRESULT WalkOneRCW(_In_ RCWInstance* rcw, _In_ TrackerRCWEnum* rcwEnum)
    {
        assert(rcw != nullptr && rcwEnum != nullptr);

        HRESULT hr;

        // Get IReferenceTracker * from RCW - we can call IReferenceTracker* from any thread and it won't be a proxy
        ComHolder<IReferenceTracker> obj;
        RETURN_IF_FAILED(rcw->GetInstanceProxy(&obj));

        FindDependentWrappersCallback cb{ rcw, rcwEnum };
        RETURN_IF_FAILED(obj->FindTrackerTargets(&cb));

        return S_OK;
    }
}

void TrackerRCWManager::OnGCStartedWorker()
{
    // Due to the nesting GCStart/GCEnd pairs (see comment for this function), we need to check
    // those flags inside nCondemnedGeneration >= 2 check
    assert(s_IsGCStarted == FALSE);
    assert(s_IsGlobalPeggingOn == TRUE);

    s_IsGCStarted = TRUE;

    //
    // Let Jupiter know we are about to walk RCWs so that they can lock their reference cache
    // Note that Jupiter doesn't need to unpeg all CCWs at this point and they can do the pegging/unpegging in FindTrackerTargetsCompleted
    //
    assert(s_TrackerManager.load() != nullptr);
    s_TrackerManager.load()->ReferenceTrackingStarted();

    // From this point, jupiter decides whether a CCW should be pegged or not as global pegging flag is now off
    s_IsGlobalPeggingOn = FALSE;

    //
    // OK. Time to walk all the reference RCWs
    //
    WalkRCWs();
}

void TrackerRCWManager::OnGCFinishedWorker()
{
    //
    // Let Jupiter know RCW walk is done and they need to:
    // 1. Unpeg all CCWs if the CCW needs to be unpegged (when the CCW is only reachable by other jupiter RCWs)
    // 2. Peg all CCWs if the CCW needs to be pegged (when the above condition is not true)
    // 3. Unlock reference cache when they are done
    //
    // If the walk has failed - Jupiter doesn't need to do anything and could just return immediately
    //
    assert(s_TrackerManager.load() != nullptr);
    s_TrackerManager.load()->ReferenceTrackingCompleted();

    s_IsGlobalPeggingOn = TRUE;
    s_IsGCStarted = FALSE;
}

//
// Walk all the jupiter RCWs in all AppDomains and build references from RCW -> CCW as we go
//
void TrackerRCWManager::WalkRCWs()
{
    BOOL walkFailed = FALSE;
    HRESULT hr;

    try
    {
        TrackerRCWEnum* rcwEnum = TrackerRCWEnum::GetInstance();
        assert(rcwEnum != nullptr);

        //
        // Reset the cache
        //
        rcwEnum->ResetDependentHandles();

        //
        // Enumerate Jupiter RCWs
        //
        hr = rcwEnum->EnumerateJupiterRCWs(WalkOneRCW);

        //
        // Shrink the dependent handle cache if necessary and clear unused handles.
        //
        rcwEnum->ShrinkDependentHandles();
    }
    catch (...)
    {
        hr = E_FAIL;
    }

    if (FAILED(hr))
    {
        // Remember the fact that we've failed and stop walking
        walkFailed = TRUE;
        s_IsGlobalPeggingOn = TRUE;
    }

    //
    // Let Jupiter know RCW walk is done and they need to:
    // 1. Unpeg all CCWs if the CCW needs to be unpegged (when the CCW is only reachable by other jupiter RCWs)
    // 2. Peg all CCWs if the CCW needs to be pegged (when the above condition is not true)
    // 3. Unlock reference cache when they are done
    //
    // If the walk has failed - Jupiter doesn't need to do anything and could just return immediately
    //
    assert(s_TrackerManager.load() != nullptr);
    s_TrackerManager.load()->FindTrackerTargetsCompleted(walkFailed);
}

bool TrackerObjectManager::TrySetReferenceTrackerHostRuntimeImpl(_In_ OBJECTHANDLE objectHandle, _In_ OBJECTHANDLE current)
{
    // Attempt to set the runtime implementation for providing hosting services to the tracker runtime. 
    return (::InterlockedCompareExchangePointer(
        &HostServices::RuntimeImpl,
        objectHandle, current) == current);
}

HRESULT TrackerObjectManager::OnIReferenceTrackerFound(_In_ IReferenceTracker* obj)
{
    _ASSERTE(obj != nullptr);
    if (s_TrackerManager.load() != nullptr)
        return S_OK;

    // Retrieve IReferenceTrackerManager
    HRESULT hr;
    ComHolder<IReferenceTrackerManager> trackerManager;
    RETURN_IF_FAILED(obj->GetReferenceTrackerManager(&trackerManager));

    ComHolder<IReferenceTrackerHost> clrServices;
    RETURN_IF_FAILED(g_HostServicesInstance.QueryInterface(IID_IReferenceTrackerHost, (void**)&clrServices));

    // [TODO] Temporarily switch back to coop and disable GC to avoid racing with the very first RCW walk
    //GCX_COOP();
    //GCX_FORBID();

    IReferenceTrackerManager* expected = nullptr;
    if (s_TrackerManager.compare_exchange_strong(expected, trackerManager.p))
    {
        (void)trackerManager.Detach(); // Ownership has been transfered

        //
        // OK. It is time to do our initialization
        // It's safe to do it here because we are in COOP and only one thread wins the race
        //
        RETURN_IF_FAILED(s_TrackerManager.load()->SetReferenceTrackerHost(clrServices));
    }

    return S_OK;
}

HRESULT TrackerObjectManager::AfterWrapperCreated(_In_ NativeObjectWrapperContext* cxt)
{
    _ASSERTE(cxt != nullptr);

    HRESULT hr;
    IReferenceTracker* obj = cxt->GetReferenceTrackerFast();
    _ASSERTE(obj != nullptr);

    // Notify tracker runtime that we've created a new wrapper for this object.
    // To avoid surprises, we should notify them before we fire the first AddRefFromTrackerSource.
    RETURN_IF_FAILED(obj->ConnectFromTrackerSource());

    // Send out AddRefFromTrackerSource callbacks to notify tracker runtime we've done AddRef()
    // for certain interfaces. We should do this *after* we made a AddRef() because we should never
    // be in a state where report refs > actual refs
    RETURN_IF_FAILED(obj->AddRefFromTrackerSource());

    return S_OK;
}

HRESULT TrackerObjectManager::BeforeWrapperDestroyed(_In_ NativeObjectWrapperContext* cxt)
{
    _ASSERTE(cxt != nullptr);

    HRESULT hr;
    ComHolder<IReferenceTracker> obj;
    RETURN_IF_FAILED(cxt->GetInstanceProxy(&obj));

    // Notify tracker runtime that we are about to destroy a wrapper
    // (same timing as short weak handle) for this object.
    // They need this information to disconnect weak refs and stop firing events,
    // so that they can avoid resurrecting the object.
    RETURN_IF_FAILED(obj->DisconnectFromTrackerSource());

    return S_OK;
}

namespace
{
    //
    // We never expect exceptions to be thrown outside of RCWWalker
    // So make sure we fail fast here, instead of going through normal
    // exception processing and fail later
    // This will make analyzing dumps much easier
    //
    LONG RCWWalker_UnhandledExceptionFilter(DWORD code, _In_ EXCEPTION_POINTERS* excep)
    {
        if ((excep->ExceptionRecord->ExceptionCode == STATUS_BREAKPOINT)
            || (excep->ExceptionRecord->ExceptionCode == STATUS_SINGLE_STEP))
        {
            // We don't want to fail fast on debugger exceptions
            return EXCEPTION_CONTINUE_SEARCH;
        }

        assert(false);
        std::abort();

        return EXCEPTION_EXECUTE_HANDLER;
    }

    using OnGCEventProc = void(*)();
    void SetupFailFastFilterAndCall(_In_ OnGCEventProc func)
    {
        __try
        {
            // Call the internal worker function which has the runtime contracts
            func();
        }
        __except (RCWWalker_UnhandledExceptionFilter(GetExceptionCode(), GetExceptionInformation()))
        {
            assert(false && "Should not get here");
        }
    }
}

//
// Note that we could get nested GCStart/GCEnd calls, such as :
// GCStart for Gen 2 background GC
//    GCStart for Gen 0/1 foregorund GC
//    GCEnd   for Gen 0/1 foreground GC
//    ....
// GCEnd for Gen 2 background GC
//
// The nCondemnedGeneration >= 2 check takes care of this nesting problem
//
void TrackerObjectManager::OnGCStarted(_In_ int nCondemnedGeneration)
{
    if (nCondemnedGeneration < 2)  // We are only doing walk in Gen2 GC
        return;

    if (!NeedToWalkRCWs()) // Have we seen Jupiter RCWs?
        return;

    // Make sure we fail fast if anything goes wrong when we interact with Jupiter
    SetupFailFastFilterAndCall(TrackerObjectManager::OnGCStartedWorker);
}

//
// Note that we could get nested GCStart/GCEnd calls, such as :
// GCStart for Gen 2 background GC
//    GCStart for Gen 0/1 foregorund GC
//    GCEnd   for Gen 0/1 foreground GC
//    ....
// GCEnd for Gen 2 background GC
//
// The nCondemnedGeneration >= 2 check takes care of this nesting problem
//
void TrackerObjectManager::OnGCFinished(_In_ int nCondemnedGeneration)
{
    //
    // Note that we need to check in both OnGCFinished and OnGCStarted
    // As there could be multiple OnGCFinished with nCondemnedGeneration < 2 in the case of Gen 2 GC
    //
    // Also, if this is background GC, the NeedToWalkRCWs predicate may change from FALSE to TRUE while
    // the GC is running. We don't want to do any work if it's the case (i.e. if s_IsGCStarted is FALSE).
    //
    if (nCondemnedGeneration >= 2   // We are only doing walk in Gen2 GC
        && NeedToWalkRCWs()         // Have we seen Jupiter RCWs?
        && s_IsGCStarted == TRUE)   // Had we seen Jupiter RCWs when the GC started?
    {
        // Make sure we fail fast if anything goes wrong when we interact with Jupiter
        SetupFailFastFilterAndCall(TrackerObjectManager::OnGCFinishedWorker);
    }
}

void TrackerObjectManager::OnShutdown()
{
    IReferenceTrackerManager* trackerManager = s_TrackerManager.exchange(nullptr);
    if (trackerManager != nullptr)
    {
        // Make sure s_TrackerManager is always either null or a valid IReferenceTrackerManager *
        // this will make crash easier to diagnose
        trackerManager->Release();
    }
}
