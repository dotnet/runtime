// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

#if defined(TARGET_XARCH)
    #include <emmintrin.h>

    typedef __m128i Vector128U;
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
            uint32_t e00;
            uint32_t e01;
            uint32_t e02;
            uint32_t e03;
        } uint32x4_t;
    #endif

    typedef uint32x4_t Vector128U;
#else
    #error Unsupported target architecture
#endif

static Vector128U Vector128UValue = { };

extern "C" DLL_EXPORT Vector128U STDMETHODCALLTYPE GetVector128U(uint32_t e00, uint32_t e01, uint32_t e02, uint32_t e03)
{
    union {
        uint32_t value[4];
        Vector128U result;
    };

    value[0] = e00;
    value[1] = e01;
    value[2] = e02;
    value[3] = e03;

    return result;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVector128UOut(uint32_t e00, uint32_t e01, uint32_t e02, uint32_t e03, Vector128U* pValue)
{
    Vector128U value = GetVector128U(e00, e01, e02, e03);

#if defined(TARGET_XARCH)
    _mm_storeu_si128(pValue, value);
#else
    *pValue = value;
#endif
}

extern "C" DLL_EXPORT const Vector128U* STDMETHODCALLTYPE GetVector128UPtr(uint32_t e00, uint32_t e01, uint32_t e02, uint32_t e03)
{
    GetVector128UOut(e00, e01, e02, e03, &Vector128UValue);
    return &Vector128UValue;
}

extern "C" DLL_EXPORT Vector128U STDMETHODCALLTYPE AddVector128U(Vector128U lhs, Vector128U rhs)
{
    throw "P/Invoke for Vector128<uint> should be unsupported.";
}

extern "C" DLL_EXPORT Vector128U STDMETHODCALLTYPE AddVector128Us(const Vector128U* pValues, uint32_t count)
{
    throw "P/Invoke for Vector128<uint> should be unsupported.";
}
