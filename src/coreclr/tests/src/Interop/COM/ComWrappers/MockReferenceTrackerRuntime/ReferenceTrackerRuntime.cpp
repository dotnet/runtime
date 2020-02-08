// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <ComHelpers.h>
#include <unordered_map>

namespace
{
    class TrackerRuntimeManagerImpl : public IReferenceTrackerManager
    {
        CComPtr<IReferenceTrackerHost> runtimeServices;
    public:
        STDMETHOD(ReferenceTrackingStarted)()
        {
            return E_NOTIMPL;
        }

        STDMETHOD(FindTrackerTargetsCompleted)(_In_ BOOL bWalkFailed)
        {
            return E_NOTIMPL;
        }

        STDMETHOD(ReferenceTrackingCompleted)()
        {
            return E_NOTIMPL;
        }

        STDMETHOD(SetReferenceTrackerHost)(_In_ IReferenceTrackerHost* pHostServices)
        {
            assert(pHostServices != nullptr);
            return pHostServices->QueryInterface(&runtimeServices);
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

            if (IsEqualIID(riid, __uuidof(IReferenceTrackerManager)))
            {
                *ppvObject = static_cast<IReferenceTrackerManager*>(this);
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

    struct DECLSPEC_UUID("447BB9ED-DA48-4ABC-8963-5BB5C3E0AA09") ITest : public IUnknown
    {
        STDMETHOD(SetValue)(int i) = 0;
    };

    struct ExternalObject : public IExternalObject, public IReferenceTracker, public UnknownImpl
    {
        const size_t _id;
        std::atomic<int> _trackerCount;
        bool _connected;
        std::unordered_map<size_t, CComPtr<IUnknown>> _elements;

        ExternalObject(size_t id) : _id{ id }, _trackerCount{0}, _connected{ false }
        { }

        STDMETHOD(AddObjectRef)(_In_ IUnknown* c)
        {
            assert(c != nullptr);

            try
            {
                if (!_elements.insert(std::make_pair(size_t(c), CComPtr<IUnknown>{ c })).second)
                    return S_FALSE;
            }
            catch (const std::bad_alloc&)
            {
                return E_OUTOFMEMORY;
            }

            CComPtr<IReferenceTrackerTarget> mowMaybe;
            if (S_OK == c->QueryInterface(&mowMaybe))
                (void)mowMaybe->AddRefFromReferenceTracker();

            return S_OK;
        }

        STDMETHOD(DropObjectRef)(_In_ IUnknown* c)
        {
            assert(c != nullptr);
            auto erased_count = _elements.erase((size_t)c);
            if (erased_count > 0)
            {
                CComPtr<IReferenceTrackerTarget> mowMaybe;
                if (S_OK == c->QueryInterface(&mowMaybe))
                {
                    for (decltype(erased_count) i = 0; i < erased_count; ++i)
                        (void)mowMaybe->ReleaseFromReferenceTracker();
                }
            }

            return S_OK;
        }

        STDMETHOD(UseObjectRefs)()
        {
            return E_NOTIMPL;
        }

        STDMETHOD(ConnectFromTrackerSource)();
        STDMETHOD(DisconnectFromTrackerSource)();
        STDMETHOD(FindTrackerTargets)(_In_ IFindReferenceTargetsCallback* pCallback);
        STDMETHOD(GetReferenceTrackerManager)(_Outptr_ IReferenceTrackerManager** ppTrackerManager);
        STDMETHOD(AddRefFromTrackerSource)();
        STDMETHOD(ReleaseFromTrackerSource)();
        STDMETHOD(PegFromTrackerSource)();

        STDMETHOD(QueryInterface)(
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject)
        {
            return DoQueryInterface(riid, ppvObject, static_cast<IReferenceTracker*>(this));
        }

        DEFINE_REF_COUNTING()
    };

    HRESULT STDMETHODCALLTYPE ExternalObject::ConnectFromTrackerSource()
    {
        _connected = true;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE ExternalObject::DisconnectFromTrackerSource()
    {
        _connected = false;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE ExternalObject::FindTrackerTargets(_In_ IFindReferenceTargetsCallback* pCallback)
    {
        assert(pCallback != nullptr);

        CComPtr<IReferenceTrackerTarget> mowMaybe;
        for (auto &e : _elements)
        {
            if (S_OK == e.second->QueryInterface(&mowMaybe))
            {
                (void)pCallback->FoundTrackerTarget(mowMaybe.p);
                mowMaybe.Release();
            }
        }

        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE ExternalObject::GetReferenceTrackerManager(_Outptr_ IReferenceTrackerManager** ppTrackerManager)
    {
        assert(ppTrackerManager != nullptr);
        return TrackerRuntimeManager.QueryInterface(__uuidof(IReferenceTrackerManager), (void**)ppTrackerManager);
    }

    HRESULT STDMETHODCALLTYPE ExternalObject::AddRefFromTrackerSource()
    {
        assert(0 <= _trackerCount);
        ++_trackerCount;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE ExternalObject::ReleaseFromTrackerSource()
    {
        assert(0 < _trackerCount);
        --_trackerCount;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE ExternalObject::PegFromTrackerSource()
    {
        /* Not used by runtime */
        return E_NOTIMPL;
    }

    std::atomic<size_t> CurrentObjectId{};
}

IExternalObject* STDMETHODCALLTYPE CreateExternalObject()
{
    return new ExternalObject{ CurrentObjectId++ };
}
