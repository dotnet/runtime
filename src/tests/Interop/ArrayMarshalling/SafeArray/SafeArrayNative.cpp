// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>
#include <oleauto.h>
#include <algorithm>

#define RETURN_IF_FAILED(x) if(FAILED(hr = (x))) { return hr; }

extern "C" DLL_EXPORT HRESULT STDMETHODCALLTYPE XorBoolArray(SAFEARRAY* d, BOOL* result)
{
    HRESULT hr;
    *result = FALSE;
    VARTYPE elementType;
    RETURN_IF_FAILED(::SafeArrayGetVartype(d, &elementType));

    if (elementType != VT_BOOL)
    {
        return E_INVALIDARG;
    }

    LONG lowerBoundIndex;
    RETURN_IF_FAILED(::SafeArrayGetLBound(d, 1, &lowerBoundIndex));
    LONG upperBoundIndex;
    RETURN_IF_FAILED(::SafeArrayGetUBound(d, 1, &upperBoundIndex));

    VARIANT_BOOL* values;
    RETURN_IF_FAILED(::SafeArrayAccessData(d, (void**)&values));

    for(long i = lowerBoundIndex; i <= upperBoundIndex; i++)
    {
        *result ^= values[i] == VARIANT_TRUE ? TRUE : FALSE;
    }

    RETURN_IF_FAILED(::SafeArrayUnaccessData(d));

    return S_OK;
}

extern "C" DLL_EXPORT HRESULT STDMETHODCALLTYPE MeanDecimalArray(SAFEARRAY* d, DECIMAL* result)
{
    HRESULT hr;
    DECIMAL sum{};
    DECIMAL_SETZERO(sum);

    VARTYPE elementType;
    RETURN_IF_FAILED(::SafeArrayGetVartype(d, &elementType));

    if (elementType != VT_DECIMAL)
    {
        return E_INVALIDARG;
    }

    LONG lowerBoundIndex;
    RETURN_IF_FAILED(::SafeArrayGetLBound(d, 1, &lowerBoundIndex));
    LONG upperBoundIndex;
    RETURN_IF_FAILED(::SafeArrayGetUBound(d, 1, &upperBoundIndex));

    DECIMAL* values;
    RETURN_IF_FAILED(::SafeArrayAccessData(d, (void**)&values));

    for(long i = lowerBoundIndex; i <= upperBoundIndex; i++)
    {
        DECIMAL lhs = sum;
        VarDecAdd(&lhs, &values[i], &sum);
    }

    DECIMAL numElements;
    VarDecFromI4(upperBoundIndex - lowerBoundIndex + 1, &numElements);

    VarDecDiv(&sum, &numElements, result);

    RETURN_IF_FAILED(::SafeArrayUnaccessData(d));

    return S_OK;
}

extern "C" DLL_EXPORT HRESULT STDMETHODCALLTYPE SumCurrencyArray(SAFEARRAY* d, CY* result)
{
    HRESULT hr;
    CY sum{};
    VARTYPE elementType;
    RETURN_IF_FAILED(::SafeArrayGetVartype(d, &elementType));

    if (elementType != VT_CY)
    {
        return E_INVALIDARG;
    }

    LONG lowerBoundIndex;
    RETURN_IF_FAILED(::SafeArrayGetLBound(d, 1, &lowerBoundIndex));
    LONG upperBoundIndex;
    RETURN_IF_FAILED(::SafeArrayGetUBound(d, 1, &upperBoundIndex));

    CY* values;
    RETURN_IF_FAILED(::SafeArrayAccessData(d, (void**)&values));

    for(long i = lowerBoundIndex; i <= upperBoundIndex; i++)
    {
        CY lhs = sum;
        VarCyAdd(lhs, values[i], &sum);
    }

    *result = sum;

    RETURN_IF_FAILED(::SafeArrayUnaccessData(d));

    return S_OK;
}

template <typename StringType>
StringType ReverseInplace(size_t len, StringType s)
{
    std::reverse(s, s + len);
    return s;
}

template<typename StringType>
HRESULT Reverse(StringType str, StringType *res)
{
    StringType tmp = str;
    size_t len = 0;
    while (*tmp++)
        ++len;

    size_t strDataLen = (len + 1) * sizeof(str[0]);
    auto resLocal = (StringType)CoreClrAlloc(strDataLen);
    if (resLocal == nullptr)
        return E_INVALIDARG;

    memcpy(resLocal, str, strDataLen);
    *res = ReverseInplace(len, resLocal);

    return S_OK;
}

HRESULT ReverseBSTR(BSTR str, BSTR *res)
{
    size_t strDataLen = TP_SysStringByteLen(str);
    BSTR resLocal = CoreClrBStrAlloc(reinterpret_cast<LPCSTR>(str), strDataLen);
    if (resLocal == nullptr)
        return E_INVALIDARG;

    size_t len = TP_SysStringLen(str);
    *res = ReverseInplace(len, resLocal);

    return S_OK;
}

