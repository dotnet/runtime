// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>
#include <platformdefines.h>
#include <ComHelpers.h>
#include <algorithm>

class IntegerEnumerator : public UnknownImpl, public IEnumVARIANT
{
    int start;
    int count;
    int current;

public:
    IntegerEnumerator(int start, int count)
        :UnknownImpl(),
        start(start),
        count(count),
        current(start)
    {
    }

    HRESULT STDMETHODCALLTYPE Next(
        ULONG celt,
        VARIANT *rgVar,
        ULONG *pCeltFetched) override
    {
        for(*pCeltFetched = 0; *pCeltFetched < celt && current < start + count; ++*pCeltFetched, ++current)
        {
            VariantClear(&(rgVar[*pCeltFetched]));
            V_VT(&rgVar[*pCeltFetched]) = VT_I4;
            V_I4(&(rgVar[*pCeltFetched])) = current;
        }

        return celt == *pCeltFetched ? S_OK : S_FALSE;
    }

    HRESULT STDMETHODCALLTYPE Skip(ULONG celt) override
    {
        int original = current;
        current = std::min(current + (int)celt, start + count);
        return original + (int)celt <= start + count ? S_OK : S_FALSE;
    }

    HRESULT STDMETHODCALLTYPE Reset(void) override
    {
        current = start;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE Clone(IEnumVARIANT **ppEnum) override
    {
        IntegerEnumerator* clone = new IntegerEnumerator(start, count);
        clone->current = current;
        *ppEnum = clone;

        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE QueryInterface(
        REFIID riid,
        void** ppvObject)
    {
        return DoQueryInterface(riid, ppvObject, static_cast<IEnumVARIANT *>(this));
    }

    DEFINE_REF_COUNTING();
};

class IntegerEnumerable : public UnknownImpl, public IDispatch
{
private:
    int start;
    int count;
public:
    IntegerEnumerable(int start, int count)
        :UnknownImpl(),
        start(start),
        count(count)
    {
    }

    HRESULT STDMETHODCALLTYPE GetTypeInfoCount(
        uint32_t *pctinfo) override
    {
        *pctinfo = 0;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE GetTypeInfo(
        uint32_t iTInfo,
        LCID lcid,
        ITypeInfo **ppTInfo) override
    {
        return E_NOTIMPL;
    }

    HRESULT STDMETHODCALLTYPE GetIDsOfNames(
        REFIID riid,
        LPOLESTR *rgszNames,
        uint32_t cNames,
        LCID lcid,
        DISPID *rgDispId) override
    {
        bool containsUnknown = false;
        DISPID *curr = rgDispId;
        for (uint32_t i = 0; i < cNames; ++i)
        {
            *curr = DISPID_UNKNOWN;
            LPOLESTR name = rgszNames[i];
            if(TP_wcmp_s(name, W("GetEnumerator")) == 0)
            {
                *curr = DISPID_NEWENUM;
            }

            containsUnknown &= (*curr == DISPID_UNKNOWN);
            curr++;
        }

        return (containsUnknown) ? DISP_E_UNKNOWNNAME : S_OK;
    }

    HRESULT STDMETHODCALLTYPE Invoke(
        DISPID dispIdMember,
        REFIID riid,
        LCID lcid,
        uint16_t wFlags,
        DISPPARAMS *pDispParams,
        VARIANT *pVarResult,
        EXCEPINFO *pExcepInfo,
        uint32_t *puArgErr) override
    {
        if (dispIdMember == DISPID_NEWENUM && (wFlags & INVOKE_PROPERTYGET) == INVOKE_PROPERTYGET)
        {
            V_VT(pVarResult) = VT_UNKNOWN;
            V_UNKNOWN(pVarResult) = new IntegerEnumerator(start, count);
            return S_OK;
        }

        return E_NOTIMPL;
    }

    HRESULT STDMETHODCALLTYPE QueryInterface(
        REFIID riid,
        void** ppvObject)
    {
        return DoQueryInterface(riid, ppvObject, static_cast<IDispatch*>(this));
    }

    DEFINE_REF_COUNTING();
};
