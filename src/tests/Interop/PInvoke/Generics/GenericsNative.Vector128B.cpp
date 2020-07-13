// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

#if defined(TARGET_XARCH)
    #include <emmintrin.h>

    typedef __m128i Vector128B;
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
        } int8x16_t;
    #endif

    typedef int8x16_t Vector128B;
#else
    #error Unsupported target architecture
#endif

static Vector128B Vector128BValue = { };

extern "C" DLL_EXPORT Vector128B STDMETHODCALLTYPE GetVector128B(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07, bool e08, bool e09, bool e10, bool e11, bool e12, bool e13, bool e14, bool e15)
{
    union {
        bool value[16];
        Vector128B result;
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

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVector128BOut(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07, bool e08, bool e09, bool e10, bool e11, bool e12, bool e13, bool e14, bool e15, Vector128B* pValue)
{
    Vector128B value = GetVector128B(e00, e01, e02, e03, e04, e05, e06, e07, e08, e09, e10, e11, e12, e13, e14, e15);

#if defined(TARGET_XARCH)
    _mm_storeu_si128(pValue, value);
#else
    *pValue = value;
#endif
}

extern "C" DLL_EXPORT const Vector128B* STDMETHODCALLTYPE GetVector128BPtr(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07, bool e08, bool e09, bool e10, bool e11, bool e12, bool e13, bool e14, bool e15)
{
    GetVector128BOut(e00, e01, e02, e03, e04, e05, e06, e07, e08, e09, e10, e11, e12, e13, e14, e15, &Vector128BValue);
    return &Vector128BValue;
}

extern "C" DLL_EXPORT Vector128B STDMETHODCALLTYPE AddVector128B(Vector128B lhs, Vector128B rhs)
{
    throw "P/Invoke for Vector128<bool> should be unsupported.";
}

extern "C" DLL_EXPORT Vector128B STDMETHODCALLTYPE AddVector128Bs(const Vector128B* pValues, uint32_t count)
{
    throw "P/Invoke for Vector128<bool> should be unsupported.";
}
