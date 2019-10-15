// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: FloatSingle.cpp
//

#include <common.h>

#include "floatsingle.h"

// Windows x86 and Windows ARM/ARM64 may not define _isnanf() or _copysignf() but they do
// define _isnan() and _copysign(). We will redirect the macros to these other functions if
// the macro is not defined for the platform. This has the side effect of a possible implicit
// upcasting for arguments passed in and an explicit downcasting for the _copysign() call.
#if (defined(_TARGET_X86_) || defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)) && !defined(FEATURE_PAL)

#if !defined(_copysignf)
#define _copysignf   (float)_copysign
#endif

#endif

// The default compilation mode is /fp:precise, which disables floating-point intrinsics. This
// default compilation mode has previously caused performance regressions in floating-point code.
// We enable /fp:fast semantics for the majority of the math functions, as it will speed up performance
// and is really unlikely to cause any other code regressions.

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

/*=====================================Abs=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Abs, float x)
    FCALL_CONTRACT;

    return (float)fabsf(x);
FCIMPLEND

/*=====================================Acos=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Acos, float x)
    FCALL_CONTRACT;

    return (float)acosf(x);
FCIMPLEND

/*=====================================Acosh====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Acosh, float x)
    FCALL_CONTRACT;

    return (float)acoshf(x);
FCIMPLEND

/*=====================================Asin=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Asin, float x)
    FCALL_CONTRACT;

    return (float)asinf(x);
FCIMPLEND

/*=====================================Asinh====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Asinh, float x)
    FCALL_CONTRACT;

    return (float)asinhf(x);
FCIMPLEND

/*=====================================Atan=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Atan, float x)
    FCALL_CONTRACT;

    return (float)atanf(x);
FCIMPLEND

/*=====================================Atanh====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Atanh, float x)
    FCALL_CONTRACT;

    return (float)atanhf(x);
FCIMPLEND

/*=====================================Atan2====================================
**
==============================================================================*/
FCIMPL2_VV(float, COMSingle::Atan2, float y, float x)
    FCALL_CONTRACT;

    return (float)atan2f(y, x);
FCIMPLEND

/*====================================Cbrt======================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Cbrt, float x)
    FCALL_CONTRACT;

    return (float)cbrtf(x);
FCIMPLEND

#if defined(_MSC_VER) && defined(_TARGET_AMD64_)
// The /fp:fast form of `ceilf` for AMD64 does not correctly handle: `-1.0 < value <= -0.0`
// https://github.com/dotnet/coreclr/issues/19739
#pragma float_control(push)
#pragma float_control(precise, on)
#endif

/*====================================Ceil======================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Ceil, float x)
    FCALL_CONTRACT;

    return (float)ceilf(x);
FCIMPLEND

#if defined(_MSC_VER) && defined(_TARGET_AMD64_)
#pragma float_control(pop)
#endif

/*=====================================Cos======================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Cos, float x)
    FCALL_CONTRACT;

    return (float)cosf(x);
FCIMPLEND

/*=====================================Cosh=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Cosh, float x)
    FCALL_CONTRACT;

    return (float)coshf(x);
FCIMPLEND

/*=====================================Exp======================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Exp, float x)
    FCALL_CONTRACT;

    return (float)expf(x);
FCIMPLEND

/*====================================Floor=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Floor, float x)
    FCALL_CONTRACT;

    return (float)floorf(x);
FCIMPLEND

/*=====================================FMod=====================================
**
==============================================================================*/
FCIMPL2_VV(float, COMSingle::FMod, float x, float y)
    FCALL_CONTRACT;

    return (float)fmodf(x, y);
FCIMPLEND

/*=====================================FusedMultiplyAdd==========================
**
==============================================================================*/
FCIMPL3_VVV(float, COMSingle::FusedMultiplyAdd, float x, float y, float z)
    FCALL_CONTRACT;

    return (float)fmaf(x, y, z);
FCIMPLEND

/*=====================================Ilog2====================================
**
==============================================================================*/
FCIMPL1_V(int, COMSingle::ILogB, float x)
    FCALL_CONTRACT;

    return (int)ilogbf(x);
FCIMPLEND

/*=====================================Log======================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Log, float x)
    FCALL_CONTRACT;

    return (float)logf(x);
FCIMPLEND

/*=====================================Log2=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Log2, float x)
    FCALL_CONTRACT;

    return (float)log2f(x);
FCIMPLEND

/*====================================Log10=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Log10, float x)
    FCALL_CONTRACT;

    return (float)log10f(x);
FCIMPLEND

/*=====================================ModF=====================================
**
==============================================================================*/
FCIMPL2_VI(float, COMSingle::ModF, float x, float* intptr)
    FCALL_CONTRACT;

    return (float)modff(x, intptr);
FCIMPLEND

/*=====================================Pow======================================
**
==============================================================================*/
FCIMPL2_VV(float, COMSingle::Pow, float x, float y)
    FCALL_CONTRACT;

    return (float)powf(x, y);
FCIMPLEND

/*=====================================ScaleB===================================
**
==============================================================================*/
FCIMPL2_VI(float, COMSingle::ScaleB, float x, int n)
    FCALL_CONTRACT;

    return (float)scalbnf(x, n);
FCIMPLEND

/*=====================================Sin======================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Sin, float x)
    FCALL_CONTRACT;

    return (float)sinf(x);
FCIMPLEND

/*=====================================Sinh=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Sinh, float x)
    FCALL_CONTRACT;

    return (float)sinhf(x);
FCIMPLEND

/*=====================================Sqrt=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Sqrt, float x)
    FCALL_CONTRACT;

    return (float)sqrtf(x);
FCIMPLEND

/*=====================================Tan======================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Tan, float x)
    FCALL_CONTRACT;

    return (float)tanf(x);
FCIMPLEND

/*=====================================Tanh=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Tanh, float x)
    FCALL_CONTRACT;

    return (float)tanhf(x);
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
