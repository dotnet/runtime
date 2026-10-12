// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>
#include <cassert>
#include <Server.Contracts.h>
#include <windows_version_helpers.h>

// COM headers
#include <objbase.h>
#include <combaseapi.h>

#define COM_CLIENT
#include <Servers.h>

#define THROW_IF_FAILED(exp) { hr = exp; if (FAILED(hr)) { ::printf("FAILURE: 0x%08x = %s\n", hr, #exp); throw hr; } }
#define THROW_FAIL_IF_FALSE(exp) { if (!(exp)) { ::printf("FALSE: %s\n", #exp); throw E_FAIL; } }

#include <string>
#include <atomic>
#include <chrono>
#include <functional>
#include <future>
#include <thread>

namespace
{
    struct __declspec(uuid("C2CF7A31-FA94-48E2-AB89-64E6B4378EF5")) IEventRemovalTesting : IUnknown
    {
        virtual HRESULT STDMETHODCALLTYPE ConfigureRemoval(BOOL throwOnce, TestingEvents* callback) = 0;
        virtual HRESULT STDMETHODCALLTYPE GetRemoveCount(int* count) = 0;
    };

    class DispIDToStringMap
    {
        struct Pair
        {
            DISPID id;
            WCHAR value[128];
        };
        Pair _pairs[8];
        const Pair* _end;

    public:
        DispIDToStringMap()
            : _pairs{}
            , _end{ _pairs + ARRAY_SIZE(_pairs) }
        {
            for (auto curr = _pairs; curr != _end; ++curr)
                curr->id = DISPID_UNKNOWN;
        }

        const WCHAR* Find(_In_ DISPID id)
        {
            for (auto curr = _pairs; curr != _end; ++curr)
            {
                if (curr->id == id)
                    return curr->value;
            }

            return nullptr;
        }

        void Insert(_In_ DISPID id, _In_z_ const WCHAR* value)
        {
            if (id == DISPID_UNKNOWN)
                throw E_UNEXPECTED;

            for (auto curr = _pairs; curr != _end; ++curr)
            {
                if (curr->id == DISPID_UNKNOWN)
                {
                    curr->id = id;
                    size_t len = ::wcslen(value) + 1; // Include null
                    ::memcpy(curr->value, value, len * sizeof(value[0]));
                    return;
                }
            }

            throw E_UNEXPECTED;
        }

        void Erase(_In_ DISPID id)
        {
            for (auto curr = _pairs; curr != _end; ++curr)
            {
                if (curr->id == id)
                {
                    curr->id = DISPID_UNKNOWN;
                    break;
                }
            }
        }
    };

    class EventSink : public UnknownImpl, public TestingEvents
    {
        DispIDToStringMap _firedEvents;
        std::function<HRESULT()> _onRemove;

    public:
        void SetRemoveCallback(std::function<HRESULT()> callback)
        {
            _onRemove = std::move(callback);
        }

        void ResetFiredState(_In_ DISPID id)
        {
            _firedEvents.Erase(id);
        }

        bool DidFire(_In_ DISPID id, _Out_ std::wstring& message)
        {
            auto value = _firedEvents.Find(id);
            if (value == nullptr)
                return false;

            message = value;
            return true;
        }

    public: // IDispatch
        virtual HRESULT STDMETHODCALLTYPE GetTypeInfoCount(
            /* [out] */ __RPC__out uint32_t* pctinfo)
        {
            return E_NOTIMPL;
        }

        virtual HRESULT STDMETHODCALLTYPE GetTypeInfo(
            /* [in] */ uint32_t iTInfo,
            /* [in] */ LCID lcid,
            /* [out] */ __RPC__deref_out_opt ITypeInfo** ppTInfo)
        {
            return E_NOTIMPL;
        }

        virtual HRESULT STDMETHODCALLTYPE GetIDsOfNames(
            /* [in] */ __RPC__in REFIID riid,
            /* [size_is][in] */ __RPC__in_ecount_full(cNames) LPOLESTR* rgszNames,
            /* [range][in] */ __RPC__in_range(0, 16384) uint32_t cNames,
            /* [in] */ LCID lcid,
            /* [size_is][out] */ __RPC__out_ecount_full(cNames) DISPID* rgDispId)
        {
            return E_NOTIMPL;
        }

