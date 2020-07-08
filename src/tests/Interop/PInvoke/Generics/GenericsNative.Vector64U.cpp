// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

#if defined(TARGET_XARCH)
    #include <mmintrin.h>

    typedef __m64 Vector64U;
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
        } uint32x2_t;
    #endif

    typedef uint32x2_t Vector64U;
#else
    #error Unsupported target architecture
#endif

static Vector64U Vector64UValue = { };

extern "C" DLL_EXPORT Vector64U STDMETHODCALLTYPE GetVector64U(uint32_t e00, uint32_t e01)
{
    union {
        uint32_t value[2];
        Vector64U result;
    };

    value[0] = e00;
    value[1] = e01;

    return result;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVector64UOut(uint32_t e00, uint32_t e01, Vector64U* pValue)
{
    *pValue = GetVector64U(e00, e01);

#if defined(_MSC_VER) && defined(TARGET_X86)
    _mm_empty();
#endif // _MSC_VER && TARGET_X86
}

extern "C" DLL_EXPORT const Vector64U* STDMETHODCALLTYPE GetVector64UPtr(uint32_t e00, uint32_t e01)
{
    GetVector64UOut(e00, e01, &Vector64UValue);
    return &Vector64UValue;
}

extern "C" DLL_EXPORT Vector64U STDMETHODCALLTYPE AddVector64U(Vector64U lhs, Vector64U rhs)
{
    throw "P/Invoke for Vector64<uint> should be unsupported.";
}

extern "C" DLL_EXPORT Vector64U STDMETHODCALLTYPE AddVector64Us(const Vector64U* pValues, uint32_t count)
{
    throw "P/Invoke for Vector64<uint> should be unsupported.";
}
