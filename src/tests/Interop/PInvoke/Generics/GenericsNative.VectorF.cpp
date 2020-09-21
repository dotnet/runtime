// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

typedef struct {
    float e00;
    float e01;
    float e02;
    float e03;
} VectorF128;

typedef struct {
    float e00;
    float e01;
    float e02;
    float e03;
    float e04;
    float e05;
    float e06;
    float e07;
} VectorF256;

static VectorF128 VectorF128Value = { };
static VectorF256 VectorF256Value = { };

extern "C" DLL_EXPORT VectorF128 STDMETHODCALLTYPE GetVectorF128(float e00, float e01, float e02, float e03)
{
    float value[4] = { e00, e01, e02, e03 };
    return *reinterpret_cast<VectorF128*>(value);
}

extern "C" DLL_EXPORT VectorF256 STDMETHODCALLTYPE GetVectorF256(float e00, float e01, float e02, float e03, float e04, float e05, float e06, float e07)
{
    float value[8] = { e00, e01, e02, e03, e04, e05, e06, e07 };
    return *reinterpret_cast<VectorF256*>(value);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVectorF128Out(float e00, float e01, float e02, float e03, VectorF128* pValue)
{
    *pValue = GetVectorF128(e00, e01, e02, e03);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVectorF256Out(float e00, float e01, float e02, float e03, float e04, float e05, float e06, float e07, VectorF256* pValue)
{
    *pValue = GetVectorF256(e00, e01, e02, e03, e04, e05, e06, e07);
}

extern "C" DLL_EXPORT const VectorF128* STDMETHODCALLTYPE GetVectorF128Ptr(float e00, float e01, float e02, float e03)
{
    GetVectorF128Out(e00, e01, e02, e03, &VectorF128Value);
    return &VectorF128Value;
}

extern "C" DLL_EXPORT const VectorF256* STDMETHODCALLTYPE GetVectorF256Ptr(float e00, float e01, float e02, float e03, float e04, float e05, float e06, float e07)
{
    GetVectorF256Out(e00, e01, e02, e03, e04, e05, e06, e07, &VectorF256Value);
    return &VectorF256Value;
}

extern "C" DLL_EXPORT VectorF128 STDMETHODCALLTYPE AddVectorF128(VectorF128 lhs, VectorF128 rhs)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}

extern "C" DLL_EXPORT VectorF256 STDMETHODCALLTYPE AddVectorF256(VectorF256 lhs, VectorF256 rhs)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}

extern "C" DLL_EXPORT VectorF128 STDMETHODCALLTYPE AddVectorF128s(const VectorF128* pValues, float count)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}

extern "C" DLL_EXPORT VectorF256 STDMETHODCALLTYPE AddVectorF256s(const VectorF256* pValues, float count)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}
