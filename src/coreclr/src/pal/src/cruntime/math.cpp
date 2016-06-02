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

#if defined(_IA64_) && defined (_HPUX_)
    ret = !isnan(x) && (x != PAL_POSINF_DBL) && (x != PAL_NEGINF_DBL);
#else
    ret = isfinite(x);
#endif

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
    labs

See MSDN.
--*/
PALIMPORT LONG __cdecl PAL_labs(LONG l)
{
    long lRet;
    PERF_ENTRY(labs);
    ENTRY("labs (l=%ld)\n", l);
    
    lRet = labs(l);    

    LOGEXIT("labs returns long %ld\n", lRet);
    PERF_EXIT(labs);
    return (LONG)lRet; // This explicit cast to LONG is used to silence any potential warnings due to implicitly casting the native long lRet to LONG when returning.
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
            ret = PAL_NAN_DBL;    // NaN
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
            ret = PAL_NAN_DBL;    // NaN
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

    if ((y == 0.0) && isnan(x))
    {
        // Windows returns NaN for pow(NaN, 0), but POSIX specifies
        // a return value of 1 for that case.  We need to return
        // the same result as Windows.
        ret = PAL_NAN_DBL;
    }
    else
    {
        ret = pow(x, y);
    }

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
