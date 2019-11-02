// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    math.cpp

Abstract:

    Implementation of math family functions.



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"

#include <math.h>

#if HAVE_IEEEFP_H
#include <ieeefp.h>
#endif  // HAVE_IEEEFP_H

#include <errno.h>

#define PAL_NAN_DBL     sqrt(-1.0)
#define PAL_POSINF_DBL -log(0.0)
#define PAL_NEGINF_DBL  log(0.0)

#define IS_DBL_NEGZERO(x)         (((*((INT64*)((void*)&x))) & I64(0xFFFFFFFFFFFFFFFF)) == I64(0x8000000000000000))

#define PAL_NAN_FLT     sqrtf(-1.0f)
#define PAL_POSINF_FLT -logf(0.0f)
#define PAL_NEGINF_FLT  logf(0.0f)

#define IS_FLT_NEGZERO(x)         (((*((INT32*)((void*)&x))) & 0xFFFFFFFF) == 0x80000000)

SET_DEFAULT_DEBUG_CHANNEL(CRT);

/*++
Function:
  _finite

Determines whether given double-precision floating point value is finite.

Return Value

_finite returns a nonzero value (TRUE) if its argument x is not
infinite, that is, if -INF < x < +INF. It returns 0 (FALSE) if the
argument is infinite or a NaN.

Parameter

x  Double-precision floating-point value

--*/
int __cdecl _finite(double x)
{
    int ret;
    PERF_ENTRY(_finite);
    ENTRY("_finite (x=%f)\n", x);

    ret = isfinite(x);

    LOGEXIT("_finite returns int %d\n", ret);
    PERF_EXIT(_finite);
    return ret;
}

/*++
Function:
  _isnan

See MSDN doc
--*/
int __cdecl _isnan(double x)
{
    int ret;
    PERF_ENTRY(_isnan);
    ENTRY("_isnan (x=%f)\n", x);

    ret = isnan(x);

    LOGEXIT("_isnan returns int %d\n", ret);
    PERF_EXIT(_isnan);
    return ret;
}

/*++
Function:
  _copysign

See MSDN doc
--*/
double __cdecl _copysign(double x, double y)
{
    double ret;
    PERF_ENTRY(_copysign);
    ENTRY("_copysign (x=%f, y=%f)\n", x, y);

    ret = copysign(x, y);

    LOGEXIT("_copysign returns double %f\n", ret);
    PERF_EXIT(_copysign);
    return ret;
}

/*++
Function:
    acos

See MSDN.
--*/
PALIMPORT double __cdecl PAL_acos(double x)
{
    double ret;
    PERF_ENTRY(acos);
    ENTRY("acos (x=%f)\n", x);

#if !HAVE_COMPATIBLE_ACOS
    errno = 0;
#endif  // HAVE_COMPATIBLE_ACOS

    ret = acos(x);

#if !HAVE_COMPATIBLE_ACOS
    if (errno == EDOM)
    {
        ret = PAL_NAN_DBL;  // NaN
    }
#endif  // HAVE_COMPATIBLE_ACOS

    LOGEXIT("acos returns double %f\n", ret);
    PERF_EXIT(acos);
    return ret;
}

/*++
Function:
    acosh

See MSDN.
--*/
PALIMPORT double __cdecl PAL_acosh(double x)
{
    double ret;
    PERF_ENTRY(acosh);
    ENTRY("acosh (x=%f)\n", x);

    ret = acosh(x);

    LOGEXIT("acosh returns double %f\n", ret);
    PERF_EXIT(acosh);
    return ret;
}

/*++
Function:
    asin

See MSDN.
--*/
PALIMPORT double __cdecl PAL_asin(double x)
{
    double ret;
    PERF_ENTRY(asin);
    ENTRY("asin (x=%f)\n", x);

#if !HAVE_COMPATIBLE_ASIN
    errno = 0;
#endif  // HAVE_COMPATIBLE_ASIN

    ret = asin(x);

#if !HAVE_COMPATIBLE_ASIN
    if (errno == EDOM)
    {
        ret = PAL_NAN_DBL;  // NaN
    }
#endif  // HAVE_COMPATIBLE_ASIN

    LOGEXIT("asin returns double %f\n", ret);
    PERF_EXIT(asin);
    return ret;
}

