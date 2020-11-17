// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

#if defined(TARGET_XARCH)
    #include <emmintrin.h>

    typedef __m128i Vector128L;
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
            int64_t e00;
            int64_t e01;
        } int64x2_t;
    #endif

    typedef int64x2_t Vector128L;
#else
    #error Unsupported target architecture
#endif

static Vector128L Vector128LValue = { };

extern "C" DLL_EXPORT Vector128L STDMETHODCALLTYPE GetVector128L(int64_t e00, int64_t e01)
{
    union {
        int64_t value[2];
        Vector128L result;
    };

    value[0] = e00;
    value[1] = e01;

    return result;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVector128LOut(int64_t e00, int64_t e01, Vector128L* pValue)
{
    Vector128L value = GetVector128L(e00, e01);

#if defined(TARGET_XARCH)
    _mm_storeu_si128(pValue, value);
#else
    *pValue = value;
#endif
}

extern "C" DLL_EXPORT const Vector128L* STDMETHODCALLTYPE GetVector128LPtr(int64_t e00, int64_t e01)
{
    GetVector128LOut(e00, e01, &Vector128LValue);
    return &Vector128LValue;
}

extern "C" DLL_EXPORT Vector128L STDMETHODCALLTYPE AddVector128L(Vector128L lhs, Vector128L rhs)
{
    throw "P/Invoke for Vector128<long> should be unsupported.";
}

extern "C" DLL_EXPORT Vector128L STDMETHODCALLTYPE AddVector128Ls(const Vector128L* pValues, uint32_t count)
{
    throw "P/Invoke for Vector128<long> should be unsupported.";
}
