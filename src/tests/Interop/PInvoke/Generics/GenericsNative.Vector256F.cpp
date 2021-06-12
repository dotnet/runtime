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
    typedef __m256 Vector256F;
#else
    typedef struct {
        float e00;
        float e01;
        float e02;
        float e03;
        float e04;
        float e05;
        float e06;
        float e07;
    } Vector256F;
#endif

static Vector256F Vector256FValue = { };

extern "C" DLL_EXPORT Vector256F STDMETHODCALLTYPE GetVector256F(float e00, float e01, float e02, float e03, float e04, float e05, float e06, float e07)
{
    union {
        float value[8];
        Vector256F result;
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

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVector256FOut(float e00, float e01, float e02, float e03, float e04, float e05, float e06, float e07, Vector256F* pValue)
{
    Vector256F value = GetVector256F(e00, e01, e02, e03, e04, e05, e06, e07);

#if defined(TARGET_XARCH)
    _mm_storeu_ps((float*)(((__m128*)pValue) + 0), *(((__m128*)&value) + 0));
    _mm_storeu_ps((float*)(((__m128*)pValue) + 1), *(((__m128*)&value) + 1));
#else
    *pValue = value;
#endif
}

extern "C" DLL_EXPORT const Vector256F* STDMETHODCALLTYPE GetVector256FPtr(float e00, float e01, float e02, float e03, float e04, float e05, float e06, float e07)
{
    GetVector256FOut(e00, e01, e02, e03, e04, e05, e06, e07, &Vector256FValue);
    return &Vector256FValue;
}

extern "C" DLL_EXPORT Vector256F STDMETHODCALLTYPE AddVector256F(Vector256F lhs, Vector256F rhs)
{
    throw "P/Invoke for Vector256<float> should be unsupported.";
}

extern "C" DLL_EXPORT Vector256F STDMETHODCALLTYPE AddVector256Fs(const Vector256F* pValues, uint32_t count)
{
    throw "P/Invoke for Vector256<float> should be unsupported.";
}
