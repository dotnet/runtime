// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

#if defined(TARGET_XARCH)
    #include <mmintrin.h>

    typedef __m64 Vector64C;
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
            int16_t e00;
            int16_t e01;
            int16_t e02;
            int16_t e03;
        } int16x4_t;
    #endif

    typedef int16x4_t Vector64C;
#else
    #error Unsupported target architecture
#endif

static Vector64C Vector64CValue = { };

extern "C" DLL_EXPORT Vector64C STDMETHODCALLTYPE GetVector64C(char16_t e00, char16_t e01, char16_t e02, char16_t e03)
{
    union {
        char16_t value[4];
        Vector64C result;
    };

    value[0] = e00;
    value[1] = e01;
    value[2] = e02;
    value[3] = e03;

    return result;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVector64COut(char16_t e00, char16_t e01, char16_t e02, char16_t e03, Vector64C* pValue)
{
    *pValue = GetVector64C(e00, e01, e02, e03);

#if defined(_MSC_VER) && defined(TARGET_X86)
    _mm_empty();
#endif // _MSC_VER && TARGET_X86
}

extern "C" DLL_EXPORT const Vector64C* STDMETHODCALLTYPE GetVector64CPtr(char16_t e00, char16_t e01, char16_t e02, char16_t e03)
{
    GetVector64COut(e00, e01, e02, e03, &Vector64CValue);
    return &Vector64CValue;
}

extern "C" DLL_EXPORT Vector64C STDMETHODCALLTYPE AddVector64C(Vector64C lhs, Vector64C rhs)
{
    throw "P/Invoke for Vector64<char> should be unsupported.";
}

extern "C" DLL_EXPORT Vector64C STDMETHODCALLTYPE AddVector64Cs(const Vector64C* pValues, uint32_t count)
{
    throw "P/Invoke for Vector64<char> should be unsupported.";
}
