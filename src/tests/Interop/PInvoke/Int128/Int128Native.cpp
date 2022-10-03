// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

#if (INT128_WIDTH == 128)
    typedef int128_t Int128;
#elif defined(__SIZEOF_INT128__)
    typedef __int128 Int128;
#else
    typedef struct {
        uint64_t lower;
        uint64_t upper;
    } Int128;
#endif

static Int128 Int128Value = { };

extern "C" DLL_EXPORT Int128 STDMETHODCALLTYPE GetInt128(uint64_t upper, uint64_t lower)
{
    Int128 result;

#if (INT128_WIDTH == 128) || defined(__SIZEOF_INT128__)
    result = upper;
    result = result << 64;
    result = result | lower;
#else
    result.lower = lower;
    result.upper = upper;
#endif

    return result;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetInt128Out(uint64_t upper, uint64_t lower, Int128* pValue)
{
    Int128 value = GetInt128(upper, lower);
    *pValue = value;
}

extern "C" DLL_EXPORT const Int128* STDMETHODCALLTYPE GetInt128Ptr(uint64_t upper, uint64_t lower)
{
    GetInt128Out(upper, lower, &Int128Value);
    return &Int128Value;
}

extern "C" DLL_EXPORT Int128 STDMETHODCALLTYPE AddInt128(Int128 lhs, Int128 rhs)
{
    Int128 result;

#if (INT128_WIDTH == 128) || defined(__SIZEOF_INT128__)
    result = lhs + rhs;
#else
    result.lower = lhs.lower + rhs.lower;
    uint64_t carry = (result.lower < lhs.lower) ? 1 : 0;
    result.upper = lhs.upper + rhs.upper + carry;
#endif

    return result;
}

extern "C" DLL_EXPORT Int128 STDMETHODCALLTYPE AddInt128s(const Int128* pValues, uint32_t count)
{
    Int128 result = {};

    for (uint32_t i = 0; i < count; i++)
    {
        result = AddInt128(result, pValues[i]);
    }

    return result;
}