/*++
Function:
    asinh

See MSDN.
--*/
PALIMPORT double __cdecl PAL_asinh(double x)
{
    double ret;
    PERF_ENTRY(asinh);
    ENTRY("asinh (x=%f)\n", x);

    ret = asinh(x);

    LOGEXIT("asinh returns double %f\n", ret);
    PERF_EXIT(asinh);
    return ret;
}

/*++
Function:
    atan2

See MSDN.
--*/
PALIMPORT double __cdecl PAL_atan2(double y, double x)
{
    double ret;
    PERF_ENTRY(atan2);
    ENTRY("atan2 (y=%f, x=%f)\n", y, x);

#if !HAVE_COMPATIBLE_ATAN2
    errno = 0;
#endif  // !HAVE_COMPATIBLE_ATAN2

    ret = atan2(y, x);

#if !HAVE_COMPATIBLE_ATAN2
    if ((errno == EDOM) && (x == 0.0) && (y == 0.0))
    {
        const double sign_x = copysign(1.0, x);
        const double sign_y = copysign(1.0, y);

        if (sign_x > 0)
        {
            ret = copysign(0.0, sign_y);
        }
        else
        {
            ret = copysign(atan2(0.0, -1.0), sign_y);
        }
    }
#endif  // !HAVE_COMPATIBLE_ATAN2

    LOGEXIT("atan2 returns double %f\n", ret);
    PERF_EXIT(atan2);
    return ret;
}

/*++
Function:
    exp

See MSDN.
--*/
PALIMPORT double __cdecl PAL_exp(double x)
{
    double ret;
    PERF_ENTRY(exp);
    ENTRY("exp (x=%f)\n", x);

#if !HAVE_COMPATIBLE_EXP
    if (x == 1.0)
    {
        ret = M_E;
    }
    else
    {
#endif  // HAVE_COMPATIBLE_EXP

    ret = exp(x);

#if !HAVE_COMPATIBLE_EXP
    }
#endif // HAVE_COMPATIBLE_EXP

    LOGEXIT("exp returns double %f\n", ret);
    PERF_EXIT(exp);
    return ret;
}

/*++
Function:
    fma

See MSDN.
--*/
PALIMPORT double __cdecl PAL_fma(double x, double y, double z)
{
    double ret;
    PERF_ENTRY(fma);
    ENTRY("fma (x=%f, y=%f, z=%f)\n", x, y, z);

    ret = fma(x, y, z);

    LOGEXIT("fma returns double %f\n", ret);
    PERF_EXIT(fma);
    return ret;
}

/*++
Function:
    ilogb

See MSDN.
--*/
PALIMPORT int __cdecl PAL_ilogb(double x)
{
    int ret;
    PERF_ENTRY(ilogb);
    ENTRY("ilogb (x=%f)\n", x);

#if !HAVE_COMPATIBLE_ILOGB0
    if (x == 0.0)
    {
        ret = -2147483648;
    }
    else
#endif // !HAVE_COMPATIBLE_ILOGB0

#if !HAVE_COMPATIBLE_ILOGBNAN
    if (isnan(x))
    {
        ret = 2147483647;
    }
    else
#endif // !HAVE_COMPATIBLE_ILOGBNAN

    {
        ret = ilogb(x);
    }

    LOGEXIT("ilogb returns int %d\n", ret);
    PERF_EXIT(ilogb);
    return ret;
}

/*++
Function:
    log

See MSDN.
--*/
PALIMPORT double __cdecl PAL_log(double x)
{
    double ret;
    PERF_ENTRY(log);
    ENTRY("log (x=%f)\n", x);

#if !HAVE_COMPATIBLE_LOG
    errno = 0;
#endif  // !HAVE_COMPATIBLE_LOG

    ret = log(x);

#if !HAVE_COMPATIBLE_LOG
    if ((errno == EDOM) && (x < 0))
    {
        ret = PAL_NAN_DBL;    // NaN
    }
#endif  // !HAVE_COMPATIBLE_LOG

    LOGEXIT("log returns double %f\n", ret);
    PERF_EXIT(log);
    return ret;
}

