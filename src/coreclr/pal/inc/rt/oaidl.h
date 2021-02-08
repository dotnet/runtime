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

typedef struct tagEXCEPINFO {
    WORD wCode;
    WORD wReserved;
    BSTR bstrSource;
    BSTR bstrDescription;
    BSTR bstrHelpFile;
    DWORD dwHelpContext;
    PVOID pvReserved;
    HRESULT (__stdcall *pfnDeferredFillIn)(struct tagEXCEPINFO *);
    SCODE scode;
} EXCEPINFO, * LPEXCEPINFO;

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

typedef interface ICreateErrorInfo ICreateErrorInfo;

EXTERN_C const IID IID_ICreateErrorInfo;

typedef /* [unique] */ ICreateErrorInfo *LPCREATEERRORINFO;

    interface
    ICreateErrorInfo : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetGUID(
            /* [in] */ REFGUID rguid) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetSource(
            /* [in] */ LPOLESTR szSource) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetDescription(
            /* [in] */ LPOLESTR szDescription) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetHelpFile(
            /* [in] */ LPOLESTR szHelpFile) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetHelpContext(
            /* [in] */ DWORD dwHelpContext) = 0;

    };

STDAPI
SetErrorInfo(ULONG dwReserved, IErrorInfo FAR* perrinfo);

STDAPI
GetErrorInfo(ULONG dwReserved, IErrorInfo FAR* FAR* pperrinfo);

STDAPI
CreateErrorInfo(ICreateErrorInfo FAR* FAR* pperrinfo);


typedef interface ISupportErrorInfo ISupportErrorInfo;

typedef /* [unique] */ ISupportErrorInfo *LPSUPPORTERRORINFO;

EXTERN_C const IID IID_ISupportErrorInfo;


    interface
    ISupportErrorInfo : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE InterfaceSupportsErrorInfo(
            /* [in] */ REFIID riid) = 0;

    };

#endif //__OAIDL_H__
