// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

#if defined(TARGET_XARCH)
    #include <immintrin.h>
#elif defined(TARGET_ARMARCH)
    // Intentionally empty
#else
    #error Unsupported target architecture
#endif

#if defined(__AVX2__)
    typedef __m256i Vector256B;
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
        bool e16;
        bool e17;
        bool e18;
        bool e19;
        bool e20;
        bool e21;
        bool e22;
        bool e23;
        bool e24;
        bool e25;
        bool e26;
        bool e27;
        bool e28;
        bool e29;
        bool e30;
        bool e31;
    } Vector256B;
#endif

static Vector256B Vector256BValue = { };

extern "C" DLL_EXPORT Vector256B STDMETHODCALLTYPE GetVector256B(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07, bool e08, bool e09, bool e10, bool e11, bool e12, bool e13, bool e14, bool e15, bool e16, bool e17, bool e18, bool e19, bool e20, bool e21, bool e22, bool e23, bool e24, bool e25, bool e26, bool e27, bool e28, bool e29, bool e30, bool e31)
{
    union {
        bool value[32];
        Vector256B result;
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
    value[16] = e16;
    value[17] = e17;
    value[18] = e18;
    value[19] = e19;
    value[20] = e20;
    value[21] = e21;
    value[22] = e22;
    value[23] = e23;
    value[24] = e24;
    value[25] = e25;
    value[26] = e26;
    value[27] = e27;
    value[28] = e28;
    value[29] = e29;
    value[30] = e30;
    value[31] = e31;

    return result;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetVector256BOut(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07, bool e08, bool e09, bool e10, bool e11, bool e12, bool e13, bool e14, bool e15, bool e16, bool e17, bool e18, bool e19, bool e20, bool e21, bool e22, bool e23, bool e24, bool e25, bool e26, bool e27, bool e28, bool e29, bool e30, bool e31, Vector256B* pValue)
{
    Vector256B value = GetVector256B(e00, e01, e02, e03, e04, e05, e06, e07, e08, e09, e10, e11, e12, e13, e14, e15, e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31);

#if defined(TARGET_XARCH)
    _mm_storeu_si128(((__m128i*)pValue) + 0, *(((__m128i*)&value) + 0));
    _mm_storeu_si128(((__m128i*)pValue) + 1, *(((__m128i*)&value) + 1));
#else
    *pValue = value;
#endif
}

extern "C" DLL_EXPORT const Vector256B* STDMETHODCALLTYPE GetVector256BPtr(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07, bool e08, bool e09, bool e10, bool e11, bool e12, bool e13, bool e14, bool e15, bool e16, bool e17, bool e18, bool e19, bool e20, bool e21, bool e22, bool e23, bool e24, bool e25, bool e26, bool e27, bool e28, bool e29, bool e30, bool e31)
{
    GetVector256BOut(e00, e01, e02, e03, e04, e05, e06, e07, e08, e09, e10, e11, e12, e13, e14, e15, e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31, &Vector256BValue);
    return &Vector256BValue;
}

extern "C" DLL_EXPORT Vector256B STDMETHODCALLTYPE AddVector256B(Vector256B lhs, Vector256B rhs)
{
    throw "P/Invoke for Vector256<bool> should be unsupported.";
}

extern "C" DLL_EXPORT Vector256B STDMETHODCALLTYPE AddVector256Bs(const Vector256B* pValues, uint32_t count)
{
    throw "P/Invoke for Vector256<bool> should be unsupported.";
}
