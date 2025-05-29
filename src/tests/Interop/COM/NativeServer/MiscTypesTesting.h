// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <xplatform.h>
#include "Servers.h"

class MiscTypesTesting : public UnknownImpl, public IMiscTypesTesting
{
    struct InterfaceImpl : public UnknownImpl, public IInterface2
    {
        public: // IInterface1
        public: // IInterface2
        public: // IUnknown
            STDMETHOD(QueryInterface)(
                /* [in] */ REFIID riid,
                /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
            {
                return DoQueryInterface(riid, ppvObject, static_cast<IInterface1 *>(this), static_cast<IInterface2 *>(this));
            }

        DEFINE_REF_COUNTING();
    };
public: // IMiscTypesTesting
    DEF_FUNC(Marshal_Variant)(_In_ VARIANT obj, _Out_ VARIANT* result)
    {
        return ::VariantCopy(result, &obj);
    }

    DEF_FUNC(Marshal_Instance_Variant)(_In_ LPCWSTR init, _Out_ VARIANT* result)
    {
        return E_NOTIMPL;
    }

    DEF_FUNC(Marshal_ByRefVariant)(_Inout_ VARIANT* result, _In_ VARIANT value)
    {
        return E_NOTIMPL;
    }

    DEF_FUNC(Marshal_Interface)(_In_ IUnknown* input, _Outptr_ IInterface2** value)
    {
        HRESULT hr;

        IInterface2* ifaceMaybe = nullptr;
        hr = input->QueryInterface(__uuidof(IInterface2), (void**)&ifaceMaybe);
        if (FAILED(hr))
            return hr;
        (void)ifaceMaybe->Release();

        InterfaceImpl* inst = new InterfaceImpl();
        hr = inst->QueryInterface(__uuidof(IInterface2), (void**)value);
        (void)inst->Release();
        return hr;
    }

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject, static_cast<IMiscTypesTesting *>(this));
    }

    DEFINE_REF_COUNTING();
};
