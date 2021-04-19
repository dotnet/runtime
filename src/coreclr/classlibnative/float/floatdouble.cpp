// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: FloatDouble.cpp
//

#include <common.h>

#include "floatdouble.h"
#include "clrmath.h"

/*=====================================Abs======================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Abs, double x)
    FCALL_CONTRACT;

    return clrmath_fabs(x);
FCIMPLEND

/*=====================================Acos=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Acos, double x)
    FCALL_CONTRACT;

    return clrmath_acos(x);
FCIMPLEND

/*=====================================Acosh====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Acosh, double x)
    FCALL_CONTRACT;

    return clrmath_acosh(x);
FCIMPLEND

/*=====================================Asin=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Asin, double x)
    FCALL_CONTRACT;

    return clrmath_asin(x);
FCIMPLEND

/*=====================================Asinh====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Asinh, double x)
    FCALL_CONTRACT;

    return clrmath_asinh(x);
FCIMPLEND

/*=====================================Atan=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Atan, double x)
    FCALL_CONTRACT;

    return clrmath_atan(x);
FCIMPLEND

/*=====================================Atanh====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Atanh, double x)
    FCALL_CONTRACT;

    return clrmath_atanh(x);
FCIMPLEND

/*=====================================Atan2====================================
**
==============================================================================*/
FCIMPL2_VV(double, COMDouble::Atan2, double y, double x)
    FCALL_CONTRACT;

    return clrmath_atan2(y, x);
FCIMPLEND

/*====================================Cbrt======================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Cbrt, double x)
    FCALL_CONTRACT;

    return clrmath_cbrt(x);
FCIMPLEND

/*====================================Ceil======================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Ceil, double x)
    FCALL_CONTRACT;

    return clrmath_ceil(x);
FCIMPLEND

/*=====================================Cos======================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Cos, double x)
    FCALL_CONTRACT;

    return clrmath_cos(x);
FCIMPLEND

/*=====================================Cosh=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Cosh, double x)
    FCALL_CONTRACT;

    return clrmath_cosh(x);
FCIMPLEND

/*=====================================Exp======================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Exp, double x)
    FCALL_CONTRACT;

    return clrmath_exp(x);
FCIMPLEND

/*====================================Floor=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Floor, double x)
    FCALL_CONTRACT;

    return clrmath_floor(x);
FCIMPLEND

/*=====================================FMod=====================================
**
==============================================================================*/
FCIMPL2_VV(double, COMDouble::FMod, double x, double y)
    FCALL_CONTRACT;

    return clrmath_fmod(x, y);
FCIMPLEND

/*=====================================FusedMultiplyAdd==========================
**
==============================================================================*/
FCIMPL3_VVV(double, COMDouble::FusedMultiplyAdd, double x, double y, double z)
    FCALL_CONTRACT;

    return clrmath_fma(x, y, z);
FCIMPLEND

/*=====================================Ilog2====================================
**
==============================================================================*/
FCIMPL1_V(int, COMDouble::ILogB, double x)
    FCALL_CONTRACT;

    return clrmath_ilogb(x);
FCIMPLEND

/*=====================================Log======================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Log, double x)
    FCALL_CONTRACT;

    return clrmath_log(x);
FCIMPLEND

/*=====================================Log2=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Log2, double x)
    FCALL_CONTRACT;

    return clrmath_log2(x);
FCIMPLEND

/*====================================Log10=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Log10, double x)
    FCALL_CONTRACT;

    return clrmath_log10(x);
FCIMPLEND

/*=====================================ModF=====================================
**
==============================================================================*/
FCIMPL2_VI(double, COMDouble::ModF, double x, double* intptr)
    FCALL_CONTRACT;

    return clrmath_modf(x, intptr);
FCIMPLEND

/*=====================================Pow======================================
**
==============================================================================*/
FCIMPL2_VV(double, COMDouble::Pow, double x, double y)
    FCALL_CONTRACT;

    return clrmath_pow(x, y);
FCIMPLEND

/*=====================================Sin======================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Sin, double x)
    FCALL_CONTRACT;

    return clrmath_sin(x);
FCIMPLEND

/*====================================SinCos====================================
**
==============================================================================*/
FCIMPL3_VII(void, COMDouble::SinCos, double x, double* sinr, double* cosr)
    FCALL_CONTRACT;

#ifdef _MSC_VER
    *sinr = sin(x);
    *cosr = cos(x);
#else
    sincos(x, sinr, cosr);
#endif

FCIMPLEND

/*=====================================Sinh=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Sinh, double x)
    FCALL_CONTRACT;

    return clrmath_sinh(x);
FCIMPLEND

/*=====================================Sqrt=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Sqrt, double x)
    FCALL_CONTRACT;

    return clrmath_sqrt(x);
FCIMPLEND

/*=====================================Tan======================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Tan, double x)
    FCALL_CONTRACT;

    return clrmath_tan(x);
FCIMPLEND

/*=====================================Tanh=====================================
**
==============================================================================*/
FCIMPL1_V(double, COMDouble::Tanh, double x)
    FCALL_CONTRACT;

    return clrmath_tanh(x);
FCIMPLEND
