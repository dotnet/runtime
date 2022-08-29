// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "ClientTests.h"

namespace
{
    void VerifyExpectedException(_In_ IErrorMarshalTesting *et)
    {
       ::printf("Verify expected exception from HRESULT\n");

        HRESULT hrs[] =
        {
            E_NOTIMPL,
            E_POINTER,
            E_ACCESSDENIED,
            E_OUTOFMEMORY,
            E_INVALIDARG,
            E_UNEXPECTED,
            HRESULT{-1}
        };

        for (int i = 0; i < ARRAY_SIZE(hrs); ++i)
        {
            HRESULT hr = hrs[i];
            HRESULT hrMaybe = et->Throw_HResult(hr);
            THROW_FAIL_IF_FALSE(hr == hrMaybe);
        }
    }

    void VerifyReturnHResult(_In_ IErrorMarshalTesting *et)
    {
        ::printf("Verify preserved function signature\n");

        HRESULT hrs[] =
        {
            E_NOTIMPL,
            E_POINTER,
            E_ACCESSDENIED,
            E_INVALIDARG,
            E_UNEXPECTED,
            HRESULT{-1},
            S_FALSE,
            HRESULT{2}
        };

        for (int i = 0; i < ARRAY_SIZE(hrs); ++i)
        {
            HRESULT hr = hrs[i];
            HRESULT hrMaybe = et->Return_As_HResult(hr);
            THROW_FAIL_IF_FALSE(hr == hrMaybe);
        }
    }

    void VerifyReturnHResultStruct(_In_ IErrorMarshalTesting *et)
    {
        ::printf("Verify preserved function signature\n");

        HRESULT hrs[] =
        {
            E_NOTIMPL,
            E_POINTER,
            E_ACCESSDENIED,
            E_INVALIDARG,
            E_UNEXPECTED,
            HRESULT{-1},
            S_FALSE,
            HRESULT{2}
        };

        for (int i = 0; i < ARRAY_SIZE(hrs); ++i)
        {
            HRESULT hr = hrs[i];
            HRESULT hrMaybe = et->Return_As_HResult_Struct(hr);
            THROW_FAIL_IF_FALSE(hr == hrMaybe);
        }
    }

    void VerifyHelpContext(_In_ IErrorMarshalTesting *et)
    {
        ::printf("Verify expected helplink and context\n");

        HRESULT hrs[] =
        {
            E_NOTIMPL,
            E_POINTER,
            E_ACCESSDENIED,
            E_OUTOFMEMORY,
            E_INVALIDARG,
            E_UNEXPECTED,
            HRESULT{-1}
        };

        LPCWSTR helpLink = L"X:\\NotA\\RealPath\\dummy.hlp";

        for (int i = 0; i < ARRAY_SIZE(hrs); ++i)
        {
            HRESULT hr = hrs[i];
            DWORD helpContext = (DWORD)(i + 0x1234);
            HRESULT hrMaybe = et->Throw_HResult_HelpLink(hr, helpLink, helpContext);
            THROW_FAIL_IF_FALSE(hr == hrMaybe);

            ComSmartPtr<IErrorInfo> pErrInfo;
            THROW_IF_FAILED(GetErrorInfo(0, &pErrInfo));

            BSTR helpLinkMaybe;
            THROW_IF_FAILED(pErrInfo->GetHelpFile(&helpLinkMaybe));
            THROW_FAIL_IF_FALSE(TP_wcmp_s(helpLink, helpLinkMaybe) == 0);
            SysFreeString(helpLinkMaybe);

            DWORD helpContextMaybe;
            THROW_IF_FAILED(pErrInfo->GetHelpContext(&helpContextMaybe));
            THROW_FAIL_IF_FALSE(helpContext == helpContextMaybe);
        }
    }
}

void Run_ErrorTests()
{
    HRESULT hr;

    CoreShimComActivation csact{ W("NETServer"), W("ErrorMarshalTesting") };

    ComSmartPtr<IErrorMarshalTesting> errorMarshal;
    THROW_IF_FAILED(::CoCreateInstance(CLSID_ErrorMarshalTesting, nullptr, CLSCTX_INPROC, IID_IErrorMarshalTesting, (void**)&errorMarshal));

    VerifyExpectedException(errorMarshal);
    VerifyReturnHResult(errorMarshal);
    VerifyReturnHResultStruct(errorMarshal);
    VerifyHelpContext(errorMarshal);
}
