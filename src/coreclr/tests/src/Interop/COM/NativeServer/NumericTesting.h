// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <type_traits>
#include "Servers.h"

class DECLSPEC_UUID("53169A33-E85D-4E3C-B668-24E438D0929B") NumericTesting : public UnknownImpl, public INumericTesting
{
public:
    DEF_RAWFUNC(Add_Byte)(
        /*[in]*/ unsigned char a,
        /*[in]*/ unsigned char b,
        /*[out,retval]*/ unsigned char * pRetVal)
    {
        *pRetVal = static_cast<unsigned char>(a + b);
        return S_OK;
    }
    DEF_RAWFUNC(Add_Short)(
        /*[in]*/ short a,
        /*[in]*/ short b,
        /*[out,retval]*/ short * pRetVal)
    {
        *pRetVal = static_cast<short>(a + b);
        return S_OK;
    }
    DEF_RAWFUNC(Add_UShort)(
        /*[in]*/ unsigned short a,
        /*[in]*/ unsigned short b,
        /*[out,retval]*/ unsigned short * pRetVal)
    {
        *pRetVal = static_cast<unsigned short>(a + b);
        return S_OK;
    }
    DEF_RAWFUNC(Add_Int)(
        /*[in]*/ long a,
        /*[in]*/ long b,
        /*[out,retval]*/ long * pRetVal)
    {
        *pRetVal = a + b;
        return S_OK;
    }
    DEF_RAWFUNC(Add_UInt)(
        /*[in]*/ unsigned long a,
        /*[in]*/ unsigned long b,
        /*[out,retval]*/ unsigned long * pRetVal)
    {
        *pRetVal = a + b;
        return S_OK;
    }
    DEF_RAWFUNC(Add_Long)(
        /*[in]*/ __int64 a,
        /*[in]*/ __int64 b,
        /*[out,retval]*/ __int64 * pRetVal)
    {
        *pRetVal = a + b;
        return S_OK;
    }
    DEF_RAWFUNC(Add_ULong)(
        /*[in]*/ unsigned __int64 a,
        /*[in]*/ unsigned __int64 b,
        /*[out,retval]*/ unsigned __int64 * pRetVal)
    {
        *pRetVal = a + b;
        return S_OK;
    }
    DEF_RAWFUNC(Add_Float)(
        /*[in]*/ float a,
        /*[in]*/ float b,
        /*[out,retval]*/ float * pRetVal)
    {
        *pRetVal = a + b;
        return S_OK;
    }
    DEF_RAWFUNC(Add_Double)(
        /*[in]*/ double a,
        /*[in]*/ double b,
        /*[out,retval]*/ double * pRetVal)
    {
        *pRetVal = a + b;
        return S_OK;
    }
    DEF_RAWFUNC(Add_Byte_Ref)(
        /*[in]*/ unsigned char a,
        /*[in]*/ unsigned char b,
        /*[in,out]*/ unsigned char * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = static_cast<unsigned char>(a + b);
        return S_OK;
    }
    DEF_RAWFUNC(Add_Short_Ref)(
        /*[in]*/ short a,
        /*[in]*/ short b,
        /*[in,out]*/ short * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = static_cast<short>(a + b);
        return S_OK;
    }
    DEF_RAWFUNC(Add_UShort_Ref)(
        /*[in]*/ unsigned short a,
        /*[in]*/ unsigned short b,
        /*[in,out]*/ unsigned short * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = static_cast<unsigned short>(a + b);
        return S_OK;
    }
    DEF_RAWFUNC(Add_Int_Ref)(
        /*[in]*/ long a,
        /*[in]*/ long b,
        /*[in,out]*/ long * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = a + b;
        return S_OK;
    }
    DEF_RAWFUNC(Add_UInt_Ref)(
        /*[in]*/ unsigned long a,
        /*[in]*/ unsigned long b,
        /*[in,out]*/ unsigned long * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = a + b;
        return S_OK;
    }
    DEF_RAWFUNC(Add_Long_Ref)(
        /*[in]*/ __int64 a,
        /*[in]*/ __int64 b,
        /*[in,out]*/ __int64 * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = a + b;
        return S_OK;
    }
    DEF_RAWFUNC(Add_ULong_Ref)(
        /*[in]*/ unsigned __int64 a,
        /*[in]*/ unsigned __int64 b,
        /*[in,out]*/ unsigned __int64 * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = a + b;
        return S_OK;
    }
    DEF_RAWFUNC(Add_Float_Ref)(
        /*[in]*/ float a,
        /*[in]*/ float b,
        /*[in,out]*/ float * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = a + b;
        return S_OK;
    }
    DEF_RAWFUNC(Add_Double_Ref)(
        /*[in]*/ double a,
        /*[in]*/ double b,
        /*[in,out]*/ double * c)
    {
        if (*c != std::numeric_limits<std::remove_reference<decltype(*c)>::type>::max())
            return E_UNEXPECTED;
        *c = a + b;
        return S_OK;
    }
    DEF_RAWFUNC(Add_Byte_Out)(
        /*[in]*/ unsigned char a,
        /*[in]*/ unsigned char b,
        /*[out]*/ unsigned char * c)
    {
        *c = static_cast<unsigned char>(a + b);
        return S_OK;
    }
    DEF_RAWFUNC(Add_Short_Out)(
        /*[in]*/ short a,
        /*[in]*/ short b,
        /*[out]*/ short * c)
    {
        *c = static_cast<short>(a + b);
        return S_OK;
    }
    DEF_RAWFUNC(Add_UShort_Out)(
        /*[in]*/ unsigned short a,
        /*[in]*/ unsigned short b,
        /*[out]*/ unsigned short * c)
    {
        *c = static_cast<unsigned short>(a + b);
        return S_OK;
    }
    DEF_RAWFUNC(Add_Int_Out)(
        /*[in]*/ long a,
        /*[in]*/ long b,
        /*[out]*/ long * c)
    {
        *c = a + b;
        return S_OK;
    }
    DEF_RAWFUNC(Add_UInt_Out)(
        /*[in]*/ unsigned long a,
        /*[in]*/ unsigned long b,
        /*[out]*/ unsigned long * c)
    {
        *c = a + b;
        return S_OK;
    }
    DEF_RAWFUNC(Add_Long_Out)(
        /*[in]*/ __int64 a,
        /*[in]*/ __int64 b,
        /*[out]*/ __int64 * c)
    {
        *c = a + b;
        return S_OK;
    }
    DEF_RAWFUNC(Add_ULong_Out)(
        /*[in]*/ unsigned __int64 a,
        /*[in]*/ unsigned __int64 b,
        /*[out]*/ unsigned __int64 * c)
    {
        *c = a + b;
        return S_OK;
    }
    DEF_RAWFUNC(Add_Float_Out)(
        /*[in]*/ float a,
        /*[in]*/ float b,
        /*[out]*/ float * c)
    {
        *c = a + b;
        return S_OK;
    }
    DEF_RAWFUNC(Add_Double_Out)(
        /*[in]*/ double a,
        /*[in]*/ double b,
        /*[out]*/ double * c)
    {
        *c = a + b;
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
