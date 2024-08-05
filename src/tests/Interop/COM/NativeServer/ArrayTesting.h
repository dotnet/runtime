// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <cstdint>
#include "Servers.h"

class ArrayTesting : public UnknownImpl, public IArrayTesting
{
private:
    template<typename L, typename D>
    double Mean(L l, D *d)
    {
        double t = 0.0;
        for (L i = 0; i < l; ++i)
            t += d[i];

        return (t / l);
    }
    template<VARTYPE E>
    HRESULT Mean(SAFEARRAY *d, int *l, double *r)
    {
        HRESULT hr;

        VARTYPE type;
        RETURN_IF_FAILED(::SafeArrayGetVartype(d, &type));

        if (E != type)
            return E_UNEXPECTED;

        LONG upperBoundIndex;
        RETURN_IF_FAILED(::SafeArrayGetUBound(d, 1, &upperBoundIndex));

        // Upper bound is index so add '1'
        *l = (upperBoundIndex + 1);

        switch (type)
        {
        case VT_UI1:
            *r = Mean(*l, static_cast<uint8_t*>(d->pvData));
            break;
        case VT_I2:
            *r = Mean(*l, static_cast<int16_t*>(d->pvData));
            break;
        case VT_UI2:
            *r = Mean(*l, static_cast<uint16_t*>(d->pvData));
            break;
        case VT_I4:
            *r = Mean(*l, static_cast<int32_t*>(d->pvData));
            break;
        case VT_UI4:
            *r = Mean(*l, static_cast<uint32_t*>(d->pvData));
            break;
        case VT_I8:
            *r = Mean(*l, static_cast<int64_t*>(d->pvData));
            break;
        case VT_UI8:
            *r = Mean(*l, static_cast<uint64_t*>(d->pvData));
            break;
        case VT_R4:
            *r = Mean(*l, static_cast<float*>(d->pvData));
            break;
        case VT_R8:
            *r = Mean(*l, static_cast<double *>(d->pvData));
            break;
        default:
            return E_INVALIDARG;
        }

        return S_OK;
    }

public: // IArrayTesting
    DEF_FUNC(Mean_Byte_LP_PreLen)(
        /*[in]*/ int len,
        /*[in]*/ unsigned char * d,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        *pRetVal = Mean(len, d);
        return S_OK;
    }
    DEF_FUNC(Mean_Short_LP_PreLen)(
        /*[in]*/ int len,
        /*[in]*/ short * d,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        *pRetVal = Mean(len, d);
        return S_OK;
    }
    DEF_FUNC(Mean_UShort_LP_PreLen)(
        /*[in]*/ int len,
        /*[in]*/ unsigned short * d,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        *pRetVal = Mean(len, d);
        return S_OK;
    }
    DEF_FUNC(Mean_Int_LP_PreLen)(
        /*[in]*/ int len,
        /*[in]*/ int * d,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        *pRetVal = Mean(len, d);
        return S_OK;
    }
    DEF_FUNC(Mean_UInt_LP_PreLen)(
        /*[in]*/ int len,
        /*[in]*/ unsigned int * d,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        *pRetVal = Mean(len, d);
        return S_OK;
    }
    DEF_FUNC(Mean_Long_LP_PreLen)(
        /*[in]*/ int len,
        /*[in]*/ __int64 * d,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        *pRetVal = Mean(len, d);
        return S_OK;
    }
    DEF_FUNC(Mean_ULong_LP_PreLen)(
        /*[in]*/ int len,
        /*[in]*/ unsigned __int64 * d,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        *pRetVal = Mean(len, d);
        return S_OK;
    }
    DEF_FUNC(Mean_Float_LP_PreLen)(
        /*[in]*/ int len,
        /*[in]*/ float * d,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        *pRetVal = Mean(len, d);
        return S_OK;
    }
    DEF_FUNC(Mean_Double_LP_PreLen)(
        /*[in]*/ int len,
        /*[in]*/ double * d,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        *pRetVal = Mean(len, d);
        return S_OK;
    }
    DEF_FUNC(Mean_Byte_LP_PostLen)(
        /*[in]*/ unsigned char * d,
        /*[in]*/ int len,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        *pRetVal = Mean(len, d);
        return S_OK;
    }
    DEF_FUNC(Mean_Short_LP_PostLen)(
        /*[in]*/ short * d,
        /*[in]*/ int len,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        *pRetVal = Mean(len, d);
        return S_OK;
    }
    DEF_FUNC(Mean_UShort_LP_PostLen)(
        /*[in]*/ unsigned short * d,
        /*[in]*/ int len,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        *pRetVal = Mean(len, d);
        return S_OK;
    }
    DEF_FUNC(Mean_Int_LP_PostLen)(
        /*[in]*/ int * d,
        /*[in]*/ int len,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        *pRetVal = Mean(len, d);
        return S_OK;
    }
    DEF_FUNC(Mean_UInt_LP_PostLen)(
        /*[in]*/ unsigned int * d,
        /*[in]*/ int len,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        *pRetVal = Mean(len, d);
        return S_OK;
    }
    DEF_FUNC(Mean_Long_LP_PostLen)(
        /*[in]*/ __int64 * d,
        /*[in]*/ int len,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        *pRetVal = Mean(len, d);
        return S_OK;
    }
    DEF_FUNC(Mean_ULong_LP_PostLen)(
        /*[in]*/ unsigned __int64 * d,
        /*[in]*/ int len,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        *pRetVal = Mean(len, d);
        return S_OK;
    }
    DEF_FUNC(Mean_Float_LP_PostLen)(
        /*[in]*/ float * d,
        /*[in]*/ int len,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        *pRetVal = Mean(len, d);
        return S_OK;
    }
    DEF_FUNC(Mean_Double_LP_PostLen)(
        /*[in]*/ double * d,
        /*[in]*/ int len,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        *pRetVal = Mean(len, d);
        return S_OK;
    }
    DEF_FUNC(Mean_Byte_SafeArray_OutLen)(
        /*[in]*/ SAFEARRAY * d,
        /*[out]*/ int * len,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        return Mean<VT_UI1>(d, len, pRetVal);
    }
    DEF_FUNC(Mean_Short_SafeArray_OutLen)(
        /*[in]*/ SAFEARRAY * d,
        /*[out]*/ int * len,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        return Mean<VT_I2>(d, len, pRetVal);
    }
    DEF_FUNC(Mean_UShort_SafeArray_OutLen)(
        /*[in]*/ SAFEARRAY * d,
        /*[out]*/ int * len,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        return Mean<VT_UI2>(d, len, pRetVal);
    }
    DEF_FUNC(Mean_Int_SafeArray_OutLen)(
        /*[in]*/ SAFEARRAY * d,
        /*[out]*/ int * len,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        return Mean<VT_I4>(d, len, pRetVal);
    }
    DEF_FUNC(Mean_UInt_SafeArray_OutLen)(
        /*[in]*/ SAFEARRAY * d,
        /*[out]*/ int * len,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        return Mean<VT_UI4>(d, len, pRetVal);
    }
    DEF_FUNC(Mean_Long_SafeArray_OutLen)(
        /*[in]*/ SAFEARRAY * d,
        /*[out]*/ int * len,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        return Mean<VT_I8>(d, len, pRetVal);
    }
    DEF_FUNC(Mean_ULong_SafeArray_OutLen)(
        /*[in]*/ SAFEARRAY * d,
        /*[out]*/ int * len,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        return Mean<VT_UI8>(d, len, pRetVal);
    }
    DEF_FUNC(Mean_Float_SafeArray_OutLen)(
        /*[in]*/ SAFEARRAY * d,
        /*[out]*/ int * len,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        return Mean<VT_R4>(d, len, pRetVal);
    }
    DEF_FUNC(Mean_Double_SafeArray_OutLen)(
        /*[in]*/ SAFEARRAY * d,
        /*[out]*/ int * len,
        /*[out,retval]*/ double * pRetVal)
    {
        if (pRetVal == nullptr)
            return E_POINTER;
        return Mean<VT_R8>(d, len, pRetVal);
    }

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject, static_cast<IArrayTesting *>(this));
    }

    DEFINE_REF_COUNTING();
};
