// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

#if defined(TARGET_XARCH)
    #include <mmintrin.h>

    typedef __m64 Vector64B;
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
            int8_t e00;
            int8_t e01;
            int8_t e02;
            int8_t e03;
            int8_t e04;
            int8_t e05;
            int8_t e06;
            int8_t e07;
        } int8x8_t;
    #endif

    typedef int8x8_t Vector64B;
#else
    #error Unsupported target architecture
#endif

static Vector64B Vector64BValue = { };

extern "C" DLL_EXPORT Vector64B STDMETHODCALLTYPE GetVector64B(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07)
{
    union {
        bool value[8];
        Vector64B result;
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

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVector64BOut(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07, Vector64B* pValue)
{
    *pValue = GetVector64B(e00, e01, e02, e03, e04, e05, e06, e07);

#if defined(_MSC_VER) && defined(TARGET_X86)
    _mm_empty();
#endif // _MSC_VER && TARGET_X86
}

extern "C" DLL_EXPORT const Vector64B* STDMETHODCALLTYPE GetVector64BPtr(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07)
{
    GetVector64BOut(e00, e01, e02, e03, e04, e05, e06, e07, &Vector64BValue);
    return &Vector64BValue;
}

extern "C" DLL_EXPORT Vector64B STDMETHODCALLTYPE AddVector64B(Vector64B lhs, Vector64B rhs)
{
    throw "P/Invoke for Vector64<bool> should be unsupported.";
}

extern "C" DLL_EXPORT Vector64B STDMETHODCALLTYPE AddVector64Bs(const Vector64B* pValues, uint32_t count)
{
    throw "P/Invoke for Vector64<bool> should be unsupported.";
}
