// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include <math.h>

#ifdef FEATURE_NATIVEAOT
#include "rhassert.h"
#endif

//
// Floating point and 64-bit integer math helpers.
//

FCIMPL1_D(uint64_t, RhpDbl2ULng, double val)
{
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    const double uint64_max_plus_1 = 4294967296.0 * 4294967296.0;
    return (val > 0) ? ((val >= uint64_max_plus_1) ? UINT64_MAX : (uint64_t)val) : 0;
#else
    return (uint64_t)val;
#endif
}
FCIMPLEND

FCIMPL1_D(int64_t, RhpDbl2Lng, double val)
{
#if defined(TARGET_X86) || defined(TARGET_AMD64) || defined(TARGET_ARM)
    const double int64_min = -2147483648.0 * 4294967296.0;
    const double int64_max = 2147483648.0 * 4294967296.0;
    return (val != val) ? 0 : (val <= int64_min) ? INT64_MIN : (val >= int64_max) ? INT64_MAX : (int64_t)val;
#else
    return (int64_t)val;
#endif
}
FCIMPLEND

FCIMPL2_DD(double, Rhp_fmod, double x, double y)
{
    return fmod(x, y);
}
FCIMPLEND

FCIMPL2_FF(float, Rhp_fmodf, float x, float y)
{
    return fmodf(x, y);
}
FCIMPLEND

#ifdef TARGET_32BIT
FCIMPL2_LL(int64_t, DivInt64Internal, int64_t i, int64_t j)
{
    _ASSERTE(j && "Divide by zero!");
    return i / j;
}
FCIMPLEND

FCIMPL2_LL(uint64_t, DivUInt64Internal, uint64_t i, uint64_t j)
{
    _ASSERTE(j && "Divide by zero!");
    return i / j;
}
FCIMPLEND

FCIMPL2_LL(int64_t, ModInt64Internal, int64_t i, int64_t j)
{
    _ASSERTE(j && "Divide by zero!");
    return i % j;
}
FCIMPLEND

FCIMPL2_LL(uint64_t, ModUInt64Internal, uint64_t i, uint64_t j)
{
    _ASSERTE(j && "Divide by zero!");
    return i % j;
}
FCIMPLEND

FCIMPL2(int32_t, DivInt32Internal, int32_t i, int32_t j)
{
    _ASSERTE(j && "Divide by zero!");
    return i / j;
}
FCIMPLEND

FCIMPL2(uint32_t, DivUInt32Internal, uint32_t i, uint32_t j)
{
    _ASSERTE(j && "Divide by zero!");
    return i / j;
}
FCIMPLEND

FCIMPL2(int32_t, ModInt32Internal, int32_t i, int32_t j)
{
    _ASSERTE(j && "Divide by zero!");
    return i % j;
}
FCIMPLEND

FCIMPL2(uint32_t, ModUInt32Internal, uint32_t i, uint32_t j)
{
    _ASSERTE(j && "Divide by zero!");
    return i % j;
}
FCIMPLEND

FCIMPL1_L(double, RhpLng2Dbl, int64_t val)
{
    return (double)val;
}
FCIMPLEND

FCIMPL1_L(double, RhpULng2Dbl, uint64_t val)
{
    return (double)val;
}
FCIMPLEND

FCIMPL1_L(float, RhpLng2Flt, int64_t val)
{
    return (float)val;
}
FCIMPLEND

FCIMPL1_L(float, RhpULng2Flt, uint64_t val)
{
    return (float)val;
}
FCIMPLEND

FCIMPL2_LL(int64_t, RhpLMul, int64_t val1, int64_t val2)
{
    return val1 * val2;
}
FCIMPLEND

// The x86 shift helpers take the value in EDX:EAX and the shift count in ECX, so they are implemented
// in assembly (i386/MiscStubs).
#ifndef TARGET_X86
FCIMPL2(uint64_t, RhpLLsh, uint64_t num, int32_t shift)
{
    return num << (shift & 0x3F);
}
FCIMPLEND

FCIMPL2(int64_t, RhpLRsh, int64_t num, int32_t shift)
{
    return num >> (shift & 0x3F);
}
FCIMPLEND

FCIMPL2(uint64_t, RhpLRsz, uint64_t num, int32_t shift)
{
    return num >> (shift & 0x3F);
}
FCIMPLEND
#endif // !TARGET_X86
#endif // TARGET_32BIT

