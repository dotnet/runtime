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
    typedef __m256i Vector256L;
#else
    typedef struct {
        int64_t e00;
        int64_t e01;
        int64_t e02;
        int64_t e03;
    } Vector256L;
#endif

static Vector256L Vector256LValue = { };

extern "C" DLL_EXPORT Vector256L STDMETHODCALLTYPE GetVector256L(int64_t e00, int64_t e01, int64_t e02, int64_t e03)
{
    union {
        int64_t value[4];
        Vector256L result;
    };

    value[0] = e00;
    value[1] = e01;
    value[2] = e02;
    value[3] = e03;

    return result;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVector256LOut(int64_t e00, int64_t e01, int64_t e02, int64_t e03, Vector256L* pValue)
{
    Vector256L value = GetVector256L(e00, e01, e02, e03);

#if defined(TARGET_XARCH)
    _mm_storeu_si128(((__m128i*)pValue) + 0, *(((__m128i*)&value) + 0));
    _mm_storeu_si128(((__m128i*)pValue) + 1, *(((__m128i*)&value) + 1));
#else
    *pValue = value;
#endif
}

extern "C" DLL_EXPORT const Vector256L* STDMETHODCALLTYPE GetVector256LPtr(int64_t e00, int64_t e01, int64_t e02, int64_t e03)
{
    GetVector256LOut(e00, e01, e02, e03, &Vector256LValue);
    return &Vector256LValue;
}

extern "C" DLL_EXPORT Vector256L STDMETHODCALLTYPE AddVector256L(Vector256L lhs, Vector256L rhs)
{
    throw "P/Invoke for Vector256<long> should be unsupported.";
}

extern "C" DLL_EXPORT Vector256L STDMETHODCALLTYPE AddVector256Ls(const Vector256L* pValues, uint32_t count)
{
    throw "P/Invoke for Vector256<long> should be unsupported.";
}
