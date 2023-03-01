// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// ===========================================================================
// File: oaidl.h
//
// ===========================================================================

#ifndef __OAIDL_H__
#define __OAIDL_H__

#include "rpc.h"
#include "rpcndr.h"

#include "unknwn.h"

typedef interface IErrorInfo IErrorInfo;
typedef /* [unique] */ IErrorInfo *LPERRORINFO;

EXTERN_C const IID IID_IErrorInfo;

    interface
    IErrorInfo : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetGUID(
            /* [out] */ GUID *pGUID) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetSource(
            /* [out] */ BSTR *pBstrSource) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetDescription(
            /* [out] */ BSTR *pBstrDescription) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetHelpFile(
            /* [out] */ BSTR *pBstrHelpFile) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetHelpContext(
            /* [out] */ DWORD *pdwHelpContext) = 0;

    };

#endif //__OAIDL_H__
