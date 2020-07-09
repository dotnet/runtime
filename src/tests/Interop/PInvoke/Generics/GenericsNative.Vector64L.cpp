// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

#if defined(TARGET_XARCH)
    #include <mmintrin.h>

    typedef __m64 Vector64L;
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
        } int64x1_t;
    #endif

    typedef int64x1_t Vector64L;
#else
    #error Unsupported target architecture
#endif

static Vector64L Vector64LValue = { };

extern "C" DLL_EXPORT Vector64L STDMETHODCALLTYPE GetVector64L(int64_t e00)
{
    union {
        int64_t value[1];
        Vector64L result;
    };

    value[0] = e00;

    return result;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVector64LOut(int64_t e00, Vector64L* pValue)
{
    *pValue = GetVector64L(e00);

#if defined(_MSC_VER) && defined(TARGET_X86)
    _mm_empty();
#endif // _MSC_VER && TARGET_X86
}

extern "C" DLL_EXPORT const Vector64L* STDMETHODCALLTYPE GetVector64LPtr(int64_t e00)
{
    GetVector64LOut(e00, &Vector64LValue);
    return &Vector64LValue;
}

extern "C" DLL_EXPORT Vector64L STDMETHODCALLTYPE AddVector64L(Vector64L lhs, Vector64L rhs)
{
    throw "P/Invoke for Vector64<long> should be unsupported.";
}

extern "C" DLL_EXPORT Vector64L STDMETHODCALLTYPE AddVector64Ls(const Vector64L* pValues, uint32_t count)
{
    throw "P/Invoke for Vector64<long> should be unsupported.";
}