/*++
Function:
    log2

See MSDN.
--*/
PALIMPORT double __cdecl PAL_log2(double x)
{
    double ret;
    PERF_ENTRY(log2);
    ENTRY("log2 (x=%f)\n", x);

    ret = log2(x);

    LOGEXIT("log2 returns double %f\n", ret);
    PERF_EXIT(log2);
    return ret;
}

/*++
Function:
    log10

See MSDN.
--*/
PALIMPORT double __cdecl PAL_log10(double x)
{
    double ret;
    PERF_ENTRY(log10);
    ENTRY("log10 (x=%f)\n", x);

#if !HAVE_COMPATIBLE_LOG10
    errno = 0;
#endif  // !HAVE_COMPATIBLE_LOG10

    ret = log10(x);

#if !HAVE_COMPATIBLE_LOG10
    if ((errno == EDOM) && (x < 0))
    {
        ret = PAL_NAN_DBL;    // NaN
    }
#endif  // !HAVE_COMPATIBLE_LOG10

    LOGEXIT("log10 returns double %f\n", ret);
    PERF_EXIT(log10);
    return ret;
}

/*++
Function:
    pow

See MSDN.
--*/
PALIMPORT double __cdecl PAL_pow(double x, double y)
{
    double ret;
    PERF_ENTRY(pow);
    ENTRY("pow (x=%f, y=%f)\n", x, y);

#if !HAVE_COMPATIBLE_POW
    if ((y == PAL_POSINF_DBL) && !isnan(x))    // +Inf
    {
        if (x == 1.0)
        {
            ret = x;
        }
        else if (x == -1.0)
        {
            ret = 1.0;
        }
        else if ((x > -1.0) && (x < 1.0))
        {
            ret = 0.0;
        }
        else
        {
            ret = PAL_POSINF_DBL;    // +Inf
        }
    }
    else if ((y == PAL_NEGINF_DBL) && !isnan(x))   // -Inf
    {
        if (x == 1.0)
        {
            ret = x;
        }
        else if (x == -1.0)
        {
            ret = 1.0;
        }
        else if ((x > -1.0) && (x < 1.0))
        {
            ret = PAL_POSINF_DBL;    // +Inf
        }
        else
        {
            ret = 0.0;
        }
    }
    else if (IS_DBL_NEGZERO(x) && (y == -1.0))
    {
        ret = PAL_NEGINF_DBL;    // -Inf
    }
    else if ((x == 0.0) && (y < 0.0))
    {
        ret = PAL_POSINF_DBL;    // +Inf
    }
    else
#endif  // !HAVE_COMPATIBLE_POW

    ret = pow(x, y);

#if !HAVE_VALID_NEGATIVE_INF_POW
    if ((ret == PAL_POSINF_DBL) && (x < 0) && isfinite(x) && (ceil(y / 2) != floor(y / 2)))
    {
        ret = PAL_NEGINF_DBL;   // -Inf
    }
#endif  // !HAVE_VALID_NEGATIVE_INF_POW

#if !HAVE_VALID_POSITIVE_INF_POW
    /*
    * The even/odd test in the if (this one and the one above) used to be ((long long) y % 2 == 0)
    * on SPARC (long long) y for large y (>2**63) is always 0x7fffffff7fffffff, which
    * is an odd number, so the test ((long long) y % 2 == 0) will always fail for
    * large y. Since large double numbers are always even (e.g., the representation of
    * 1E20+1 is the same as that of 1E20, the last .+1. is too insignificant to be part
    * of the representation), this test will always return the wrong result for large y.
    *
    * The (ceil(y/2) == floor(y/2)) test is slower, but more robust.
    */
    if ((ret == PAL_NEGINF_DBL) && (x < 0) && isfinite(x) && (ceil(y / 2) == floor(y / 2)))
    {
        ret = PAL_POSINF_DBL;   // +Inf
    }
#endif  // !HAVE_VALID_POSITIVE_INF_POW

    LOGEXIT("pow returns double %f\n", ret);
    PERF_EXIT(pow);
    return ret;
}

