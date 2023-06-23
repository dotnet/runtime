// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "rhassert.h"

//
// Floating point and 64-bit integer math helpers.
//

FORCEINLINE int64_t FastDbl2Lng(double val)
{
#ifdef TARGET_X86
    return HCCALL1_V(JIT_Dbl2Lng, val);
#else
    return((__int64) val);
#endif
}

//------------------------------------------------------------------------
// TruncateDouble: helper function to truncate double 
//                 numbers to nearest integer (round towards zero).
//
// Arguments:
//    val  - double number to be truncated.
//
// Return Value:
//    double: truncated number (rounded towards zero)
// 
double TruncateDouble(double val)
{
    int64_t *dintVal = (int64_t *)&val;

    uint64_t uintVal = (uint64_t)*dintVal;
    int exponent = (int)((uintVal >> 52) & 0x7FF);
    if (exponent < 1023)
    {
        uintVal = uintVal & 0x8000000000000000ull;
    }
    else if (exponent < 1075)
    {
        uintVal = uintVal &  (unsigned long long)(~(0xFFFFFFFFFFFFF >> (exponent - 1023)));
    }
    int64_t intVal = (int64_t)uintVal;
    double *doubleVal = (double *)&intVal;
    double retVal = *doubleVal;

    return retVal;
}

EXTERN_C NATIVEAOT_API uint64_t REDHAWK_CALLCONV RhpDbl2ULng(double val)
{
#if defined(TARGET_X86) || defined(TARGET_AMD64)

    const double uint64_max_plus_1 = -2.0 * (double)LONG_MIN;
    val = TruncateDouble(val);
    return ((val != val) || (val < 0) || (val >= uint64_max_plus_1)) ? ULONG_MAX : (uint64_t)val;

#else
    const double two63  = 2147483648.0 * 4294967296.0;
    uint64_t ret;
    if (val < two63)
    {
        ret = FastDbl2Lng(val);
    }
    else
    {
        // subtract 0x8000000000000000, do the convert then add it back again
        ret = FastDbl2Lng(val - two63) + I64(0x8000000000000000);
    }
    return ret;
#endif // TARGET_X86 || TARGET_AMD64
}

#undef min
#undef max
#include <cmath>

EXTERN_C NATIVEAOT_API float REDHAWK_CALLCONV RhpFltRem(float dividend, float divisor)
{
    //
    // From the ECMA standard:
    //
    // If [divisor] is zero or [dividend] is infinity
    //   the result is NaN.
    // If [divisor] is infinity,
    //   the result is [dividend] (negated for -infinity***).
    //
    // ***"negated for -infinity" has been removed from the spec
    //

    if (divisor==0 || !std::isfinite(dividend))
    {
        return -nanf("");
    }
    else if (!std::isfinite(divisor) && !std::isnan(divisor))
    {
        return dividend;
    }
    // else...
    return fmodf(dividend,divisor);
}

EXTERN_C NATIVEAOT_API double REDHAWK_CALLCONV RhpDblRem(double dividend, double divisor)
{
    //
    // From the ECMA standard:
    //
    // If [divisor] is zero or [dividend] is infinity
    //   the result is NaN.
    // If [divisor] is infinity,
    //   the result is [dividend] (negated for -infinity***).
    //
    // ***"negated for -infinity" has been removed from the spec
    //
    if (divisor==0 || !std::isfinite(dividend))
    {
        return -nan("");
    }
    else if (!std::isfinite(divisor) && !std::isnan(divisor))
    {
        return dividend;
    }
    // else...
    return(fmod(dividend,divisor));
}

EXTERN_C NATIVEAOT_API double REDHAWK_CALLCONV RhpDblRound(double value)
{
    return round(value);
}

EXTERN_C NATIVEAOT_API float REDHAWK_CALLCONV RhpFltRound(float value)
{
    return roundf(value);
}

#ifdef HOST_ARM
EXTERN_C NATIVEAOT_API int32_t REDHAWK_CALLCONV RhpIDiv(int32_t i, int32_t j)
{
    ASSERT(j && "Divide by zero!");
    return i / j;
}

EXTERN_C NATIVEAOT_API uint32_t REDHAWK_CALLCONV RhpUDiv(uint32_t i, uint32_t j)
{
    ASSERT(j && "Divide by zero!");
    return i / j;
}

EXTERN_C NATIVEAOT_API int64_t REDHAWK_CALLCONV RhpLDiv(int64_t i, int64_t j)
{
    ASSERT(j && "Divide by zero!");
    return i / j;
}

EXTERN_C NATIVEAOT_API uint64_t REDHAWK_CALLCONV RhpULDiv(uint64_t i, uint64_t j)
{
    ASSERT(j && "Divide by zero!");
    return i / j;
}

EXTERN_C NATIVEAOT_API int32_t REDHAWK_CALLCONV RhpIMod(int32_t i, int32_t j)
{
    ASSERT(j && "Divide by zero!");
    return i % j;
}

EXTERN_C NATIVEAOT_API uint32_t REDHAWK_CALLCONV RhpUMod(uint32_t i, uint32_t j)
{
    ASSERT(j && "Divide by zero!");
    return i % j;
}

EXTERN_C NATIVEAOT_API int64_t REDHAWK_CALLCONV RhpLMod(int64_t i, int64_t j)
{
    ASSERT(j && "Divide by zero!");
    return i % j;
}

EXTERN_C NATIVEAOT_API uint64_t REDHAWK_CALLCONV RhpULMod(uint64_t i, uint64_t j)
{
    ASSERT(j && "Divide by zero!");
    return i % j;
}

EXTERN_C NATIVEAOT_API int64_t REDHAWK_CALLCONV RhpLMul(int64_t i, int64_t j)
{
    return i * j;
}

EXTERN_C NATIVEAOT_API uint64_t REDHAWK_CALLCONV RhpULMul(uint64_t i, uint64_t j)
{
    return i * j;
}

EXTERN_C NATIVEAOT_API uint64_t REDHAWK_CALLCONV RhpLRsz(uint64_t i, int32_t j)
{
    return i >> j;
}

EXTERN_C NATIVEAOT_API int64_t REDHAWK_CALLCONV RhpLRsh(int64_t i, int32_t j)
{
    return i >> j;
}

EXTERN_C NATIVEAOT_API int64_t REDHAWK_CALLCONV RhpLLsh(int64_t i, int32_t j)
{
    return i << j;
}

EXTERN_C NATIVEAOT_API int64_t REDHAWK_CALLCONV RhpDbl2Lng(double val)
{
    return (int64_t)val;
}

EXTERN_C NATIVEAOT_API int32_t REDHAWK_CALLCONV RhpDbl2Int(double val)
{
    return (int32_t)val;
}

EXTERN_C NATIVEAOT_API uint32_t REDHAWK_CALLCONV RhpDbl2UInt(double val)
{
    return (uint32_t)val;
}

EXTERN_C NATIVEAOT_API double REDHAWK_CALLCONV RhpLng2Dbl(int64_t val)
{
    return (double)val;
}

EXTERN_C NATIVEAOT_API double REDHAWK_CALLCONV RhpULng2Dbl(uint64_t val)
{
    return (double)val;
}

#endif // HOST_ARM
