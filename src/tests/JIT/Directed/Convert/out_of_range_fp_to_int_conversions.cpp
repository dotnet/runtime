// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <limits.h>
#include <stdio.h>
#include <stdint.h>
#include <math.h>

#ifdef _MSC_VER
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))
#endif

typedef enum {
    CONVERT_BACKWARD_COMPATIBLE,
    CONVERT_SENTINEL,
    CONVERT_SATURATING,
    CONVERT_NATIVECOMPILERBEHAVIOR,
} FPtoIntegerConversionType;

extern "C" DLLEXPORT int32_t ConvertDoubleToInt32(double x, FPtoIntegerConversionType t)
{
    if (t == CONVERT_NATIVECOMPILERBEHAVIOR)
        return (int32_t)x;

    x = trunc(x); // truncate (round toward zero)

    switch (t) {
    case CONVERT_BACKWARD_COMPATIBLE:
    case CONVERT_SENTINEL:
        return ((x != x) || (x < INT32_MIN) || (x > INT32_MAX)) ? INT32_MIN : (int32_t)x;

    case CONVERT_SATURATING:
        return (x != x) ? 0 : (x < INT32_MIN) ? INT32_MIN : (x > INT32_MAX) ? INT32_MAX : (int32_t)x;
    case CONVERT_NATIVECOMPILERBEHAVIOR: // handled above, but add case to silence warning
        return 0;
    }

    return 0;
}

extern "C" DLLEXPORT uint32_t ConvertDoubleToUInt32(double x, FPtoIntegerConversionType t)
{
    if (t == CONVERT_NATIVECOMPILERBEHAVIOR)
        return (uint32_t)x;

    x = trunc(x); // truncate (round toward zero)
    const double int64_max_plus_1 = 0x1.p63; // 0x43e0000000000000 // (uint64_t)INT64_MAX + 1;

    switch (t) {
    case CONVERT_BACKWARD_COMPATIBLE:
        return ((x != x) || (x < INT64_MIN) || (x >= int64_max_plus_1)) ? 0 : (uint32_t)(int64_t)x;

    case CONVERT_SENTINEL:
        return ((x != x) || (x < 0) || (x > UINT32_MAX)) ? UINT32_MAX  : (uint32_t)x;

    case CONVERT_SATURATING:
        return ((x != x) || (x < 0)) ? 0 : (x > UINT32_MAX) ? UINT32_MAX : (uint32_t)x;
    case CONVERT_NATIVECOMPILERBEHAVIOR: // handled above, but add case to silence warning
        return 0;
    }

    return 0;
}

extern "C" DLLEXPORT int64_t ConvertDoubleToInt64(double x, FPtoIntegerConversionType t)
{
    if (t == CONVERT_NATIVECOMPILERBEHAVIOR)
        return (int64_t)x;

    x = trunc(x); // truncate (round toward zero)

    // (double)INT64_MAX cannot be represented exactly as double
    const double int64_max_plus_1 = 0x1.p63; // 0x43e0000000000000 // (uint64_t)INT64_MAX + 1;
    const double int64_min = -0x1.p63; //       0xC3E0000000000000 // INT64_MIN
    const double uint64_max_plus_1 = 0x1.p64; // 43f0000000000000; // -2.0 * (double)INT64_MIN;
    const double two63 = 0x1.p63; // 0x43e0000000000000 // (uint64_t)INT64_MAX + 1;
    const double int32_min = -0x1.p31;// c1e0000000000000// INT32_MIN;
    const double int32_max = 0x1.fffffffcp30;//  41dfffffffc00000 // INT32_MAX;
    const double int32_max_plus1 = ((double)INT32_MAX) + 1;

    switch (t) {
    case CONVERT_BACKWARD_COMPATIBLE:
    case CONVERT_SENTINEL:
        return ((x != x) || (x < INT64_MIN) || (x >= int64_max_plus_1)) ? INT64_MIN : (int64_t)x;

    case CONVERT_SATURATING:
        return (x != x) ? 0 : (x < INT64_MIN) ? INT64_MIN : (x >= int64_max_plus_1) ? INT64_MAX : (int64_t)x;
    case CONVERT_NATIVECOMPILERBEHAVIOR: // handled above, but add case to silence warning
        return 0;
    }

    return 0;
}

extern "C" DLLEXPORT  uint64_t ConvertDoubleToUInt64(double x, FPtoIntegerConversionType t)
{
    if (t == CONVERT_NATIVECOMPILERBEHAVIOR)
        return (uint64_t)x;

    x = trunc(x); // truncate (round toward zero)

    // (double)UINT64_MAX cannot be represented exactly as double
    const double uint64_max_plus_1 = -2.0 * (double)INT64_MIN;
    // (double)INT64_MAX cannot be represented exactly as double
    const double int64_max_plus_1 = 0x1.p63; // 0x43e0000000000000 // (uint64_t)INT64_MAX + 1;


    switch (t) {
    case CONVERT_BACKWARD_COMPATIBLE:
        return ((x != x) || (x < INT64_MIN) || (x >= uint64_max_plus_1)) ? (uint64_t)INT64_MIN : (x < 0) ? (uint64_t)(int64_t)x : (uint64_t)x;

    case CONVERT_SENTINEL:
        return ((x != x) || (x < 0) || (x >= uint64_max_plus_1)) ? UINT64_MAX : (uint64_t)x;

    case CONVERT_SATURATING:
        return ((x != x) || (x < 0)) ? 0 : (x >= uint64_max_plus_1) ? UINT64_MAX : (uint64_t)x;

    case CONVERT_NATIVECOMPILERBEHAVIOR: // handled above, but add case to silence warning
        return 0;
    }

    return 0;
}