        virtual /* [local] */ HRESULT STDMETHODCALLTYPE Invoke(
            /* [annotation][in] */
            _In_  DISPID dispIdMember,
            /* [annotation][in] */
            _In_  REFIID riid,
            /* [annotation][in] */
            _In_  LCID lcid,
            /* [annotation][in] */
            _In_  uint16_t wFlags,
            /* [annotation][out][in] */
            _In_  DISPPARAMS* pDispParams,
            /* [annotation][out] */
            _Out_opt_  VARIANT* pVarResult,
            /* [annotation][out] */
            _Out_opt_  EXCEPINFO* pExcepInfo,
            /* [annotation][out] */
            _Out_opt_  uint32_t* puArgErr)
        {
            //
            // Note that arguments are received in reverse order for IDispatch::Invoke()
            //

            switch (dispIdMember)
            {
            case DISPATCHTESTINGEVENTS_DISPID_ONEVENT:
            {
                return OnFireEventHandler(dispIdMember, pDispParams);
            }
            }

            return E_NOTIMPL;
        }

    private:
        HRESULT OnFireEventHandler(_In_ DISPID dispId, _In_ DISPPARAMS* dispParams)
        {
            if (_onRemove)
                return _onRemove();

            if (dispParams == nullptr)
                return E_POINTER;

            if (dispParams->cArgs != 1)
                return E_INVALIDARG;

            VARIANTARG* msgMaybe = dispParams->rgvarg;
            if (msgMaybe->vt != VT_BSTR)
                return E_INVALIDARG;

            _firedEvents.Insert(dispId, msgMaybe->bstrVal);
            return S_OK;
        }

    public: // IUnknown
        STDMETHOD(QueryInterface)(
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject)
        {
            return DoQueryInterface(riid, ppvObject,
                static_cast<TestingEvents*>(this),
                static_cast<IDispatch*>(this));
        }

        DEFINE_REF_COUNTING();
    };

    void VerifyAdviseUnadviseFromEvent()
    {
        HRESULT hr;

        ComSmartPtr<IEventTesting> et;
        THROW_IF_FAILED(::CoCreateInstance(CLSID_EventTesting, nullptr, CLSCTX_INPROC, IID_IEventTesting, (void**)&et));

        ComSmartPtr<IConnectionPointContainer> cpc;
        THROW_IF_FAILED(et->QueryInterface(&cpc));

        ComSmartPtr<IConnectionPoint> cp;
        THROW_IF_FAILED(cpc->FindConnectionPoint(IID_TestingEvents, &cp));

        // Create event sink
        ComSmartPtr<EventSink> es;
        es.Attach(new EventSink());

        DWORD cookie;
        ComSmartPtr<IUnknown> uk;
        THROW_IF_FAILED(es->QueryInterface(IID_IUnknown, (void**)&uk));
        THROW_IF_FAILED(cp->Advise(uk, &cookie));

        // Ensure state is valid.
        es->ResetFiredState(DISPATCHTESTINGEVENTS_DISPID_ONEVENT);

        THROW_IF_FAILED(et->FireEvent());

        // Validate the event fired.
        {
            std::wstring eventName;
            THROW_FAIL_IF_FALSE(es->DidFire(DISPATCHTESTINGEVENTS_DISPID_ONEVENT, eventName));
            THROW_FAIL_IF_FALSE(eventName.compare(L"FireEvent") == 0);
        }

        THROW_IF_FAILED(cp->Unadvise(cookie));

        // Reset state.
        es->ResetFiredState(DISPATCHTESTINGEVENTS_DISPID_ONEVENT);

        THROW_IF_FAILED(et->FireEvent());

        // Validate the event was not fired.
        {
            std::wstring eventName;
            THROW_FAIL_IF_FALSE(!es->DidFire(DISPATCHTESTINGEVENTS_DISPID_ONEVENT, eventName));
            THROW_FAIL_IF_FALSE(eventName.empty());
        }
    }

    void VerifyEnumConnectionPoints()
    {
        HRESULT hr;

        ComSmartPtr<IEventTesting> et;
        THROW_IF_FAILED(::CoCreateInstance(CLSID_EventTesting, nullptr, CLSCTX_INPROC, IID_IEventTesting, (void**)&et));

        ComSmartPtr<IConnectionPointContainer> cpc;
        THROW_IF_FAILED(et->QueryInterface(&cpc));

        ComSmartPtr<IEnumConnectionPoints> ecp;
        THROW_IF_FAILED(cpc->EnumConnectionPoints(&ecp));

        bool foundEventInterface = false;
        ULONG fetched;
        LPCONNECTIONPOINT ptRaw = nullptr;
        while ((hr = ecp->Next(1, &ptRaw, &fetched)) == S_OK)
        {
            THROW_FAIL_IF_FALSE(fetched == 1);
            THROW_FAIL_IF_FALSE(ptRaw != nullptr);

            ComSmartPtr<IConnectionPoint> pt;
            pt.Attach(ptRaw);
            ptRaw = nullptr;

            IID iidMaybe;
            THROW_IF_FAILED(pt->GetConnectionInterface(&iidMaybe));
            foundEventInterface = (iidMaybe == IID_TestingEvents);

            // There should only be one event interface
            THROW_FAIL_IF_FALSE(foundEventInterface);
        }

        THROW_IF_FAILED(hr);
        THROW_FAIL_IF_FALSE(foundEventInterface);
    }

