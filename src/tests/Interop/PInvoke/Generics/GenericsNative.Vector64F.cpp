// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

#if defined(TARGET_XARCH)
    #include <mmintrin.h>

    typedef __m64 Vector64F;
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
        } float32x2_t;
    #endif

    typedef float32x2_t Vector64F;
#else
    #error Unsupported target architecture
#endif

static Vector64F Vector64FValue = { };

extern "C" DLL_EXPORT Vector64F STDMETHODCALLTYPE GetVector64F(float e00, float e01)
{
    union {
        float value[2];
        Vector64F result;
    };

    value[0] = e00;
    value[1] = e01;

    return result;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVector64FOut(float e00, float e01, Vector64F* pValue)
{
    *pValue = GetVector64F(e00, e01);

#if defined(_MSC_VER) && defined(TARGET_X86)
    _mm_empty();
#endif // _MSC_VER && TARGET_X86
}

extern "C" DLL_EXPORT const Vector64F* STDMETHODCALLTYPE GetVector64FPtr(float e00, float e01)
{
    GetVector64FOut(e00, e01, &Vector64FValue);
    return &Vector64FValue;
}

extern "C" DLL_EXPORT Vector64F STDMETHODCALLTYPE AddVector64F(Vector64F lhs, Vector64F rhs)
{
    throw "P/Invoke for Vector64<float> should be unsupported.";
}

extern "C" DLL_EXPORT Vector64F STDMETHODCALLTYPE AddVector64Fs(const Vector64F* pValues, uint32_t count)
{
    throw "P/Invoke for Vector64<float> should be unsupported.";
}
