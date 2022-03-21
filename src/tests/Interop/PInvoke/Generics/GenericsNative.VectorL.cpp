// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

typedef struct {
    int64_t e00;
    int64_t e01;
} VectorL128;

typedef struct {
    int64_t e00;
    int64_t e01;
    int64_t e02;
    int64_t e03;
} VectorL256;

static VectorL128 VectorL128Value = { };
static VectorL256 VectorL256Value = { };

extern "C" DLL_EXPORT VectorL128 STDMETHODCALLTYPE GetVectorL128(int64_t e00, int64_t e01)
{
    int64_t value[2] = { e00, e01 };
    return *reinterpret_cast<VectorL128*>(value);
}

extern "C" DLL_EXPORT VectorL256 STDMETHODCALLTYPE ENABLE_AVX GetVectorL256(int64_t e00, int64_t e01, int64_t e02, int64_t e03)
{
    int64_t value[4] = { e00, e01, e02, e03 };
    return *reinterpret_cast<VectorL256*>(value);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVectorL128Out(int64_t e00, int64_t e01, VectorL128* pValue)
{
    *pValue = GetVectorL128(e00, e01);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE ENABLE_AVX GetVectorL256Out(int64_t e00, int64_t e01, int64_t e02, int64_t e03, VectorL256* pValue)
{
    *pValue = GetVectorL256(e00, e01, e02, e03);
}

extern "C" DLL_EXPORT const VectorL128* STDMETHODCALLTYPE GetVectorL128Ptr(int64_t e00, int64_t e01)
{
    GetVectorL128Out(e00, e01, &VectorL128Value);
    return &VectorL128Value;
}

extern "C" DLL_EXPORT const VectorL256* STDMETHODCALLTYPE ENABLE_AVX GetVectorL256Ptr(int64_t e00, int64_t e01, int64_t e02, int64_t e03)
{
    GetVectorL256Out(e00, e01, e02, e03, &VectorL256Value);
    return &VectorL256Value;
}

extern "C" DLL_EXPORT VectorL128 STDMETHODCALLTYPE AddVectorL128(VectorL128 lhs, VectorL128 rhs)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}

extern "C" DLL_EXPORT VectorL256 STDMETHODCALLTYPE ENABLE_AVX AddVectorL256(VectorL256 lhs, VectorL256 rhs)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}

extern "C" DLL_EXPORT VectorL128 STDMETHODCALLTYPE AddVectorL128s(const VectorL128* pValues, int64_t count)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}

extern "C" DLL_EXPORT VectorL256 STDMETHODCALLTYPE ENABLE_AVX AddVectorL256s(const VectorL256* pValues, int64_t count)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}
