// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "rhassert.h"

//
// Floating point and 64-bit integer math helpers.
//


EXTERN_C NATIVEAOT_API int64_t REDHAWK_CALLCONV RhpDbl2Lng(double val)
{
#if defined(HOST_X86) || defined(HOST_AMD64)
    const double int64_min = (double)INT64_MIN;
    const double int64_max = (double)INT64_MAX;
    return (val!= val) ? 0 : (val <= int64_min) ? INT64_MIN : (val >= int64_max) ? INT64_MAX : (int64_t)val;
#else
    return (int64_t)val;
#endif //HOST_X86 || HOST_AMD64
}

EXTERN_C NATIVEAOT_API int32_t REDHAWK_CALLCONV RhpDbl2Int(double val)
{
#if defined(HOST_X86) || defined(HOST_AMD64)
    const double int32_min = (double)INT32_MIN - 1.0;
    const double int32_max = -2.0 * (double)INT32_MIN;
    return (val!= val) ? 0 : (val <= int32_min) ? INT32_MIN : (val >= int32_max) ? INT32_MAX : (int32_t)val;
#else
    return (int32_t)val;
#endif //HOST_X86 || HOST_AMD64
}

EXTERN_C NATIVEAOT_API uint32_t REDHAWK_CALLCONV RhpDbl2UInt(double val)
{
#if defined(HOST_X86) || defined(HOST_AMD64)
    const double uint32_max_plus_1 = -2.0 * (double)INT32_MIN;
    return (val < 0) ? 0 : (val != val || val >= uint32_max_plus_1) ? UINT32_MAX : (uint32_t)val;
#else
    return (uint32_t)val;
#endif //HOST_X86 || HOST_AMD64
}

EXTERN_C NATIVEAOT_API uint32_t REDHAWK_CALLCONV RhpFlt2UInt(float val)
{
#if defined(HOST_X86) || defined(HOST_AMD64)
    const float uint32_max_plus_1 = -2.0 * (float)INT32_MIN;
    return (val != val || val < 0) ? 0 : (val >= uint32_max_plus_1) ? UINT32_MAX : (uint32_t)val;
#else
    return (uint32_t)val;
#endif //HOST_X86 || HOST_AMD64
}

#undef min
#undef max
#include <cmath>

//
// Floating point and 64-bit integer math helpers.
//

#ifdef HOST_ARM
EXTERN_C int32_t REDHAWK_CALLCONV RhpIDiv(int32_t i, int32_t j)
{
    ASSERT(j && "Divide by zero!");
    return i / j;
}

EXTERN_C uint32_t REDHAWK_CALLCONV RhpUDiv(uint32_t i, uint32_t j)
{
    ASSERT(j && "Divide by zero!");
    return i / j;
}

EXTERN_C int64_t REDHAWK_CALLCONV RhpLDiv(int64_t i, int64_t j)
{
    ASSERT(j && "Divide by zero!");
    return i / j;
}

EXTERN_C uint64_t REDHAWK_CALLCONV RhpULDiv(uint64_t i, uint64_t j)
{
    ASSERT(j && "Divide by zero!");
    return i / j;
}

EXTERN_C int32_t REDHAWK_CALLCONV RhpIMod(int32_t i, int32_t j)
{
    ASSERT(j && "Divide by zero!");
    return i % j;
}

EXTERN_C uint32_t REDHAWK_CALLCONV RhpUMod(uint32_t i, uint32_t j)
{
    ASSERT(j && "Divide by zero!");
    return i % j;
}

EXTERN_C int64_t REDHAWK_CALLCONV RhpLMod(int64_t i, int64_t j)
{
    ASSERT(j && "Divide by zero!");
    return i % j;
}

EXTERN_C uint64_t REDHAWK_CALLCONV RhpULMod(uint64_t i, uint64_t j)
{
    ASSERT(j && "Divide by zero!");
    return i % j;
}

EXTERN_C int64_t REDHAWK_CALLCONV RhpLMul(int64_t i, int64_t j)
{
    return i * j;
}

EXTERN_C uint64_t REDHAWK_CALLCONV RhpLRsz(uint64_t i, int32_t j)
{
    return i >> (j & 0x3f);
}

EXTERN_C int64_t REDHAWK_CALLCONV RhpLRsh(int64_t i, int32_t j)
{
    return i >> (j & 0x3f);
}

EXTERN_C int64_t REDHAWK_CALLCONV RhpLLsh(int64_t i, int32_t j)
{
    return i << (j & 0x3f);
}

EXTERN_C int64_t REDHAWK_CALLCONV RhpDbl2Lng(double val)
{
    return (int64_t)val;
}

EXTERN_C NATIVEAOT_API double REDHAWK_CALLCONV RhpLng2Dbl(int64_t val)
{
    return (double)val;
}

#endif // HOST_ARM