// Math and MathF FCalls. On targets other than x86 and wasm, NativeAOT calls the CRT directly instead,
// except for SinCos.

// The default compilation mode is /fp:precise, which disables floating-point intrinsics.
// We enable /fp:fast semantics for the majority of the math functions, as it will speed up performance
// and is really unlikely to cause any other code regressions.

////////////////////////////////////////////////////////////////////////////////////
///
///                         beginning of /fp:fast scope
///
////////////////////////////////////////////////////////////////////////////////////

#ifdef _MSC_VER
#pragma float_control(push)
#pragma float_control(precise, off)
#endif

FCIMPL1_D(double, Rhp_acos, double x)
    return acos(x);
FCIMPLEND

FCIMPL1_F(float, Rhp_acosf, float x)
    return acosf(x);
FCIMPLEND

FCIMPL1_D(double, Rhp_acosh, double x)
    return acosh(x);
FCIMPLEND

FCIMPL1_F(float, Rhp_acoshf, float x)
    return acoshf(x);
FCIMPLEND

FCIMPL1_D(double, Rhp_asin, double x)
    return asin(x);
FCIMPLEND

FCIMPL1_F(float, Rhp_asinf, float x)
    return asinf(x);
FCIMPLEND

FCIMPL1_D(double, Rhp_asinh, double x)
    return asinh(x);
FCIMPLEND

FCIMPL1_F(float, Rhp_asinhf, float x)
    return asinhf(x);
FCIMPLEND

FCIMPL1_D(double, Rhp_atan, double x)
    return atan(x);
FCIMPLEND

FCIMPL1_F(float, Rhp_atanf, float x)
    return atanf(x);
FCIMPLEND

FCIMPL2_DD(double, Rhp_atan2, double y, double x)
    return atan2(y, x);
FCIMPLEND

FCIMPL2_FF(float, Rhp_atan2f, float y, float x)
    return atan2f(y, x);
FCIMPLEND

FCIMPL1_D(double, Rhp_atanh, double x)
    return atanh(x);
FCIMPLEND

FCIMPL1_F(float, Rhp_atanhf, float x)
    return atanhf(x);
FCIMPLEND

FCIMPL1_D(double, Rhp_cbrt, double x)
    return cbrt(x);
FCIMPLEND

FCIMPL1_F(float, Rhp_cbrtf, float x)
    return cbrtf(x);
FCIMPLEND

#if defined(_MSC_VER) && defined(TARGET_AMD64)
// The /fp:fast form of `ceil` and `ceilf` for AMD64 does not correctly handle: `-1.0 < value <= -0.0`
// https://github.com/dotnet/runtime/issues/11003
#pragma float_control(push)
#pragma float_control(precise, on)
#endif

FCIMPL1_D(double, Rhp_ceil, double x)
    return ceil(x);
FCIMPLEND

FCIMPL1_F(float, Rhp_ceilf, float x)
    return ceilf(x);
FCIMPLEND

#if defined(_MSC_VER) && defined(TARGET_AMD64)
#pragma float_control(pop)
#endif

FCIMPL1_D(double, Rhp_cos, double x)
    return cos(x);
FCIMPLEND

FCIMPL1_F(float, Rhp_cosf, float x)
    return cosf(x);
FCIMPLEND

FCIMPL1_D(double, Rhp_cosh, double x)
    return cosh(x);
FCIMPLEND

FCIMPL1_F(float, Rhp_coshf, float x)
    return coshf(x);
FCIMPLEND

FCIMPL1_D(double, Rhp_exp, double x)
    return exp(x);
FCIMPLEND

FCIMPL1_F(float, Rhp_expf, float x)
    return expf(x);
FCIMPLEND

#if defined(_MSC_VER) && defined(TARGET_X86)
// The /fp:fast form of `floor` for x86 does not correctly handle: `-0.0`
// https://github.com/dotnet/runtime/issues/11003
#pragma float_control(push)
#pragma float_control(precise, on)
#endif

FCIMPL1_D(double, Rhp_floor, double x)
    return floor(x);
FCIMPLEND

#if defined(_MSC_VER) && defined(TARGET_X86)
#pragma float_control(pop)
#endif

FCIMPL1_F(float, Rhp_floorf, float x)
    return floorf(x);
FCIMPLEND

FCIMPL1_D(double, Rhp_log, double x)
    return log(x);
FCIMPLEND

FCIMPL1_F(float, Rhp_logf, float x)
    return logf(x);
