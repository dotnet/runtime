// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

#if defined(TARGET_XARCH)
    #include <mmintrin.h>

    typedef __m64 Vector64D;
#elif defined(TARGET_ARMARCH)
    #if defined(_MSC_VER)
        #if defined(TARGET_ARM64)
            #include <arm64_neon.h>
        #else
            #include <arm_neon.h>
        #endif

        typedef __n64 float64x1_t;
    #elif defined(TARGET_ARM64)
        #include <arm_neon.h>
    #else
        typedef struct {
            double e00;
        } float64x1_t;
    #endif

    typedef float64x1_t Vector64D;
#else
    #error Unsupported target architecture
#endif

static Vector64D Vector64DValue = { };

extern "C" DLL_EXPORT Vector64D STDMETHODCALLTYPE GetVector64D(double e00)
{
    union {
        double value[1];
        Vector64D result;
    };

    value[0] = e00;

    return result;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVector64DOut(double e00, Vector64D* pValue)
{
    *pValue = GetVector64D(e00);

#if defined(_MSC_VER) && defined(TARGET_X86)
    _mm_empty();
#endif // _MSC_VER && TARGET_X86
}

extern "C" DLL_EXPORT const Vector64D* STDMETHODCALLTYPE GetVector64DPtr(double e00)
{
    GetVector64DOut(e00, &Vector64DValue);
    return &Vector64DValue;
}

extern "C" DLL_EXPORT Vector64D STDMETHODCALLTYPE AddVector64D(Vector64D lhs, Vector64D rhs)
{
    throw "P/Invoke for Vector64<double> should be unsupported.";
}

extern "C" DLL_EXPORT Vector64D STDMETHODCALLTYPE AddVector64Ds(const Vector64D* pValues, uint32_t count)
{
    throw "P/Invoke for Vector64<double> should be unsupported.";
}