/*++
Function:
    scalbn

See MSDN.
--*/
PALIMPORT double __cdecl PAL_scalbn(double x, int n)
{
    double ret;
    PERF_ENTRY(scalbn);
    ENTRY("scalbn (x=%f, n=%d)\n", x, n);

    ret = scalbn(x, n);

    LOGEXIT("scalbn returns double %f\n", ret);
    PERF_EXIT(scalbn);
    return ret;
}

/*++
Function:
  _finitef

Determines whether given single-precision floating point value is finite.

Return Value

_finitef returns a nonzero value (TRUE) if its argument x is not
infinite, that is, if -INF < x < +INF. It returns 0 (FALSE) if the
argument is infinite or a NaN.

Parameter

x  Single-precision floating-point value

--*/
int __cdecl _finitef(float x)
{
    int ret;
    PERF_ENTRY(_finitef);
    ENTRY("_finitef (x=%f)\n", x);

    ret = isfinite(x);

    LOGEXIT("_finitef returns int %d\n", ret);
    PERF_EXIT(_finitef);
    return ret;
}

/*++
Function:
  _isnanf

See MSDN doc
--*/
int __cdecl _isnanf(float x)
{
    int ret;
    PERF_ENTRY(_isnanf);
    ENTRY("_isnanf (x=%f)\n", x);

    ret = isnan(x);

    LOGEXIT("_isnanf returns int %d\n", ret);
    PERF_EXIT(_isnanf);
    return ret;
}

/*++
Function:
  _copysignf

See MSDN doc
--*/
float __cdecl _copysignf(float x, float y)
{
    float ret;
    PERF_ENTRY(_copysignf);
    ENTRY("_copysignf (x=%f, y=%f)\n", x, y);

    ret = copysign(x, y);

    LOGEXIT("_copysignf returns float %f\n", ret);
    PERF_EXIT(_copysignf);
    return ret;
}

/*++
Function:
    acosf

See MSDN.
--*/
PALIMPORT float __cdecl PAL_acosf(float x)
{
    float ret;
    PERF_ENTRY(acosf);
    ENTRY("acosf (x=%f)\n", x);

#if !HAVE_COMPATIBLE_ACOS
    errno = 0;
#endif  // HAVE_COMPATIBLE_ACOS

    ret = acosf(x);

#if !HAVE_COMPATIBLE_ACOS
    if (errno == EDOM)
    {
        ret = PAL_NAN_FLT;  // NaN
    }
#endif  // HAVE_COMPATIBLE_ACOS

    LOGEXIT("acosf returns float %f\n", ret);
    PERF_EXIT(acosf);
    return ret;
}

/*++
Function:
    acoshf

See MSDN.
--*/
PALIMPORT float __cdecl PAL_acoshf(float x)
{
    float ret;
    PERF_ENTRY(acoshf);
    ENTRY("acoshf (x=%f)\n", x);

    ret = acoshf(x);

    LOGEXIT("acoshf returns float %f\n", ret);
    PERF_EXIT(acoshf);
    return ret;
}

/*++
Function:
    asinf

See MSDN.
--*/
PALIMPORT float __cdecl PAL_asinf(float x)
{
    float ret;
    PERF_ENTRY(asinf);
    ENTRY("asinf (x=%f)\n", x);

#if !HAVE_COMPATIBLE_ASIN
    errno = 0;
#endif  // HAVE_COMPATIBLE_ASIN

    ret = asinf(x);

#if !HAVE_COMPATIBLE_ASIN
    if (errno == EDOM)
    {
        ret = PAL_NAN_FLT;  // NaN
    }
#endif  // HAVE_COMPATIBLE_ASIN

    LOGEXIT("asinf returns float %f\n", ret);
    PERF_EXIT(asinf);
    return ret;
}

/*++
Function:
    asinhf

See MSDN.
--*/
PALIMPORT float __cdecl PAL_asinhf(float x)
{
    float ret;
    PERF_ENTRY(asinhf);
    ENTRY("asinhf (x=%f)\n", x);

    ret = asinhf(x);

    LOGEXIT("asinhf returns float %f\n", ret);
    PERF_EXIT(asinhf);
    return ret;
}


