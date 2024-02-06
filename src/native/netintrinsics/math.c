// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    math.cpp

Abstract:

    Implementation of math family functions.



--*/

#include <math.h>
#include <stdint.h>

#if HAVE_IEEEFP_H
#include <ieeefp.h>
#endif  // HAVE_IEEEFP_H

#include <errno.h>
#include "config.h"

#define PAL_NAN_DBL     sqrt(-1.0)
#define PAL_POSINF_DBL -log(0.0)
#define PAL_NEGINF_DBL  log(0.0)

#define IS_DBL_NEGZERO(x)         (((*((int64_t*)((void*)&x))) & 0xFFFFFFFFFFFFFFFFLL) == 0x8000000000000000LL)

#define PAL_NAN_FLT     sqrtf(-1.0f)
#define PAL_POSINF_FLT -logf(0.0f)
#define PAL_NEGINF_FLT  logf(0.0f)

#define IS_FLT_NEGZERO(x)         (((*((INT32*)((void*)&x))) & 0xFFFFFFFF) == 0x80000000)

double netintrinsics_acos(double x)
{
    double ret;

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

    return ret;
}

double netintrinsics_asin(double x)
{
    double ret;

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

    return ret;
}

double netintrinsics_atan2(double y, double x)
{
    double ret;

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

    return ret;
}

double netintrinsics_exp(double x)
{
    double ret;

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

    return ret;
}

int netintrinsics_ilogb(double x)
{
    int ret;
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
    return ret;
}

double netintrinsics_log(double x)
{
    double ret;
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

    return ret;
}

double netintrinsics_log10(double x)
{
    double ret;

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

    return ret;
}

double netintrinsics_pow(double x, double y)
{
    double ret;

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

    return ret;
}

float netintrinsics_acosf(float x)
{
    float ret;

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

    return ret;
}

float netintrinsics_asinf(float x)
{
    float ret;

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

    return ret;
}

float netintrinsics_atan2f(float y, float x)
{
    float ret;

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

    return ret;
}

float netintrinsics_expf(float x)
{
    float ret;

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

    return ret;
}

int netintrinsics_ilogbf(float x)
{
    int ret;

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

    return ret;
}

float netintrinsics_logf(float x)
{
    float ret;

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

    return ret;
}

float netintrinsics_log10f(float x)
{
    float ret;

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

    return ret;
}

float netintrinsics_powf(float x, float y)
{
    float ret;

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

    return ret;
}