    void VerifyRemovalClaim(bool concurrent)
    {
        HRESULT hr;
        ComSmartPtr<IEventTesting> et;
        THROW_IF_FAILED(::CoCreateInstance(CLSID_EventTesting, nullptr, CLSCTX_INPROC, IID_IEventTesting, (void**)&et));
        ComSmartPtr<IEventRemovalTesting> control;
        THROW_IF_FAILED(et->QueryInterface(__uuidof(IEventRemovalTesting), (void**)&control));
        ComSmartPtr<IConnectionPointContainer> cpc;
        THROW_IF_FAILED(et->QueryInterface(&cpc));
        ComSmartPtr<IConnectionPoint> cp;
        THROW_IF_FAILED(cpc->FindConnectionPoint(IID_TestingEvents, &cp));
        ComSmartPtr<EventSink> sink;
        sink.Attach(new EventSink());
        DWORD cookie;
        THROW_IF_FAILED(cp->Advise(static_cast<TestingEvents*>(sink.p), &cookie));

        THROW_IF_FAILED(control->ConfigureRemoval(TRUE, nullptr));
        THROW_FAIL_IF_FALSE(cp->Unadvise(cookie) == E_FAIL);
        ComSmartPtr<IEnumConnections> connections;
        THROW_IF_FAILED(cp->EnumConnections(&connections));
        CONNECTDATA data{};
        ULONG fetched;
        THROW_FAIL_IF_FALSE(connections->Next(1, &data, &fetched) == S_OK);
        ComSmartPtr<IUnknown> enumerated;
        enumerated.Attach(data.pUnk);
        THROW_FAIL_IF_FALSE(fetched == 1 && data.dwCookie == cookie);

        ComSmartPtr<EventSink> callback;
        callback.Attach(new EventSink());
        HRESULT competingResult = E_UNEXPECTED;
        if (concurrent)
        {
            std::promise<void> entered;
            std::future<void> enteredFuture = entered.get_future();
            std::promise<void> resume;
            std::shared_future<void> resumeFuture = resume.get_future().share();
            std::atomic<int> calls{0};
            callback->SetRemoveCallback([&]()
            {
                if (calls.fetch_add(1) == 0)
                    entered.set_value();
                return resumeFuture.wait_for(std::chrono::seconds(30)) == std::future_status::ready ? S_OK : E_FAIL;
            });
            THROW_IF_FAILED(control->ConfigureRemoval(FALSE, static_cast<TestingEvents*>(callback.p)));
            HRESULT workerResult = E_UNEXPECTED;
            std::thread worker([&]()
            {
                HRESULT init = ::CoInitializeEx(nullptr, COINIT_MULTITHREADED);
                workerResult = FAILED(init) ? init : cp->Unadvise(cookie);
                if (SUCCEEDED(init))
                    ::CoUninitialize();
            });
            bool ready = enteredFuture.wait_for(std::chrono::seconds(30)) == std::future_status::ready;
            bool visibleDuringRemoval = false;
            if (ready)
            {
                if (connections->Reset() == S_OK && connections->Next(1, &data, &fetched) == S_OK)
                {
                    enumerated.Attach(data.pUnk);
                    visibleDuringRemoval = fetched == 1 && data.dwCookie == cookie;
                }
                competingResult = cp->Unadvise(cookie);
            }
            resume.set_value();
            worker.join();
            THROW_FAIL_IF_FALSE(ready && visibleDuringRemoval && workerResult == S_OK && calls.load() == 1);
        }
        else
        {
            bool entered = false;
            callback->SetRemoveCallback([&]()
            {
                if (!entered)
                {
                    entered = true;
                    competingResult = cp->Unadvise(cookie);
                }
                return S_OK;
            });
            THROW_IF_FAILED(control->ConfigureRemoval(FALSE, static_cast<TestingEvents*>(callback.p)));
            THROW_IF_FAILED(cp->Unadvise(cookie));
        }
        THROW_FAIL_IF_FALSE(competingResult == CONNECT_E_NOCONNECTION);
        int removeCount;
        THROW_IF_FAILED(control->GetRemoveCount(&removeCount));
        THROW_FAIL_IF_FALSE(removeCount == 2);
        THROW_FAIL_IF_FALSE(cp->Unadvise(cookie) == CONNECT_E_NOCONNECTION);

        THROW_IF_FAILED(connections->Reset());
        THROW_FAIL_IF_FALSE(connections->Next(1, &data, &fetched) == S_FALSE && fetched == 0);
        // Insertion after rollback and removal exercises the last-inserted bookkeeping.
        THROW_IF_FAILED(control->ConfigureRemoval(FALSE, nullptr));
        THROW_IF_FAILED(cp->Advise(static_cast<TestingEvents*>(sink.p), &cookie));
        THROW_IF_FAILED(cp->Unadvise(cookie));
    }

