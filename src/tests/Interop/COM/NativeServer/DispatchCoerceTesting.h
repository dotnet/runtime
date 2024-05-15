// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "Servers.h"

class DispatchCoerceTesting : public UnknownImpl, public IDispatchCoerceTesting
{
private:
    static const WCHAR * const Names[];
    static const int NamesCount;

public: // IDispatch
        virtual HRESULT STDMETHODCALLTYPE GetTypeInfoCount(
            /* [out] */ __RPC__out uint32_t *pctinfo)
        {
            *pctinfo = 0;
            return S_OK;
        }

        virtual HRESULT STDMETHODCALLTYPE GetTypeInfo(
            /* [in] */ uint32_t iTInfo,
            /* [in] */ LCID lcid,
            /* [out] */ __RPC__deref_out_opt ITypeInfo **ppTInfo)
        {
            return E_NOTIMPL;
        }

        virtual HRESULT STDMETHODCALLTYPE GetIDsOfNames(
            /* [in] */ __RPC__in REFIID,
            /* [size_is][in] */ __RPC__in_ecount_full(cNames) LPOLESTR *rgszNames,
            /* [range][in] */ __RPC__in_range(0,16384) uint32_t cNames,
            /* [in] */ LCID,
            /* [size_is][out] */ __RPC__out_ecount_full(cNames) DISPID *rgDispId)
        {
            bool containsUnknown = false;
            DISPID *curr = rgDispId;
            for (uint32_t i = 0; i < cNames; ++i)
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
            /* [annotation][in] */ _In_  uint16_t wFlags,
            /* [annotation][out][in] */ _In_  DISPPARAMS *pDispParams,
            /* [annotation][out] */ _Out_opt_  VARIANT *pVarResult,
            /* [annotation][out] */ _Out_opt_  EXCEPINFO *pExcepInfo,
            /* [annotation][out] */ _Out_opt_  uint32_t *puArgErr)
        {
            //
            // Note that arguments are received in reverse order for IDispatch::Invoke()
            //

            // HRESULT hr;

            switch (dispIdMember)
            {
            case 1:
            {
                return ReturnToManaged_Dispatch(pDispParams, pVarResult);
            }
            case 2:
            {
                return ManagedArgument_Dispatch(pDispParams, pVarResult);
            }
            case 3:
            {
                return BoolToString_Dispatch(pDispParams, pVarResult);
            }
            case 4:
            case 5:
            case 6:
            case 7:
            case 8:
            case 9:
            case 10:
            case 11:
            case 12:
            {
                return ReturnToManaged_Any_Dispatch(pDispParams, pVarResult);
            }
            }

            return E_NOTIMPL;
        }

public: // IDispatchCoerceTesting
    // Methods should only be invoked via IDispatch

private:

    HRESULT ReturnToManaged_Dispatch(_In_ DISPPARAMS *pDispParams, _Inout_ VARIANT *pVarResult)
    {
        HRESULT hr;

        short *args[1];
        size_t expectedArgCount = 1;
        RETURN_IF_FAILED(VerifyValues(uint32_t(expectedArgCount), pDispParams->cArgs));

        if (pVarResult == nullptr)
            return E_POINTER;

        VARENUM currType;
        VARIANTARG *currArg;
        size_t argIdx = expectedArgCount - 1;

        // Extract args
        {
            currType = VT_I2;
            currArg = NextArg(pDispParams->rgvarg, argIdx);
            RETURN_IF_FAILED(VerifyValues(VARENUM(currType), VARENUM(currArg->vt)));
            args[0] = &currArg->iVal;
        }

        VARENUM resultType = (VARENUM)*args[0];
        VariantInit(pVarResult);
        V_VT(pVarResult) = resultType & 0x7FFF;

        switch ((uint16_t)resultType)
        {
            case VT_BSTR:
            {
                BSTR str = ::SysAllocString(L"123");
                V_BSTR(pVarResult) = str;
                break;
            }
            case VT_R4:
            {
                V_R4(pVarResult) = 1.23f;
                break;
            }
            case VT_DATE:
            case VT_R8:
            {
                V_R8(pVarResult) = 1.23;
                break;
            }
            case VT_CY:
            {
                VarCyFromI4(123, &V_CY(pVarResult));
                break;
            }
            case VT_DECIMAL:
            {
                VarDecFromI4(123, &V_DECIMAL(pVarResult));
                break;
            }
            case ((VT_ERROR | 0x8000)):
            {
                V_I4(pVarResult) = DISP_E_PARAMNOTFOUND;
                break;
            }
            case VT_UNKNOWN:
            {
                (void)QueryInterface(IID_IUnknown, (void**)&V_UNKNOWN(pVarResult));
                break;
            }
            default:
            {
                V_I1(pVarResult) = 123;
                break;
            }
        }

        return S_OK;
    }

    HRESULT ManagedArgument_Dispatch(_In_ DISPPARAMS *pDispParams, _Inout_ VARIANT *pVarResult)
    {
        HRESULT hr;

        int *args[1];
        size_t expectedArgCount = 1;
        RETURN_IF_FAILED(VerifyValues(uint32_t(expectedArgCount), pDispParams->cArgs));

        if (pVarResult == nullptr)
            return E_POINTER;

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

        V_VT(pVarResult) = VT_I2;
        V_I2(pVarResult) = *args[0];
        return S_OK;
    }

    HRESULT ReturnToManaged_Any_Dispatch(_In_ DISPPARAMS *pDispParams, _Inout_ VARIANT *pVarResult)
    {
        HRESULT hr;

        int *args[1];
        size_t expectedArgCount = 1;
        RETURN_IF_FAILED(VerifyValues(uint32_t(expectedArgCount), pDispParams->cArgs));

        if (pVarResult == nullptr)
            return E_POINTER;

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

        V_VT(pVarResult) = VT_I4;
        V_I4(pVarResult) = *args[0];
        return S_OK;
    }

    HRESULT BoolToString_Dispatch(_In_ DISPPARAMS *pDispParams, _Inout_ VARIANT *pVarResult)
    {
        HRESULT hr;

        size_t expectedArgCount = 0;
        RETURN_IF_FAILED(VerifyValues(uint32_t(expectedArgCount), pDispParams->cArgs));

        if (pVarResult == nullptr)
            return E_POINTER;

        V_VT(pVarResult) = VT_BOOL;
        V_BOOL(pVarResult) = VARIANT_TRUE;
        return S_OK;
    }

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject, static_cast<IDispatch *>(this), static_cast<IDispatchCoerceTesting *>(this));
    }

    DEFINE_REF_COUNTING();
};

const WCHAR * const DispatchCoerceTesting::Names[] =
{
    W("__RESERVED__"),
    W("ReturnToManaged"),
    W("ManagedArgument"),
    W("BoolToString"),
    W("ReturnToManaged_Void"),
    W("ReturnToManaged_Double"),
    W("ReturnToManaged_String"),
    W("ReturnToManaged_Decimal"),
    W("ReturnToManaged_DateTime"),
    W("ReturnToManaged_Color"),
    W("ReturnToManaged_Missing"),
    W("ReturnToManaged_DBNull"),
};

const int DispatchCoerceTesting::NamesCount = ARRAY_SIZE(DispatchCoerceTesting::Names);
