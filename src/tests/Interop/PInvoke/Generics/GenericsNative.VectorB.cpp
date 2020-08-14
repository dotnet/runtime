// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

typedef struct {
    bool e00;
    bool e01;
    bool e02;
    bool e03;
    bool e04;
    bool e05;
    bool e06;
    bool e07;
    bool e08;
    bool e09;
    bool e10;
    bool e11;
    bool e12;
    bool e13;
    bool e14;
    bool e15;
} VectorB128;

typedef struct {
    bool e00;
    bool e01;
    bool e02;
    bool e03;
    bool e04;
    bool e05;
    bool e06;
    bool e07;
    bool e08;
    bool e09;
    bool e10;
    bool e11;
    bool e12;
    bool e13;
    bool e14;
    bool e15;
    bool e16;
    bool e17;
    bool e18;
    bool e19;
    bool e20;
    bool e21;
    bool e22;
    bool e23;
    bool e24;
    bool e25;
    bool e26;
    bool e27;
    bool e28;
    bool e29;
    bool e30;
    bool e31;
} VectorB256;

static VectorB128 VectorB128Value = { };
static VectorB256 VectorB256Value = { };

extern "C" DLL_EXPORT VectorB128 STDMETHODCALLTYPE GetVectorB128(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07, bool e08, bool e09, bool e10, bool e11, bool e12, bool e13, bool e14, bool e15)
{
    bool value[16] = { e00, e01, e02, e03, e04, e05, e06, e07, e08, e09, e10, e11, e12, e13, e14, e15 };
    return *reinterpret_cast<VectorB128*>(value);
}

extern "C" DLL_EXPORT VectorB256 STDMETHODCALLTYPE GetVectorB256(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07, bool e08, bool e09, bool e10, bool e11, bool e12, bool e13, bool e14, bool e15, bool e16, bool e17, bool e18, bool e19, bool e20, bool e21, bool e22, bool e23, bool e24, bool e25, bool e26, bool e27, bool e28, bool e29, bool e30, bool e31)
{
    bool value[32] = { e00, e01, e02, e03, e04, e05, e06, e07, e08, e09, e10, e11, e12, e13, e14, e15, e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31 };
    return *reinterpret_cast<VectorB256*>(value);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVectorB128Out(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07, bool e08, bool e09, bool e10, bool e11, bool e12, bool e13, bool e14, bool e15, VectorB128* pValue)
{
    *pValue = GetVectorB128(e00, e01, e02, e03, e04, e05, e06, e07, e08, e09, e10, e11, e12, e13, e14, e15);
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVectorB256Out(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07, bool e08, bool e09, bool e10, bool e11, bool e12, bool e13, bool e14, bool e15, bool e16, bool e17, bool e18, bool e19, bool e20, bool e21, bool e22, bool e23, bool e24, bool e25, bool e26, bool e27, bool e28, bool e29, bool e30, bool e31, VectorB256* pValue)
{
    *pValue = GetVectorB256(e00, e01, e02, e03, e04, e05, e06, e07, e08, e09, e10, e11, e12, e13, e14, e15, e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31);
}

extern "C" DLL_EXPORT const VectorB128* STDMETHODCALLTYPE GetVectorB128Ptr(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07, bool e08, bool e09, bool e10, bool e11, bool e12, bool e13, bool e14, bool e15)
{
    GetVectorB128Out(e00, e01, e02, e03, e04, e05, e06, e07, e08, e09, e10, e11, e12, e13, e14, e15, &VectorB128Value);
    return &VectorB128Value;
}

extern "C" DLL_EXPORT const VectorB256* STDMETHODCALLTYPE GetVectorB256Ptr(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07, bool e08, bool e09, bool e10, bool e11, bool e12, bool e13, bool e14, bool e15, bool e16, bool e17, bool e18, bool e19, bool e20, bool e21, bool e22, bool e23, bool e24, bool e25, bool e26, bool e27, bool e28, bool e29, bool e30, bool e31)
{
    GetVectorB256Out(e00, e01, e02, e03, e04, e05, e06, e07, e08, e09, e10, e11, e12, e13, e14, e15, e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31, &VectorB256Value);
    return &VectorB256Value;
}

extern "C" DLL_EXPORT VectorB128 STDMETHODCALLTYPE AddVectorB128(VectorB128 lhs, VectorB128 rhs)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}

extern "C" DLL_EXPORT VectorB256 STDMETHODCALLTYPE AddVectorB256(VectorB256 lhs, VectorB256 rhs)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}

extern "C" DLL_EXPORT VectorB128 STDMETHODCALLTYPE AddVectorB128s(const VectorB128* pValues, uint32_t count)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}

extern "C" DLL_EXPORT VectorB256 STDMETHODCALLTYPE AddVectorB256s(const VectorB256* pValues, uint32_t count)
{
    throw "P/Invoke for Vector<char> should be unsupported.";
}
