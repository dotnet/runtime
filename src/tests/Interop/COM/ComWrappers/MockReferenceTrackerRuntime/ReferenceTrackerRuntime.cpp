// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>
#include <ComHelpers.h>
#include <unordered_map>
#include <list>
#include <inspectable.h>

namespace API
{
    // Documentation found at https://docs.microsoft.com/windows/win32/api/windows.ui.xaml.hosting.referencetracker/
    class DECLSPEC_UUID("64bd43f8-bfee-4ec4-b7eb-2935158dae21") IReferenceTrackerTarget : public IUnknown
    {
    public:
        STDMETHOD_(ULONG, AddRefFromReferenceTracker)() = 0;
        STDMETHOD_(ULONG, ReleaseFromReferenceTracker)() = 0;
        STDMETHOD(Peg)() = 0;
        STDMETHOD(Unpeg)() = 0;
    };

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
    struct DECLSPEC_UUID("447BB9ED-DA48-4ABC-8963-5BB5C3E0AA09") ITest : public IUnknown
    {
        STDMETHOD(SetValue)(int i) = 0;
    };

    struct DECLSPEC_UUID("42951130-245C-485E-B60B-4ED4254256F8") ITrackerObject : public IUnknown
    {
        STDMETHOD(AddObjectRef)(_In_ IUnknown* c, _Out_ int* id) = 0;
        STDMETHOD(DropObjectRef)(_In_ int id) = 0;
    };

    struct TrackerObject : public ITrackerObject, public API::IReferenceTracker, public UnknownImpl
    {
        const size_t _id;
        std::atomic<int> _trackerSourceCount;
        bool _connected;
        std::atomic<int> _elementId;
        std::unordered_map<int, ComSmartPtr<IUnknown>> _elements;

        TrackerObject(size_t id) : _id{ id }, _trackerSourceCount{ 0 }, _connected{ false }, _elementId{ 1 }
        { }

        HRESULT ToggleTargets(_In_ bool shouldPeg)
        {
            HRESULT hr;

            auto curr = std::begin(_elements);
            while (curr != std::end(_elements))
            {
                ComSmartPtr<API::IReferenceTrackerTarget> mowMaybe;
                if (S_OK == curr->second->QueryInterface(&mowMaybe))
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

            return S_OK;
        }

        STDMETHOD(AddObjectRef)(_In_ IUnknown* c, _Out_ int* id)
        {
            assert(c != nullptr && id != nullptr);

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

            ComSmartPtr<API::IReferenceTrackerTarget> mowMaybe;
            if (S_OK == c->QueryInterface(&mowMaybe))
                (void)mowMaybe->AddRefFromReferenceTracker();

            return S_OK;
        }

        STDMETHOD(DropObjectRef)(_In_ int id)
        {
            auto iter = _elements.find(id);
            if (iter == std::end(_elements))
                return S_FALSE;

            ComSmartPtr<API::IReferenceTrackerTarget> mowMaybe;
            if (S_OK == iter->second->QueryInterface(&mowMaybe))
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
            /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject)
        {
            return DoQueryInterface(riid, ppvObject, static_cast<IReferenceTracker*>(this), static_cast<ITrackerObject*>(this));
        }

        DEFINE_REF_COUNTING()
    };

    std::atomic<size_t> CurrentObjectId{};

    class TrackerRuntimeManagerImpl : public API::IReferenceTrackerManager
    {
        ComSmartPtr<API::IReferenceTrackerHost> _runtimeServices;
        std::list<ComSmartPtr<TrackerObject>> _objects;

    public:
        void RecordObject(_In_ TrackerObject* obj)
        {
            _objects.push_back(ComSmartPtr<TrackerObject>{ obj });

            if (_runtimeServices != nullptr)
                _runtimeServices->AddMemoryPressure(sizeof(TrackerObject));
        }

        void ReleaseObjects()
        {
            size_t count = _objects.size();
            _objects.clear();
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
            // Unpeg all instances
            for (auto& i : _objects)
                i->ToggleTargets(/* should peg */ false);

            return S_OK;
        }

        STDMETHOD(FindTrackerTargetsCompleted)(_In_ BOOL bWalkFailed)
        {
            // Verify and ensure all connected types are pegged
            for (auto& i : _objects)
                i->ToggleTargets(/* should peg */ true);

            return S_OK;
        }

        STDMETHOD(ReferenceTrackingCompleted)()
        {
            return S_OK;
        }

        STDMETHOD(SetReferenceTrackerHost)(_In_ API::IReferenceTrackerHost* pHostServices)
        {
            assert(pHostServices != nullptr);
            return pHostServices->QueryInterface(&_runtimeServices);
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

            if (IsEqualIID(riid, __uuidof(API::IReferenceTrackerManager)))
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

    HRESULT STDMETHODCALLTYPE TrackerObject::ConnectFromTrackerSource()
    {
        _connected = true;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE TrackerObject::DisconnectFromTrackerSource()
    {
        _connected = false;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE TrackerObject::FindTrackerTargets(_In_ API::IFindReferenceTargetsCallback* pCallback)
    {
        assert(pCallback != nullptr);

        ComSmartPtr<API::IReferenceTrackerTarget> mowMaybe;
        for (auto& e : _elements)
        {
            if (S_OK == e.second->QueryInterface(&mowMaybe))
            {
                (void)pCallback->FoundTrackerTarget(mowMaybe.p);
                mowMaybe.Release();
            }
        }

        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE TrackerObject::GetReferenceTrackerManager(_Outptr_ API::IReferenceTrackerManager** ppTrackerManager)
    {
        assert(ppTrackerManager != nullptr);
        return TrackerRuntimeManager.QueryInterface(__uuidof(API::IReferenceTrackerManager), (void**)ppTrackerManager);
    }

    HRESULT STDMETHODCALLTYPE TrackerObject::AddRefFromTrackerSource()
    {
        assert(0 <= _trackerSourceCount);
        ++_trackerSourceCount;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE TrackerObject::ReleaseFromTrackerSource()
    {
        assert(0 < _trackerSourceCount);
        --_trackerSourceCount;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE TrackerObject::PegFromTrackerSource()
    {
        /* Not used by runtime */
        return E_NOTIMPL;
    }
}

// Create external object
extern "C" DLL_EXPORT ITrackerObject* STDMETHODCALLTYPE CreateTrackerObject()
{
    auto obj = new TrackerObject{ CurrentObjectId++ };

    TrackerRuntimeManager.RecordObject(obj);

    return obj;
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

extern "C" DLL_EXPORT int STDMETHODCALLTYPE UpdateTestObjectAsIUnknown(IUnknown *obj, int i, IUnknown **out)
{
    if (obj == nullptr)
        return E_POINTER;

    HRESULT hr;
    ComSmartPtr<ITest> testObj;
    RETURN_IF_FAILED(obj->QueryInterface(&testObj))
    RETURN_IF_FAILED(testObj->SetValue(i));

    *out = testObj.Detach();
    return S_OK;
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE UpdateTestObjectAsIDispatch(IDispatch *obj, int i, IDispatch **out)
{
    if (obj == nullptr)
        return E_POINTER;

    return UpdateTestObjectAsIUnknown(obj, i, (IUnknown**)out);
}

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
