// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <type_traits>
#include <limits>
#include "Servers.h"

class NumericTesting : public UnknownImpl, public INumericTesting
{
public:
    DEF_FUNC(Add_Byte)(
        /*[in]*/ unsigned char a,
        /*[in]*/ unsigned char b,
        /*[out,retval]*/ unsigned char * pRetVal)
    {
        *pRetVal = static_cast<unsigned char>(a + b);
        return S_OK;
    }
    DEF_FUNC(Add_Short)(
        /*[in]*/ short a,
        /*[in]*/ short b,
        /*[out,retval]*/ short * pRetVal)
    {
        *pRetVal = static_cast<short>(a + b);
        return S_OK;
    }
    DEF_FUNC(Add_UShort)(
        /*[in]*/ unsigned short a,
        /*[in]*/ unsigned short b,
        /*[out,retval]*/ unsigned short * pRetVal)
    {
        *pRetVal = static_cast<unsigned short>(a + b);
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
        /*[in]*/ unsigned int a,
        /*[in]*/ unsigned int b,
        /*[out,retval]*/ unsigned int * pRetVal)
    {
        *pRetVal = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_Long)(
        /*[in]*/ __int64 a,
        /*[in]*/ __int64 b,
        /*[out,retval]*/ __int64 * pRetVal)
    {
        *pRetVal = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_ULong)(
        /*[in]*/ unsigned __int64 a,
        /*[in]*/ unsigned __int64 b,
        /*[out,retval]*/ unsigned __int64 * pRetVal)
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
        /*[in]*/ unsigned char a,
        /*[in]*/ unsigned char b,
        /*[in,out]*/ unsigned char * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = static_cast<unsigned char>(a + b);
        return S_OK;
    }
    DEF_FUNC(Add_Short_Ref)(
        /*[in]*/ short a,
        /*[in]*/ short b,
        /*[in,out]*/ short * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = static_cast<short>(a + b);
        return S_OK;
    }
    DEF_FUNC(Add_UShort_Ref)(
        /*[in]*/ unsigned short a,
        /*[in]*/ unsigned short b,
        /*[in,out]*/ unsigned short * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = static_cast<unsigned short>(a + b);
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
        /*[in]*/ unsigned int a,
        /*[in]*/ unsigned int b,
        /*[in,out]*/ unsigned int * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_Long_Ref)(
        /*[in]*/ __int64 a,
        /*[in]*/ __int64 b,
        /*[in,out]*/ __int64 * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_ULong_Ref)(
        /*[in]*/ unsigned __int64 a,
        /*[in]*/ unsigned __int64 b,
        /*[in,out]*/ unsigned __int64 * c)
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
        /*[in]*/ unsigned char a,
        /*[in]*/ unsigned char b,
        /*[out]*/ unsigned char * c)
    {
        *c = static_cast<unsigned char>(a + b);
        return S_OK;
    }
    DEF_FUNC(Add_Short_Out)(
        /*[in]*/ short a,
        /*[in]*/ short b,
        /*[out]*/ short * c)
    {
        *c = static_cast<short>(a + b);
        return S_OK;
    }
    DEF_FUNC(Add_UShort_Out)(
        /*[in]*/ unsigned short a,
        /*[in]*/ unsigned short b,
        /*[out]*/ unsigned short * c)
    {
        *c = static_cast<unsigned short>(a + b);
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
        /*[in]*/ unsigned int a,
        /*[in]*/ unsigned int b,
        /*[out]*/ unsigned int * c)
    {
        *c = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_Long_Out)(
        /*[in]*/ __int64 a,
        /*[in]*/ __int64 b,
        /*[out]*/ __int64 * c)
    {
        *c = a + b;
        return S_OK;
    }
    DEF_FUNC(Add_ULong_Out)(
        /*[in]*/ unsigned __int64 a,
        /*[in]*/ unsigned __int64 b,
        /*[out]*/ unsigned __int64 * c)
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
        return DoQueryInterface<NumericTesting, INumericTesting>(this, riid, ppvObject);
    }

    DEFINE_REF_COUNTING();
};
