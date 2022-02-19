// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "rhassert.h"

//
// Floating point and 64-bit integer math helpers.
//

#undef min
#undef max
#include <cmath>
#include <limits>

double PlatformInt64ToDouble(int64_t val)
{
    // Previous versions of compilers have had incorrect implementations here, however
    // all currently supported compiler implementations are believed to be correct.

    return double(val);
}

EXTERN_C NATIVEAOT_API double REDHAWK_CALLCONV RhpInt64ToDouble(int64_t val)
{
    // ** NOTE **
    // This should be kept in sync with with CORINFO_HELP_Int64ToDouble
    // This should be kept in sync with FloatingPointUtils::convertInt64ToDouble
    // ** NOTE **

    return PlatformInt64ToDouble(val);
}

double PlatformUInt64ToDouble(uint64_t val)
{
    // Previous versions of compilers have had incorrect implementations here, however
    // all currently supported compiler implementations are believed to be correct.

    return double(val);
}

EXTERN_C NATIVEAOT_API double REDHAWK_CALLCONV RhpUInt64ToDouble(uint64_t val)
{
    // ** NOTE **
    // This should be kept in sync with CORINFO_HELP_UInt64ToDouble
    // This should be kept in sync with FloatingPointUtils::convertUInt64ToDouble
    // ** NOTE **

    return PlatformUInt64ToDouble(val);
}

int8_t PlatformDoubleToInt8(double val)
{
    // Previous versions of compilers have had incorrect implementations here, however
    // all currently supported compiler implementations are believed to be correct.

    return int8_t(val);
}

EXTERN_C NATIVEAOT_API int8_t REDHAWK_CALLCONV RhpDoubleToInt8(double val)
{
    // ** NOTE **
    // This should be kept in sync with CORINFO_HELP_DoubleToInt8
    // This should be kept in sync with FloatingPointUtils::convertDoubleToInt8
    // ** NOTE **

    if (std::isnan(val)) {
        // NAN should return 0
        return 0;
    }

    if (val <= -129.0) {
        // Too small should saturate to int8::min
        return std::numeric_limits<int8_t>::min();
    }

    if (val >= +128.0) {
        // Too large should saturate to int8::max
        return std::numeric_limits<int8_t>::max();
    }

    return PlatformDoubleToInt8(val);
}

int16_t PlatformDoubleToInt16(double val)
{
    // Previous versions of compilers have had incorrect implementations here, however
    // all currently supported compiler implementations are believed to be correct.

    return int16_t(val);
}

EXTERN_C NATIVEAOT_API int16_t REDHAWK_CALLCONV RhpDoubleToInt16(double val)
{
    // ** NOTE **
    // This should be kept in sync with CORINFO_HELP_DoubleToInt16
    // This should be kept in sync with FloatingPointUtils::convertDoubleToInt16
    // ** NOTE **

    if (std::isnan(val)) {
        // NAN should return 0
        return 0;
    }

    if (val <= -32769.0) {
        // Too small should saturate to int16::min
        return std::numeric_limits<int16_t>::min();
    }

    if (val >= +32768.0) {
        // Too large should saturate to int16::max
        return std::numeric_limits<int16_t>::max();
    }

    return PlatformDoubleToInt16(val);
}

int32_t PlatformDoubleToInt32(double val)
{
    // Previous versions of compilers have had incorrect implementations here, however
    // all currently supported compiler implementations are believed to be correct.

    return int32_t(val);
}

EXTERN_C NATIVEAOT_API int32_t REDHAWK_CALLCONV RhpDoubleToInt32(double val)
{
    // ** NOTE **
    // This should be kept in sync with CORINFO_HELP_DoubleToInt32
    // This should be kept in sync with FloatingPointUtils::convertDoubleToInt32
    // ** NOTE **

    if (std::isnan(val)) {
        // NAN should return 0
        return 0;
    }

    if (val <= -2147483649.0) {
        // Too small should saturate to int32::min
        return std::numeric_limits<int32_t>::min();
    }

    if (val >= +2147483648.0) {
        // Too large should saturate to int32::max
        return std::numeric_limits<int32_t>::max();
    }

    return PlatformDoubleToInt32(val);
}

int64_t PlatformDoubleToInt64(double val)
{
    // Previous versions of compilers have had incorrect implementations here, however
    // all currently supported compiler implementations are believed to be correct.

    return int64_t(val);
}

