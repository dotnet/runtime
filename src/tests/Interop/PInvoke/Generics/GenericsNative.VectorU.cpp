// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

typedef struct {
    uint32_t e00;
    uint32_t e01;
    uint32_t e02;
    uint32_t e03;
} VectorU128;

typedef struct {
    uint32_t e00;
    uint32_t e01;
    uint32_t e02;
    uint32_t e03;
    uint32_t e04;
    uint32_t e05;
    uint32_t e06;
    uint32_t e07;
} VectorU256;

static VectorU128 VectorU128Value = { };
static VectorU256 VectorU256Value = { };

extern "C" DLL_EXPORT VectorU128 STDMETHODCALLTYPE GetVectorU128(uint32_t e00, uint32_t e01, uint32_t e02, uint32_t e03)
{
    uint32_t value[4] = { e00, e01, e02, e03 };
    return *reinterpret_cast<VectorU128*>(value);
}

extern "C" DLL_EXPORT VectorU256 STDMETHODCALLTYPE GetVectorU256(uint32_t e00, uint32_t e01, uint32_t e02, uint32_t e03, uint32_t e04, uint32_t e05, uint32_t e06, uint32_t e07)
{
    uint32_t value[8] = { e00, e01, e02, e03, e04, e05, e06, e07 };
    return *reinterpret_cast<VectorU256*>(value);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVectorU128Out(uint32_t e00, uint32_t e01, uint32_t e02, uint32_t e03, VectorU128* pValue)
{
    *pValue = GetVectorU128(e00, e01, e02, e03);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVectorU256Out(uint32_t e00, uint32_t e01, uint32_t e02, uint32_t e03, uint32_t e04, uint32_t e05, uint32_t e06, uint32_t e07, VectorU256* pValue)
{
    *pValue = GetVectorU256(e00, e01, e02, e03, e04, e05, e06, e07);
}

extern "C" DLL_EXPORT const VectorU128* STDMETHODCALLTYPE GetVectorU128Ptr(uint32_t e00, uint32_t e01, uint32_t e02, uint32_t e03)
{
    GetVectorU128Out(e00, e01, e02, e03, &VectorU128Value);
    return &VectorU128Value;
}

extern "C" DLL_EXPORT const VectorU256* STDMETHODCALLTYPE GetVectorU256Ptr(uint32_t e00, uint32_t e01, uint32_t e02, uint32_t e03, uint32_t e04, uint32_t e05, uint32_t e06, uint32_t e07)
{
    GetVectorU256Out(e00, e01, e02, e03, e04, e05, e06, e07, &VectorU256Value);
    return &VectorU256Value;
}

extern "C" DLL_EXPORT VectorU128 STDMETHODCALLTYPE AddVectorU128(VectorU128 lhs, VectorU128 rhs)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}

extern "C" DLL_EXPORT VectorU256 STDMETHODCALLTYPE AddVectorU256(VectorU256 lhs, VectorU256 rhs)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}

extern "C" DLL_EXPORT VectorU128 STDMETHODCALLTYPE AddVectorU128s(const VectorU128* pValues, uint32_t count)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}

extern "C" DLL_EXPORT VectorU256 STDMETHODCALLTYPE AddVectorU256s(const VectorU256* pValues, uint32_t count)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}
