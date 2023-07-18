// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>
#include <ComHelpers.h>
#ifdef WINDOWS
#include <inspectable.h>
#endif //WINDOWS

#include <atomic>
#include <cassert>
#include <exception>
#include <stdexcept>
#include <list>
#include <mutex>
#include <unordered_map>

namespace API
{
    // Documentation found at https://docs.microsoft.com/windows/win32/api/windows.ui.xaml.hosting.referencetracker/
    //64bd43f8-bfee-4ec4-b7eb-2935158dae21
    const GUID IID_IReferenceTrackerTarget = { 0x64bd43f8, 0xbfee, 0x4ec4, { 0xb7, 0xeb, 0x29, 0x35, 0x15, 0x8d, 0xae, 0x21} };
    class DECLSPEC_UUID("64bd43f8-bfee-4ec4-b7eb-2935158dae21") IReferenceTrackerTarget : public IUnknown
    {
    public:
        STDMETHOD_(ULONG, AddRefFromReferenceTracker)() = 0;
        STDMETHOD_(ULONG, ReleaseFromReferenceTracker)() = 0;
        STDMETHOD(Peg)() = 0;
        STDMETHOD(Unpeg)() = 0;
    };

    //29a71c6a-3c42-4416-a39d-e2825a07a773
    const GUID IID_IReferenceTrackerHost = { 0x29a71c6a, 0x3c42, 0x4416, { 0xa3, 0x9d, 0xe2, 0x82, 0x5a, 0x07, 0xa7, 0x73} };
    class DECLSPEC_UUID("29a71c6a-3c42-4416-a39d-e2825a07a773") IReferenceTrackerHost : public IUnknown
    {
    public:
        STDMETHOD(DisconnectUnusedReferenceSources)(_In_ DWORD dwFlags) = 0;
        STDMETHOD(ReleaseDisconnectedReferenceSources)() = 0;
        STDMETHOD(NotifyEndOfReferenceTrackingOnThread)() = 0;
        STDMETHOD(GetTrackerTarget)(_In_ IUnknown* obj, _Outptr_ IReferenceTrackerTarget** ppNewReference) = 0;
        STDMETHOD(AddMemoryPressure)(_In_ UINT64 bytesAllocated) = 0;
        STDMETHOD(RemoveMemoryPressure)(_In_ UINT64 bytesAllocated) = 0;
    };

    //3cf184b4-7ccb-4dda-8455-7e6ce99a3298
    const GUID IID_IReferenceTrackerManager = { 0x3cf184b4, 0x7ccb, 0x4dda, { 0x84, 0x55, 0x7e, 0x6c, 0xe9, 0x9a, 0x32, 0x98} };
    class DECLSPEC_UUID("3cf184b4-7ccb-4dda-8455-7e6ce99a3298") IReferenceTrackerManager : public IUnknown
    {
    public:
        STDMETHOD(ReferenceTrackingStarted)() = 0;
        STDMETHOD(FindTrackerTargetsCompleted)(_In_ BOOL bWalkFailed) = 0;
        STDMETHOD(ReferenceTrackingCompleted)() = 0;
        STDMETHOD(SetReferenceTrackerHost)(_In_ IReferenceTrackerHost *pCLRServices) = 0;
    };

    class DECLSPEC_UUID("04b3486c-4687-4229-8d14-505ab584dd88") IFindReferenceTargetsCallback : public IUnknown
    {
    public:
        STDMETHOD(FoundTrackerTarget)(_In_ IReferenceTrackerTarget* target) = 0;
    };

