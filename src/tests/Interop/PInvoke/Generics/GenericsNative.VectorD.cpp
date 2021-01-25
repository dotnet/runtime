// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

typedef struct {
    double e00;
    double e01;
} VectorD128;

typedef struct {
    double e00;
    double e01;
    double e02;
    double e03;
} VectorD256;

static VectorD128 VectorD128Value = { };
static VectorD256 VectorD256Value = { };

extern "C" DLL_EXPORT VectorD128 STDMETHODCALLTYPE GetVectorD128(double e00, double e01)
{
    double value[2] = { e00, e01 };
    return *reinterpret_cast<VectorD128*>(value);
}

extern "C" DLL_EXPORT VectorD256 STDMETHODCALLTYPE GetVectorD256(double e00, double e01, double e02, double e03)
{
    double value[4] = { e00, e01, e02, e03 };
    return *reinterpret_cast<VectorD256*>(value);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVectorD128Out(double e00, double e01, VectorD128* pValue)
{
    *pValue = GetVectorD128(e00, e01);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVectorD256Out(double e00, double e01, double e02, double e03, VectorD256* pValue)
{
    *pValue = GetVectorD256(e00, e01, e02, e03);
}

extern "C" DLL_EXPORT const VectorD128* STDMETHODCALLTYPE GetVectorD128Ptr(double e00, double e01)
{
    GetVectorD128Out(e00, e01, &VectorD128Value);
    return &VectorD128Value;
}

extern "C" DLL_EXPORT const VectorD256* STDMETHODCALLTYPE GetVectorD256Ptr(double e00, double e01, double e02, double e03)
{
    GetVectorD256Out(e00, e01, e02, e03, &VectorD256Value);
    return &VectorD256Value;
}

extern "C" DLL_EXPORT VectorD128 STDMETHODCALLTYPE AddVectorD128(VectorD128 lhs, VectorD128 rhs)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}

extern "C" DLL_EXPORT VectorD256 STDMETHODCALLTYPE AddVectorD256(VectorD256 lhs, VectorD256 rhs)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}

extern "C" DLL_EXPORT VectorD128 STDMETHODCALLTYPE AddVectorD128s(const VectorD128* pValues, double count)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}

extern "C" DLL_EXPORT VectorD256 STDMETHODCALLTYPE AddVectorD256s(const VectorD256* pValues, double count)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}
