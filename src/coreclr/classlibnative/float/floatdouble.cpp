// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: FloatDouble.cpp
//

#include <common.h>

#include "floatdouble.h"

// The default compilation mode is /fp:precise, which disables floating-point intrinsics. This
// default compilation mode has previously caused performance regressions in floating-point code.
// We enable /fp:fast semantics for the majority of the math functions, as it will speed up performance
// and is really unlikely to cause any other code regressions.

// Sin, Cos, and Tan on AMD64 Windows were previously implemented in vm\amd64\JitHelpers_Fast.asm
// by calling x87 floating point code (fsin, fcos, fptan) because the CRT helpers were too slow. This
// is no longer the case and the CRT call is used on all platforms.

// Log, Log10 and Exp were previously slower with /fp:fast on SSE2 enabled hardware (see #500373).
// This is no longer the case and they now consume use the /fp:fast versions.

// Exp(+/-INFINITY) did not previously return the expected results of +0.0 (for -INFINITY)
// and +INFINITY (for +INFINITY) so these cases were handled specially. As this is no longer
// the case and the expected results are now returned, the special handling has been removed.

// Previously there was more special handling for the x86 Windows version of Pow.
// This additional handling was unnecessary and has since been removed.

////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////
///
///                         beginning of /fp:fast scope
///
////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////

#ifdef _MSC_VER
#pragma float_control(push)
#pragma float_control(precise, off)
#endif

/*=====================================Acos=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Acos, double x)
    FCALL_CONTRACT;

    return acos(x);
FCIMPLEND

/*=====================================Acosh====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Acosh, double x)
    FCALL_CONTRACT;

    return acosh(x);
FCIMPLEND

/*=====================================Asin=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Asin, double x)
    FCALL_CONTRACT;

    return asin(x);
FCIMPLEND

/*=====================================Asinh====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Asinh, double x)
    FCALL_CONTRACT;

    return asinh(x);
FCIMPLEND

/*=====================================Atan=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Atan, double x)
    FCALL_CONTRACT;

    return atan(x);
FCIMPLEND

/*=====================================Atanh====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Atanh, double x)
    FCALL_CONTRACT;

    return atanh(x);
FCIMPLEND

/*=====================================Atan2====================================
**
==============================================================================*/
FCIMPL2_VV(double, COMDouble::Atan2, double y, double x)
    FCALL_CONTRACT;

    return atan2(y, x);
FCIMPLEND

/*====================================Cbrt======================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Cbrt, double x)
    FCALL_CONTRACT;

    return cbrt(x);
FCIMPLEND

#if defined(_MSC_VER) && defined(TARGET_AMD64)
// The /fp:fast form of `ceil` for AMD64 does not correctly handle: `-1.0 < value <= -0.0`
// https://github.com/dotnet/runtime/issues/11003
#pragma float_control(push)
#pragma float_control(precise, on)
#endif

/*====================================Ceil======================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Ceil, double x)
    FCALL_CONTRACT;

    return ceil(x);
FCIMPLEND

#if defined(_MSC_VER) && defined(TARGET_AMD64)
#pragma float_control(pop)
#endif

/*=====================================Cos======================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Cos, double x)
    FCALL_CONTRACT;

    return cos(x);
FCIMPLEND

/*=====================================Cosh=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Cosh, double x)
    FCALL_CONTRACT;

    return cosh(x);
FCIMPLEND

/*=====================================Exp======================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Exp, double x)
    FCALL_CONTRACT;

    return exp(x);
FCIMPLEND

#if defined(_MSC_VER) && defined(TARGET_X86)
// The /fp:fast form of `floor` for x86 does not correctly handle: `-0.0`
// https://github.com/dotnet/runtime/issues/11003
#pragma float_control(push)
#pragma float_control(precise, on)
#endif

/*====================================Floor=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Floor, double x)
    FCALL_CONTRACT;

    return floor(x);
FCIMPLEND

#if defined(_MSC_VER) && defined(TARGET_X86)
#pragma float_control(pop)
#endif

/*=====================================FMod=====================================
**
==============================================================================*/
FCIMPL2_VV(double, COMDouble::FMod, double x, double y)
    FCALL_CONTRACT;

    return fmod(x, y);
FCIMPLEND

/*=====================================FusedMultiplyAdd==========================
**
==============================================================================*/
FCIMPL3_VVV(double, COMDouble::FusedMultiplyAdd, double x, double y, double z)
    FCALL_CONTRACT;

    return fma(x, y, z);
FCIMPLEND

/*=====================================Log======================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Log, double x)
    FCALL_CONTRACT;

    return log(x);
FCIMPLEND

/*=====================================Log2=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Log2, double x)
    FCALL_CONTRACT;

    return log2(x);
FCIMPLEND

/*====================================Log10=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Log10, double x)
    FCALL_CONTRACT;

    return log10(x);
FCIMPLEND

/*=====================================ModF=====================================
**
==============================================================================*/
FCIMPL2_VI(double, COMDouble::ModF, double x, double* intptr)
    FCALL_CONTRACT;

    return modf(x, intptr);
FCIMPLEND

/*=====================================Pow======================================
**
==============================================================================*/
FCIMPL2_VV(double, COMDouble::Pow, double x, double y)
    FCALL_CONTRACT;

    return pow(x, y);
FCIMPLEND

/*=====================================Sin======================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Sin, double x)
    FCALL_CONTRACT;

    return sin(x);
FCIMPLEND

#if defined(_MSC_VER)
// The /fp:fast form of `sincos` for xarch returns sin twice, rather than sincos
// https://developercommunity.visualstudio.com/t/MSVCs-sincos-implementation-is-incorrec/10582378
#pragma float_control(push)
#pragma float_control(precise, on)
#endif

/*====================================SinCos====================================
**
==============================================================================*/
FCIMPL3_VII(void, COMDouble::SinCos, double x, double* pSin, double* pCos)
    FCALL_CONTRACT;

#ifdef _MSC_VER
    *pSin = sin(x);
    *pCos = cos(x);
#elif defined(__APPLE__)
    __sincos(x, pSin, pCos);
#else
    sincos(x, pSin, pCos);
#endif

FCIMPLEND

#if defined(_MSC_VER)
#pragma float_control(pop)
#endif

/*=====================================Sinh=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Sinh, double x)
    FCALL_CONTRACT;

    return sinh(x);
FCIMPLEND

/*=====================================Sqrt=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Sqrt, double x)
    FCALL_CONTRACT;

    return sqrt(x);
FCIMPLEND

/*=====================================Tan======================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Tan, double x)
    FCALL_CONTRACT;

    return tan(x);
FCIMPLEND

/*=====================================Tanh=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Tanh, double x)
    FCALL_CONTRACT;

    return tanh(x);
FCIMPLEND

#ifdef _MSC_VER
#pragma float_control(pop)
#endif

////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////
///
///                         End of /fp:fast scope
///
////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////
