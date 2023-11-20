// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <ComHelpers.h>
#include <Contract.h>
#include "DispatchImpl.h"
#include <vector>

class BasicTest : public DispatchImpl, public IBasicTest
{
public:
    BasicTest()
        : DispatchImpl(IID_IBasicTest, static_cast<IBasicTest*>(this))
        , _boolean { VARIANT_FALSE }
        , _string { nullptr }
        , _dispatch { nullptr }
    {
        ::VariantInit(&_variant);
    }

    ~BasicTest()
    {
        if (_string != nullptr)
            ::SysFreeString(_string);

        ::VariantClear(&_variant);
    }

public: // IBasicTest
    virtual HRESULT STDMETHODCALLTYPE Default(
        /* [in] */ int32_t val,
        /* [retval][out] */ int32_t *ret);

    virtual HRESULT STDMETHODCALLTYPE get_Boolean_Property(
        /* [retval][out] */ VARIANT_BOOL *ret);

    virtual HRESULT STDMETHODCALLTYPE put_Boolean_Property(
        /* [in] */ VARIANT_BOOL val);

    virtual HRESULT STDMETHODCALLTYPE Boolean_Inverse_InOut(
        /* [out][in] */ VARIANT_BOOL *val);

    virtual HRESULT STDMETHODCALLTYPE Boolean_Inverse_Ret(
        /* [in] */ VARIANT_BOOL val,
        /* [retval][out] */ VARIANT_BOOL *ret);

    virtual HRESULT STDMETHODCALLTYPE get_SByte_Property(
        /* [retval][out] */ int8_t *ret);

    virtual HRESULT STDMETHODCALLTYPE put_SByte_Property(
        /* [in] */ int8_t val);

    virtual HRESULT STDMETHODCALLTYPE SByte_Doubled_InOut(
        /* [out][in] */ int8_t *val);

    virtual HRESULT STDMETHODCALLTYPE SByte_Doubled_Ret(
        /* [in] */ int8_t val,
        /* [retval][out] */ int8_t *ret);

    virtual HRESULT STDMETHODCALLTYPE get_Byte_Property(
        /* [retval][out] */ uint8_t *ret);

    virtual HRESULT STDMETHODCALLTYPE put_Byte_Property(
        /* [in] */ uint8_t val);

    virtual HRESULT STDMETHODCALLTYPE Byte_Doubled_InOut(
        /* [out][in] */ uint8_t *val);

    virtual HRESULT STDMETHODCALLTYPE Byte_Doubled_Ret(
        /* [in] */ uint8_t val,
        /* [retval][out] */ uint8_t *ret);

    virtual HRESULT STDMETHODCALLTYPE get_Short_Property(
        /* [retval][out] */ int16_t *ret);

    virtual HRESULT STDMETHODCALLTYPE put_Short_Property(
        /* [in] */ int16_t val);

    virtual HRESULT STDMETHODCALLTYPE Short_Doubled_InOut(
        /* [out][in] */ int16_t *val);

    virtual HRESULT STDMETHODCALLTYPE Short_Doubled_Ret(
        /* [in] */ int16_t val,
        /* [retval][out] */ int16_t *ret);

    virtual HRESULT STDMETHODCALLTYPE get_UShort_Property(
        /* [retval][out] */ uint16_t *ret);

    virtual HRESULT STDMETHODCALLTYPE put_UShort_Property(
        /* [in] */ uint16_t val);

    virtual HRESULT STDMETHODCALLTYPE UShort_Doubled_InOut(
        /* [out][in] */ uint16_t *val);

    virtual HRESULT STDMETHODCALLTYPE UShort_Doubled_Ret(
        /* [in] */ uint16_t val,
        /* [retval][out] */ uint16_t *ret);

    virtual HRESULT STDMETHODCALLTYPE get_Int_Property(
        /* [retval][out] */ int *ret);

    virtual HRESULT STDMETHODCALLTYPE put_Int_Property(
        /* [in] */ int val);

    virtual HRESULT STDMETHODCALLTYPE Int_Doubled_InOut(
        /* [out][in] */ int *val);

    virtual HRESULT STDMETHODCALLTYPE Int_Doubled_Ret(
        /* [in] */ int val,
        /* [retval][out] */ int *ret);

    virtual HRESULT STDMETHODCALLTYPE get_UInt_Property(
        /* [retval][out] */ uint32_t *ret);

    virtual HRESULT STDMETHODCALLTYPE put_UInt_Property(
        /* [in] */ uint32_t val);

    virtual HRESULT STDMETHODCALLTYPE UInt_Doubled_InOut(
        /* [out][in] */ uint32_t *val);

    virtual HRESULT STDMETHODCALLTYPE UInt_Doubled_Ret(
        /* [in] */ uint32_t val,
        /* [retval][out] */ uint32_t *ret);

    virtual HRESULT STDMETHODCALLTYPE get_Int64_Property(
        /* [retval][out] */ int64_t *ret);

    virtual HRESULT STDMETHODCALLTYPE put_Int64_Property(
        /* [in] */ int64_t val);

