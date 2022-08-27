// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

#if (UINT128_WIDTH == 128)
    typedef uint128_t UInt128;
#elif defined(__SIZEOF_INT128__)
    typedef unsigned __int128 UInt128;
#else
    typedef struct {
        uint64_t lower;
        uint64_t upper;
    } UInt128;
#endif

static UInt128 UInt128Value = { };

extern "C" DLL_EXPORT UInt128 STDMETHODCALLTYPE GetUInt128(uint64_t upper, uint64_t lower)
{
    UInt128 result;

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

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetUInt128Out(uint64_t upper, uint64_t lower, char* pValue /* This is a char*, as .NET does not currently guarantee that Int128 values are aligned */)
{
    UInt128 value = GetUInt128(upper, lower);
    memcpy(pValue, &value, sizeof(value));  // Perform unaligned write
}

extern "C" DLL_EXPORT const UInt128* STDMETHODCALLTYPE GetUInt128Ptr(uint64_t upper, uint64_t lower)
{
    GetUInt128Out(upper, lower, (char*)&UInt128Value);
    return &UInt128Value;
}

extern "C" DLL_EXPORT UInt128 STDMETHODCALLTYPE AddUInt128(UInt128 lhs, UInt128 rhs)
{
    UInt128 result;

#if (UINT128_WIDTH == 128) || defined(__SIZEOF_INT128__)
    result = lhs + rhs;
#else
    result.lower = lhs.lower + rhs.lower;
    uint64_t carry = (result.lower < lhs.lower) ? 1 : 0;
    result.upper = lhs.upper + rhs.upper + carry;
#endif

    return result;
}

extern "C" DLL_EXPORT UInt128 STDMETHODCALLTYPE AddUInt128s(const char* pValues /* These are char*, as .NET does not currently guarantee that Int128 values are aligned */, uint32_t count)
{
    UInt128 result = {};

    for (uint32_t i = 0; i < count; i++)
    {
        UInt128 input;
        memcpy(&input, pValues + (sizeof(UInt128) * i), sizeof(UInt128));  // Perform unaligned read
        result = AddUInt128(result, input);
    }

    return result;
}
