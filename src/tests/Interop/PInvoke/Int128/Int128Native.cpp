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
struct 
#if defined(_M_ARM64) || defined(_M_AMD64) || defined(_M_IX86)
alignas(16)
#endif
Int128 {
        uint64_t lower;
        uint64_t upper;
    };
#endif

static Int128 Int128Value = { };

struct StructWithInt128
{
    int8_t messUpPadding;
    Int128 value;
};

struct StructJustInt128
{
    Int128 value;
};

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


extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetInt128Out(uint64_t upper, uint64_t lower, char* pValue /* This is a char*, as .NET does not currently guarantee that Int128 values are aligned */)
{
    Int128 value = GetInt128(upper, lower);
    memcpy(pValue, &value, sizeof(value)); // Perform unaligned write
}

extern "C" DLL_EXPORT uint64_t STDMETHODCALLTYPE GetInt128Lower(Int128 value)
{
#if (INT128_WIDTH == 128) || defined(__SIZEOF_INT128__)
    return (uint64_t)value;
#else
    return value.lower;
#endif
}

extern "C" DLL_EXPORT uint64_t STDMETHODCALLTYPE GetInt128Lower_S(StructJustInt128 value)
{
#if (INT128_WIDTH == 128) || defined(__SIZEOF_INT128__)
    return (uint64_t)value.value;
#else
    return value.value.lower;
#endif
}