    //11d3b13a-180e-4789-a8be-7712882893e6
    const GUID IID_IReferenceTracker = { 0x11d3b13a, 0x180e, 0x4789, { 0xa8, 0xbe, 0x77, 0x12, 0x88, 0x28, 0x93, 0xe6} };
    class DECLSPEC_UUID("11d3b13a-180e-4789-a8be-7712882893e6") IReferenceTracker : public IUnknown
    {
    public:
        STDMETHOD(ConnectFromTrackerSource)() = 0;
        STDMETHOD(DisconnectFromTrackerSource)() = 0;
        STDMETHOD(FindTrackerTargets)(_In_ IFindReferenceTargetsCallback *pCallback) = 0;
        STDMETHOD(GetReferenceTrackerManager)(_Outptr_ IReferenceTrackerManager **ppTrackerManager) = 0;
        STDMETHOD(AddRefFromTrackerSource)() = 0;
        STDMETHOD(ReleaseFromTrackerSource)() = 0;
        STDMETHOD(PegFromTrackerSource)() = 0;
    };
}

namespace
{
    // Testing types
    //447BB9ED-DA48-4ABC-8963-5BB5C3E0AA09
    const GUID IID_ITest = { 0x447BB9ED, 0xda48, 0x4abc, { 0x89, 0x63, 0x5b, 0xb5, 0xc3, 0xe0, 0xaa, 0x9} };
    struct DECLSPEC_UUID("447BB9ED-DA48-4ABC-8963-5BB5C3E0AA09") ITest : public IUnknown
    {
        STDMETHOD(SetValue)(int i) = 0;
    };

    //42951130-245C-485E-B60B-4ED4254256F8
    const GUID IID_ITrackerObject = { 0x42951130, 0x245c, 0x485e, { 0xb6, 0x0b, 0x4e, 0xd4, 0x25, 0x42, 0x56, 0xf8} };
    struct DECLSPEC_UUID("42951130-245C-485E-B60B-4ED4254256F8") ITrackerObject : public IUnknown
    {
        STDMETHOD(AddObjectRef)(_In_ IUnknown* c, _Out_ int* id) = 0;
        STDMETHOD(DropObjectRef)(_In_ int id) = 0;
    };

    struct TrackerObject : public IUnknown, public UnknownImpl
    {
        static std::atomic<int32_t> AllocationCount;

        static const int32_t DisableTrackedCount = -1;
        static const int32_t EnableTrackedCount = 0;
        static std::atomic<int32_t> TrackedAllocationCount;

        TrackerObject(_In_ size_t id, _In_opt_ IUnknown* pUnkOuter)
            : _outer{ pUnkOuter == nullptr ? static_cast<IUnknown*>(this) : pUnkOuter }
            , _impl{ id, _outer }
        {
            ++AllocationCount;

            if (TrackedAllocationCount != DisableTrackedCount)
                ++TrackedAllocationCount;
        }

        ~TrackerObject()
        {
            // There is a cleanup race when tracking is enabled.
            // It is possible previously allocated objects could be
            // cleaned up during alloc tracking scenarios - these can be
            // ignored.
            //
            // See the locking around the tracking scenarios in the
            // managed P/Invoke usage.
            if (TrackedAllocationCount > 0)
                --TrackedAllocationCount;

            --AllocationCount;
        }

        HRESULT TogglePeg(_In_ bool shouldPeg)
        {
            HRESULT hr;

            auto curr = std::begin(_impl._elements);
            while (curr != std::end(_impl._elements))
            {
                ComSmartPtr<API::IReferenceTrackerTarget> mowMaybe;
                if (S_OK == curr->second->QueryInterface(API::IID_IReferenceTrackerTarget, (void**)&mowMaybe))
                {
                    if (shouldPeg)
                    {
                        RETURN_IF_FAILED(mowMaybe->Peg());
                    }
                    else
                    {
                        RETURN_IF_FAILED(mowMaybe->Unpeg());
                    }
                }
                ++curr;
            }

            // Handle the case for aggregation
            //
            // Pegging occurs during a GC. We can't QI for this during
            // a GC because the COM scenario would fallback to
            // ICustomQueryInterface (i.e. managed code).
            if (_impl._outerRefTrackerTarget)
            {
                ComSmartPtr<API::IReferenceTrackerTarget> thisTgtMaybe;
                if (S_OK == _outer->QueryInterface(API::IID_IReferenceTrackerTarget, (void**)&thisTgtMaybe))
                {
                    if (shouldPeg)
                    {
                        RETURN_IF_FAILED(thisTgtMaybe->Peg());
                    }
                    else
                    {
                        RETURN_IF_FAILED(thisTgtMaybe->Unpeg());
                    }
                }
            }

            return S_OK;
        }