/*++
Function:
    atan2f

See MSDN.
--*/
PALIMPORT float __cdecl PAL_atan2f(float y, float x)
{
    float ret;
    PERF_ENTRY(atan2f);
    ENTRY("atan2f (y=%f, x=%f)\n", y, x);

#if !HAVE_COMPATIBLE_ATAN2
    errno = 0;
#endif  // !HAVE_COMPATIBLE_ATAN2

    ret = atan2f(y, x);

#if !HAVE_COMPATIBLE_ATAN2
    if ((errno == EDOM) && (x == 0.0f) && (y == 0.0f))
    {
        const float sign_x = copysign(1.0f, x);
        const float sign_y = copysign(1.0f, y);

        if (sign_x > 0)
        {
            ret = copysign(0.0f, sign_y);
        }
        else
        {
            ret = copysign(atan2f(0.0f, -1.0f), sign_y);
        }
    }
#endif  // !HAVE_COMPATIBLE_ATAN2

    LOGEXIT("atan2f returns float %f\n", ret);
    PERF_EXIT(atan2f);
    return ret;
}

/*++
Function:
    expf

See MSDN.
--*/
PALIMPORT float __cdecl PAL_expf(float x)
{
    float ret;
    PERF_ENTRY(expf);
    ENTRY("expf (x=%f)\n", x);

#if !HAVE_COMPATIBLE_EXP
    if (x == 1.0f)
    {
        ret = M_E;
    }
    else
    {
#endif  // HAVE_COMPATIBLE_EXP

    ret = expf(x);

#if !HAVE_COMPATIBLE_EXP
    }
#endif // HAVE_COMPATIBLE_EXP

    LOGEXIT("expf returns float %f\n", ret);
    PERF_EXIT(expf);
    return ret;
}

/*++
Function:
    fmaf

See MSDN.
--*/
PALIMPORT float __cdecl PAL_fmaf(float x, float y, float z)
{
    float ret;
    PERF_ENTRY(fmaf);
    ENTRY("fmaf (x=%f, y=%f, z=%f)\n", x, y, z);

    ret = fmaf(x, y, z);

    LOGEXIT("fma returns float %f\n", ret);
    PERF_EXIT(fmaf);
    return ret;
}

/*++
Function:
    ilogbf

See MSDN.
--*/
PALIMPORT int __cdecl PAL_ilogbf(float x)
{
    int ret;
    PERF_ENTRY(ilogbf);
    ENTRY("ilogbf (x=%f)\n", x);

#if !HAVE_COMPATIBLE_ILOGB0
    if (x == 0.0f)
    {
        ret = -2147483648;
    }
    else
#endif // !HAVE_COMPATIBLE_ILOGB0

#if !HAVE_COMPATIBLE_ILOGBNAN
    if (isnan(x))
    {
        ret = 2147483647;
    }
    else
#endif // !HAVE_COMPATIBLE_ILOGBNAN

    {
        ret = ilogbf(x);
    }

    LOGEXIT("ilogbf returns int %d\n", ret);
    PERF_EXIT(ilogbf);
    return ret;
}

/*++
Function:
    logf

See MSDN.
--*/
PALIMPORT float __cdecl PAL_logf(float x)
{
    float ret;
    PERF_ENTRY(logf);
    ENTRY("logf (x=%f)\n", x);

#if !HAVE_COMPATIBLE_LOG
    errno = 0;
#endif  // !HAVE_COMPATIBLE_LOG

    ret = logf(x);

#if !HAVE_COMPATIBLE_LOG
    if ((errno == EDOM) && (x < 0))
    {
        ret = PAL_NAN_FLT;    // NaN
    }
#endif  // !HAVE_COMPATIBLE_LOG

    LOGEXIT("logf returns float %f\n", ret);
    PERF_EXIT(logf);
    return ret;
}

