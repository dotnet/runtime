// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "Servers.h"

class ErrorMarshalTesting : public UnknownImpl, public IErrorMarshalTesting
{
public: // IErrorMarshalTesting
    DEF_FUNC(Throw_HResult)(
        /*[in]*/ int hresultToReturn)
    {
        return HRESULT{ hresultToReturn };
    }

    int STDMETHODCALLTYPE Return_As_HResult(
        /*[in]*/ int hresultToReturn)
    {
        return hresultToReturn;
    }

    int STDMETHODCALLTYPE Return_As_HResult_Struct(
        /*[in]*/ int hresultToReturn)
    {
        return hresultToReturn;
    }

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject, static_cast<IErrorMarshalTesting *>(this));
    }

    DEFINE_REF_COUNTING();
};
