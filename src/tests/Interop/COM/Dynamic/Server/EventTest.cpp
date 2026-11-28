// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "EventTest.h"

namespace
{
    class ConnectionPoint : public UnknownImpl, public IConnectionPoint
    {
    public:
        ConnectionPoint(const IID iid)
            : _iid { iid }
        {
            _sinks.fill(nullptr);
        }

    public:
        HRESULT STDMETHODCALLTYPE FireEventImpl(DISPID dispId, VARIANTARG *arg)
        {
            HRESULT hr;
            for (uint32_t i = 0; i < _sinks.size(); ++i)
            {
                IDispatch *handler = _sinks[i];
                if (handler == nullptr)
                    continue;

                DISPPARAMS params{};
                params.rgvarg = arg;
                params.cArgs = 1;
                hr = handler->Invoke(
                    dispId,
                    IID_NULL,
                    0,
                    DISPATCH_METHOD,
                    &params,
                    nullptr,
                    nullptr,
                    nullptr);

                if (FAILED(hr))
                    return hr;
            }

            return S_OK;
        }

    public: // IConnectionPoint
        HRESULT STDMETHODCALLTYPE GetConnectionInterface(
            /* [out] */ __RPC__out IID *pIID)
        {
            *pIID = _iid;
            return S_OK;
        }

        HRESULT STDMETHODCALLTYPE GetConnectionPointContainer(
            /* [out] */ __RPC__deref_out_opt IConnectionPointContainer **ppCPC)
        {
            return E_NOTIMPL;
        }

        HRESULT STDMETHODCALLTYPE Advise(
            /* [in] */ __RPC__in_opt IUnknown *pUnkSink,
            /* [out] */ __RPC__out DWORD *pdwCookie)
        {
            if (pUnkSink == nullptr || pdwCookie == nullptr)
                return E_POINTER;

            for (uint32_t i = 0; i < _sinks.size(); ++i)
            {
                if (_sinks[i] != nullptr)
                    continue;

                IDispatch *sink;
                HRESULT hr = pUnkSink->QueryInterface(IID_IDispatch, (void **)&sink);
                if (hr == S_OK)
                {
                    _sinks[i] = sink;
                    *pdwCookie = i;
                    return S_OK;
                }
            }

            return CONNECT_E_ADVISELIMIT;
        }

        HRESULT STDMETHODCALLTYPE Unadvise(
            /* [in] */ DWORD dwCookie)
        {
            if (dwCookie < 0 || dwCookie >= _sinks.size())
                return E_INVALIDARG;

            IDispatch *sink = _sinks[dwCookie];
            if (sink == nullptr)
                return E_INVALIDARG;

            _sinks[dwCookie] = nullptr;
            sink->Release();
            return S_OK;
        }

        HRESULT STDMETHODCALLTYPE EnumConnections(
            /* [out] */ __RPC__deref_out_opt IEnumConnections **ppEnum)
        {
            return E_NOTIMPL;
        }

    public: // IUnknown
        HRESULT STDMETHODCALLTYPE QueryInterface(
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
        {
            return DoQueryInterface(riid, ppvObject,
                static_cast<IConnectionPoint *>(this));
        }

        DEFINE_REF_COUNTING();

    private:
        IID _iid;
        std::array<IDispatch *, 8> _sinks;
    };
}

HRESULT STDMETHODCALLTYPE EventTest::FireEvent(
    /* [in] */ int id)
{
    VARIANTARG arg;
    ::VariantInit(&arg);
    arg.vt = VT_I4;
    arg.lVal = id;

    ConnectionPoint *cp = static_cast<ConnectionPoint *>(_sinkConnection);
    HRESULT hr = cp->FireEventImpl(DISPID_IEVENTTESTSINK_ONEVENT, &arg);

    return ::VariantClear(&arg);
}

HRESULT STDMETHODCALLTYPE EventTest::FireEventMessage(
    /* [in] */ BSTR message)
{
    VARIANTARG arg;
    ::VariantInit(&arg);
    arg.vt = VT_BSTR;
    arg.bstrVal = ::SysAllocString(message);

    ConnectionPoint *cp = static_cast<ConnectionPoint *>(_sinkExConnection);
    HRESULT hr = cp->FireEventImpl(DISPID_IEVENTTESTSINKEX_ONEVENTMESSAGE, &arg);

    return ::VariantClear(&arg);
}

HRESULT STDMETHODCALLTYPE EventTest::FireEventMessageByRef(
    /* [in] */ BSTR message,
    /* [out,retval] */ BSTR *result)
{
    if (result == nullptr)
        return E_POINTER;

    BSTR original = ::SysAllocString(message);
    BSTR value = original;

    VARIANTARG arg;
    ::VariantInit(&arg);
    arg.vt = VT_BSTR | VT_BYREF;
    arg.pbstrVal = &value;

    ConnectionPoint *cp = static_cast<ConnectionPoint *>(_sinkExConnection);
    HRESULT hr = cp->FireEventImpl(DISPID_IEVENTTESTSINKEX_ONEVENTMESSAGEBYREF, &arg);

    // A sink that replaces the value doesn't release the original.
    if (value != original)
        ::SysFreeString(original);

    if (FAILED(hr))
    {
        ::SysFreeString(value);
        return hr;
    }

    *result = value;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE EventTest::FireEventArrayByRef(
    /* [out,retval] */ SAFEARRAY **result)
{
    if (result == nullptr)
        return E_POINTER;

    SAFEARRAY *original = ::SafeArrayCreateVector(VT_I4, 0, 0);
    if (original == nullptr)
        return E_OUTOFMEMORY;

    SAFEARRAY *value = original;

    VARIANTARG arg;
    ::VariantInit(&arg);
    arg.vt = VT_ARRAY | VT_I4 | VT_BYREF;
    arg.pparray = &value;

    ConnectionPoint *cp = static_cast<ConnectionPoint *>(_sinkExConnection);
    HRESULT hr = cp->FireEventImpl(DISPID_IEVENTTESTSINKEX_ONEVENTARRAYBYREF, &arg);

    // A sink that replaces the value doesn't release the original.
    if (value != original)
        ::SafeArrayDestroy(original);

    if (FAILED(hr))
    {
        ::SafeArrayDestroy(value);
        return hr;
    }

    *result = value;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE EventTest::FindConnectionPoint(
    /* [in] */ __RPC__in REFIID riid,
    /* [out] */ __RPC__deref_out_opt IConnectionPoint **ppCP)
{
    if (riid == IID_IEventTestSink)
    {
        if (_sinkConnection == nullptr)
            _sinkConnection = new ConnectionPoint(IID_IEventTestSink);

        return _sinkConnection->QueryInterface(__uuidof(*ppCP), (void **)ppCP);
    }

    if (riid == IID_IEventTestSinkEx)
    {
        if (_sinkExConnection == nullptr)
            _sinkExConnection = new ConnectionPoint(IID_IEventTestSinkEx);

        return _sinkExConnection->QueryInterface(__uuidof(*ppCP), (void **)ppCP);
    }

    return CONNECT_E_NOCONNECTION;
}