/*++
Function:
    log2f

See MSDN.
--*/
PALIMPORT float __cdecl PAL_log2f(float x)
{
    float ret;
    PERF_ENTRY(log2f);
    ENTRY("log2f (x=%f)\n", x);

    ret = log2f(x);

    LOGEXIT("log2f returns float %f\n", ret);
    PERF_EXIT(log2f);
    return ret;
}

/*++
Function:
    log10f

See MSDN.
--*/
PALIMPORT float __cdecl PAL_log10f(float x)
{
    float ret;
    PERF_ENTRY(log10f);
    ENTRY("log10f (x=%f)\n", x);

#if !HAVE_COMPATIBLE_LOG10
    errno = 0;
#endif  // !HAVE_COMPATIBLE_LOG10

    ret = log10f(x);

#if !HAVE_COMPATIBLE_LOG10
    if ((errno == EDOM) && (x < 0))
    {
        ret = PAL_NAN_FLT;    // NaN
    }
#endif  // !HAVE_COMPATIBLE_LOG10

    LOGEXIT("log10f returns float %f\n", ret);
    PERF_EXIT(log10f);
    return ret;
}

/*++
Function:
    powf

See MSDN.
--*/
PALIMPORT float __cdecl PAL_powf(float x, float y)
{
    float ret;
    PERF_ENTRY(powf);
    ENTRY("powf (x=%f, y=%f)\n", x, y);

#if !HAVE_COMPATIBLE_POW
    if ((y == PAL_POSINF_FLT) && !isnan(x))    // +Inf
    {
        if (x == 1.0f)
        {
            ret = x;
        }
        else if (x == -1.0f)
        {
            ret = 1.0f;
        }
        else if ((x > -1.0f) && (x < 1.0f))
        {
            ret = 0.0f;
        }
        else
        {
            ret = PAL_POSINF_FLT;    // +Inf
        }
    }
    else if ((y == PAL_NEGINF_FLT) && !isnan(x))   // -Inf
    {
        if (x == 1.0f)
        {
            ret = x;
        }
        else if (x == -1.0f)
        {
            ret = 1.0f;
        }
        else if ((x > -1.0f) && (x < 1.0f))
        {
            ret = PAL_POSINF_FLT;    // +Inf
        }
        else
        {
            ret = 0.0f;
        }
    }
    else if (IS_FLT_NEGZERO(x) && (y == -1.0f))
    {
        ret = PAL_NEGINF_FLT;    // -Inf
    }
    else if ((x == 0.0f) && (y < 0.0f))
    {
        ret = PAL_POSINF_FLT;    // +Inf
    }
    else
#endif  // !HAVE_COMPATIBLE_POW

    ret = powf(x, y);

#if !HAVE_VALID_NEGATIVE_INF_POW
    if ((ret == PAL_POSINF_FLT) && (x < 0) && isfinite(x) && (ceilf(y / 2) != floorf(y / 2)))
    {
        ret = PAL_NEGINF_FLT;   // -Inf
    }
#endif  // !HAVE_VALID_NEGATIVE_INF_POW

#if !HAVE_VALID_POSITIVE_INF_POW
    /*
    * The (ceil(y/2) == floor(y/2)) test is slower, but more robust for platforms where large y
    * will return the wrong result for ((long) y % 2 == 0). See PAL_pow(double) above for more details.
    */
    if ((ret == PAL_NEGINF_FLT) && (x < 0) && isfinite(x) && (ceilf(y / 2) == floorf(y / 2)))
    {
        ret = PAL_POSINF_FLT;   // +Inf
    }
#endif  // !HAVE_VALID_POSITIVE_INF_POW

    LOGEXIT("powf returns float %f\n", ret);
    PERF_EXIT(powf);
    return ret;
}

/*++
Function:
    scalbnf

See MSDN.
--*/
PALIMPORT float __cdecl PAL_scalbnf(float x, int n)
{
    float ret;
    PERF_ENTRY(scalbnf);
    ENTRY("scalbnf (x=%f, n=%d)\n", x, n);

    ret = scalbnf(x, n);

    LOGEXIT("scalbnf returns double %f\n", ret);
    PERF_EXIT(scalbnf);
    return ret;
}
