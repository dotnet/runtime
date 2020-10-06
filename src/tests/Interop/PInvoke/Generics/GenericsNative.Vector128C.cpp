// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

#if defined(TARGET_XARCH)
    #include <emmintrin.h>

    typedef __m128i Vector128C;
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
            char16_t e00;
            char16_t e01;
            char16_t e02;
            char16_t e03;
            char16_t e04;
            char16_t e05;
            char16_t e06;
            char16_t e07;
        } int16x8_t;
    #endif

    typedef int16x8_t Vector128C;
#else
    #error Unsupported target architecture
#endif

static Vector128C Vector128CValue = { };

extern "C" DLL_EXPORT Vector128C STDMETHODCALLTYPE GetVector128C(char16_t e00, char16_t e01, char16_t e02, char16_t e03, char16_t e04, char16_t e05, char16_t e06, char16_t e07)
{
    union {
        char16_t value[8];
        Vector128C result;
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

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVector128COut(char16_t e00, char16_t e01, char16_t e02, char16_t e03, char16_t e04, char16_t e05, char16_t e06, char16_t e07, Vector128C* pValue)
{
    Vector128C value = GetVector128C(e00, e01, e02, e03, e04, e05, e06, e07);

#if defined(TARGET_XARCH)
    _mm_storeu_si128(pValue, value);
#else
    *pValue = value;
#endif
}

extern "C" DLL_EXPORT const Vector128C* STDMETHODCALLTYPE GetVector128CPtr(char16_t e00, char16_t e01, char16_t e02, char16_t e03, char16_t e04, char16_t e05, char16_t e06, char16_t e07)
{
    GetVector128COut(e00, e01, e02, e03, e04, e05, e06, e07, &Vector128CValue);
    return &Vector128CValue;
}

extern "C" DLL_EXPORT Vector128C STDMETHODCALLTYPE AddVector128C(Vector128C lhs, Vector128C rhs)
{
    throw "P/Invoke for Vector128<char> should be unsupported.";
}

extern "C" DLL_EXPORT Vector128C STDMETHODCALLTYPE AddVector128Cs(const Vector128C* pValues, uint32_t count)
{
    throw "P/Invoke for Vector128<char> should be unsupported.";
}
