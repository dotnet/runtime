// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "IEnumeratorNative.h"
#include <xplatform.h>

extern "C" DLL_EXPORT HRESULT STDMETHODCALLTYPE GetIntegerEnumerator(int start, int count, IEnumVARIANT** ppEnum)
{
    if (count < 0)
    {
        return E_INVALIDARG;
    }

    *ppEnum = new IntegerEnumerator(start, count);

    return S_OK;
}

extern "C" DLL_EXPORT HRESULT STDMETHODCALLTYPE VerifyIntegerEnumerator(IEnumVARIANT* pEnum, int start, int count)
{
    if (count < 0)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;

    VARIANT element;
    VariantInit(&element);
    ULONG numFetched;

    for (int i = start; i < start + count; ++i)
    {
        VariantClear(&element);
        hr = pEnum->Next(1, &element, &numFetched);
        if(FAILED(hr) || numFetched != 1)
        {
            return hr;
        }

        if (V_I4(&element) != i)
        {
            return E_UNEXPECTED;
        }
    }

    hr = pEnum->Next(1, &element, &numFetched);
    if (hr != S_FALSE || numFetched != 0)
    {
        return E_UNEXPECTED;
    }

    return S_OK;
}

extern "C" DLL_EXPORT HRESULT STDMETHODCALLTYPE GetIntegerEnumeration(int start, int count, IDispatch** ppDisp)
{
    if (count < 0)
    {
        return E_INVALIDARG;
    }

    *ppDisp = new IntegerEnumerable(start, count);

    return S_OK;
}

extern "C" DLL_EXPORT HRESULT STDMETHODCALLTYPE VerifyIntegerEnumeration(IDispatch* pDisp, int start, int count)
{
    DISPPARAMS params{};
    VARIANT result;
    VariantInit(&result);
    HRESULT hr = pDisp->Invoke(
        DISPID_NEWENUM,
        IID_NULL,
        LOCALE_USER_DEFAULT,
        DISPATCH_METHOD | DISPATCH_PROPERTYGET,
        &params,
        &result,
        NULL,
        NULL
    );

    if (FAILED(hr))
    {
        return hr;
    }

    if(!((V_VT(&result) == VT_UNKNOWN) || (V_VT(&result) == VT_DISPATCH)))
    {
        return E_UNEXPECTED;
    }

    IEnumVARIANT* pEnum;

    hr = V_UNKNOWN(&result)->QueryInterface<IEnumVARIANT>(&pEnum);
    VariantClear(&result);

    if (FAILED(hr))
    {
        return hr;
    }

    hr = VerifyIntegerEnumerator(pEnum, start, count);

    pEnum->Release();

    return hr;
}

extern "C" DLL_EXPORT HRESULT STDMETHODCALLTYPE PassThroughEnumerator(IEnumVARIANT* in, IEnumVARIANT** out)
{
    return in->QueryInterface(out);
}