    virtual HRESULT STDMETHODCALLTYPE Int64_Doubled_InOut(
        /* [out][in] */ int64_t *val);

    virtual HRESULT STDMETHODCALLTYPE Int64_Doubled_Ret(
        /* [in] */ int64_t val,
        /* [retval][out] */ int64_t *ret);

    virtual HRESULT STDMETHODCALLTYPE get_UInt64_Property(
        /* [retval][out] */ uint64_t *ret);

    virtual HRESULT STDMETHODCALLTYPE put_UInt64_Property(
        /* [in] */ uint64_t val);

    virtual HRESULT STDMETHODCALLTYPE UInt64_Doubled_InOut(
        /* [out][in] */ uint64_t *val);

    virtual HRESULT STDMETHODCALLTYPE UInt64_Doubled_Ret(
        /* [in] */ uint64_t val,
        /* [retval][out] */ uint64_t *ret);

    virtual HRESULT STDMETHODCALLTYPE get_Float_Property(
        /* [retval][out] */ float *ret);

    virtual HRESULT STDMETHODCALLTYPE put_Float_Property(
        /* [in] */ float val);

    virtual HRESULT STDMETHODCALLTYPE Float_Ceil_InOut(
        /* [out][in] */ float *val);

    virtual HRESULT STDMETHODCALLTYPE Float_Ceil_Ret(
        /* [in] */ float val,
        /* [retval][out] */ float *ret);

    virtual HRESULT STDMETHODCALLTYPE get_Double_Property(
        /* [retval][out] */ double *ret);

    virtual HRESULT STDMETHODCALLTYPE put_Double_Property(
        /* [in] */ double val);

    virtual HRESULT STDMETHODCALLTYPE Double_Ceil_InOut(
        /* [out][in] */ double *val);

    virtual HRESULT STDMETHODCALLTYPE Double_Ceil_Ret(
        /* [in] */ double val,
        /* [retval][out] */ double *ret);

    virtual HRESULT STDMETHODCALLTYPE get_String_Property(
        /* [retval][out] */ BSTR *ret);

    virtual HRESULT STDMETHODCALLTYPE put_String_Property(
        /* [in] */ BSTR val);

    virtual HRESULT STDMETHODCALLTYPE String_Reverse_InOut(
        /* [out][in] */ BSTR *val);

    virtual HRESULT STDMETHODCALLTYPE String_Reverse_Ret(
        /* [in] */ BSTR val,
        /* [retval][out] */ BSTR *ret);

    virtual HRESULT STDMETHODCALLTYPE get_Date_Property(
        /* [retval][out] */ DATE *ret);

    virtual HRESULT STDMETHODCALLTYPE put_Date_Property(
        /* [in] */ DATE val);

    virtual HRESULT STDMETHODCALLTYPE Date_AddDay_InOut(
        /* [out][in] */ DATE *val);

    virtual HRESULT STDMETHODCALLTYPE Date_AddDay_Ret(
        /* [in] */ DATE val,
        /* [retval][out] */ DATE *ret);

    virtual HRESULT STDMETHODCALLTYPE get_Dispatch_Property(
        /* [retval][out] */ IDispatch **ret);

    virtual HRESULT STDMETHODCALLTYPE put_Dispatch_Property(
        /* [in] */ IDispatch *val);

    virtual HRESULT STDMETHODCALLTYPE Dispatch_InOut(
        /* [out][in] */ IDispatch **val);

    virtual HRESULT STDMETHODCALLTYPE Dispatch_Ret(
        /* [in] */ IDispatch *val,
        /* [retval][out] */ IDispatch **ret);

    virtual HRESULT STDMETHODCALLTYPE get_Variant_Property(
        /* [retval][out] */ VARIANT *ret);

    virtual HRESULT STDMETHODCALLTYPE put_Variant_Property(
        /* [in] */ VARIANT val);

    virtual HRESULT STDMETHODCALLTYPE Variant_InOut(
        /* [out][in] */ VARIANT *val);

    virtual HRESULT STDMETHODCALLTYPE Variant_Ret(
        /* [in] */ VARIANT val,
        /* [retval][out] */ VARIANT *ret);

    virtual HRESULT STDMETHODCALLTYPE Fail(
        /* [in] */ int errorCode,
        /* [in] */ BSTR message);

    virtual HRESULT STDMETHODCALLTYPE Throw() { throw std::exception(); }

public: // IDispatch
    DEFINE_DISPATCH();

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject,
            static_cast<IDispatch *>(this),
            static_cast<IBasicTest *>(this));
    }

    DEFINE_REF_COUNTING();

private:
    VARIANT_BOOL _boolean;
    int8_t _sbyte;
    uint8_t _byte;
    int16_t _short;
    uint16_t _ushort;
    int _int;
    uint32_t _uint;
    int64_t _long;
    uint64_t _ulong;
    float _float;
    double _double;
    BSTR _string;
    DATE _date;
    IDispatch *_dispatch;
    VARIANT _variant;
};
