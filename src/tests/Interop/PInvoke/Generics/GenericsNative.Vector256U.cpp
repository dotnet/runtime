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
    typedef __m256i Vector256U;
#else
    typedef struct {
        uint32_t e00;
        uint32_t e01;
        uint32_t e02;
        uint32_t e03;
        uint32_t e04;
        uint32_t e05;
        uint32_t e06;
        uint32_t e07;
    } Vector256U;
#endif

static Vector256U Vector256UValue = { };

extern "C" DLL_EXPORT Vector256U STDMETHODCALLTYPE GetVector256U(uint32_t e00, uint32_t e01, uint32_t e02, uint32_t e03, uint32_t e04, uint32_t e05, uint32_t e06, uint32_t e07)
{
    union {
        uint32_t value[8];
        Vector256U result;
    };

    value[0] = e00;
    value[1] = e01;
    value[2] = e02;
    value[3] = e03;
    value[4] = e04;
    value[5] = e05;
    value[6] = e06;
    value[7] = e07;

    return result;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVector256UOut(uint32_t e00, uint32_t e01, uint32_t e02, uint32_t e03, uint32_t e04, uint32_t e05, uint32_t e06, uint32_t e07, Vector256U* pValue)
{
    Vector256U value = GetVector256U(e00, e01, e02, e03, e04, e05, e06, e07);

#if defined(TARGET_XARCH)
    _mm_storeu_si128(((__m128i*)pValue) + 0, *(((__m128i*)&value) + 0));
    _mm_storeu_si128(((__m128i*)pValue) + 1, *(((__m128i*)&value) + 1));
#else
    *pValue = value;
#endif
}

extern "C" DLL_EXPORT const Vector256U* STDMETHODCALLTYPE GetVector256UPtr(uint32_t e00, uint32_t e01, uint32_t e02, uint32_t e03, uint32_t e04, uint32_t e05, uint32_t e06, uint32_t e07)
{
    GetVector256UOut(e00, e01, e02, e03, e04, e05, e06, e07, &Vector256UValue);
    return &Vector256UValue;
}

extern "C" DLL_EXPORT Vector256U STDMETHODCALLTYPE AddVector256U(Vector256U lhs, Vector256U rhs)
{
    throw "P/Invoke for Vector256<uint> should be unsupported.";
}

extern "C" DLL_EXPORT Vector256U STDMETHODCALLTYPE AddVector256Us(const Vector256U* pValues, uint32_t count)
{
    throw "P/Invoke for Vector256<uint> should be unsupported.";
}