        HRESULT DisconnectFromReferenceTrackerRuntime()
        {
            HRESULT hr;

            RETURN_IF_FAILED(TogglePeg(/* should peg */ false));

            // Handle the case for aggregation in the release case.
            if (_impl._outerRefTrackerTarget)
            {
                ComSmartPtr<API::IReferenceTrackerTarget> thisTgtMaybe;
                if (S_OK == _outer->QueryInterface(API::IID_IReferenceTrackerTarget, (void**)&thisTgtMaybe))
                    RETURN_IF_FAILED(thisTgtMaybe->ReleaseFromReferenceTracker());
            }

            return S_OK;
        }

        struct TrackerObjectImpl : public ITrackerObject, public API::IReferenceTracker
        {
            IUnknown* _implOuter;
            bool _outerRefTrackerTarget;
            const size_t _id;
            std::atomic<int> _trackerSourceCount;
            bool _connected;
            std::atomic<int> _elementId;
            std::unordered_map<int, ComSmartPtr<IUnknown>> _elements;

            TrackerObjectImpl(_In_ size_t id, _In_ IUnknown* pUnkOuter)
                : _implOuter{ pUnkOuter }
                , _outerRefTrackerTarget{ false }
                , _id{ id }
                , _trackerSourceCount{ 0 }
                , _connected{ false }
                , _elementId{ 1 }
            {
                // Check if we are aggregating with a tracker target
                ComSmartPtr<API::IReferenceTrackerTarget> tgt;
                if (SUCCEEDED(_implOuter->QueryInterface(API::IID_IReferenceTrackerTarget, (void**)&tgt)))
                {
                    _outerRefTrackerTarget = true;
                    (void)tgt->AddRefFromReferenceTracker();
                    if (FAILED(tgt->Peg()))
                    {
                        throw std::runtime_error{ "Peg failure" };
                    }
                }
            }

            bool IsConnected()
            {
                return _connected;
            }

            STDMETHOD(AddObjectRef)(_In_ IUnknown* c, _Out_ int* id)
            {
                assert(c != nullptr && id != nullptr);

                ComSmartPtr<API::IReferenceTrackerTarget> mowMaybe;
                if (S_OK == c->QueryInterface(API::IID_IReferenceTrackerTarget, (void**)&mowMaybe))
                {
                    (void)mowMaybe->AddRefFromReferenceTracker();
                    c = mowMaybe.p;
                }

                try
                {
                    *id = _elementId;
                    if (!_elements.insert(std::make_pair(*id, ComSmartPtr<IUnknown>{ c })).second)
                        return S_FALSE;

                    _elementId++;
                }
                catch (const std::bad_alloc&)
                {
                    return E_OUTOFMEMORY;
                }

                return S_OK;
            }

            STDMETHOD(DropObjectRef)(_In_ int id)
            {
                auto iter = _elements.find(id);
                if (iter == std::end(_elements))
                    return S_FALSE;

                ComSmartPtr<API::IReferenceTrackerTarget> mowMaybe;
                if (S_OK == iter->second->QueryInterface(API::IID_IReferenceTrackerTarget, (void**)&mowMaybe))
                {
                    (void)mowMaybe->ReleaseFromReferenceTracker();
                    (void)mowMaybe->Unpeg();
                }

                _elements.erase(iter);

                return S_OK;
            }

