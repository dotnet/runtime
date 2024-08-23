// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "Servers.h"

class StringTesting : public UnknownImpl, public IStringTesting
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
        uint32_t strDataLen = ::SysStringByteLen(str);
        BSTR resLocal = ::SysAllocStringByteLen(reinterpret_cast<LPCSTR>(str), strDataLen);
        if (resLocal == nullptr)
            return E_OUTOFMEMORY;

        uint32_t len = ::SysStringLen(str);
        *res = ReverseInplace(len, resLocal);

        return S_OK;
    }

public: // IStringTesting
    DEF_FUNC(Add_LPStr)(
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
    DEF_FUNC(Add_LPWStr)(
        /*[in]*/ LPWSTR a,
        /*[in]*/ LPWSTR b,
        /*[out,retval]*/ LPWSTR * pRetVal)
    {
        if (a == nullptr || b == nullptr)
            return E_POINTER;

        size_t aLen = ::TP_slen(a);
        size_t bLen = ::TP_slen(b);
        auto buf = (LPWSTR)::CoTaskMemAlloc((aLen + bLen + 1) * sizeof(*b));

        ::TP_scpy_s(buf, aLen + 1, a);
        ::TP_scpy_s(buf + aLen, bLen + 1, b);

        *pRetVal = buf;
        return S_OK;
    }
    DEF_FUNC(Add_BStr)(
        /*[in]*/ BSTR a,
        /*[in]*/ BSTR b,
        /*[out,retval]*/ BSTR * pRetVal)
    {
        if (a == nullptr || b == nullptr)
            return E_POINTER;

        uint32_t aLen = ::SysStringLen(a);
        uint32_t bLen = ::SysStringLen(b);
        BSTR buf = ::SysAllocStringByteLen(nullptr, (aLen + bLen) * sizeof(a[0]));

        ::TP_scpy_s(buf, aLen + 1, a);
        ::TP_scpy_s(buf + aLen, bLen + 1, b);

        *pRetVal = buf;
        return S_OK;
    }
    DEF_FUNC(Reverse_LPStr)(
        /*[in]*/ LPSTR a,
        /*[out,retval]*/ LPSTR * pRetVal)
    {
        return Reverse(a, pRetVal);
    }
    DEF_FUNC(Reverse_LPStr_Ref)(
        /*[in,out]*/ LPSTR * a,
        /*[out,retval]*/ LPSTR * pRetVal)
    {
        HRESULT hr;
        RETURN_IF_FAILED(Reverse(*a, pRetVal));
        ReverseInplace(::strlen(*a), *a);
        return S_OK;
    }
    DEF_FUNC(Reverse_LPStr_InRef)(
        /*[in]*/ LPSTR * a,
        /*[out,retval]*/ LPSTR * pRetVal)
    {
        return Reverse(*a, pRetVal);
    }
    DEF_FUNC(Reverse_LPStr_Out)(
        /*[in]*/ LPSTR a,
        /*[out]*/ LPSTR * b)
    {
        return Reverse(a, b);
    }
    DEF_FUNC(Reverse_LPStr_OutAttr)(
        /*[in]*/ LPSTR a,
        /*[out]*/ LPSTR b)
    {
        ReverseInplace(::strlen(b), b);
        return S_OK;
    }
    DEF_FUNC(Reverse_SB_LPStr)(
        /*[in,out]*/ LPSTR a,
        /*[out,retval]*/ LPSTR * pRetVal)
    {
        HRESULT hr;
        RETURN_IF_FAILED(Reverse(a, pRetVal));
        ReverseInplace(::strlen(a), a);
        return S_OK;
    }
    DEF_FUNC(Reverse_SB_LPStr_Ref)(
        /*[in,out]*/ LPSTR * a,
        /*[out,retval]*/ LPSTR * pRetVal)
    {
        HRESULT hr;
        RETURN_IF_FAILED(Reverse(*a, pRetVal));
        ReverseInplace(::strlen(*a), *a);
        return S_OK;
    }
    DEF_FUNC(Reverse_SB_LPStr_InRef)(
        /*[in]*/ LPSTR * a,
        /*[out,retval]*/ LPSTR * pRetVal)
    {
        HRESULT hr;
        RETURN_IF_FAILED(Reverse(*a, pRetVal));
        ReverseInplace(::strlen(*a), *a);
        return S_OK;
    }
    DEF_FUNC(Reverse_SB_LPStr_Out)(
        /*[in,out]*/ LPSTR a,
        /*[out]*/ LPSTR * b)
    {
        HRESULT hr;
        RETURN_IF_FAILED(Reverse(a, b));
        ReverseInplace(::strlen(a), a);
        return S_OK;
    }
    DEF_FUNC(Reverse_SB_LPStr_OutAttr)(
        /*[in,out]*/ LPSTR a,
        /*[out]*/ LPSTR b)
    {
        size_t len = ::strlen(a);
        ReverseInplace(len, a);
        size_t byteLen = (len + 1) * sizeof(*a);
        ::memcpy_s(b, byteLen, a, byteLen);
        return S_OK;
    }
    DEF_FUNC(Reverse_LPWStr)(
        /*[in]*/ LPWSTR a,
        /*[out,retval]*/ LPWSTR * pRetVal)
    {
        return Reverse(a, pRetVal);
    }
    DEF_FUNC(Reverse_LPWStr_Ref)(
        /*[in,out]*/ LPWSTR * a,
        /*[out,retval]*/ LPWSTR * pRetVal)
    {
        HRESULT hr;
        RETURN_IF_FAILED(Reverse(*a, pRetVal));
        ReverseInplace(::TP_slen(*a), *a);
        return S_OK;
    }
    DEF_FUNC(Reverse_LPWStr_InRef)(
        /*[in]*/ LPWSTR * a,
        /*[out,retval]*/ LPWSTR * pRetVal)
    {
        return Reverse(*a, pRetVal);
    }
    DEF_FUNC(Reverse_LPWStr_Out)(
        /*[in]*/ LPWSTR a,
        /*[out]*/ LPWSTR * b)
    {
        return Reverse(a, b);
    }
    DEF_FUNC(Reverse_LPWStr_OutAttr)(
        /*[in]*/ LPWSTR a,
        /*[out]*/ LPWSTR b)
    {
        // Not possible to test from native server
        // since the out string is a pointer to the
        // actual CLR string and modifying it breaks
        // the immutability invariant of CLR strings.
        return S_FALSE;
    }
    DEF_FUNC(Reverse_SB_LPWStr)(
        /*[in,out]*/ LPWSTR a,
        /*[out,retval]*/ LPWSTR * pRetVal)
    {
        HRESULT hr;
        RETURN_IF_FAILED(Reverse(a, pRetVal));
        ReverseInplace(::TP_slen(a), a);
        return S_OK;
    }
    DEF_FUNC(Reverse_SB_LPWStr_Ref)(
        /*[in,out]*/ LPWSTR * a,
        /*[out,retval]*/ LPWSTR * pRetVal)
    {
        HRESULT hr;
        RETURN_IF_FAILED(Reverse(*a, pRetVal));
        ReverseInplace(::TP_slen(*a), *a);
        return S_OK;
    }
    DEF_FUNC(Reverse_SB_LPWStr_InRef)(
        /*[in]*/ LPWSTR * a,
        /*[out,retval]*/ LPWSTR * pRetVal)
    {
        HRESULT hr;
        RETURN_IF_FAILED(Reverse(*a, pRetVal));
        ReverseInplace(::TP_slen(*a), *a);
        return S_OK;
    }
    DEF_FUNC(Reverse_SB_LPWStr_Out)(
        /*[in,out]*/ LPWSTR a,
        /*[out]*/ LPWSTR * b)
    {
        HRESULT hr;
        RETURN_IF_FAILED(Reverse(a, b));
        ReverseInplace(::TP_slen(a), a);
        return S_OK;
    }
    DEF_FUNC(Reverse_SB_LPWStr_OutAttr)(
        /*[in,out]*/ LPWSTR a,
        /*[out]*/ LPWSTR b)
    {
        size_t len = ::TP_slen(a);
        ReverseInplace(len, a);
        size_t byteLen = (len + 1) * sizeof(*a);
        ::memcpy_s(b, byteLen, a, byteLen);
        return S_OK;
    }
    DEF_FUNC(Reverse_BStr)(
        /*[in]*/ BSTR a,
        /*[out,retval]*/ BSTR * pRetVal)
    {
        return ReverseBstr(a, pRetVal);
    }
    DEF_FUNC(Reverse_BStr_Ref)(
        /*[in,out]*/ BSTR * a,
        /*[out,retval]*/ BSTR * pRetVal)
    {
        HRESULT hr;
        RETURN_IF_FAILED(ReverseBstr(*a, pRetVal));
        ReverseInplace(::SysStringLen(*a), *a);
        return S_OK;
    }
    DEF_FUNC(Reverse_BStr_InRef)(
        /*[in]*/ BSTR * a,
        /*[out,retval]*/ BSTR * pRetVal)
    {
        return ReverseBstr(*a, pRetVal);
    }
    DEF_FUNC(Reverse_BStr_Out)(
        /*[in]*/ BSTR a,
        /*[out]*/ BSTR * b)
    {
        return ReverseBstr(a, b);
    }
    DEF_FUNC(Reverse_BStr_OutAttr)(
        /*[in]*/ BSTR a,
        /*[out]*/ BSTR b)
    {
        ReverseInplace(::SysStringLen(b), b);
        return S_OK;
    }

    DEF_FUNC(Reverse_LPWSTR_With_LCID)(
        /*[in]*/ LPWSTR a,
        /*[in]*/ LCID lcid, // This parameter is only used as a placeholder to check that we've injected it into the correct arg slot.
        /*[out]*/ LPWSTR*  b)
    {
        return Reverse_LPWStr(a, b);
    }

    DEF_FUNC(Pass_Through_LCID)(
        /*[in]*/ LCID lcidFromCulture,
        /*[out]*/ LCID* outLcid)
    {
        *outLcid = lcidFromCulture;
        return S_OK;
    }

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject, static_cast<IStringTesting *>(this));
    }

    DEFINE_REF_COUNTING();
};
