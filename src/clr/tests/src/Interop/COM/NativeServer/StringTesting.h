// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "Servers.h"

class DECLSPEC_UUID("C73C83E8-51A2-47F8-9B5C-4284458E47A6") StringTesting : public UnknownImpl, public IStringTesting
{
private:
    template <typename STRING>
    bool EqualValue(STRING l, STRING r)
    {
        STRING tmp = l;
        int len = 1; // Include 1 for null
        while (*tmp++)
            ++len;

        return (0 == ::memcmp(l, r, len * sizeof(l[0])));
    }

    template <typename STRING>
    STRING ReverseInplace(size_t len, STRING s)
    {
        std::reverse(s, s + len);
        return s;
    }

    template<typename STRING>
    HRESULT Reverse(STRING str, STRING *res)
    {
        STRING tmp = str;
        size_t len = 0;
        while (*tmp++)
            ++len;

        size_t strDataLen = (len + 1) * sizeof(str[0]);
        auto resLocal = (STRING)::CoTaskMemAlloc(strDataLen);
        if (resLocal == nullptr)
            return E_OUTOFMEMORY;

        ::memcpy_s(resLocal, strDataLen, str, strDataLen);
        *res = ReverseInplace(len, resLocal);

        return S_OK;
    }

    HRESULT ReverseBstr(BSTR str, BSTR *res)
    {
        UINT strDataLen = ::SysStringByteLen(str);
        BSTR resLocal = ::SysAllocStringByteLen(reinterpret_cast<LPCSTR>(str), strDataLen);
        if (resLocal == nullptr)
            return E_OUTOFMEMORY;

        UINT len = ::SysStringLen(str);
        *res = ReverseInplace(len, resLocal);

        return S_OK;
    }

public: // IStringTesting
    DEF_RAWFUNC(Add_LPStr)(
        /*[in]*/ LPSTR a,
        /*[in]*/ LPSTR b,
        /*[out,retval]*/ LPSTR * pRetVal)
    {
        if (a == nullptr || b == nullptr)
            return E_POINTER;

        size_t aLen = ::strlen(a);
        size_t bLen = ::strlen(b);
        auto buf = (LPSTR)::CoTaskMemAlloc((aLen + bLen + 1) * sizeof(*b));

        ::strcpy_s(buf, aLen + 1, a);
        ::strcpy_s(buf + aLen, bLen + 1, b);

        *pRetVal = buf;
        return S_OK;
    }
    DEF_RAWFUNC(Add_LPWStr)(
        /*[in]*/ LPWSTR a,
        /*[in]*/ LPWSTR b,
        /*[out,retval]*/ LPWSTR * pRetVal)
    {
        if (a == nullptr || b == nullptr)
            return E_POINTER;

        size_t aLen = ::wcslen(a);
        size_t bLen = ::wcslen(b);
        auto buf = (LPWSTR)::CoTaskMemAlloc((aLen + bLen + 1) * sizeof(*b));

        ::wcscpy_s(buf, aLen + 1, a);
        ::wcscpy_s(buf + aLen, bLen + 1, b);

        *pRetVal = buf;
        return S_OK;
    }
    DEF_RAWFUNC(Add_BStr)(
        /*[in]*/ BSTR a,
        /*[in]*/ BSTR b,
        /*[out,retval]*/ BSTR * pRetVal)
    {
        if (a == nullptr || b == nullptr)
            return E_POINTER;

        UINT aLen = ::SysStringLen(a);
        UINT bLen = ::SysStringLen(b);
        BSTR buf = ::SysAllocStringByteLen(nullptr, (aLen + bLen) * sizeof(a[0]));

        ::wcscpy_s(buf, aLen + 1, a);
        ::wcscpy_s(buf + aLen, bLen + 1, b);

        *pRetVal = buf;
        return S_OK;
    }
    DEF_RAWFUNC(Reverse_LPStr)(
        /*[in]*/ LPSTR a,
        /*[out,retval]*/ LPSTR * pRetVal)
    {
        return Reverse(a, pRetVal);
    }
    DEF_RAWFUNC(Reverse_LPStr_Ref)(
        /*[in,out]*/ LPSTR * a,
        /*[out,retval]*/ LPSTR * pRetVal)
    {
        HRESULT hr;
        RETURN_IF_FAILED(Reverse(*a, pRetVal));
        ReverseInplace(::strlen(*a), *a);
        return S_OK;
    }
    DEF_RAWFUNC(Reverse_LPStr_InRef)(
        /*[in]*/ LPSTR * a,
        /*[out,retval]*/ LPSTR * pRetVal)
    {
        return Reverse(*a, pRetVal);
    }
    DEF_RAWFUNC(Reverse_LPStr_Out)(
        /*[in]*/ LPSTR a,
        /*[out]*/ LPSTR * b)
    {
        return Reverse(a, b);
    }
    DEF_RAWFUNC(Reverse_LPStr_OutAttr)(
        /*[in]*/ LPSTR a,
        /*[out]*/ LPSTR b)
    {
        ReverseInplace(::strlen(b), b);
        return S_OK;
    }
    DEF_RAWFUNC(Reverse_SB_LPStr)(
        /*[in,out]*/ LPSTR a,
        /*[out,retval]*/ LPSTR * pRetVal)
    {
        HRESULT hr;
        RETURN_IF_FAILED(Reverse(a, pRetVal));
        ReverseInplace(::strlen(a), a);
        return S_OK;
    }
    DEF_RAWFUNC(Reverse_SB_LPStr_Ref)(
        /*[in,out]*/ LPSTR * a,
        /*[out,retval]*/ LPSTR * pRetVal)
    {
        HRESULT hr;
        RETURN_IF_FAILED(Reverse(*a, pRetVal));
        ReverseInplace(::strlen(*a), *a);
        return S_OK;
    }
    DEF_RAWFUNC(Reverse_SB_LPStr_InRef)(
        /*[in]*/ LPSTR * a,
        /*[out,retval]*/ LPSTR * pRetVal)
    {
        HRESULT hr;
        RETURN_IF_FAILED(Reverse(*a, pRetVal));
        ReverseInplace(::strlen(*a), *a);
        return S_OK;
    }
    DEF_RAWFUNC(Reverse_SB_LPStr_Out)(
        /*[in,out]*/ LPSTR a,
        /*[out]*/ LPSTR * b)
    {
        HRESULT hr;
        RETURN_IF_FAILED(Reverse(a, b));
        ReverseInplace(::strlen(a), a);
        return S_OK;
    }
    DEF_RAWFUNC(Reverse_SB_LPStr_OutAttr)(
        /*[in,out]*/ LPSTR a,
        /*[out]*/ LPSTR b)
    {
        size_t len = ::strlen(a);
        ReverseInplace(len, a);
        size_t byteLen = (len + 1) * sizeof(*a);
        ::memcpy_s(b, byteLen, a, byteLen);
        return S_OK;
    }
    DEF_RAWFUNC(Reverse_LPWStr)(
        /*[in]*/ LPWSTR a,
        /*[out,retval]*/ LPWSTR * pRetVal)
    {
        return Reverse(a, pRetVal);
    }
    DEF_RAWFUNC(Reverse_LPWStr_Ref)(
        /*[in,out]*/ LPWSTR * a,
        /*[out,retval]*/ LPWSTR * pRetVal)
    {
        HRESULT hr;
        RETURN_IF_FAILED(Reverse(*a, pRetVal));
        ReverseInplace(::wcslen(*a), *a);
        return S_OK;
    }
    DEF_RAWFUNC(Reverse_LPWStr_InRef)(
        /*[in]*/ LPWSTR * a,
        /*[out,retval]*/ LPWSTR * pRetVal)
    {
        return Reverse(*a, pRetVal);
    }
    DEF_RAWFUNC(Reverse_LPWStr_Out)(
        /*[in]*/ LPWSTR a,
        /*[out]*/ LPWSTR * b)
    {
        return Reverse(a, b);
    }
    DEF_RAWFUNC(Reverse_LPWStr_OutAttr)(
        /*[in]*/ LPWSTR a,
        /*[out]*/ LPWSTR b)
    {
        ReverseInplace(::wcslen(b), b);
        return S_OK;
    }
    DEF_RAWFUNC(Reverse_SB_LPWStr)(
        /*[in,out]*/ LPWSTR a,
        /*[out,retval]*/ LPWSTR * pRetVal)
    {
        HRESULT hr;
        RETURN_IF_FAILED(Reverse(a, pRetVal));
        ReverseInplace(::wcslen(a), a);
        return S_OK;
    }
    DEF_RAWFUNC(Reverse_SB_LPWStr_Ref)(
        /*[in,out]*/ LPWSTR * a,
        /*[out,retval]*/ LPWSTR * pRetVal)
    {
        HRESULT hr;
        RETURN_IF_FAILED(Reverse(*a, pRetVal));
        ReverseInplace(::wcslen(*a), *a);
        return S_OK;
    }
    DEF_RAWFUNC(Reverse_SB_LPWStr_InRef)(
        /*[in]*/ LPWSTR * a,
        /*[out,retval]*/ LPWSTR * pRetVal)
    {
        HRESULT hr;
        RETURN_IF_FAILED(Reverse(*a, pRetVal));
        ReverseInplace(::wcslen(*a), *a);
        return S_OK;
    }
    DEF_RAWFUNC(Reverse_SB_LPWStr_Out)(
        /*[in,out]*/ LPWSTR a,
        /*[out]*/ LPWSTR * b)
    {
        HRESULT hr;
        RETURN_IF_FAILED(Reverse(a, b));
        ReverseInplace(::wcslen(a), a);
        return S_OK;
    }
    DEF_RAWFUNC(Reverse_SB_LPWStr_OutAttr)(
        /*[in,out]*/ LPWSTR a,
        /*[out]*/ LPWSTR b)
    {
        size_t len = ::wcslen(a);
        ReverseInplace(len, a);
        size_t byteLen = (len + 1) * sizeof(*a);
        ::memcpy_s(b, byteLen, a, byteLen);
        return S_OK;
    }
    DEF_RAWFUNC(Reverse_BStr)(
        /*[in]*/ BSTR a,
        /*[out,retval]*/ BSTR * pRetVal)
    {
        return ReverseBstr(a, pRetVal);
    }
    DEF_RAWFUNC(Reverse_BStr_Ref)(
        /*[in,out]*/ BSTR * a,
        /*[out,retval]*/ BSTR * pRetVal)
    {
        HRESULT hr;
        RETURN_IF_FAILED(ReverseBstr(*a, pRetVal));
        ReverseInplace(::SysStringLen(*a), *a);
        return S_OK;
    }
    DEF_RAWFUNC(Reverse_BStr_InRef)(
        /*[in]*/ BSTR * a,
        /*[out,retval]*/ BSTR * pRetVal)
    {
        return ReverseBstr(*a, pRetVal);
    }
    DEF_RAWFUNC(Reverse_BStr_Out)(
        /*[in]*/ BSTR a,
        /*[out]*/ BSTR * b)
    {
        return ReverseBstr(a, b);
    }
    DEF_RAWFUNC(Reverse_BStr_OutAttr)(
        /*[in]*/ BSTR a,
        /*[out]*/ BSTR b)
    {
        ReverseInplace(::SysStringLen(b), b);
        return S_OK;
    }

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface<StringTesting, IStringTesting>(this, riid, ppvObject);
    }

    DEFINE_REF_COUNTING();
};