            STDMETHOD(ConnectFromTrackerSource)();
            STDMETHOD(DisconnectFromTrackerSource)();
            STDMETHOD(FindTrackerTargets)(_In_ API::IFindReferenceTargetsCallback* pCallback);
            STDMETHOD(GetReferenceTrackerManager)(_Outptr_ API::IReferenceTrackerManager** ppTrackerManager);
            STDMETHOD(AddRefFromTrackerSource)();
            STDMETHOD(ReleaseFromTrackerSource)();
            STDMETHOD(PegFromTrackerSource)();

            STDMETHOD(QueryInterface)(
                /* [in] */ REFIID riid,
                /* [iid_is][out] */ void ** ppvObject)
            {
                return _implOuter->QueryInterface(riid, ppvObject);
            }
            STDMETHOD_(ULONG, AddRef)(void)
            {
                return _implOuter->AddRef();
            }
            STDMETHOD_(ULONG, Release)(void)
            {
                return _implOuter->Release();
            }
        };

        STDMETHOD(QueryInterface)(
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void ** ppvObject)
        {
            if (ppvObject == nullptr)
                return E_POINTER;

            IUnknown* tgt;

            // Aggregation implementation.
            if (riid == IID_IUnknown)
            {
                tgt = static_cast<IUnknown*>(this);
            }
            else
            {
                // Send non-IUnknown queries to the implementation.
                if (riid == API::IID_IReferenceTracker)
                {
                    tgt = static_cast<API::IReferenceTracker*>(&_impl);
                }
                else if (riid == __uuidof(ITrackerObject))
                {
                    tgt = static_cast<ITrackerObject*>(&_impl);
                }
                else
                {
                    *ppvObject = nullptr;
                    return E_NOINTERFACE;
                }
            }

            (void)tgt->AddRef();
            *ppvObject = tgt;
            return S_OK;
        }

        DEFINE_REF_COUNTING();

        IUnknown* _outer;
        TrackerObjectImpl _impl;
    };

    std::atomic<int32_t> TrackerObject::AllocationCount{};
    std::atomic<int32_t> TrackerObject::TrackedAllocationCount{ TrackerObject::DisableTrackedCount };
    std::atomic<size_t> CurrentObjectId{};

    class TrackerRuntimeManagerImpl : public API::IReferenceTrackerManager
    {
        ComSmartPtr<API::IReferenceTrackerHost> _runtimeServices;
        std::mutex _objectsLock;
        std::list<ComSmartPtr<TrackerObject>> _objects;

    public:
        ITrackerObject* RecordObject(_In_ TrackerObject* obj, _Outptr_ IUnknown** inner)
        {
            {
                std::lock_guard<std::mutex> guard{ _objectsLock };
                _objects.push_back(ComSmartPtr<TrackerObject>{ obj });
            }

            if (_runtimeServices != nullptr)
                _runtimeServices->AddMemoryPressure(sizeof(TrackerObject));

            // Perform a QI to get the proper identity.
            (void)obj->QueryInterface(IID_IUnknown, (void**)inner);

            // Get the default interface.
            ITrackerObject* type;
            (void)obj->QueryInterface(__uuidof(ITrackerObject), (void**)&type);

            return type;
        }

        void ReleaseObjects()
        {
            std::list<ComSmartPtr<TrackerObject>> objectsLocal;
            {
                std::lock_guard<std::mutex> guard{ _objectsLock };
                objectsLocal = std::move(_objects);
            }

            // Unpeg all instances
            for (auto& i : objectsLocal)
                (void)i->DisconnectFromReferenceTrackerRuntime();

            size_t count = objectsLocal.size();
            if (_runtimeServices != nullptr)
                _runtimeServices->RemoveMemoryPressure(sizeof(TrackerObject) * count);
        }

        HRESULT NotifyEndOfReferenceTrackingOnThread()
        {
            if (_runtimeServices != nullptr)
                return _runtimeServices->NotifyEndOfReferenceTrackingOnThread();

            return S_OK;
        }

    public: // IReferenceTrackerManager
        STDMETHOD(ReferenceTrackingStarted)()
        {
            std::lock_guard<std::mutex> guard{ _objectsLock };

            // Unpeg all instances
            for (auto& i : _objects)
                i->TogglePeg(/* should peg */ false);

            return S_OK;
        }

