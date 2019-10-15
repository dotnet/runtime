// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "Servers.h"

class AggregationTesting : public UnknownImpl, public IUnknown
{
public:
    AggregationTesting(_In_opt_ IUnknown *pUnkOuter)
        : _outer{ pUnkOuter == nullptr ? this : pUnkOuter }
        , _impl{ _outer, _outer != this }
    { }

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        if (ppvObject == nullptr)
            return E_POINTER;

        if (riid == IID_IUnknown)
        {
            *ppvObject = static_cast<IUnknown*>(this);
        }
        else if(riid == IID_IAggregationTesting)
        {
            *ppvObject = static_cast<IAggregationTesting*>(&_impl);
        }
        else
        {
            return E_NOINTERFACE;
        }

        ((IUnknown*)*ppvObject)->AddRef();
        return S_OK;
    }

    DEFINE_REF_COUNTING();

private:
    // Implementation for class to support COM aggregation
    class AggregationTestingImpl : public IAggregationTesting
    {
    public:
        AggregationTestingImpl(_In_  IUnknown *pUnkOuter, _In_ bool isAggregated)
            : _implOuter{ pUnkOuter }
            , _isAggregated{ isAggregated }
        { }

    public: // IAggregationTesting
        HRESULT STDMETHODCALLTYPE Add_Int(
            /*[in]*/ int a,
            /*[in]*/ int b,
            /*[out,retval]*/ int * pRetVal)
        {
            *pRetVal = a + b;
            return S_OK;
        }

        HRESULT STDMETHODCALLTYPE IsAggregated(
            _Out_ VARIANT_BOOL *isAggregated)
        {
            *isAggregated = _isAggregated ? VARIANT_TRUE : VARIANT_FALSE;
            return S_OK;
        }

        HRESULT STDMETHODCALLTYPE AreAggregated(
            _In_ IUnknown *aggregateMaybe1,
            _In_ IUnknown *aggregateMaybe2,
            _Out_ VARIANT_BOOL *areAggregated)
        {
            HRESULT hr;

            *areAggregated = VARIANT_FALSE;

            IUnknown *unknown1;
            RETURN_IF_FAILED(aggregateMaybe1->QueryInterface(IID_IUnknown, (void**)&unknown1));

            IUnknown *unknown2;
            RETURN_IF_FAILED(aggregateMaybe2->QueryInterface(IID_IUnknown, (void**)&unknown2));

            if (unknown1 == unknown2)
                *areAggregated = VARIANT_TRUE;

            unknown1->Release();
            unknown2->Release();
            return S_OK;
        }

    public: // IUnknown
        STDMETHOD(QueryInterface)(
                /* [in] */ REFIID riid,
                /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
        {
            return _implOuter->QueryInterface(riid, ppvObject);
        }

        STDMETHODIMP_(ULONG) AddRef(void) 
        {
            return _implOuter->AddRef();
        }

        STDMETHODIMP_(ULONG) Release(void) 
        {
            return _implOuter->Release();
        }

    private:
        IUnknown *_implOuter;
        const bool _isAggregated;
    };

    IUnknown *_outer;
    AggregationTestingImpl _impl;
};
