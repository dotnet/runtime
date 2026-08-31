// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <ComHelpers.h>
#include <Contract.h>
#include "DispatchImpl.h"
#include <array>
#include <unordered_map>

class EventTest
    : public DispatchImpl
    , public IEventTest
    , public IConnectionPointContainer
{
public:
    EventTest()
        : DispatchImpl(IID_IEventTest, static_cast<IEventTest *>(this))
        , _sinkConnection { nullptr }
        , _sinkExConnection { nullptr }
    { }

    ~EventTest()
    {
        if (_sinkConnection != nullptr)
            _sinkConnection->Release();

        if (_sinkExConnection != nullptr)
            _sinkExConnection->Release();
    }

public: // IEventTest
    HRESULT STDMETHODCALLTYPE FireEvent(
        /* [in] */ int id);

    HRESULT STDMETHODCALLTYPE FireEventMessage(
        /* [in] */ BSTR message);

public: // IConnectionPointContainer
    virtual HRESULT STDMETHODCALLTYPE EnumConnectionPoints(
        /* [out] */ __RPC__deref_out_opt IEnumConnectionPoints **ppEnum)
    {
        return E_NOTIMPL;
    }

    virtual HRESULT STDMETHODCALLTYPE FindConnectionPoint(
        /* [in] */ __RPC__in REFIID riid,
        /* [out] */ __RPC__deref_out_opt IConnectionPoint **ppCP);

public: // IDispatch
    DEFINE_DISPATCH();

public: // IUnknown
    HRESULT STDMETHODCALLTYPE QueryInterface(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject,
            static_cast<IDispatch *>(this),
            static_cast<IEventTest *>(this),
            static_cast<IConnectionPointContainer *>(this));
    }

    DEFINE_REF_COUNTING();

private:
    IConnectionPoint *_sinkConnection;
    IConnectionPoint *_sinkExConnection;
};
