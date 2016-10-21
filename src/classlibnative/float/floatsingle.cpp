// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: FloatSingle.cpp
//

#include <common.h>

#include "floatsingle.h"

#define IS_FLT_INFINITY(x)         (((*((INT32*)((void*)&x))) & 0x7FFFFFFF) == 0x7F800000)

// Windows x86 and Windows ARM/ARM64 may not define _isnanf() or _copysignf() but they do
// define _isnan() and _copysign(). We will redirect the macros to these other functions if
// the macro is not defined for the platform. This has the side effect of a possible implicit
// upcasting for arguments passed in and an explicit downcasting for the _copysign() call.
#if (defined(_TARGET_X86_) || defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)) && !defined(FEATURE_PAL)

#if !defined(_isnanf)
#define _isnanf      _isnan
#endif

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
#pragma float_control(precise, off)
#endif

/*=====================================Abs=====================================
**
==============================================================================*/
FCIMPL1(float, COMSingle::Abs, float x)
    FCALL_CONTRACT;

    return (float)fabsf(x);
FCIMPLEND

/*=====================================Acos=====================================
**
==============================================================================*/
FCIMPL1(float, COMSingle::Acos, float x)
    FCALL_CONTRACT;

    return (float)acosf(x);
FCIMPLEND

/*=====================================Asin=====================================
**
==============================================================================*/
FCIMPL1(float, COMSingle::Asin, float x)
    FCALL_CONTRACT;

    return (float)asinf(x);
FCIMPLEND

/*=====================================Atan=====================================
**
==============================================================================*/
FCIMPL1(float, COMSingle::Atan, float x)
    FCALL_CONTRACT;

    return (float)atanf(x);
FCIMPLEND

/*=====================================Atan2====================================
**
==============================================================================*/
FCIMPL2(float, COMSingle::Atan2, float y, float x)
    FCALL_CONTRACT;

    // atan2f(+/-INFINITY, +/-INFINITY) produces +/-0.785398163f (x is +INFINITY) and
    // +/-2.35619449f (x is -INFINITY) instead of the expected value of NaN. We handle
    // that case here ourselves.
    if (IS_FLT_INFINITY(y) && IS_FLT_INFINITY(x)) {
        return (float)(y / x);
    }

    return (float)atan2f(y, x);
FCIMPLEND

/*====================================Ceil======================================
**
==============================================================================*/
FCIMPL1(float, COMSingle::Ceil, float x)
    FCALL_CONTRACT;

    return (float)ceilf(x);
FCIMPLEND

/*=====================================Cos======================================
**
==============================================================================*/
FCIMPL1(float, COMSingle::Cos, float x)
    FCALL_CONTRACT;

    return (float)cosf(x);
FCIMPLEND

/*=====================================Cosh=====================================
**
==============================================================================*/
FCIMPL1(float, COMSingle::Cosh, float x)
    FCALL_CONTRACT;

    return (float)coshf(x);
FCIMPLEND

/*=====================================Exp======================================
**
==============================================================================*/
FCIMPL1(float, COMSingle::Exp, float x)
    FCALL_CONTRACT;

    return (float)expf(x);
FCIMPLEND

/*====================================Floor=====================================
**
==============================================================================*/
FCIMPL1(float, COMSingle::Floor, float x)
    FCALL_CONTRACT;

    return (float)floorf(x);
FCIMPLEND

/*=====================================Log======================================
**
==============================================================================*/
FCIMPL1(float, COMSingle::Log, float x)
    FCALL_CONTRACT;

    return (float)logf(x);
FCIMPLEND

/*====================================Log10=====================================
**
==============================================================================*/
FCIMPL1(float, COMSingle::Log10, float x)
    FCALL_CONTRACT;

    return (float)log10f(x);
FCIMPLEND

/*=====================================ModF=====================================
**
==============================================================================*/
FCIMPL1(float, COMSingle::ModF, float* iptr)
    FCALL_CONTRACT;

    return (float)modff(*iptr, iptr);
FCIMPLEND

/*=====================================Pow======================================
**
==============================================================================*/
FCIMPL2(float, COMSingle::Pow, float x, float y)
    FCALL_CONTRACT;

    // The CRT version of pow preserves the NaN payload of x over the NaN payload of y.

    if(_isnanf(y)) {
        return y; // IEEE 754-2008: NaN payload must be preserved
    }

    if(_isnanf(x)) {
        return x; // IEEE 754-2008: NaN payload must be preserved
    }

    // The CRT version of powf does not return NaN for powf(-1.0f, +/-INFINITY) and
    // instead returns +1.0f.

    if(IS_FLT_INFINITY(y) && (x == -1.0f)) {
        INT32 result = CLR_NAN_32;
        return (*((float*)((INT32*)&result)));
    }

    return (float)powf(x, y);
FCIMPLEND

/*====================================Round=====================================
**
==============================================================================*/
FCIMPL1(float, COMSingle::Round, float x)
    FCALL_CONTRACT;

    // If the number has no fractional part do nothing
    // This shortcut is necessary to workaround precision loss in borderline cases on some platforms
    if (x == (float)((INT32)x)) {
        return x;
    }

    // We had a number that was equally close to 2 integers.
    // We need to return the even one.

    float tempVal = (x + 0.5f);
    float flrTempVal = floorf(tempVal);

    if ((flrTempVal == tempVal) && (fmodf(tempVal, 2.0f) != 0)) {
        flrTempVal -= 1.0f;
    }

    return _copysignf(flrTempVal, x);
FCIMPLEND

/*=====================================Sin======================================
**
==============================================================================*/
FCIMPL1(float, COMSingle::Sin, float x)
    FCALL_CONTRACT;

    return (float)sinf(x);
FCIMPLEND

/*=====================================Sinh=====================================
**
==============================================================================*/
FCIMPL1(float, COMSingle::Sinh, float x)
    FCALL_CONTRACT;

    return (float)sinhf(x);
FCIMPLEND

/*=====================================Sqrt=====================================
**
==============================================================================*/
FCIMPL1(float, COMSingle::Sqrt, float x)
    FCALL_CONTRACT;

    return (float)sqrtf(x);
FCIMPLEND

/*=====================================Tan======================================
**
==============================================================================*/
FCIMPL1(float, COMSingle::Tan, float x)
    FCALL_CONTRACT;

    return (float)tanf(x);
FCIMPLEND

/*=====================================Tanh=====================================
**
==============================================================================*/
FCIMPL1(float, COMSingle::Tanh, float x)
    FCALL_CONTRACT;

    return (float)tanhf(x);
FCIMPLEND

#ifdef _MSC_VER
#pragma float_control(precise, on )
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
