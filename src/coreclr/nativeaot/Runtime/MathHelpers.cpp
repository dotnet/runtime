// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "rhassert.h"

//
// Floating point and 64-bit integer math helpers.
//

EXTERN_C NativeAOT_API uint64_t NativeAOT_CALLCONV RhpDbl2ULng(double val)
{
    return((uint64_t)val);
}

#undef min
#undef max
#include <cmath>

EXTERN_C NativeAOT_API float NativeAOT_CALLCONV RhpFltRem(float dividend, float divisor)
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

EXTERN_C NativeAOT_API double NativeAOT_CALLCONV RhpDblRem(double dividend, double divisor)
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

EXTERN_C NativeAOT_API double NativeAOT_CALLCONV RhpDblRound(double value)
{
    return round(value);
}

EXTERN_C NativeAOT_API float NativeAOT_CALLCONV RhpFltRound(float value)
{
    return roundf(value);
}

#ifdef HOST_ARM
EXTERN_C NativeAOT_API int32_t NativeAOT_CALLCONV RhpIDiv(int32_t i, int32_t j)
{
    ASSERT(j && "Divide by zero!");
    return i / j;
}

EXTERN_C NativeAOT_API uint32_t NativeAOT_CALLCONV RhpUDiv(uint32_t i, uint32_t j)
{
    ASSERT(j && "Divide by zero!");
    return i / j;
}

EXTERN_C NativeAOT_API int64_t NativeAOT_CALLCONV RhpLDiv(int64_t i, int64_t j)
{
    ASSERT(j && "Divide by zero!");
    return i / j;
}

EXTERN_C NativeAOT_API uint64_t NativeAOT_CALLCONV RhpULDiv(uint64_t i, uint64_t j)
{
    ASSERT(j && "Divide by zero!");
    return i / j;
}

EXTERN_C NativeAOT_API int32_t NativeAOT_CALLCONV RhpIMod(int32_t i, int32_t j)
{
    ASSERT(j && "Divide by zero!");
    return i % j;
}

EXTERN_C NativeAOT_API uint32_t NativeAOT_CALLCONV RhpUMod(uint32_t i, uint32_t j)
{
    ASSERT(j && "Divide by zero!");
    return i % j;
}

EXTERN_C NativeAOT_API int64_t NativeAOT_CALLCONV RhpLMod(int64_t i, int64_t j)
{
    ASSERT(j && "Divide by zero!");
    return i % j;
}

EXTERN_C NativeAOT_API uint64_t NativeAOT_CALLCONV RhpULMod(uint64_t i, uint64_t j)
{
    ASSERT(j && "Divide by zero!");
    return i % j;
}

EXTERN_C NativeAOT_API int64_t NativeAOT_CALLCONV RhpLMul(int64_t i, int64_t j)
{
    return i * j;
}

EXTERN_C NativeAOT_API uint64_t NativeAOT_CALLCONV RhpULMul(uint64_t i, uint64_t j)
{
    return i * j;
}

EXTERN_C NativeAOT_API uint64_t NativeAOT_CALLCONV RhpLRsz(uint64_t i, int32_t j)
{
    return i >> j;
}

EXTERN_C NativeAOT_API int64_t NativeAOT_CALLCONV RhpLRsh(int64_t i, int32_t j)
{
    return i >> j;
}

EXTERN_C NativeAOT_API int64_t NativeAOT_CALLCONV RhpLLsh(int64_t i, int32_t j)
{
    return i << j;
}

EXTERN_C NativeAOT_API int64_t NativeAOT_CALLCONV RhpDbl2Lng(double val)
{
    return (int64_t)val;
}

EXTERN_C NativeAOT_API int32_t NativeAOT_CALLCONV RhpDbl2Int(double val)
{
    return (int32_t)val;
}

EXTERN_C NativeAOT_API uint32_t NativeAOT_CALLCONV RhpDbl2UInt(double val)
{
    return (uint32_t)val;
}

EXTERN_C NativeAOT_API double NativeAOT_CALLCONV RhpLng2Dbl(int64_t val)
{
    return (double)val;
}

EXTERN_C NativeAOT_API double NativeAOT_CALLCONV RhpULng2Dbl(uint64_t val)
{
    return (double)val;
}

#endif // HOST_ARM
