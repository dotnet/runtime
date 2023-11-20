// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <type_traits>
#include <limits>
#include "Servers.h"

class NumericTesting : public UnknownImpl, public INumericTesting
{
public:
    DEF_FUNC(Add_Byte)(
        /*[in]*/ uint8_t a,
        /*[in]*/ uint8_t b,
        /*[out,retval]*/ uint8_t * pRetVal)
    {
        *pRetVal = static_cast<uint8_t>(a + b);
        return S_OK;
    }
    DEF_FUNC(Add_Short)(
        /*[in]*/ int16_t a,
        /*[in]*/ int16_t b,
        /*[out,retval]*/ int16_t * pRetVal)
    {
        *pRetVal = static_cast<int16_t>(a + b);
        return S_OK;
    }
    DEF_FUNC(Add_UShort)(
        /*[in]*/ uint16_t a,
        /*[in]*/ uint16_t b,
        /*[out,retval]*/ uint16_t * pRetVal)
    {
        *pRetVal = static_cast<uint16_t>(a + b);
        return S_OK;
    }
    DEF_FUNC(Add_Int)(
        /*[in]*/ int a,
        /*[in]*/ int b,
        /*[out,retval]*/ int * pRetVal)
    {
        *pRetVal = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_UInt)(
        /*[in]*/ uint32_t a,
        /*[in]*/ uint32_t b,
        /*[out,retval]*/ uint32_t * pRetVal)
    {
        *pRetVal = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_Long)(
        /*[in]*/ int64_t a,
        /*[in]*/ int64_t b,
        /*[out,retval]*/ int64_t * pRetVal)
    {
        *pRetVal = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_ULong)(
        /*[in]*/ uint64_t a,
        /*[in]*/ uint64_t b,
        /*[out,retval]*/ uint64_t * pRetVal)
    {
        *pRetVal = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_Float)(
        /*[in]*/ float a,
        /*[in]*/ float b,
        /*[out,retval]*/ float * pRetVal)
    {
        *pRetVal = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_Double)(
        /*[in]*/ double a,
        /*[in]*/ double b,
        /*[out,retval]*/ double * pRetVal)
    {
        *pRetVal = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_Byte_Ref)(
        /*[in]*/ uint8_t a,
        /*[in]*/ uint8_t b,
        /*[in,out]*/ uint8_t * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = static_cast<uint8_t>(a + b);
        return S_OK;
    }
    DEF_FUNC(Add_Short_Ref)(
        /*[in]*/ int16_t a,
        /*[in]*/ int16_t b,
        /*[in,out]*/ int16_t * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = static_cast<int16_t>(a + b);
        return S_OK;
    }
    DEF_FUNC(Add_UShort_Ref)(
        /*[in]*/ uint16_t a,
        /*[in]*/ uint16_t b,
        /*[in,out]*/ uint16_t * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = static_cast<uint16_t>(a + b);
        return S_OK;
    }
    DEF_FUNC(Add_Int_Ref)(
        /*[in]*/ int a,
        /*[in]*/ int b,
        /*[in,out]*/ int * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_UInt_Ref)(
        /*[in]*/ uint32_t a,
        /*[in]*/ uint32_t b,
        /*[in,out]*/ uint32_t * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_Long_Ref)(
        /*[in]*/ int64_t a,
        /*[in]*/ int64_t b,
        /*[in,out]*/ int64_t * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_ULong_Ref)(
        /*[in]*/ uint64_t a,
        /*[in]*/ uint64_t b,
        /*[in,out]*/ uint64_t * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_Float_Ref)(
        /*[in]*/ float a,
        /*[in]*/ float b,
        /*[in,out]*/ float * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_Double_Ref)(
        /*[in]*/ double a,
        /*[in]*/ double b,
        /*[in,out]*/ double * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_Byte_Out)(
        /*[in]*/ uint8_t a,
        /*[in]*/ uint8_t b,
        /*[out]*/ uint8_t * c)
    {
        *c = static_cast<uint8_t>(a + b);
        return S_OK;
    }
    DEF_FUNC(Add_Short_Out)(
        /*[in]*/ int16_t a,
        /*[in]*/ int16_t b,
        /*[out]*/ int16_t * c)
    {
        *c = static_cast<int16_t>(a + b);
        return S_OK;
    }
    DEF_FUNC(Add_UShort_Out)(
        /*[in]*/ uint16_t a,
        /*[in]*/ uint16_t b,
        /*[out]*/ uint16_t * c)
    {
        *c = static_cast<uint16_t>(a + b);
        return S_OK;
    }
    DEF_FUNC(Add_Int_Out)(
        /*[in]*/ int a,
        /*[in]*/ int b,
        /*[out]*/ int * c)
    {
        *c = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_UInt_Out)(
        /*[in]*/ uint32_t a,
        /*[in]*/ uint32_t b,
        /*[out]*/ uint32_t * c)
    {
        *c = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_Long_Out)(
        /*[in]*/ int64_t a,
        /*[in]*/ int64_t b,
        /*[out]*/ int64_t * c)
    {
        *c = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_ULong_Out)(
        /*[in]*/ uint64_t a,
        /*[in]*/ uint64_t b,
        /*[out]*/ uint64_t * c)
    {
        *c = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_Float_Out)(
        /*[in]*/ float a,
        /*[in]*/ float b,
        /*[out]*/ float * c)
    {
        *c = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_Double_Out)(
        /*[in]*/ double a,
        /*[in]*/ double b,
        /*[out]*/ double * c)
    {
        *c = a + b;
        return S_OK;
    }

    DEF_FUNC(Add_ManyInts11)(
        /*[in]*/ int i1,
        /*[in]*/ int i2,
        /*[in]*/ int i3,
        /*[in]*/ int i4,
        /*[in]*/ int i5,
        /*[in]*/ int i6,
        /*[in]*/ int i7,
        /*[in]*/ int i8,
        /*[in]*/ int i9,
        /*[in]*/ int i10,
        /*[in]*/ int i11,
        /*[out]*/ int * result )
    {
        *result = i1 + i2 + i3 + i4 + i5 + i6 + i7 + i8 + i9 + i10 + i11;
        return S_OK;
    }

    DEF_FUNC(Add_ManyInts12)(
        /*[in]*/ int i1,
        /*[in]*/ int i2,
        /*[in]*/ int i3,
        /*[in]*/ int i4,
        /*[in]*/ int i5,
        /*[in]*/ int i6,
        /*[in]*/ int i7,
        /*[in]*/ int i8,
        /*[in]*/ int i9,
        /*[in]*/ int i10,
        /*[in]*/ int i11,
        /*[in]*/ int i12,
        /*[out]*/ int * result )
    {
        *result = i1 + i2 + i3 + i4 + i5 + i6 + i7 + i8 + i9 + i10 + i11 + i12;
        return S_OK;
    }

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject, static_cast<INumericTesting *>(this));
    }

    DEFINE_REF_COUNTING();
};
