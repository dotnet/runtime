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
    typedef __m256 Vector256D;
#else
    typedef struct {
        double e00;
        double e01;
        double e02;
        double e03;
    } Vector256D;
#endif

static Vector256D Vector256DValue = { };

extern "C" DLL_EXPORT Vector256D STDMETHODCALLTYPE GetVector256D(double e00, double e01, double e02, double e03)
{
    union {
        double value[4];
        Vector256D result;
    };

    value[0] = e00;
    value[1] = e01;
    value[2] = e02;
    value[3] = e03;

    return result;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVector256DOut(double e00, double e01, double e02, double e03, Vector256D* pValue)
{
    Vector256D value = GetVector256D(e00, e01, e02, e03);

#if defined(TARGET_XARCH)
    _mm_storeu_pd((double*)(((__m128d*)pValue) + 0), *(((__m128d*)&value) + 0));
    _mm_storeu_pd((double*)(((__m128d*)pValue) + 1), *(((__m128d*)&value) + 1));
#else
    *pValue = value;
#endif
}

extern "C" DLL_EXPORT const Vector256D* STDMETHODCALLTYPE GetVector256DPtr(double e00, double e01, double e02, double e03)
{
    GetVector256DOut(e00, e01, e02, e03, &Vector256DValue);
    return &Vector256DValue;
}

extern "C" DLL_EXPORT Vector256D STDMETHODCALLTYPE AddVector256D(Vector256D lhs, Vector256D rhs)
{
    throw "P/Invoke for Vector256<double> should be unsupported.";
}

extern "C" DLL_EXPORT Vector256D STDMETHODCALLTYPE AddVector256Ds(const Vector256D* pValues, uint32_t count)
{
    throw "P/Invoke for Vector256<double> should be unsupported.";
}
