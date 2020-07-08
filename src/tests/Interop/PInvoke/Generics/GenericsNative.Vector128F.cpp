// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

#if defined(TARGET_XARCH)
    #include <xmmintrin.h>

    typedef __m128 Vector128F;
#elif defined(TARGET_ARMARCH)
    #if defined(_MSC_VER)
        #if defined(TARGET_ARM64)
            #include <arm64_neon.h>
        #else
            #include <arm_neon.h>
        #endif
    #elif defined(TARGET_ARM64)
        #include <arm_neon.h>
    #else
        typedef struct {
            float e00;
            float e01;
            float e02;
            float e03;
        } float32x4_t;
    #endif

    typedef float32x4_t Vector128F;
#else
    #error Unsupported target architecture
#endif

static Vector128F Vector128FValue = { };

extern "C" DLL_EXPORT Vector128F STDMETHODCALLTYPE GetVector128F(float e00, float e01, float e02, float e03)
{
    union {
        float value[4];
        Vector128F result;
    };

    value[0] = e00;
    value[1] = e01;
    value[2] = e02;
    value[3] = e03;

    return result;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVector128FOut(float e00, float e01, float e02, float e03, Vector128F* pValue)
{
    Vector128F value = GetVector128F(e00, e01, e02, e03);

#if defined(TARGET_XARCH)
    _mm_storeu_ps((float*)pValue, value);
#else
    *pValue = value;
#endif
}

extern "C" DLL_EXPORT const Vector128F* STDMETHODCALLTYPE GetVector128FPtr(float e00, float e01, float e02, float e03)
{
    GetVector128FOut(e00, e01, e02, e03, &Vector128FValue);
    return &Vector128FValue;
}

extern "C" DLL_EXPORT Vector128F STDMETHODCALLTYPE AddVector128F(Vector128F lhs, Vector128F rhs)
{
    throw "P/Invoke for Vector128<float> should be unsupported.";
}

extern "C" DLL_EXPORT Vector128F STDMETHODCALLTYPE AddVector128Fs(const Vector128F* pValues, uint32_t count)
{
    throw "P/Invoke for Vector128<float> should be unsupported.";
}