extern "C" DLL_EXPORT const Int128* STDMETHODCALLTYPE GetInt128Ptr(uint64_t upper, uint64_t lower)
{
    GetInt128Out(upper, lower, (char*)&Int128Value);
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

// Test that struct alignment behavior matches with the standard OS compiler
extern "C" DLL_EXPORT void STDMETHODCALLTYPE AddStructWithInt128_ByRef(char *pLhs, char *pRhs) /* These are char*, as .NET does not currently guarantee that Int128 values are aligned */
{
    StructWithInt128 result = {};
    StructWithInt128 lhs;
    memcpy(&lhs, pLhs, sizeof(lhs)); // Perform unaligned read
    StructWithInt128 rhs;
    memcpy(&rhs, pRhs, sizeof(rhs)); // Perform unaligned read

    result.messUpPadding = lhs.messUpPadding;

#if (INT128_WIDTH == 128) || defined(__SIZEOF_INT128__)
    result.value = lhs.value + rhs.value;
#else
    result.value.lower = lhs.value.lower + rhs.value.lower;
    uint64_t carry = (result.value.lower < lhs.value.lower) ? 1 : 0;
    result.value.upper = lhs.value.upper + rhs.value.upper + carry;
#endif

    memcpy(pLhs, &result, sizeof(result)); // Perform unaligned write
}

extern "C" DLL_EXPORT StructWithInt128 STDMETHODCALLTYPE AddStructWithInt128(StructWithInt128 lhs, StructWithInt128 rhs)
{
    StructWithInt128 result = {};
    result.messUpPadding = lhs.messUpPadding;

#if (INT128_WIDTH == 128) || defined(__SIZEOF_INT128__)
    result.value = lhs.value + rhs.value;
#else
    result.value.lower = lhs.value.lower + rhs.value.lower;
    uint64_t carry = (result.value.lower < lhs.value.lower) ? 1 : 0;
    result.value.upper = lhs.value.upper + rhs.value.upper + carry;
#endif

    return result;
}

extern "C" DLL_EXPORT StructWithInt128 STDMETHODCALLTYPE AddStructWithInt128_1(int64_t dummy1, StructWithInt128 lhs, StructWithInt128 rhs)
{
    StructWithInt128 result = {};
    result.messUpPadding = lhs.messUpPadding;

#if (INT128_WIDTH == 128) || defined(__SIZEOF_INT128__)
    result.value = lhs.value + rhs.value;
#else
    result.value.lower = lhs.value.lower + rhs.value.lower;
    uint64_t carry = (result.value.lower < lhs.value.lower) ? 1 : 0;
    result.value.upper = lhs.value.upper + rhs.value.upper + carry;
#endif

    return result;
}

extern "C" DLL_EXPORT StructWithInt128 STDMETHODCALLTYPE AddStructWithInt128_9(int64_t dummy1, int64_t dummy2, int64_t dummy3, int64_t dummy4, int64_t dummy5, int64_t dummy6, int64_t dummy7, int64_t dummy8, int64_t dummy9, StructWithInt128 lhs, StructWithInt128 rhs)
{
    StructWithInt128 result = {};
    result.messUpPadding = lhs.messUpPadding;

#if (INT128_WIDTH == 128) || defined(__SIZEOF_INT128__)
    result.value = lhs.value + rhs.value;
#else
    result.value.lower = lhs.value.lower + rhs.value.lower;
    uint64_t carry = (result.value.lower < lhs.value.lower) ? 1 : 0;
    result.value.upper = lhs.value.upper + rhs.value.upper + carry;
#endif

    return result;
}

extern "C" DLL_EXPORT Int128 STDMETHODCALLTYPE AddInt128_1(int64_t dummy1, Int128 lhs, Int128 rhs)
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

extern "C" DLL_EXPORT Int128 STDMETHODCALLTYPE AddInt128_2(int64_t dummy1, int64_t dummy2, Int128 lhs, Int128 rhs)
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

extern "C" DLL_EXPORT Int128 STDMETHODCALLTYPE AddInt128_3(int64_t dummy1, int64_t dummy2, int64_t dummy3, Int128 lhs, Int128 rhs)
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

extern "C" DLL_EXPORT Int128 STDMETHODCALLTYPE AddInt128_4(int64_t dummy1, int64_t dummy2, int64_t dummy3, int64_t dummy4, Int128 lhs, Int128 rhs)
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

extern "C" DLL_EXPORT Int128 STDMETHODCALLTYPE AddInt128_5(int64_t dummy1, int64_t dummy2, int64_t dummy3, int64_t dummy4, int64_t dummy5, Int128 lhs, Int128 rhs)
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

extern "C" DLL_EXPORT Int128 STDMETHODCALLTYPE AddInt128_6(int64_t dummy1, int64_t dummy2, int64_t dummy3, int64_t dummy4, int64_t dummy5, int64_t dummy6, Int128 lhs, Int128 rhs)
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

extern "C" DLL_EXPORT Int128 STDMETHODCALLTYPE AddInt128_7(int64_t dummy1, int64_t dummy2, int64_t dummy3, int64_t dummy4, int64_t dummy5, int64_t dummy6, int64_t dummy7, Int128 lhs, Int128 rhs)
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

extern "C" DLL_EXPORT Int128 STDMETHODCALLTYPE AddInt128_8(int64_t dummy1, int64_t dummy2, int64_t dummy3, int64_t dummy4, int64_t dummy5, int64_t dummy6, int64_t dummy7, int64_t dummy8, Int128 lhs, Int128 rhs)
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

extern "C" DLL_EXPORT Int128 STDMETHODCALLTYPE AddInt128_9(int64_t dummy1, int64_t dummy2, int64_t dummy3, int64_t dummy4, int64_t dummy5, int64_t dummy6, int64_t dummy7, int64_t dummy8, int64_t dummy9, Int128 lhs, Int128 rhs)
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


extern "C" DLL_EXPORT Int128 STDMETHODCALLTYPE AddInt128s(const char* pValues /* These are char*, as .NET does not currently guarantee that Int128 values are aligned */, uint32_t count)
{
    Int128 result = {};

    for (uint32_t i = 0; i < count; i++)
    {
        Int128 input;
        memcpy(&input, pValues + (sizeof(Int128) * i), sizeof(Int128));  // Perform unaligned read
        result = AddInt128(result, input);
    }

    return result;
}
