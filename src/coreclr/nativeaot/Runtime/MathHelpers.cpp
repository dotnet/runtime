// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "rhassert.h"

#undef min
#undef max
#include <cmath>

//
// Floating point and 64-bit integer math helpers.
//

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
    return i >> (j & 0x3f);
}

EXTERN_C NATIVEAOT_API int64_t REDHAWK_CALLCONV RhpLRsh(int64_t i, int32_t j)
{
    return i >> (j & 0x3f);
}

EXTERN_C NATIVEAOT_API int64_t REDHAWK_CALLCONV RhpLLsh(int64_t i, int32_t j)
{
    return i << (j & 0x3f);
}

EXTERN_C NATIVEAOT_API int64_t REDHAWK_CALLCONV RhpDbl2Lng(double val)
{
    return (int64_t)val;
}

EXTERN_C NATIVEAOT_API double REDHAWK_CALLCONV RhpLng2Dbl(int64_t val)
{
    return (double)val;
}

#endif // HOST_ARM
