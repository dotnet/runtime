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

// Creates a 2D SAFEARRAY of VT_I4 with dimensions [rows x cols].
// Data is filled by logical indices with value = row * cols + col.
extern "C" DLL_EXPORT HRESULT STDMETHODCALLTYPE Create2DIntSafeArray(int rows, int cols, SAFEARRAY** ppResult)
{
    if (rows < 0 || cols < 0)
        return E_INVALIDARG;

    SAFEARRAYBOUND bounds[2];
    bounds[0].lLbound = 0;
    bounds[0].cElements = (ULONG)rows;
    bounds[1].lLbound = 0;
    bounds[1].cElements = (ULONG)cols;

    SAFEARRAY* psa = ::SafeArrayCreate(VT_I4, 2, bounds);
    if (!psa)
        return E_OUTOFMEMORY;

    for (LONG r = 0; r < rows; r++)
    {
        for (LONG c = 0; c < cols; c++)
        {
            LONG indices[2] = { r, c };
            int value = r * cols + c;
            HRESULT hr = ::SafeArrayPutElement(psa, indices, &value);
            if (FAILED(hr))
            {
                ::SafeArrayDestroy(psa);
                return hr;
            }
        }
    }

    *ppResult = psa;
    return S_OK;
}

// Verifies a 2D SAFEARRAY of VT_I4 with dimensions [rows x cols].
// Expected value at [r,c] = r * cols + c.
extern "C" DLL_EXPORT HRESULT STDMETHODCALLTYPE Verify2DIntSafeArray(SAFEARRAY* psa, int rows, int cols)
{
    HRESULT hr;
    VARTYPE vt;
    RETURN_IF_FAILED(::SafeArrayGetVartype(psa, &vt));
    if (vt != VT_I4)
        return E_INVALIDARG;

    if (psa->cDims != 2)
        return E_INVALIDARG;

    for (LONG r = 0; r < rows; r++)
    {
        for (LONG c = 0; c < cols; c++)
        {
            LONG indices[2] = { r, c };
            int value = 0;
            RETURN_IF_FAILED(::SafeArrayGetElement(psa, indices, &value));
            int expected = r * cols + c;
            if (value != expected)
                return E_FAIL;
        }
    }

    return S_OK;
}

// Creates a 2D SAFEARRAY of VT_BOOL with dimensions [rows x cols].
// Value at [r,c] = ((r + c) % 2 == 0) ? VARIANT_TRUE : VARIANT_FALSE.
extern "C" DLL_EXPORT HRESULT STDMETHODCALLTYPE Create2DBoolSafeArray(int rows, int cols, SAFEARRAY** ppResult)
{
    if (rows < 0 || cols < 0)
        return E_INVALIDARG;

    SAFEARRAYBOUND bounds[2];
    bounds[0].lLbound = 0;
    bounds[0].cElements = (ULONG)rows;
    bounds[1].lLbound = 0;
    bounds[1].cElements = (ULONG)cols;

    SAFEARRAY* psa = ::SafeArrayCreate(VT_BOOL, 2, bounds);
    if (!psa)
        return E_OUTOFMEMORY;

    for (LONG r = 0; r < rows; r++)
    {
        for (LONG c = 0; c < cols; c++)
        {
            LONG indices[2] = { r, c };
            VARIANT_BOOL value = ((r + c) % 2 == 0) ? VARIANT_TRUE : VARIANT_FALSE;
            HRESULT hr = ::SafeArrayPutElement(psa, indices, &value);
            if (FAILED(hr))
            {
                ::SafeArrayDestroy(psa);
                return hr;
            }
        }
    }

    *ppResult = psa;
    return S_OK;
}

// Verifies a 2D SAFEARRAY of VT_BOOL with dimensions [rows x cols].
extern "C" DLL_EXPORT HRESULT STDMETHODCALLTYPE Verify2DBoolSafeArray(SAFEARRAY* psa, int rows, int cols)
{
    HRESULT hr;
    VARTYPE vt;
    RETURN_IF_FAILED(::SafeArrayGetVartype(psa, &vt));
    if (vt != VT_BOOL)
        return E_INVALIDARG;

    if (psa->cDims != 2)
        return E_INVALIDARG;

    for (LONG r = 0; r < rows; r++)
    {
        for (LONG c = 0; c < cols; c++)
        {
            LONG indices[2] = { r, c };
            VARIANT_BOOL value = VARIANT_FALSE;
            RETURN_IF_FAILED(::SafeArrayGetElement(psa, indices, &value));
            VARIANT_BOOL expected = ((r + c) % 2 == 0) ? VARIANT_TRUE : VARIANT_FALSE;
            if (value != expected)
                return E_FAIL;
        }
    }

    return S_OK;
}

// Creates a 2D SAFEARRAY of VT_BSTR with dimensions [rows x cols].
// Value at [r,c] = "r,c".
extern "C" DLL_EXPORT HRESULT STDMETHODCALLTYPE Create2DStringSafeArray(int rows, int cols, SAFEARRAY** ppResult)
{
    if (rows < 0 || cols < 0)
        return E_INVALIDARG;

    SAFEARRAYBOUND bounds[2];
    bounds[0].lLbound = 0;
    bounds[0].cElements = (ULONG)rows;
    bounds[1].lLbound = 0;
    bounds[1].cElements = (ULONG)cols;

    SAFEARRAY* psa = ::SafeArrayCreate(VT_BSTR, 2, bounds);
    if (!psa)
        return E_OUTOFMEMORY;

    for (LONG r = 0; r < rows; r++)
    {
        for (LONG c = 0; c < cols; c++)
        {
            LONG indices[2] = { r, c };
            WCHAR buf[32];
            swprintf_s(buf, 32, L"%d,%d", (int)r, (int)c);
            BSTR bstr = TP_SysAllocString(buf);
            if (!bstr)
            {
                ::SafeArrayDestroy(psa);
                return E_OUTOFMEMORY;
            }
            HRESULT hr = ::SafeArrayPutElement(psa, indices, bstr);
            ::SysFreeString(bstr);
            if (FAILED(hr))
            {
                ::SafeArrayDestroy(psa);
                return hr;
            }
        }
    }

    *ppResult = psa;
    return S_OK;
}

// Verifies a 2D SAFEARRAY of VT_BSTR with dimensions [rows x cols].
extern "C" DLL_EXPORT HRESULT STDMETHODCALLTYPE Verify2DStringSafeArray(SAFEARRAY* psa, int rows, int cols)
{
    HRESULT hr;
    VARTYPE vt;
    RETURN_IF_FAILED(::SafeArrayGetVartype(psa, &vt));
    if (vt != VT_BSTR)
        return E_INVALIDARG;

    if (psa->cDims != 2)
        return E_INVALIDARG;

    for (LONG r = 0; r < rows; r++)
    {
        for (LONG c = 0; c < cols; c++)
        {
            LONG indices[2] = { r, c };
            BSTR value = nullptr;
            RETURN_IF_FAILED(::SafeArrayGetElement(psa, indices, &value));

            WCHAR expected[32];
            swprintf_s(expected, 32, L"%d,%d", (int)r, (int)c);

            bool match = (value != nullptr && wcscmp(value, expected) == 0);
            ::SysFreeString(value);
            if (!match)
                return E_FAIL;
        }
    }

    return S_OK;
}
