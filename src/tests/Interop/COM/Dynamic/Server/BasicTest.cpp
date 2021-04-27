// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "BasicTest.h"
#include <cmath>

HRESULT STDMETHODCALLTYPE BasicTest::Default(
    /* [in] */ LONG val,
    /* [retval][out] */ LONG *ret)
{
    *ret = val * 2;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::get_Boolean_Property(
    /* [retval][out] */ VARIANT_BOOL *ret)
{
    *ret = _boolean;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::put_Boolean_Property(
    /* [in] */ VARIANT_BOOL val)
{
    _boolean = val;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::Boolean_Inverse_InOut(
    /* [out][in] */ VARIANT_BOOL *val)
{
    *val = *val == VARIANT_TRUE ? VARIANT_FALSE : VARIANT_TRUE;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::Boolean_Inverse_Ret(
    /* [in] */ VARIANT_BOOL val,
    /* [retval][out] */ VARIANT_BOOL *ret)
{
    *ret = val == VARIANT_TRUE ? VARIANT_FALSE : VARIANT_TRUE;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::get_SByte_Property(
    /* [retval][out] */ signed char *ret)
{
    *ret = _sbyte;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::put_SByte_Property(
    /* [in] */ signed char val)
{
    _sbyte = val;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::SByte_Doubled_InOut(
    /* [out][in] */ signed char *val)
{
    *val = *val * 2;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::SByte_Doubled_Ret(
    /* [in] */ signed char val,
    /* [retval][out] */ signed char *ret)
{
    *ret = val * 2;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::get_Byte_Property(
    /* [retval][out] */ unsigned char *ret)
{
    *ret = _byte;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::put_Byte_Property(
    /* [in] */ unsigned char val)
{
    _byte = val;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::Byte_Doubled_InOut(
    /* [out][in] */ unsigned char *val)
{
    *val = *val * 2;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::Byte_Doubled_Ret(
    /* [in] */ unsigned char val,
    /* [retval][out] */ unsigned char *ret)
{
    *ret = val * 2;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::get_Short_Property(
    /* [retval][out] */ short *ret)
{
    *ret = _short;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::put_Short_Property(
    /* [in] */ short val)
{
    _short = val;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::Short_Doubled_InOut(
    /* [out][in] */ short *val)
{
    *val = *val * 2;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::Short_Doubled_Ret(
    /* [in] */ short val,
    /* [retval][out] */ short *ret)
{
    *ret = val * 2;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::get_UShort_Property(
    /* [retval][out] */ unsigned short *ret)
{
    *ret = _ushort;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::put_UShort_Property(
    /* [in] */ unsigned short val)
{
    _ushort = val;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::UShort_Doubled_InOut(
    /* [out][in] */ unsigned short *val)
{
    *val = *val * 2;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::UShort_Doubled_Ret(
    /* [in] */ unsigned short val,
    /* [retval][out] */ unsigned short *ret)
{
    *ret = val * 2;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::get_Int_Property(
    /* [retval][out] */ int *ret)
{
    *ret = _int;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::put_Int_Property(
    /* [in] */ int val)
{
    _int = val;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::Int_Doubled_InOut(
    /* [out][in] */ int *val)
{
    *val = *val * 2;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::Int_Doubled_Ret(
    /* [in] */ int val,
    /* [retval][out] */ int *ret)
{
    *ret = val * 2;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::get_UInt_Property(
    /* [retval][out] */ unsigned int *ret)
{
    *ret = _uint;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::put_UInt_Property(
    /* [in] */ unsigned int val)
{
    _uint = val;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::UInt_Doubled_InOut(
    /* [out][in] */ unsigned int *val)
{
    *val = *val * 2;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::UInt_Doubled_Ret(
    /* [in] */ unsigned int val,
    /* [retval][out] */ unsigned int *ret)
{
    *ret = val * 2;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::get_Int64_Property(
    /* [retval][out] */ __int64 *ret)
{
    *ret = _long;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::put_Int64_Property(
    /* [in] */ __int64 val)
{
    _long = val;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::Int64_Doubled_InOut(
    /* [out][in] */ __int64 *val)
{
    *val = *val * 2;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::Int64_Doubled_Ret(
    /* [in] */ __int64 val,
    /* [retval][out] */ __int64 *ret)
{
    *ret = val * 2;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::get_UInt64_Property(
    /* [retval][out] */ unsigned __int64 *ret)
{
    *ret = _ulong;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::put_UInt64_Property(
    /* [in] */ unsigned __int64 val)
{
    _ulong = val;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::UInt64_Doubled_InOut(
    /* [out][in] */ unsigned __int64 *val)
{
    *val = *val * 2;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::UInt64_Doubled_Ret(
    /* [in] */ unsigned __int64 val,
    /* [retval][out] */ unsigned __int64 *ret)
{
    *ret = val * 2;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::get_Float_Property(
    /* [retval][out] */ float *ret)
{
    *ret = _float;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::put_Float_Property(
    /* [in] */ float val)
{
    _float = val;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::Float_Ceil_InOut(
    /* [out][in] */ float *val)
{
    *val = std::ceil(*val);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::Float_Ceil_Ret(
    /* [in] */ float val,
    /* [retval][out] */ float *ret)
{
    *ret = std::ceil(val);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::get_Double_Property(
    /* [retval][out] */ double *ret)
{
    *ret = _double;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::put_Double_Property(
    /* [in] */ double val)
{
    _double = val;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::Double_Ceil_InOut(
    /* [out][in] */ double *val)
{
    *val = std::ceil(*val);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::Double_Ceil_Ret(
    /* [in] */ double val,
    /* [retval][out] */ double *ret)
{
    *ret = std::ceil(val);
    return S_OK;
}

namespace
{
    void ReverseInPlace(BSTR val)
    {
        std::reverse(val, val + ::SysStringLen(val));
    }
}

HRESULT STDMETHODCALLTYPE BasicTest::get_String_Property(
    /* [retval][out] */ BSTR *ret)
{
    *ret = ::SysAllocString(_string);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::put_String_Property(
    /* [in] */ BSTR val)
{
    if (_string != nullptr)
        ::SysFreeString(_string);

    _string = ::SysAllocString(val);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::String_Reverse_InOut(
    /* [out][in] */ BSTR *val)
{
    ReverseInPlace(*val);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::String_Reverse_Ret(
    /* [in] */ BSTR val,
    /* [retval][out] */ BSTR *ret)
{
    UINT len = ::SysStringLen(val);
    *ret = ::SysAllocStringLen(val, len);
    ReverseInPlace(*ret);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::get_Date_Property(
    /* [retval][out] */ DATE *ret)
{
    *ret = _date;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::put_Date_Property(
    /* [in] */ DATE val)
{
    _date = val;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::Date_AddDay_InOut(
    /* [out][in] */ DATE *val)
{
    *val += 1;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::Date_AddDay_Ret(
    /* [in] */ DATE val,
    /* [retval][out] */ DATE *ret)
{
    *ret = val + 1;
    return S_OK;
}

namespace
{
    void UpdateComObject(IUnknown *val)
    {
        IBasicTest *test;
        HRESULT hr = val->QueryInterface<IBasicTest>(&test);
        if (FAILED(hr))
            return;

        test->put_Boolean_Property(VARIANT_TRUE);
        test->Release();
    }
}

HRESULT STDMETHODCALLTYPE BasicTest::get_Dispatch_Property(
    /* [retval][out] */ IDispatch **ret)
{
    if (_dispatch != nullptr)
        _dispatch->AddRef();

    *ret = _dispatch;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::put_Dispatch_Property(
    /* [in] */ IDispatch *val)
{
    if (_dispatch != nullptr)
        _dispatch->Release();

    _dispatch = val;
    if (_dispatch != nullptr)
        _dispatch->AddRef();

    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::Dispatch_InOut(
    /* [out][in] */ IDispatch **val)
{
    UpdateComObject(*val);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::Dispatch_Ret(
    /* [in] */ IDispatch *val,
    /* [retval][out] */ IDispatch **ret)
{
    IBasicTest *test;
    HRESULT hr = val->QueryInterface<IBasicTest>(&test);
    if (FAILED(hr))
        return hr;

    IBasicTest *retLocal = new BasicTest();
    UpdateComObject(retLocal);
    *ret = retLocal;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::get_Variant_Property(
    /* [retval][out] */ VARIANT *ret)
{
    return ::VariantCopy(ret, &_variant);
}

HRESULT STDMETHODCALLTYPE BasicTest::put_Variant_Property(
    /* [in] */ VARIANT val)
{
    ::VariantClear(&_variant);
    return ::VariantCopy(&_variant, &val);
}

namespace
{
    void UpdateVariantByRef(VARIANT *val)
    {
        int vt = val->vt & ~VARENUM::VT_BYREF;
        switch (vt)
        {
        // Inverse
        case VARENUM::VT_BOOL:
            *val->pboolVal = *val->pboolVal == VARIANT_TRUE ? VARIANT_FALSE : VARIANT_TRUE;
            break;

        // Double
        case VARENUM::VT_I1:
            *val->pcVal = *val->pcVal * 2;
            break;
        case VARENUM::VT_UI1:
            *val->pbVal = *val->pbVal * 2;
            break;
        case VARENUM::VT_I2:
            *val->piVal = *val->piVal * 2;
            break;
        case VARENUM::VT_UI2:
            *val->puiVal = *val->puiVal * 2;
            break;
        case VARENUM::VT_I4:
        case VARENUM::VT_INT:
            *val->plVal = *val->plVal * 2;
            break;
        case VARENUM::VT_UI4:
        case VARENUM::VT_UINT:
            *val->pulVal = *val->pulVal * 2;
            break;
        case VARENUM::VT_I8:
            *val->pllVal = *val->pllVal * 2;
            break;
        case VARENUM::VT_UI8:
            *val->pullVal = *val->pullVal * 2;
            break;

        // Ceil
        case VARENUM::VT_R4:
            *val->pfltVal = std::ceil(*val->pfltVal);
            break;
        case VARENUM::VT_R8:
            *val->pdblVal = std::ceil(*val->pdblVal);
            break;

        // Reverse
        case VARENUM::VT_BSTR:
            ReverseInPlace(*val->pbstrVal);
            break;

        // Add day
        case VARENUM::VT_DATE:
            *val->pdate = *val->pdate + 1;
            break;

        // IDispatch / IUnknown
        case VARENUM::VT_DISPATCH:
            UpdateComObject(*val->ppdispVal);
            break;
        case VARENUM::VT_UNKNOWN:
            UpdateComObject(*val->ppunkVal);
            break;
        }
    }

    void UpdateVariant(VARIANT *val)
    {
        if ((val->vt & VARENUM::VT_BYREF) != 0)
        {
            UpdateVariantByRef(val);
            return;
        }

        switch (val->vt)
        {
            // Inverse
            case VARENUM::VT_BOOL:
                val->boolVal = val->boolVal == VARIANT_TRUE ? VARIANT_FALSE : VARIANT_TRUE;
                break;

            // Double
            case VARENUM::VT_I1:
                val->cVal = val->cVal * 2;
                break;
            case VARENUM::VT_UI1:
                val->bVal = val->bVal * 2;
                break;
            case VARENUM::VT_I2:
                val->iVal = val->iVal * 2;
                break;
            case VARENUM::VT_UI2:
                val->uiVal = val->uiVal * 2;
                break;
            case VARENUM::VT_I4:
            case VARENUM::VT_INT:
                val->lVal = val->lVal * 2;
                break;
            case VARENUM::VT_UI4:
            case VARENUM::VT_UINT:
                val->ulVal = val->ulVal * 2;
                break;
            case VARENUM::VT_I8:
                val->llVal = val->llVal * 2;
                break;
            case VARENUM::VT_UI8:
                val->ullVal = val->ullVal * 2;
                break;

            // Ceil
            case VARENUM::VT_R4:
                val->fltVal = std::ceil(val->fltVal);
                break;
            case VARENUM::VT_R8:
                val->dblVal = std::ceil(val->dblVal);
                break;

            // Reverse
            case VARENUM::VT_BSTR:
                ReverseInPlace(val->bstrVal);
                break;

            // Add day
            case VARENUM::VT_DATE:
                val->date = val->date + 1;
                break;

            // IDispatch / IUnknown
            case VARENUM::VT_DISPATCH:
                UpdateComObject(val->pdispVal);
                break;
            case VARENUM::VT_UNKNOWN:
                UpdateComObject(val->punkVal);
                break;
        }
    }
}

HRESULT STDMETHODCALLTYPE BasicTest::Variant_InOut(
    /* [out][in] */ VARIANT *val)
{
    UpdateVariant(val);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::Variant_Ret(
    /* [in] */ VARIANT val,
    /* [retval][out] */ VARIANT *ret)
{
    ::VariantCopy(ret, &val);
    UpdateVariant(ret);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE BasicTest::Fail(
    /* [in] */ int errorCode,
    /* [in] */ BSTR message)
{
    ICreateErrorInfo *cei;
    if (SUCCEEDED(::CreateErrorInfo(&cei)))
    {
        if (SUCCEEDED(cei->SetGUID(IID_IBasicTest)))
        {
            if (SUCCEEDED(cei->SetDescription(message)))
            {
                IErrorInfo *errorInfo;
                if (SUCCEEDED(cei->QueryInterface(IID_IErrorInfo, (void **)&errorInfo)))
                {
                    ::SetErrorInfo(0, errorInfo);
                    errorInfo->Release();
                }
            }
        }

        cei->Release();
    }

    return errorCode;
}
