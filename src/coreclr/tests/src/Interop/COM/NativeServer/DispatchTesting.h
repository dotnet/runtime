// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "Servers.h"

template<typename T>
HRESULT VerifyValues(_In_ const T expected, _In_ const T actual)
{
    return (expected == actual) ? S_OK : E_INVALIDARG;
}

VARIANTARG *NextArg(_In_ VARIANTARG *args, _Inout_ size_t &currIndex)
{
    return (args + (currIndex--));
}

class DispatchTesting : public UnknownImpl, public IDispatchTesting
{
private:
    static const WCHAR * const Names[];
    static const int NamesCount;

public: // IDispatch
        virtual HRESULT STDMETHODCALLTYPE GetTypeInfoCount( 
            /* [out] */ __RPC__out UINT *pctinfo)
        {
            *pctinfo = 0;
            return S_OK;
        }
        
        virtual HRESULT STDMETHODCALLTYPE GetTypeInfo( 
            /* [in] */ UINT iTInfo,
            /* [in] */ LCID lcid,
            /* [out] */ __RPC__deref_out_opt ITypeInfo **ppTInfo)
        {
            return E_NOTIMPL;
        }
        
        virtual HRESULT STDMETHODCALLTYPE GetIDsOfNames( 
            /* [in] */ __RPC__in REFIID,
            /* [size_is][in] */ __RPC__in_ecount_full(cNames) LPOLESTR *rgszNames,
            /* [range][in] */ __RPC__in_range(0,16384) UINT cNames,
            /* [in] */ LCID,
            /* [size_is][out] */ __RPC__out_ecount_full(cNames) DISPID *rgDispId)
        {
            bool containsUnknown = false;
            DISPID *curr = rgDispId;
            for (UINT i = 0; i < cNames; ++i)
            {
                *curr = DISPID_UNKNOWN;
                LPOLESTR name = rgszNames[i];
                for (int j = 1; j < NamesCount; ++j)
                {
                    const WCHAR *nameMaybe = Names[j];
                    if (::TP_wcmp_s(name, nameMaybe) == 0)
                    {
                        *curr = DISPID{ j };
                        break;
                    }
                }

                containsUnknown &= (*curr == DISPID_UNKNOWN);
                curr++;
            }

            return (containsUnknown) ? DISP_E_UNKNOWNNAME : S_OK;
        }