        STDMETHOD(FindTrackerTargetsCompleted)(_In_ BOOL bWalkFailed)
        {
            std::lock_guard<std::mutex> guard{ _objectsLock };

            // Verify and ensure all connected types are pegged
            for (auto& i : _objects)
                i->TogglePeg(/* should peg */ true);

            return S_OK;
        }

        STDMETHOD(ReferenceTrackingCompleted)()
        {
            return S_OK;
        }

        STDMETHOD(SetReferenceTrackerHost)(_In_ API::IReferenceTrackerHost* pHostServices)
        {
            assert(pHostServices != nullptr);
            return pHostServices->QueryInterface(API::IID_IReferenceTrackerHost, (void**)&_runtimeServices);
        }

        // Lifetime maintained by stack - we don't care about ref counts
        STDMETHOD_(ULONG, AddRef)() { return 1; }
        STDMETHOD_(ULONG, Release)() { return 1; }

        STDMETHOD(QueryInterface)(
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void ** ppvObject)
        {
            if (ppvObject == nullptr)
                return E_POINTER;

            if (IsEqualIID(riid, API::IID_IReferenceTrackerManager))
            {
                *ppvObject = static_cast<API::IReferenceTrackerManager*>(this);
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

            AddRef();
            return S_OK;
        }
    };

    TrackerRuntimeManagerImpl TrackerRuntimeManager;

