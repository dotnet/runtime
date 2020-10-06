// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

#if defined(TARGET_XARCH)
    #include <emmintrin.h>

    typedef __m128d Vector128D;
#elif defined(TARGET_ARMARCH)
    #if defined(_MSC_VER)
        #if defined(TARGET_ARM64)
            #include <arm64_neon.h>
        #else
            #include <arm_neon.h>

            typedef __n128 float64x2_t;
        #endif
    #elif defined(TARGET_ARM64)
        #include <arm_neon.h>
    #else
        typedef struct {
            double e00;
            double e01;
        } float64x2_t;
    #endif

    typedef float64x2_t Vector128D;
#else
    #error Unsupported target architecture
#endif

static Vector128D Vector128DValue = { };

extern "C" DLL_EXPORT Vector128D STDMETHODCALLTYPE GetVector128D(double e00, double e01)
{
    union {
        double value[2];
        Vector128D result;
    };

    value[0] = e00;
    value[1] = e01;

    return result;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVector128DOut(double e00, double e01, Vector128D* pValue)
{
    Vector128D value = GetVector128D(e00, e01);

#if defined(TARGET_XARCH)
    _mm_storeu_pd((double*)pValue, value);
#else
    *pValue = value;
#endif
}

extern "C" DLL_EXPORT const Vector128D* STDMETHODCALLTYPE GetVector128DPtr(double e00, double e01)
{
    GetVector128DOut(e00, e01, &Vector128DValue);
    return &Vector128DValue;
}

extern "C" DLL_EXPORT Vector128D STDMETHODCALLTYPE AddVector128D(Vector128D lhs, Vector128D rhs)
{
    throw "P/Invoke for Vector128<double> should be unsupported.";
}

extern "C" DLL_EXPORT Vector128D STDMETHODCALLTYPE AddVector128Ds(const Vector128D* pValues, uint32_t count)
{
    throw "P/Invoke for Vector128<double> should be unsupported.";
}
