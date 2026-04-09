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
        /* [in] */ LONG val,
        /* [retval][out] */ LONG *ret);

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
        /* [retval][out] */ signed char *ret);
    
    virtual HRESULT STDMETHODCALLTYPE put_SByte_Property( 
        /* [in] */ signed char val);
    
    virtual HRESULT STDMETHODCALLTYPE SByte_Doubled_InOut( 
        /* [out][in] */ signed char *val);
    
    virtual HRESULT STDMETHODCALLTYPE SByte_Doubled_Ret( 
        /* [in] */ signed char val,
        /* [retval][out] */ signed char *ret);
    
    virtual HRESULT STDMETHODCALLTYPE get_Byte_Property( 
        /* [retval][out] */ unsigned char *ret);
    
    virtual HRESULT STDMETHODCALLTYPE put_Byte_Property( 
        /* [in] */ unsigned char val);
    
    virtual HRESULT STDMETHODCALLTYPE Byte_Doubled_InOut( 
        /* [out][in] */ unsigned char *val);
    
    virtual HRESULT STDMETHODCALLTYPE Byte_Doubled_Ret( 
        /* [in] */ unsigned char val,
        /* [retval][out] */ unsigned char *ret);
    
    virtual HRESULT STDMETHODCALLTYPE get_Short_Property( 
        /* [retval][out] */ short *ret);
    
    virtual HRESULT STDMETHODCALLTYPE put_Short_Property( 
        /* [in] */ short val);
    
    virtual HRESULT STDMETHODCALLTYPE Short_Doubled_InOut( 
        /* [out][in] */ short *val);
    
    virtual HRESULT STDMETHODCALLTYPE Short_Doubled_Ret( 
        /* [in] */ short val,
        /* [retval][out] */ short *ret);
    
    virtual HRESULT STDMETHODCALLTYPE get_UShort_Property( 
        /* [retval][out] */ unsigned short *ret);
    
    virtual HRESULT STDMETHODCALLTYPE put_UShort_Property( 
        /* [in] */ unsigned short val);
    
    virtual HRESULT STDMETHODCALLTYPE UShort_Doubled_InOut( 
        /* [out][in] */ unsigned short *val);
    
    virtual HRESULT STDMETHODCALLTYPE UShort_Doubled_Ret( 
        /* [in] */ unsigned short val,
        /* [retval][out] */ unsigned short *ret);
    
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
        /* [retval][out] */ unsigned int *ret);
    
    virtual HRESULT STDMETHODCALLTYPE put_UInt_Property( 
        /* [in] */ unsigned int val);
    
    virtual HRESULT STDMETHODCALLTYPE UInt_Doubled_InOut( 
        /* [out][in] */ unsigned int *val);
    
    virtual HRESULT STDMETHODCALLTYPE UInt_Doubled_Ret( 
        /* [in] */ unsigned int val,
        /* [retval][out] */ unsigned int *ret);
    
    virtual HRESULT STDMETHODCALLTYPE get_Int64_Property( 
        /* [retval][out] */ __int64 *ret);
    
    virtual HRESULT STDMETHODCALLTYPE put_Int64_Property( 
        /* [in] */ __int64 val);
    
    virtual HRESULT STDMETHODCALLTYPE Int64_Doubled_InOut( 
        /* [out][in] */ __int64 *val);
    
    virtual HRESULT STDMETHODCALLTYPE Int64_Doubled_Ret( 
        /* [in] */ __int64 val,
        /* [retval][out] */ __int64 *ret);
    
    virtual HRESULT STDMETHODCALLTYPE get_UInt64_Property( 
        /* [retval][out] */ unsigned __int64 *ret);
    
    virtual HRESULT STDMETHODCALLTYPE put_UInt64_Property( 
        /* [in] */ unsigned __int64 val);
    
    virtual HRESULT STDMETHODCALLTYPE UInt64_Doubled_InOut( 
        /* [out][in] */ unsigned __int64 *val);
    
    virtual HRESULT STDMETHODCALLTYPE UInt64_Doubled_Ret( 
        /* [in] */ unsigned __int64 val,
        /* [retval][out] */ unsigned __int64 *ret);
    
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
    signed char _sbyte;
    unsigned char _byte;
    short _short;
    unsigned short _ushort;
    int _int;
    unsigned int _uint;
    __int64 _long;
    unsigned __int64 _ulong;
    float _float;
    double _double;
    BSTR _string;
    DATE _date;
    IDispatch *_dispatch;
    VARIANT _variant;
};