    HRESULT STDMETHODCALLTYPE TrackerObject::TrackerObjectImpl::ConnectFromTrackerSource()
    {
        _connected = true;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE TrackerObject::TrackerObjectImpl::DisconnectFromTrackerSource()
    {
        _connected = false;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE TrackerObject::TrackerObjectImpl::FindTrackerTargets(_In_ API::IFindReferenceTargetsCallback* pCallback)
    {
        assert(pCallback != nullptr);

        ComSmartPtr<API::IReferenceTrackerTarget> mowMaybe;
        for (auto& e : _elements)
        {
            if (S_OK == e.second->QueryInterface(API::IID_IReferenceTrackerTarget, (void**)&mowMaybe))
            {
                (void)pCallback->FoundTrackerTarget(mowMaybe.p);
                mowMaybe.Release();
            }
        }

        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE TrackerObject::TrackerObjectImpl::GetReferenceTrackerManager(_Outptr_ API::IReferenceTrackerManager** ppTrackerManager)
    {
        assert(ppTrackerManager != nullptr);
        return TrackerRuntimeManager.QueryInterface(API::IID_IReferenceTrackerManager, (void**)ppTrackerManager);
    }

    HRESULT STDMETHODCALLTYPE TrackerObject::TrackerObjectImpl::AddRefFromTrackerSource()
    {
        assert(0 <= _trackerSourceCount);
        ++_trackerSourceCount;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE TrackerObject::TrackerObjectImpl::ReleaseFromTrackerSource()
    {
        assert(0 < _trackerSourceCount);
        --_trackerSourceCount;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE TrackerObject::TrackerObjectImpl::PegFromTrackerSource()
    {
        /* Not used by runtime */
        return E_NOTIMPL;
    }
}

// Create external object
extern "C" DLL_EXPORT ITrackerObject * STDMETHODCALLTYPE CreateTrackerObject_SkipTrackerRuntime()
{
    auto obj = new TrackerObject{ static_cast<size_t>(-1), nullptr };
    return &obj->_impl;
}

extern "C" DLL_EXPORT ITrackerObject* STDMETHODCALLTYPE CreateTrackerObject_Unsafe(_In_opt_ IUnknown* outer, _Outptr_ IUnknown** inner)
{
    ComSmartPtr<TrackerObject> obj;
    obj.Attach(new TrackerObject{ CurrentObjectId++, outer });

    return TrackerRuntimeManager.RecordObject(obj, inner);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE StartTrackerObjectAllocationCount_Unsafe()
{
    TrackerObject::TrackedAllocationCount = TrackerObject::EnableTrackedCount;
}

extern "C" DLL_EXPORT int32_t STDMETHODCALLTYPE StopTrackerObjectAllocationCount_Unsafe()
{
    int32_t count = TrackerObject::TrackedAllocationCount;
    TrackerObject::TrackedAllocationCount = TrackerObject::DisableTrackedCount;
    return count;
}

// Release the reference on all internally held tracker objects
extern "C" DLL_EXPORT void STDMETHODCALLTYPE ReleaseAllTrackerObjects()
{
    TrackerRuntimeManager.ReleaseObjects();
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE Trigger_NotifyEndOfReferenceTrackingOnThread()
{
    return TrackerRuntimeManager.NotifyEndOfReferenceTrackingOnThread();
}

extern "C" DLL_EXPORT bool STDMETHODCALLTYPE IsTrackerObjectConnected(IUnknown* inst)
{
    auto trackerObject = reinterpret_cast<TrackerObject::TrackerObjectImpl*>(inst);
    return trackerObject->IsConnected();
}

extern "C" DLL_EXPORT void* STDMETHODCALLTYPE TrackerTarget_AddRefFromReferenceTrackerAndReturn(IUnknown *obj)
{
    assert(obj != nullptr);

    API::IReferenceTrackerTarget* targetMaybe;
    if (S_OK == obj->QueryInterface(API::IID_IReferenceTrackerTarget, (void**)&targetMaybe))
    {
        (void)targetMaybe->AddRefFromReferenceTracker();
        (void)targetMaybe->Release();

        // The IReferenceTrackerTarget instance is returned even after calling Release since
        // the Reference Tracker count is now extending the lifetime of the object.
        return targetMaybe;
    }

    return nullptr;
}

extern "C" DLL_EXPORT LONG STDMETHODCALLTYPE TrackerTarget_ReleaseFromReferenceTracker(API::IReferenceTrackerTarget *target)
{
    assert(target != nullptr);
    return (LONG)target->ReleaseFromReferenceTracker();
}

namespace
{
    using QueryInterface_t = HRESULT(STDMETHODCALLTYPE*)(void*,GUID*,void**);
    QueryInterface_t _qiToWrap;

    HRESULT STDMETHODCALLTYPE QueryInterfaceWrapper(void* _this, GUID* riid, void** ppvObject)
    {
        if (_qiToWrap == nullptr)
            return E_UNEXPECTED;
        return _qiToWrap(_this, riid, ppvObject);
    }
}

extern "C" DLL_EXPORT void* WrapQueryInterface(QueryInterface_t qiToWrap)
{
    _qiToWrap = qiToWrap;
    return (void*)&QueryInterfaceWrapper;
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE UpdateTestObjectAsIUnknown(IUnknown *obj, int i, IUnknown **out)
{
    if (obj == nullptr)
        return E_POINTER;

    HRESULT hr;
    ComSmartPtr<ITest> testObj;
    RETURN_IF_FAILED(obj->QueryInterface(IID_ITest, (void**)&testObj));
    RETURN_IF_FAILED(testObj->SetValue(i));

    *out = testObj.Detach();
    return S_OK;
}

#ifdef WINDOWS
extern "C" DLL_EXPORT int STDMETHODCALLTYPE UpdateTestObjectAsIDispatch(IDispatch *obj, int i, IDispatch **out)
{
    if (obj == nullptr)
        return E_POINTER;

    return UpdateTestObjectAsIUnknown(obj, i, (IUnknown**)out);
}
#endif // WINDOWS

extern "C" DLL_EXPORT int STDMETHODCALLTYPE UpdateTestObjectAsInterface(ITest *obj, int i, ITest **out)
{
    if (obj == nullptr)
        return E_POINTER;

    HRESULT hr;
    RETURN_IF_FAILED(obj->SetValue(i));

    obj->AddRef();
    *out = obj;
    return S_OK;
}