FCIMPLEND

FCIMPL1_D(double, Rhp_log2, double x)
    return log2(x);
FCIMPLEND

FCIMPL1_F(float, Rhp_log2f, float x)
    return log2f(x);
FCIMPLEND

FCIMPL1_D(double, Rhp_log10, double x)
    return log10(x);
FCIMPLEND

FCIMPL1_F(float, Rhp_log10f, float x)
    return log10f(x);
FCIMPLEND

FCIMPL2_DD(double, Rhp_pow, double x, double y)
#if defined(TARGET_ARM) || defined(TARGET_ARM64)
    // The ARM/ARM64 CRT implementation of `pow` returns `0` rather than `x` for
    // subnormal `x` when `y` is exactly `1`; `pow(x, 1)` is `x` for all `x`
    // https://github.com/dotnet/runtime/issues/12139
    if (y == 1.0)
    {
        return x;
    }
#endif

    return pow(x, y);
FCIMPLEND

FCIMPL2_FF(float, Rhp_powf, float x, float y)
#if defined(TARGET_ARM) || defined(TARGET_ARM64)
    // The ARM/ARM64 CRT implementation of `powf` returns `0` rather than `x` for
    // subnormal `x` when `y` is exactly `1`; `powf(x, 1)` is `x` for all `x`
    // https://github.com/dotnet/runtime/issues/12139
    if (y == 1.0f)
    {
        return x;
    }
#endif

    return powf(x, y);
FCIMPLEND

FCIMPL1_D(double, Rhp_sin, double x)
    return sin(x);
FCIMPLEND

FCIMPL1_F(float, Rhp_sinf, float x)
    return sinf(x);
FCIMPLEND

FCIMPL1_D(double, Rhp_sinh, double x)
    return sinh(x);
FCIMPLEND

FCIMPL1_F(float, Rhp_sinhf, float x)
    return sinhf(x);
FCIMPLEND

FCIMPL1_D(double, Rhp_sqrt, double x)
    return sqrt(x);
FCIMPLEND

FCIMPL1_F(float, Rhp_sqrtf, float x)
    return sqrtf(x);
FCIMPLEND

FCIMPL1_D(double, Rhp_tan, double x)
    return tan(x);
FCIMPLEND

FCIMPL1_F(float, Rhp_tanf, float x)
    return tanf(x);
FCIMPLEND

FCIMPL1_D(double, Rhp_tanh, double x)
    return tanh(x);
FCIMPLEND

FCIMPL1_F(float, Rhp_tanhf, float x)
    return tanhf(x);
FCIMPLEND

FCIMPL3_DDD(double, Rhp_fma, double x, double y, double z)
    return fma(x, y, z);
FCIMPLEND

FCIMPL3_FFF(float, Rhp_fmaf, float x, float y, float z)
    return fmaf(x, y, z);
FCIMPLEND

FCIMPL2_DI(double, Rhp_modf, double x, double* intptr)
    return modf(x, intptr);
FCIMPLEND

FCIMPL2_FI(float, Rhp_modff, float x, float* intptr)
    return modff(x, intptr);
FCIMPLEND

#if defined(_MSC_VER)
// The /fp:fast form of `sincos` for xarch returns sin twice, rather than sincos
// https://developercommunity.visualstudio.com/t/MSVCs-sincos-implementation-is-incorrec/10582378
#pragma float_control(push)
#pragma float_control(precise, on)
#endif

FCIMPL3_DII(void, Rhp_sincos, double x, double* pSin, double* pCos)
#ifdef _MSC_VER
    *pSin = sin(x);
    *pCos = cos(x);
#elif defined(__APPLE__)
    __sincos(x, pSin, pCos);
#else
    sincos(x, pSin, pCos);
#endif
FCIMPLEND

FCIMPL3_FII(void, Rhp_sincosf, float x, float* pSin, float* pCos)
#ifdef _MSC_VER
    *pSin = sinf(x);
    *pCos = cosf(x);
#elif defined(__APPLE__)
    __sincosf(x, pSin, pCos);
#else
    sincosf(x, pSin, pCos);
#endif
FCIMPLEND

#if defined(_MSC_VER)
#pragma float_control(pop)
#endif

#ifdef _MSC_VER
#pragma float_control(pop)
#endif

////////////////////////////////////////////////////////////////////////////////////
///
///                         End of /fp:fast scope
///
////////////////////////////////////////////////////////////////////////////////////
