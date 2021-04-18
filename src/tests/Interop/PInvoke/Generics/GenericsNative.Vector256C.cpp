// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

#if defined(TARGET_XARCH)
    #include <immintrin.h>
#elif defined(TARGET_ARMARCH)
    // Intentionally empty
#else
    #error Unsupported target architecture
#endif

#if defined(__AVX2__)
    typedef __m256i Vector256C;
#else
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
    } Vector256C;
#endif

static Vector256C Vector256CValue = { };

extern "C" DLL_EXPORT Vector256C STDMETHODCALLTYPE GetVector256C(char16_t e00, char16_t e01, char16_t e02, char16_t e03, char16_t e04, char16_t e05, char16_t e06, char16_t e07, char16_t e08, char16_t e09, char16_t e10, char16_t e11, char16_t e12, char16_t e13, char16_t e14, char16_t e15)
{
    union {
        char16_t value[16];
        Vector256C result;
    };

    value[0] = e00;
    value[1] = e01;
    value[2] = e02;
    value[3] = e03;
    value[4] = e04;
    value[5] = e05;
    value[6] = e06;
    value[7] = e07;
    value[8] = e08;
    value[9] = e09;
    value[10] = e10;
    value[11] = e11;
    value[12] = e12;
    value[13] = e13;
    value[14] = e14;
    value[15] = e15;

    return result;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVector256COut(char16_t e00, char16_t e01, char16_t e02, char16_t e03, char16_t e04, char16_t e05, char16_t e06, char16_t e07, char16_t e08, char16_t e09, char16_t e10, char16_t e11, char16_t e12, char16_t e13, char16_t e14, char16_t e15, Vector256C* pValue)
{
    Vector256C value = GetVector256C(e00, e01, e02, e03, e04, e05, e06, e07, e08, e09, e10, e11, e12, e13, e14, e15);

#if defined(TARGET_XARCH)
    _mm_storeu_si128(((__m128i*)pValue) + 0, *(((__m128i*)&value) + 0));
    _mm_storeu_si128(((__m128i*)pValue) + 1, *(((__m128i*)&value) + 1));
#else
    *pValue = value;
#endif
}

extern "C" DLL_EXPORT const Vector256C* STDMETHODCALLTYPE GetVector256CPtr(char16_t e00, char16_t e01, char16_t e02, char16_t e03, char16_t e04, char16_t e05, char16_t e06, char16_t e07, char16_t e08, char16_t e09, char16_t e10, char16_t e11, char16_t e12, char16_t e13, char16_t e14, char16_t e15)
{
    GetVector256COut(e00, e01, e02, e03, e04, e05, e06, e07, e08, e09, e10, e11, e12, e13, e14, e15, &Vector256CValue);
    return &Vector256CValue;
}

extern "C" DLL_EXPORT Vector256C STDMETHODCALLTYPE AddVector256C(Vector256C lhs, Vector256C rhs)
{
    throw "P/Invoke for Vector256<char> should be unsupported.";
}

extern "C" DLL_EXPORT Vector256C STDMETHODCALLTYPE AddVector256Cs(const Vector256C* pValues, uint32_t count)
{
    throw "P/Invoke for Vector256<char> should be unsupported.";
}