EXTERN_C NATIVEAOT_API int64_t REDHAWK_CALLCONV RhpDoubleToInt64(double val)
{
    // ** NOTE **
    // This should be kept in sync with CORINFO_HELP_DoubleToInt64
    // This should be kept in sync with FloatingPointUtils::convertDoubleToInt64
    // ** NOTE **

    if (std::isnan(val)) {
        // NAN should return 0
        return 0;
    }

    if (val <= -9223372036854777856.0) {
        // Too small should saturate to int64::min
        return std::numeric_limits<int64_t>::min();
    }

    if (val >= +9223372036854775808.0) {
        // Too large should saturate to int64::max
        return std::numeric_limits<int64_t>::max();
    }

    return PlatformDoubleToInt64(val);
}

uint8_t PlatformDoubleToUInt8(double val)
{
    // Previous versions of compilers have had incorrect implementations here, however
    // all currently supported compiler implementations are believed to be correct.

    return uint8_t(val);
}

EXTERN_C NATIVEAOT_API uint8_t REDHAWK_CALLCONV RhpDoubleToUInt8(double val)
{
    // ** NOTE **
    // This should be kept in sync with CORINFO_HELP_DoubleToUInt8
    // This should be kept in sync with FloatingPointUtils::convertDoubleToUInt8
    // ** NOTE **

    if (std::isnan(val)) {
        // NAN should return 0
        return 0;
    }

    if (val <= -1.0) {
        // Too small should saturate to uint8::min
        return std::numeric_limits<uint8_t>::min();
    }

    if (val >= +256.0) {
        // Too large should saturate to uint8::max
        return std::numeric_limits<uint8_t>::max();
    }

    return PlatformDoubleToUInt8(val);
}

uint16_t PlatformDoubleToUInt16(double val)
{
    // Previous versions of compilers have had incorrect implementations here, however
    // all currently supported compiler implementations are believed to be correct.

    return uint16_t(val);
}

EXTERN_C NATIVEAOT_API uint16_t REDHAWK_CALLCONV RhpDoubleToUInt16(double val)
{
    // ** NOTE **
    // This should be kept in sync with CORINFO_HELP_DoubleToUInt16
    // This should be kept in sync with FloatingPointUtils::convertDoubleToUInt16
    // ** NOTE **

    if (std::isnan(val)) {
        // NAN should return 0
        return 0;
    }

    if (val <= -1.0) {
        // Too small should saturate to uint16::min
        return std::numeric_limits<uint16_t>::min();
    }

    if (val >= +65536.0) {
        // Too large should saturate to uint16::max
        return std::numeric_limits<uint16_t>::max();
    }

    return PlatformDoubleToUInt16(val);
}

uint32_t PlatformDoubleToUInt32(double val)
{
    // Previous versions of compilers have had incorrect implementations here, however
    // all currently supported compiler implementations are believed to be correct.

    return uint32_t(val);
}

EXTERN_C NATIVEAOT_API uint32_t REDHAWK_CALLCONV RhpDoubleToUInt32(double val)
{
    // ** NOTE **
    // This should be kept in sync with CORINFO_HELP_DoubleToUInt32
    // This should be kept in sync with FloatingPointUtils::convertDoubleToUInt32
    // ** NOTE **

    if (std::isnan(val)) {
        // NAN should return 0
        return 0;
    }

    if (val <= -1.0) {
        // Too small should saturate to uint32::min
        return std::numeric_limits<uint32_t>::min();
    }

    if (val >= +4294967296.0) {
        // Too large should saturate to uint32::max
        return std::numeric_limits<uint32_t>::max();
    }

    return PlatformDoubleToUInt32(val);
}

uint64_t PlatformDoubleToUInt64(double val)
{
    // Previous versions of compilers have had incorrect implementations here, however
    // all currently supported compiler implementations are believed to be correct.

    return uint64_t(val);
}

EXTERN_C NATIVEAOT_API uint64_t REDHAWK_CALLCONV RhpDoubleToUInt64(double val)
{
    // ** NOTE **
    // This should be kept in sync with CORINFO_HELP_DoubleToUInt64
    // This should be kept in sync with FloatingPointUtils::convertDoubleToUInt64
    // ** NOTE **

    if (std::isnan(val)) {
        // NAN should return 0
        return 0;
    }

    if (val <= -1.0) {
        // Too small should saturate to uint64::min
        return std::numeric_limits<uint64_t>::min();
    }

    if (val >= +18446744073709551616.0) {
        // Too large values should saturate to uint64::max
        return std::numeric_limits<uint64_t>::max();
    }

    return PlatformDoubleToUInt64(val);
}

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

#endif // HOST_ARM