extern "C" DLL_EXPORT HRESULT STDMETHODCALLTYPE ReverseStrings(SAFEARRAY* d)
{
    HRESULT hr;
    VARTYPE elementType;
    RETURN_IF_FAILED(::SafeArrayGetVartype(d, &elementType));

    if (elementType != VT_LPSTR && elementType != VT_LPWSTR && elementType != VT_BSTR)
    {
        return E_INVALIDARG;
    }

    LONG lowerBoundIndex;
    RETURN_IF_FAILED(::SafeArrayGetLBound(d, 1, &lowerBoundIndex));
    LONG upperBoundIndex;
    RETURN_IF_FAILED(::SafeArrayGetUBound(d, 1, &upperBoundIndex));

    void** values;
    RETURN_IF_FAILED(::SafeArrayAccessData(d, (void**)&values));

    for(long i = lowerBoundIndex; i <= upperBoundIndex; i++)
    {
        if (elementType == VT_LPSTR)
        {
            LPSTR reversed;
            RETURN_IF_FAILED(Reverse((LPSTR)values[i], &reversed));
            values[i] = reversed;
        }
        else if (elementType == VT_LPWSTR)
        {
            LPWSTR reversed;
            RETURN_IF_FAILED(Reverse((LPWSTR)values[i], &reversed));
            values[i] = reversed;
        }
        else if (elementType == VT_BSTR)
        {
            BSTR reversed;
            RETURN_IF_FAILED(ReverseBSTR((BSTR)values[i], &reversed));
            values[i] = reversed;
        }
    }

    RETURN_IF_FAILED(::SafeArrayUnaccessData(d));

    return S_OK;
}

extern "C" DLL_EXPORT HRESULT STDMETHODCALLTYPE VerifyInterfaceArray(SAFEARRAY* d, VARTYPE expectedType)
{
    HRESULT hr;
    VARTYPE elementType;
    RETURN_IF_FAILED(::SafeArrayGetVartype(d, &elementType));

    if (elementType != expectedType)
    {
        return E_INVALIDARG;
    }

    LONG lowerBoundIndex;
    RETURN_IF_FAILED(::SafeArrayGetLBound(d, 1, &lowerBoundIndex));
    LONG upperBoundIndex;
    RETURN_IF_FAILED(::SafeArrayGetUBound(d, 1, &upperBoundIndex));

    IUnknown** values;
    RETURN_IF_FAILED(::SafeArrayAccessData(d, (void**)&values));

    for(long i = lowerBoundIndex; i <= upperBoundIndex; i++)
    {
        values[i]->AddRef();
        values[i]->Release();
    }

    RETURN_IF_FAILED(::SafeArrayUnaccessData(d));

    return S_OK;
}

extern "C" DLL_EXPORT HRESULT STDMETHODCALLTYPE MeanVariantIntArray(SAFEARRAY* d, int* result)
{
    HRESULT hr;
    *result = 0;
    VARTYPE elementType;
    RETURN_IF_FAILED(::SafeArrayGetVartype(d, &elementType));

    if (elementType != VT_VARIANT)
    {
        return E_INVALIDARG;
    }

    LONG lowerBoundIndex;
    RETURN_IF_FAILED(::SafeArrayGetLBound(d, 1, &lowerBoundIndex));
    LONG upperBoundIndex;
    RETURN_IF_FAILED(::SafeArrayGetUBound(d, 1, &upperBoundIndex));

    VARIANT* values;
    RETURN_IF_FAILED(::SafeArrayAccessData(d, (void**)&values));

    for(long i = lowerBoundIndex; i <= upperBoundIndex; i++)
    {
        if (values[i].vt != VT_I4)
        {
            RETURN_IF_FAILED(::SafeArrayUnaccessData(d));
            return E_INVALIDARG;
        }

        *result += values[i].intVal;
    }

    *result /= upperBoundIndex - lowerBoundIndex + 1;

    RETURN_IF_FAILED(::SafeArrayUnaccessData(d));

    return S_OK;
}

extern "C" DLL_EXPORT HRESULT STDMETHODCALLTYPE DistanceBetweenDates(SAFEARRAY* d, double* result)
{
    HRESULT hr;
    *result = 0;
    VARTYPE elementType;
    RETURN_IF_FAILED(::SafeArrayGetVartype(d, &elementType));

    if (elementType != VT_DATE)
    {
        return E_INVALIDARG;
    }

    LONG lowerBoundIndex;
    RETURN_IF_FAILED(::SafeArrayGetLBound(d, 1, &lowerBoundIndex));
    LONG upperBoundIndex;
    RETURN_IF_FAILED(::SafeArrayGetUBound(d, 1, &upperBoundIndex));

    DATE* values;
    RETURN_IF_FAILED(::SafeArrayAccessData(d, (void**)&values));

    bool haveLastValue = false;
    DATE lastValue = {};

    for(long i = lowerBoundIndex; i <= upperBoundIndex; i++)
    {
        if (haveLastValue)
        {
            *result += values[i] - lastValue;
        }

        lastValue = values[i];
        haveLastValue = true;
    }

    RETURN_IF_FAILED(::SafeArrayUnaccessData(d));

    return S_OK;
}

struct StructWithSafeArray
{
    SAFEARRAY* array;
};

extern "C" DLL_EXPORT HRESULT STDMETHODCALLTYPE XorBoolArrayInStruct(StructWithSafeArray str, BOOL* result)
{
    return XorBoolArray(str.array, result);
}
