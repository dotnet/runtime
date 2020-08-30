// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

typedef struct {
    char16_t e00;
    char16_t e01;
    char16_t e02;
    char16_t e03;
    char16_t e04;
    char16_t e05;
    char16_t e06;
    char16_t e07;
} VectorC128;

typedef struct {
    char16_t e00;
    char16_t e01;
    char16_t e02;
    char16_t e03;
    char16_t e04;
    char16_t e05;
    char16_t e06;
    char16_t e07;
    char16_t e08;
    char16_t e09;
    char16_t e10;
    char16_t e11;
    char16_t e12;
    char16_t e13;
    char16_t e14;
    char16_t e15;
} VectorC256;

static VectorC128 VectorC128Value = { };
static VectorC256 VectorC256Value = { };

extern "C" DLL_EXPORT VectorC128 STDMETHODCALLTYPE GetVectorC128(char16_t e00, char16_t e01, char16_t e02, char16_t e03, char16_t e04, char16_t e05, char16_t e06, char16_t e07)
{
    char16_t value[8] = { e00, e01, e02, e03, e04, e05, e06, e07 };
    return *reinterpret_cast<VectorC128*>(value);
}

extern "C" DLL_EXPORT VectorC256 STDMETHODCALLTYPE GetVectorC256(char16_t e00, char16_t e01, char16_t e02, char16_t e03, char16_t e04, char16_t e05, char16_t e06, char16_t e07, char16_t e08, char16_t e09, char16_t e10, char16_t e11, char16_t e12, char16_t e13, char16_t e14, char16_t e15)
{
    char16_t value[16] = { e00, e01, e02, e03, e04, e05, e06, e07, e08, e09, e10, e11, e12, e13, e14, e15 };
    return *reinterpret_cast<VectorC256*>(value);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVectorC128Out(char16_t e00, char16_t e01, char16_t e02, char16_t e03, char16_t e04, char16_t e05, char16_t e06, char16_t e07, VectorC128* pValue)
{
    *pValue = GetVectorC128(e00, e01, e02, e03, e04, e05, e06, e07);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVectorC256Out(char16_t e00, char16_t e01, char16_t e02, char16_t e03, char16_t e04, char16_t e05, char16_t e06, char16_t e07, char16_t e08, char16_t e09, char16_t e10, char16_t e11, char16_t e12, char16_t e13, char16_t e14, char16_t e15, VectorC256* pValue)
{
    *pValue = GetVectorC256(e00, e01, e02, e03, e04, e05, e06, e07, e08, e09, e10, e11, e12, e13, e14, e15);
}

extern "C" DLL_EXPORT const VectorC128* STDMETHODCALLTYPE GetVectorC128Ptr(char16_t e00, char16_t e01, char16_t e02, char16_t e03, char16_t e04, char16_t e05, char16_t e06, char16_t e07)
{
    GetVectorC128Out(e00, e01, e02, e03, e04, e05, e06, e07, &VectorC128Value);
    return &VectorC128Value;
}

extern "C" DLL_EXPORT const VectorC256* STDMETHODCALLTYPE GetVectorC256Ptr(char16_t e00, char16_t e01, char16_t e02, char16_t e03, char16_t e04, char16_t e05, char16_t e06, char16_t e07, char16_t e08, char16_t e09, char16_t e10, char16_t e11, char16_t e12, char16_t e13, char16_t e14, char16_t e15)
{
    GetVectorC256Out(e00, e01, e02, e03, e04, e05, e06, e07, e08, e09, e10, e11, e12, e13, e14, e15, &VectorC256Value);
    return &VectorC256Value;
}

extern "C" DLL_EXPORT VectorC128 STDMETHODCALLTYPE AddVectorC128(VectorC128 lhs, VectorC128 rhs)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}

extern "C" DLL_EXPORT VectorC256 STDMETHODCALLTYPE AddVectorC256(VectorC256 lhs, VectorC256 rhs)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}

extern "C" DLL_EXPORT VectorC128 STDMETHODCALLTYPE AddVectorC128s(const VectorC128* pValues, uint32_t count)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}

extern "C" DLL_EXPORT VectorC256 STDMETHODCALLTYPE AddVectorC256s(const VectorC256* pValues, uint32_t count)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}
