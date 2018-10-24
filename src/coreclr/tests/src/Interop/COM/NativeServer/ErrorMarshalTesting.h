// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface<ErrorMarshalTesting, IErrorMarshalTesting>(this, riid, ppvObject);
    }

    DEFINE_REF_COUNTING();
};
