// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _CLRMATH_H_
#define _CLRMATH_H_

#include <stdint.h>
#include <math.h>
#include <float.h>

#ifdef __cplusplus
extern "C" {
#endif

    /********************************/
    /* 7.12.3 Classification Macros */
    /********************************/

    // fpclassify
    // isfinite
    // isinf
    // isnan
    // isnormal
    // signbit

    /**********************************/
    /* 7.12.4 Trigonometric functions */
    /**********************************/

    extern double clrmath_acos (double x);
    extern float  clrmath_acosf(float  x);

    extern double clrmath_asin (double x);
    extern float  clrmath_asinf(float  x);

    extern double clrmath_atan (double x);
    extern float  clrmath_atanf(float  x);

    extern double clrmath_atan2 (double y, double x);
    extern float  clrmath_atan2f(float  y, float  x);

    #define clrmath_cos  cos
    #define clrmath_cosf cosf

    #define clrmath_sin  sin
    #define clrmath_sinf sinf

    #define clrmath_tan  tan
    #define clrmath_tanf tanf

    // extern double clrmath_cos (double x);
    // extern float  clrmath_cosf(float  x);

    // extern double clrmath_sin (double x);
    // extern float  clrmath_sinf(float  x);

    // extern double clrmath_tan (double x);
    // extern float  clrmath_tanf(float  x);

    /*******************************/
    /* 7.12.5 Hyperbolic functions */
    /*******************************/

    extern double clrmath_acosh (double x);
    extern float  clrmath_acoshf(float  x);

    extern double clrmath_asinh (double x);
    extern float  clrmath_asinhf(float  x);

    extern double clrmath_atanh (double x);
    extern float  clrmath_atanhf(float  x);

    extern double clrmath_cosh (double x);
    extern float  clrmath_coshf(float  x);

    extern double clrmath_sinh (double x);
    extern float  clrmath_sinhf(float  x);

    extern double clrmath_tanh (double x);
    extern float  clrmath_tanhf(float  x);

    /************************************************/
    /* 7.12.6 Exponential and logarithmic functions */
    /************************************************/

    extern double clrmath_exp (double x);
    extern float  clrmath_expf(float  x);

    #define clrmath_exp2  exp2
    #define clrmath_exp2f exp2f

    #define clrmath_expm1  expm1
    #define clrmath_expm1f expm1f

    #define clrmath_frexp  frexp
    #define clrmath_frexpf frexpf

    extern int clrmath_ilogb (double x);
    extern int clrmath_ilogbf(float  x);

    #define clrmath_ldexp  ldexp
    #define clrmath_ldexpf ldexpf

    #define clrmath_log  log
    extern float clrmath_logf(float  x);

    #define clrmath_log10  log10
    #define clrmath_log10f log10f

    #define clrmath_logb  logb
    #define clrmath_logbf logbf

    #define clrmath_log1p  log1p
    #define clrmath_log1pf log1pf

    #define clrmath_log2  log2
    #define clrmath_log2f log2f

    #define clrmath_logb  logb
    #define clrmath_logbf logbf

    extern double clrmath_modf (double x, double* iptr);
    extern float  clrmath_modff(float  x, float*  iptr);

    #define clrmath_scalbn  scalbn
    #define clrmath_scalbnf scalbnf

    #define clrmath_scalbln  scalbln
    #define clrmath_scalblnf scalblnf

    /*********************************************/
    /* 7.12.7 Power and absolute-value functions */
    /*********************************************/

    #define clrmath_cbrt  cbrt
    #define clrmath_cbrtf cbrtf

    #define clrmath_fabs  fabs
    #define clrmath_fabsf fabsf

    #define clrmath_hypot  hypot
    #define clrmath_hypotf hypot

    extern double clrmath_pow (double x, double y);
    extern float  clrmath_powf(float  x, float  y);

    #define clrmath_sqrt  sqrt
    #define clrmath_sqrtf sqrtf

    /************************************/
    /* 7.12.8 Error and gamma functions */
    /************************************/

    #define clrmath_erf  erf
    #define clrmath_erff erff

    #define clrmath_erfc  erfc
    #define clrmath_erfcf erfcf

    #define clrmath_lgamma  lgamma
    #define clrmath_lgammaf lgammaf

    #define clrmath_tgamma  tgamma
    #define clrmath_tgammaf tgammaf

    /************************************/
    /* 7.12.9 Nearest integer functions */
    /************************************/

    extern double clrmath_ceil (double x);
    extern float  clrmath_ceilf(float  x);

    extern double clrmath_floor (double x);
    extern float  clrmath_floorf(float  x);

    #define clrmath_nearbyint  nearbyint
    #define clrmath_nearbyintf nearbyintf

    #define clrmath_rint  rint
    #define clrmath_rintf rintf

    #define clrmath_lrint  lrint
    #define clrmath_lrintf lrintf

    #define clrmath_llrint  llrint
    #define clrmath_llrintf llrintf

    #define clrmath_round  round
    #define clrmath_roundf roundf

    #define clrmath_lround  lround
    #define clrmath_lroundf lroundf

    #define clrmath_llround  llround
    #define clrmath_llroundf llroundf

    #define clrmath_trunc  trunc
    #define clrmath_truncf truncf

    /*******************************/
    /* 7.12.10 Remainder functions */
    /*******************************/

    #define clrmath_fmod  fmod
    #define clrmath_fmodf fmodf

    #define clrmath_remainder  remainder
    #define clrmath_remainderf remainderf

    #define clrmath_remquo  remquo
    #define clrmath_remquof remquof

    /**********************************/
    /* 7.12.11 Manipulation functions */
    /**********************************/

    #define clrmath_copysign  _copysign
    #define clrmath_copysignf _copysignf

    #define clrmath_nan  nan
    #define clrmath_nanf nanf

    #define clrmath_nextafter  nextafter
    #define clrmath_nextafterf nextafterf

    #define clrmath_nexttoward  nexttoward
    #define clrmath_nexttowardf nexttowardf

    /***************************************************************/
    /* 7.12.12 Maximum, minimum, and positive difference functions */
    /***************************************************************/

    #define clrmath_fdim  fdim
    #define clrmath_fdimf fdimf

    #define clrmath_fmax  fmax
    #define clrmath_fmaxf fmaxf

    #define clrmath_fmin  fmin
    #define clrmath_fminf fminf

    /*************************************/
    /* 7.12.13 Floating multiply-address */
    /*************************************/

    #define clrmath_fma  fma
    #define clrmath_fmaf fmaf

    /*****************************/
    /* 7.12.14 Comparison Macros */
    /*****************************/

    // isgreater
    // isgreaterequal
    // isless
    // islessequal
    // islessgreater
    // isunordered

#ifdef __cplusplus
}
#endif

#endif // _CLRMATH_H_
