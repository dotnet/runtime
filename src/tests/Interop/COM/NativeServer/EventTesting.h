// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "Servers.h"

class EventTesting :
    public UnknownImpl,
    public IEventTesting,
    public IConnectionPointContainer,
    public IConnectionPoint
{
private: // static
    static const WCHAR * const Names[];
    static const int NamesCount;

private:
    IDispatch *_eventConnections[32];

public:
    EventTesting()
    {
        // Ensure connections array is null
        ::memset(_eventConnections, 0, sizeof(_eventConnections));
    }

public: // IDispatch
        virtual HRESULT STDMETHODCALLTYPE GetTypeInfoCount(
            /* [out] */ __RPC__out uint32_t *pctinfo)
        {
            *pctinfo = 0;
            return S_OK;
        }

        virtual HRESULT STDMETHODCALLTYPE GetTypeInfo(
            /* [in] */ uint32_t iTInfo,
            /* [in] */ LCID lcid,
            /* [out] */ __RPC__deref_out_opt ITypeInfo **ppTInfo)
        {
            return E_NOTIMPL;
        }

        virtual HRESULT STDMETHODCALLTYPE GetIDsOfNames(
            /* [in] */ __RPC__in REFIID,
            /* [size_is][in] */ __RPC__in_ecount_full(cNames) LPOLESTR *rgszNames,
            /* [range][in] */ __RPC__in_range(0,16384) uint32_t cNames,
            /* [in] */ LCID,
            /* [size_is][out] */ __RPC__out_ecount_full(cNames) DISPID *rgDispId)
        {
            bool containsUnknown = false;
            DISPID *curr = rgDispId;
            for (uint32_t i = 0; i < cNames; ++i)
            {
                *curr = DISPID_UNKNOWN;
                LPOLESTR name = rgszNames[i];
                for (int j = 1; j < NamesCount; ++j)
                {
                    const WCHAR *nameMaybe = Names[j];
                    if (::TP_wcmp_s(name, nameMaybe) == 0)
                    {
                        *curr = DISPID{ j };
                        break;
                    }
                }

                containsUnknown &= (*curr == DISPID_UNKNOWN);
                curr++;
            }

            return (containsUnknown) ? DISP_E_UNKNOWNNAME : S_OK;
        }

        virtual /* [local] */ HRESULT STDMETHODCALLTYPE Invoke(
            /* [annotation][in] */ _In_  DISPID dispIdMember,
            /* [annotation][in] */ _In_  REFIID riid,
            /* [annotation][in] */ _In_  LCID lcid,
            /* [annotation][in] */ _In_  uint16_t wFlags,
            /* [annotation][out][in] */ _In_  DISPPARAMS *pDispParams,
            /* [annotation][out] */ _Out_opt_  VARIANT *pVarResult,
            /* [annotation][out] */ _Out_opt_  EXCEPINFO *pExcepInfo,
            /* [annotation][out] */ _Out_opt_  uint32_t *puArgErr)
        {
            //
            // Note that arguments are received in reverse order for IDispatch::Invoke()
            //

            switch (dispIdMember)
            {
            case 1:
            {
                return FireEvent();
            }
            }

            return E_NOTIMPL;
        }

public: // IEventTesting
    virtual HRESULT STDMETHODCALLTYPE FireEvent()
    {
        return FireEvent_Impl(1 /* DISPID for the FireEvent function */);
    }

public: // IConnectionPointContainer
    virtual HRESULT STDMETHODCALLTYPE EnumConnectionPoints(
        /* [out] */ __RPC__deref_out_opt IEnumConnectionPoints **ppEnum)
    {
        return E_NOTIMPL;
    }
    virtual HRESULT STDMETHODCALLTYPE FindConnectionPoint(
        /* [in] */ __RPC__in REFIID riid,
        /* [out] */ __RPC__deref_out_opt IConnectionPoint **ppCP)
    {
        if (riid != IID_TestingEvents)
            return CONNECT_E_NOCONNECTION;

        return QueryInterface(__uuidof(*ppCP), (void**)ppCP);
    }

public: // IConnectionPoint
    virtual HRESULT STDMETHODCALLTYPE GetConnectionInterface(
        /* [out] */ __RPC__out IID *pIID)
    {
        return E_NOTIMPL;
    }
    virtual HRESULT STDMETHODCALLTYPE GetConnectionPointContainer(
        /* [out] */ __RPC__deref_out_opt IConnectionPointContainer **ppCPC)
    {
        return E_NOTIMPL;
    }
    virtual HRESULT STDMETHODCALLTYPE Advise(
        /* [in] */ __RPC__in_opt IUnknown *pUnkSink,
        /* [out] */ __RPC__out DWORD *pdwCookie)
    {
        if (pUnkSink == nullptr || pdwCookie == nullptr)
            return E_POINTER;

        for (DWORD i = 0; i < ARRAY_SIZE(_eventConnections); ++i)
        {
            if (_eventConnections[i] == nullptr)
            {
                IDispatch *handler;
                HRESULT hr = pUnkSink->QueryInterface(IID_IDispatch, (void**)&handler);
                if (hr != S_OK)
                    return CONNECT_E_CANNOTCONNECT;

                _eventConnections[i] = handler;
                *pdwCookie = i;
                return S_OK;
            }
        }

        return CONNECT_E_ADVISELIMIT;
    }
    virtual HRESULT STDMETHODCALLTYPE Unadvise(
        /* [in] */ DWORD dwCookie)
    {
        if (0 <= dwCookie && dwCookie < ARRAY_SIZE(_eventConnections))
        {
            IDispatch *handler = _eventConnections[dwCookie];
            if (handler != nullptr)
            {
                _eventConnections[dwCookie] = nullptr;
                handler->Release();
                return S_OK;
            }
        }

        return E_POINTER;
    }
    virtual HRESULT STDMETHODCALLTYPE EnumConnections(
        /* [out] */ __RPC__deref_out_opt IEnumConnections **ppEnum)
    {
        return E_NOTIMPL;
    }

private:
    HRESULT FireEvent_Impl(_In_ int dispId)
    {
        HRESULT hr = S_OK;

        VARIANTARG arg;
        ::VariantInit(&arg);

        arg.vt = VT_BSTR;
        arg.bstrVal = TP_SysAllocString(Names[dispId]);

        for (DWORD i = 0; i < ARRAY_SIZE(_eventConnections); ++i)
        {
            IDispatch *handler = _eventConnections[i];
            if (handler != nullptr)
            {
                DISPPARAMS params{};
                params.rgvarg = &arg;
                params.cArgs = 1;
                hr = handler->Invoke(
                    DISPATCHTESTINGEVENTS_DISPID_ONEVENT,
                    IID_NULL,
                    0,
                    DISPATCH_METHOD,
                    &params,
                    nullptr,
                    nullptr,
                    nullptr);

                if (FAILED(hr))
                    break;
            }
        }

        return ::VariantClear(&arg);
    }

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject,
            static_cast<IDispatch *>(this),
            static_cast<IEventTesting *>(this),
            static_cast<IConnectionPointContainer *>(this),
            static_cast<IConnectionPoint *>(this));
    }

    DEFINE_REF_COUNTING();
};

const WCHAR * const EventTesting::Names[] =
{
    W("__RESERVED__"),
    W("FireEvent"),
};

const int EventTesting::NamesCount = ARRAY_SIZE(EventTesting::Names);
