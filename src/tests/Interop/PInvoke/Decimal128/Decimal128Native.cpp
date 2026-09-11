// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <string.h>
#include <xplatform.h>
#include <platformdefines.h>

// Decimal128 is treated here purely as a 16-byte, 16-byte-aligned blob of two uint64 halves.
// This mirrors the native _Decimal128 ABI packing without requiring decimal arithmetic support
// (which MSVC and Clang lack), and lets us validate that the managed 16-byte alignment matches
// native. ARM32 is excluded to match the managed rule, which keeps the natural 8-byte alignment
// there as no _Decimal128 exists in its Procedure Call Standard.
struct
#if !defined(TARGET_ARM)
alignas(16)
#endif
Decimal128 {
#if BIGENDIAN
    uint64_t upper;
    uint64_t lower;
#else
    uint64_t lower;
    uint64_t upper;
#endif
};

struct StructWithDecimal128
{
    int8_t messUpPadding;
    Decimal128 value;
};

struct StructJustDecimal128
{
    Decimal128 value;
};

extern "C" DLL_EXPORT Decimal128 STDMETHODCALLTYPE GetDecimal128(uint64_t upper, uint64_t lower)
{
    Decimal128 result;
    result.lower = lower;
    result.upper = upper;
    return result;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetDecimal128Out(uint64_t upper, uint64_t lower, char* pValue /* char*, as .NET does not currently guarantee that Decimal128 values are aligned */)
{
    Decimal128 value = GetDecimal128(upper, lower);
    memcpy(pValue, &value, sizeof(value)); // Perform unaligned write
}

extern "C" DLL_EXPORT uint64_t STDMETHODCALLTYPE GetDecimal128Lower(Decimal128 value)
{
    return value.lower;
}

extern "C" DLL_EXPORT uint64_t STDMETHODCALLTYPE GetDecimal128Lower_S(StructJustDecimal128 value)
{
    return value.value.lower;
}

// Test that struct alignment behavior matches with the standard OS compiler
extern "C" DLL_EXPORT void STDMETHODCALLTYPE AddStructWithDecimal128_ByRef(char* pLhs, char* pRhs) /* char*, as .NET does not currently guarantee that Decimal128 values are aligned */
{
    StructWithDecimal128 result = {};
    StructWithDecimal128 lhs;
    memcpy(&lhs, pLhs, sizeof(lhs)); // Perform unaligned read
    StructWithDecimal128 rhs;
    memcpy(&rhs, pRhs, sizeof(rhs)); // Perform unaligned read

    result.messUpPadding = lhs.messUpPadding;

    result.value.lower = lhs.value.lower + rhs.value.lower;
    uint64_t carry = (result.value.lower < lhs.value.lower) ? 1 : 0;
    result.value.upper = lhs.value.upper + rhs.value.upper + carry;

    memcpy(pLhs, &result, sizeof(result)); // Perform unaligned write
}

// Present only so the by-value marshaling restriction has real symbols to resolve; these are never
// actually invoked because the marshaler throws MarshalDirectiveException before the call.
extern "C" DLL_EXPORT uint32_t STDMETHODCALLTYPE GetDecimal32Bits(uint32_t value)
{
    return value;
}

extern "C" DLL_EXPORT uint64_t STDMETHODCALLTYPE GetDecimal64Bits(uint64_t value)
{
    return value;
}
