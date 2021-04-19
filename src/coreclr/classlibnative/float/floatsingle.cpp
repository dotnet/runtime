// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: FloatSingle.cpp
//

#include <common.h>

#include "floatsingle.h"
#include "clrmath.h"

/*=====================================Abs=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Abs, float x)
    FCALL_CONTRACT;

    return clrmath_fabsf(x);
FCIMPLEND

/*=====================================Acos=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Acos, float x)
    FCALL_CONTRACT;

    return clrmath_acosf(x);
FCIMPLEND

/*=====================================Acosh====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Acosh, float x)
    FCALL_CONTRACT;

    return clrmath_acoshf(x);
FCIMPLEND

/*=====================================Asin=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Asin, float x)
    FCALL_CONTRACT;

    return clrmath_asinf(x);
FCIMPLEND

/*=====================================Asinh====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Asinh, float x)
    FCALL_CONTRACT;

    return clrmath_asinhf(x);
FCIMPLEND

/*=====================================Atan=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Atan, float x)
    FCALL_CONTRACT;

    return clrmath_atanf(x);
FCIMPLEND

/*=====================================Atanh====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Atanh, float x)
    FCALL_CONTRACT;

    return clrmath_atanhf(x);
FCIMPLEND

/*=====================================Atan2====================================
**
==============================================================================*/
FCIMPL2_VV(float, COMSingle::Atan2, float y, float x)
    FCALL_CONTRACT;

    return clrmath_atan2f(y, x);
FCIMPLEND

/*====================================Cbrt======================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Cbrt, float x)
    FCALL_CONTRACT;

    return clrmath_cbrtf(x);
FCIMPLEND

/*====================================Ceil======================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Ceil, float x)
    FCALL_CONTRACT;

    return clrmath_ceilf(x);
FCIMPLEND

/*=====================================Cos======================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Cos, float x)
    FCALL_CONTRACT;

    return clrmath_cosf(x);
FCIMPLEND

/*=====================================Cosh=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Cosh, float x)
    FCALL_CONTRACT;

    return clrmath_coshf(x);
FCIMPLEND

/*=====================================Exp======================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Exp, float x)
    FCALL_CONTRACT;

    return clrmath_expf(x);
FCIMPLEND

/*====================================Floor=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Floor, float x)
    FCALL_CONTRACT;

    return clrmath_floorf(x);
FCIMPLEND

/*=====================================FMod=====================================
**
==============================================================================*/
FCIMPL2_VV(float, COMSingle::FMod, float x, float y)
    FCALL_CONTRACT;

    return clrmath_fmodf(x, y);
FCIMPLEND

/*=====================================FusedMultiplyAdd==========================
**
==============================================================================*/
FCIMPL3_VVV(float, COMSingle::FusedMultiplyAdd, float x, float y, float z)
    FCALL_CONTRACT;

    return clrmath_fmaf(x, y, z);
FCIMPLEND

/*=====================================Ilog2====================================
**
==============================================================================*/
FCIMPL1_V(int, COMSingle::ILogB, float x)
    FCALL_CONTRACT;

    return clrmath_ilogbf(x);
FCIMPLEND

/*=====================================Log======================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Log, float x)
    FCALL_CONTRACT;

    return clrmath_logf(x);
FCIMPLEND

/*=====================================Log2=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Log2, float x)
    FCALL_CONTRACT;

    return clrmath_log2f(x);
FCIMPLEND

/*====================================Log10=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Log10, float x)
    FCALL_CONTRACT;

    return log10f(x);
FCIMPLEND

/*=====================================ModF=====================================
**
==============================================================================*/
FCIMPL2_VI(float, COMSingle::ModF, float x, float* intptr)
    FCALL_CONTRACT;

    return clrmath_modff(x, intptr);
FCIMPLEND

/*=====================================Pow======================================
**
==============================================================================*/
FCIMPL2_VV(float, COMSingle::Pow, float x, float y)
    FCALL_CONTRACT;

    return clrmath_powf(x, y);
FCIMPLEND

/*=====================================Sin======================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Sin, float x)
    FCALL_CONTRACT;

    return clrmath_sinf(x);
FCIMPLEND

/*====================================SinCos====================================
**
==============================================================================*/
FCIMPL3_VII(void, COMSingle::SinCos, float x, float* sinr, float* cosr)
    FCALL_CONTRACT;

#ifdef _MSC_VER
    *sinr = sinf(x);
    *cosr = cosf(x);
#else
    sincosf(x, sinr, cosr);
#endif

FCIMPLEND

/*=====================================Sinh=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Sinh, float x)
    FCALL_CONTRACT;

    return clrmath_sinhf(x);
FCIMPLEND

/*=====================================Sqrt=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Sqrt, float x)
    FCALL_CONTRACT;

    return clrmath_sqrtf(x);
FCIMPLEND

/*=====================================Tan======================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Tan, float x)
    FCALL_CONTRACT;

    return clrmath_tanf(x);
FCIMPLEND

/*=====================================Tanh=====================================
**
==============================================================================*/
FCIMPL1_V(float, COMSingle::Tanh, float x)
    FCALL_CONTRACT;

    return clrmath_tanhf(x);
FCIMPLEND
