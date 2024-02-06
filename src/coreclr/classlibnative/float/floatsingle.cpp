// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: FloatSingle.cpp
//

#include <common.h>
#include "netintrinsics.h"

#include "floatsingle.h"

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

/*=====================================Acos=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Acos, float x)
    FCALL_CONTRACT;

    return netintrinsics_acosf(x);
FCIMPLEND

/*=====================================Acosh====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Acosh, float x)
    FCALL_CONTRACT;

    return acoshf(x);
FCIMPLEND

/*=====================================Asin=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Asin, float x)
    FCALL_CONTRACT;

    return netintrinsics_asinf(x);
FCIMPLEND

/*=====================================Asinh====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Asinh, float x)
    FCALL_CONTRACT;

    return asinhf(x);
FCIMPLEND

/*=====================================Atan=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Atan, float x)
    FCALL_CONTRACT;

    return atanf(x);
FCIMPLEND

/*=====================================Atanh====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Atanh, float x)
    FCALL_CONTRACT;

    return atanhf(x);
FCIMPLEND

/*=====================================Atan2====================================
**
==============================================================================*/
FCIMPL2_VV(float, COMSingle::Atan2, float y, float x)
    FCALL_CONTRACT;

    return netintrinsics_atan2f(y, x);
FCIMPLEND

/*====================================Cbrt======================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Cbrt, float x)
    FCALL_CONTRACT;

    return cbrtf(x);
FCIMPLEND

#if defined(_MSC_VER) && defined(TARGET_AMD64)
// The /fp:fast form of `ceilf` for AMD64 does not correctly handle: `-1.0 < value <= -0.0`
// https://github.com/dotnet/runtime/issues/11003
#pragma float_control(push)
#pragma float_control(precise, on)
#endif

/*====================================Ceil======================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Ceil, float x)
    FCALL_CONTRACT;

    return ceilf(x);
FCIMPLEND

#if defined(_MSC_VER) && defined(TARGET_AMD64)
#pragma float_control(pop)
#endif

/*=====================================Cos======================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Cos, float x)
    FCALL_CONTRACT;

    return cosf(x);
FCIMPLEND

/*=====================================Cosh=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Cosh, float x)
    FCALL_CONTRACT;

    return coshf(x);
FCIMPLEND

/*=====================================Exp======================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Exp, float x)
    FCALL_CONTRACT;

    return netintrinsics_expf(x);
FCIMPLEND

/*====================================Floor=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Floor, float x)
    FCALL_CONTRACT;

    return floorf(x);
FCIMPLEND

/*=====================================FMod=====================================
**
==============================================================================*/
FCIMPL2_VV(float, COMSingle::FMod, float x, float y)
    FCALL_CONTRACT;

    return fmodf(x, y);
FCIMPLEND

/*=====================================FusedMultiplyAdd==========================
**
==============================================================================*/
FCIMPL3_VVV(float, COMSingle::FusedMultiplyAdd, float x, float y, float z)
    FCALL_CONTRACT;

    return fmaf(x, y, z);
FCIMPLEND

/*=====================================Log======================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Log, float x)
    FCALL_CONTRACT;

    return netintrinsics_logf(x);
FCIMPLEND

/*=====================================Log2=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Log2, float x)
    FCALL_CONTRACT;

    return log2f(x);
FCIMPLEND

/*====================================Log10=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Log10, float x)
    FCALL_CONTRACT;

    return netintrinsics_log10f(x);
FCIMPLEND

/*=====================================ModF=====================================
**
==============================================================================*/
FCIMPL2_VI(float, COMSingle::ModF, float x, float* intptr)
    FCALL_CONTRACT;

    return modff(x, intptr);
FCIMPLEND

/*=====================================Pow======================================
**
==============================================================================*/
FCIMPL2_VV(float, COMSingle::Pow, float x, float y)
    FCALL_CONTRACT;

    return netintrinsics_powf(x, y);
FCIMPLEND

/*=====================================Sin======================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Sin, float x)
    FCALL_CONTRACT;

    return sinf(x);
FCIMPLEND

/*====================================SinCos====================================
**
==============================================================================*/
FCIMPL3_VII(void, COMSingle::SinCos, float x, float* pSin, float* pCos)
    FCALL_CONTRACT;

#ifdef _MSC_VER
    *pSin = sinf(x);
    *pCos = cosf(x);
#elif defined(__APPLE__)
    __sincosf(x, pSin, pCos);
#else
    sincosf(x, pSin, pCos);
#endif

FCIMPLEND

/*=====================================Sinh=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Sinh, float x)
    FCALL_CONTRACT;

    return sinhf(x);
FCIMPLEND

/*=====================================Sqrt=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Sqrt, float x)
    FCALL_CONTRACT;

    return sqrtf(x);
FCIMPLEND

/*=====================================Tan======================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Tan, float x)
    FCALL_CONTRACT;

    return tanf(x);
FCIMPLEND

/*=====================================Tanh=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Tanh, float x)
    FCALL_CONTRACT;

    return tanhf(x);
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