        virtual /* [local] */ HRESULT STDMETHODCALLTYPE Invoke( 
            /* [annotation][in] */ _In_  DISPID dispIdMember,
            /* [annotation][in] */ _In_  REFIID riid,
            /* [annotation][in] */ _In_  LCID lcid,
            /* [annotation][in] */ _In_  WORD wFlags,
            /* [annotation][out][in] */ _In_  DISPPARAMS *pDispParams,
            /* [annotation][out] */ _Out_opt_  VARIANT *pVarResult,
            /* [annotation][out] */ _Out_opt_  EXCEPINFO *pExcepInfo,
            /* [annotation][out] */ _Out_opt_  UINT *puArgErr)
        {
            //
            // Note that arguments are received in reverse order for IDispatch::Invoke()
            //

            HRESULT hr;

            switch (dispIdMember)
            {
            case 1:
            {
                RETURN_IF_FAILED(VerifyValues<VARIANT*>(pVarResult, nullptr));
                return DoubleNumeric_ReturnByRef_Proxy(pDispParams);
            }
            case 2:
            {
                return Add_Float_ReturnAndUpdateByRef_Proxy(pDispParams, pVarResult);
            }
            case 3:
            {
                return Add_Double_ReturnAndUpdateByRef_Proxy(pDispParams, pVarResult);
            }
            case 4:
            {
                return TriggerException_Proxy(pDispParams, pExcepInfo, puArgErr);
            }
            case 5:
            {
                return DoubleHVAValues_Proxy(pDispParams, pVarResult);
            }
            case 6:
            {
                return PassThroughLCID_Proxy(lcid, pVarResult);
            }
            }

            return E_NOTIMPL;
        }

public: // IDispatchTesting
    virtual HRESULT STDMETHODCALLTYPE DoubleNumeric_ReturnByRef (
        /*[in]*/ unsigned char b1,
        /*[in,out]*/ unsigned char *b2,
        /*[in]*/ short s1,
        /*[in,out]*/ short *s2,
        /*[in]*/ unsigned short us1,
        /*[in,out]*/ unsigned short *us2,
        /*[in]*/ int i1,
        /*[in,out]*/ int *i2,
        /*[in]*/ unsigned int ui1,
        /*[in,out]*/ unsigned int *ui2,
        /*[in]*/ __int64 l1,
        /*[in,out]*/ __int64 *l2,
        /*[in]*/ unsigned __int64 ul1,
        /*[in,out]*/ unsigned __int64 *ul2 )
    {
        *b2 = static_cast<unsigned char>(b1 * 2);
        *s2 = static_cast<short>(s1 * 2);
        *us2 = static_cast<unsigned short>(us1 * 2);
        *i2 = i1 * 2;
        *ui2 = ui1 * 2u;
        *l2 = l1 * 2ll;
        *ul2 = ul1 * 2ull;
        return S_OK;
    }
    virtual HRESULT STDMETHODCALLTYPE Add_Float_ReturnAndUpdateByRef(
        /*[in]*/ float a,
        /*[in,out]*/ float *b,
        /*[out,retval]*/ float * pRetVal)
    {
        float c = a + *b;
        *pRetVal = *b = c;
        return S_OK;
    }
    virtual HRESULT STDMETHODCALLTYPE Add_Double_ReturnAndUpdateByRef(
        /*[in]*/ double a,
        /*[in,out]*/ double *b,
        /*[out,retval]*/ double * pRetVal)
    {
        double c = a + *b;
        *pRetVal = *b = c;
        return S_OK;
    }
    virtual HRESULT STDMETHODCALLTYPE TriggerException (
        /*[in]*/ enum IDispatchTesting_Exception excep,
        /*[in]*/ int errorCode)
    {
        switch (excep)
        {
        case IDispatchTesting_Exception_Disp:
            return DISP_E_EXCEPTION;
        case IDispatchTesting_Exception_HResult:
            return HRESULT_FROM_WIN32(errorCode);
        default:
            return S_FALSE; // Return a success case to indicate failure to trigger a failure.
        }
    }
    virtual HRESULT STDMETHODCALLTYPE DoubleHVAValues (
        /*[in,out]*/ HFA_4 *input,
        /*[out,retval]*/ HFA_4 *pRetVal)
    {
        pRetVal->x = (input->x * 2);
        pRetVal->y = (input->y * 2);
        pRetVal->z = (input->z * 2);
        pRetVal->w = (input->w * 2);
        return S_OK;
    }

private:
    HRESULT DoubleNumeric_ReturnByRef_Proxy(_In_ DISPPARAMS *pDispParams)
    {
        HRESULT hr;

        unsigned char *b_args[2];
        short *s_args[2];
        unsigned short *us_args[2];
        int *i_args[2];
        unsigned int *ui_args[2];
        __int64 *l_args[2];
        unsigned __int64 *ul_args[2];
        size_t expectedArgCount =
            ARRAYSIZE(b_args)
            + ARRAYSIZE(s_args)
            + ARRAYSIZE(us_args)
            + ARRAYSIZE(i_args)
            + ARRAYSIZE(ui_args)
            + ARRAYSIZE(l_args)
            + ARRAYSIZE(ul_args);
        RETURN_IF_FAILED(VerifyValues(UINT(expectedArgCount), pDispParams->cArgs));

        VARENUM currType;
        VARIANTARG *currArg;
        size_t argIdx = expectedArgCount - 1;

        // Extract args
        {
            currType = VT_UI1;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(currType), VARENUM(currArg->vt)));
            b_args[0] = &currArg->bVal;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(VT_BYREF | currType), VARENUM(currArg->vt)));
            b_args[1] = (unsigned char*)currArg->byref;
        }
        {
            currType = VT_I2;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(currType), VARENUM(currArg->vt)));
            s_args[0] = &currArg->iVal;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(VT_BYREF | currType), VARENUM(currArg->vt)));
            s_args[1] = (short*)currArg->byref;
        }
        {
            currType = VT_UI2;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(currType), VARENUM(currArg->vt)));
            us_args[0] = &currArg->uiVal;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(VT_BYREF | currType), VARENUM(currArg->vt)));
            us_args[1] = (unsigned short*)currArg->byref;
        }
        {
            currType = VT_I4;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(currType), VARENUM(currArg->vt)));
            i_args[0] = &currArg->intVal;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(VT_BYREF | currType), VARENUM(currArg->vt)));
            i_args[1] = (int*)currArg->byref;
        }
        {
            currType = VT_UI4;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(currType), VARENUM(currArg->vt)));
            ui_args[0] = &currArg->uintVal;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(VT_BYREF | currType), VARENUM(currArg->vt)));
            ui_args[1] = (unsigned int*)currArg->byref;
        }
        {
            currType = VT_I8;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(currType), VARENUM(currArg->vt)));
            l_args[0] = &currArg->llVal;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(VT_BYREF | currType), VARENUM(currArg->vt)));
            l_args[1] = (__int64*)currArg->byref;
        }
        {
            currType = VT_UI8;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(currType), VARENUM(currArg->vt)));
            ul_args[0] = &currArg->ullVal;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(VT_BYREF | currType), VARENUM(currArg->vt)));
            ul_args[1] = (unsigned __int64*)currArg->byref;
        }

        return DoubleNumeric_ReturnByRef(
            *b_args[0], b_args[1],
            *s_args[0], s_args[1],
            *us_args[0], us_args[1],
            *i_args[0], i_args[1],
            *ui_args[0], ui_args[1],
            *l_args[0], l_args[1],
            *ul_args[0], ul_args[1]);
    }

    HRESULT Add_Float_ReturnAndUpdateByRef_Proxy(_In_ DISPPARAMS *pDispParams, _Inout_ VARIANT *pVarResult)
    {
        HRESULT hr;

        float *args[2];
        size_t expectedArgCount = ARRAYSIZE(args);
        RETURN_IF_FAILED(VerifyValues(UINT(expectedArgCount), pDispParams->cArgs));

        if (pVarResult == nullptr)
            return E_POINTER;

        VARENUM currType;
        VARIANTARG *currArg;
        size_t argIdx = expectedArgCount - 1;

        // Extract args
        {
            currType = VT_R4;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(currType), VARENUM(currArg->vt)));
            args[0] = &currArg->fltVal;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(VT_BYREF | currType), VARENUM(currArg->vt)));
            args[1] = (float*)currArg->byref;
        }

        RETURN_IF_FAILED(::VariantChangeType(pVarResult, pVarResult, 0, VT_R4));
        return Add_Float_ReturnAndUpdateByRef(*args[0], args[1], &pVarResult->fltVal);
    }

    HRESULT Add_Double_ReturnAndUpdateByRef_Proxy(_In_ DISPPARAMS *pDispParams, _Inout_ VARIANT *pVarResult)
    {
        HRESULT hr;

        double *args[2];
        size_t expectedArgCount = ARRAYSIZE(args);
        RETURN_IF_FAILED(VerifyValues(UINT(expectedArgCount), pDispParams->cArgs));

        if (pVarResult == nullptr)
            return E_POINTER;

        VARENUM currType;
        VARIANTARG *currArg;
        size_t argIdx = expectedArgCount - 1;

        // Extract args
        {
            currType = VT_R8;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(currType), VARENUM(currArg->vt)));
            args[0] = &currArg->dblVal;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(VT_BYREF | currType), VARENUM(currArg->vt)));
            args[1] = (double*)currArg->byref;
        }

        RETURN_IF_FAILED(::VariantChangeType(pVarResult, pVarResult, 0, VT_R8));
        return Add_Double_ReturnAndUpdateByRef(*args[0], args[1], &pVarResult->dblVal);
    }

    HRESULT TriggerException_Proxy(
        _In_ DISPPARAMS *pDispParams,
        _Out_ EXCEPINFO *pExcepInfo,
        _Out_ UINT *puArgErr)
    {
        HRESULT hr;

        int *args[2];
        size_t expectedArgCount = ARRAYSIZE(args);
        RETURN_IF_FAILED(VerifyValues(UINT(expectedArgCount), pDispParams->cArgs));

        VARENUM currType;
        VARIANTARG *currArg;
        size_t argIdx = expectedArgCount - 1;

        // Extract args
        {
            currType = VT_I4;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(currType), VARENUM(currArg->vt)));
            args[0] = &currArg->intVal;
        }
        {
            currType = VT_I4;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(currType), VARENUM(currArg->vt)));
            args[1] = &currArg->intVal;
        }

        hr = TriggerException(static_cast<IDispatchTesting_Exception>(*args[0]), *args[1]);
        if (hr == DISP_E_EXCEPTION)
        {
            *puArgErr = 1;
            pExcepInfo->scode = HRESULT_FROM_WIN32(*args[1]);

            WCHAR buffer[ARRAYSIZE(W("4294967295"))];
            _snwprintf_s(buffer, ARRAYSIZE(buffer), _TRUNCATE, W("%x"), *args[1]);
            pExcepInfo->bstrDescription = SysAllocString(buffer);
        }

        return hr;
    }

    HRESULT DoubleHVAValues_Proxy(_In_ DISPPARAMS *pDispParams, _Inout_ VARIANT *pVarResult)
    {
        HRESULT hr;

        HFA_4 *args[1];
        size_t expectedArgCount = ARRAYSIZE(args);
        RETURN_IF_FAILED(VerifyValues(UINT(expectedArgCount), pDispParams->cArgs));

        VARENUM currType;
        VARIANTARG *currArg;
        size_t argIdx = expectedArgCount - 1;

        // Extract args
        {
            currType = VT_RECORD;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(currType), VARENUM(currArg->vt)));
            args[0] = (HFA_4*)currArg->pvRecord;
        }

        RETURN_IF_FAILED(::VariantChangeType(pVarResult, pVarResult, 0, VT_RECORD));
        return DoubleHVAValues(args[0], (HFA_4*)&pVarResult->pvRecord);
    }

    HRESULT PassThroughLCID_Proxy(_In_ LCID lcid, _Inout_ VARIANT* pVarResult)
    {
        V_VT(pVarResult) = VT_I4;
        V_I4(pVarResult) = lcid;
        return S_OK;
    }

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject, static_cast<IDispatch *>(this), static_cast<IDispatchTesting *>(this));
    }

    DEFINE_REF_COUNTING();
};

const WCHAR * const DispatchTesting::Names[] =
{
    W("__RESERVED__"),
    W("DoubleNumeric_ReturnByRef"),
    W("Add_Float_ReturnAndUpdateByRef"),
    W("Add_Double_ReturnAndUpdateByRef"),
    W("TriggerException"),
    W("DoubleHVAValues"),
    W("PassThroughLCID")
};

const int DispatchTesting::NamesCount = ARRAYSIZE(DispatchTesting::Names);
