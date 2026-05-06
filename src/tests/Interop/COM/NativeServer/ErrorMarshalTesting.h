// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "Servers.h"

class ErrorMarshalTesting : public UnknownImpl, public IErrorMarshalTesting, public ISupportErrorInfo
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

    DEF_FUNC(Throw_HResult_HelpLink)(
        /*[in]*/ int hresultToReturn,
        /*[in]*/ LPCWSTR helpLink,
        /*[in]*/ DWORD helpContext)
    {
        HRESULT hr;

        ComSmartPtr<ICreateErrorInfo> pCreateErrInfo;
        BSTR bstrHelpLink = SysAllocString(helpLink);
        RETURN_IF_FAILED(::CreateErrorInfo(&pCreateErrInfo));
        RETURN_IF_FAILED(pCreateErrInfo->SetHelpFile(bstrHelpLink));
        RETURN_IF_FAILED(pCreateErrInfo->SetHelpContext(helpContext));

        ComSmartPtr<IErrorInfo> pErrInfo;
        RETURN_IF_FAILED(pCreateErrInfo->QueryInterface(IID_IErrorInfo, (void**)&pErrInfo));
        RETURN_IF_FAILED(SetErrorInfo(0, pErrInfo));

        return HRESULT{ hresultToReturn };
    }

    DEF_FUNC(InterfaceSupportsErrorInfo)(
        /* [in] */ __RPC__in REFIID riid)
    {
        return riid == IID_IErrorMarshalTesting ? S_OK : S_FALSE;
    }

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject, static_cast<IErrorMarshalTesting *>(this), static_cast<ISupportErrorInfo *>(this));
    }

    DEFINE_REF_COUNTING();
};