    void VerifyEnumConnectionsWithRemoval()
    {
        HRESULT hr;
        ComSmartPtr<IEventTesting> et;
        THROW_IF_FAILED(::CoCreateInstance(CLSID_EventTesting, nullptr, CLSCTX_INPROC, IID_IEventTesting, (void**)&et));
        ComSmartPtr<IConnectionPointContainer> cpc;
        THROW_IF_FAILED(et->QueryInterface(&cpc));
        ComSmartPtr<IConnectionPoint> cp;
        THROW_IF_FAILED(cpc->FindConnectionPoint(IID_TestingEvents, &cp));
        ComSmartPtr<EventSink> sink;
        sink.Attach(new EventSink());
        DWORD cookies[3];
        for (DWORD& cookie : cookies)
            THROW_IF_FAILED(cp->Advise(static_cast<TestingEvents*>(sink.p), &cookie));

        ComSmartPtr<IEnumConnections> connections;
        THROW_IF_FAILED(cp->EnumConnections(&connections));
        THROW_IF_FAILED(cp->Unadvise(cookies[0]));
        THROW_FAIL_IF_FALSE(connections->Skip(1) == S_OK);
        ComSmartPtr<IEnumConnections> clone;
        THROW_IF_FAILED(connections->Clone(&clone));
        THROW_IF_FAILED(cp->Unadvise(cookies[2]));
        DWORD inserted;
        THROW_IF_FAILED(cp->Advise(static_cast<TestingEvents*>(sink.p), &inserted));
        THROW_FAIL_IF_FALSE(inserted == cookies[2]);

        for (IEnumConnections* current : {connections.p, clone.p})
        {
            CONNECTDATA data{};
            ULONG fetched;
            THROW_FAIL_IF_FALSE(current->Next(1, &data, &fetched) == S_OK);
            ComSmartPtr<IUnknown> enumerated;
            enumerated.Attach(data.pUnk);
            THROW_FAIL_IF_FALSE(fetched == 1 && data.dwCookie == inserted);
            THROW_FAIL_IF_FALSE(current->Next(1, &data, &fetched) == S_FALSE && fetched == 0);
            THROW_FAIL_IF_FALSE(current->Skip(1) == S_FALSE);
            THROW_FAIL_IF_FALSE(current->Skip(0) == S_OK);
        }

        THROW_IF_FAILED(connections->Reset());
        THROW_IF_FAILED(cp->Unadvise(cookies[1]));
        THROW_FAIL_IF_FALSE(connections->Skip(1) == S_OK);
        THROW_IF_FAILED(cp->Unadvise(inserted));
        THROW_IF_FAILED(cp->Advise(static_cast<TestingEvents*>(sink.p), &inserted));
        CONNECTDATA data{};
        ULONG fetched;
        THROW_FAIL_IF_FALSE(connections->Next(1, &data, &fetched) == S_FALSE && fetched == 0);
        THROW_IF_FAILED(connections->Reset());
        THROW_FAIL_IF_FALSE(connections->Next(1, &data, &fetched) == S_OK);
        ComSmartPtr<IUnknown> enumerated;
        enumerated.Attach(data.pUnk);
        THROW_FAIL_IF_FALSE(fetched == 1 && data.dwCookie == inserted);
        THROW_IF_FAILED(cp->Unadvise(inserted));
    }
}

template<COINIT TM>
struct ComInit
{
    const HRESULT Result;

    ComInit()
        : Result{ ::CoInitializeEx(nullptr, TM) }
    { }

    ~ComInit()
    {
        if (SUCCEEDED(Result))
            ::CoUninitialize();
    }
};

using ComMTA = ComInit<COINIT_MULTITHREADED>;

int __cdecl main()
{
    if (is_windows_nano() == S_OK)
    {
        ::puts("RegFree COM is not supported on Windows Nano. Auto-passing this test.\n");
        return 100;
    }
    ComMTA init;
    if (FAILED(init.Result))
        return -1;

    try
    {
        CoreShimComActivation csact{ W("NETServer"), W("EventTesting") };

        VerifyAdviseUnadviseFromEvent();
        VerifyEnumConnectionPoints();
        VerifyRemovalClaim(false);
        VerifyRemovalClaim(true);
        VerifyEnumConnectionsWithRemoval();
    }
    catch (HRESULT hr)
    {
        ::printf("Test Failure: 0x%08x\n", hr);
        return 101;
    }

    return 100;
}
