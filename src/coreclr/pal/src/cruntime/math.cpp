// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    math.cpp

Abstract:

    Implementation of math family functions.



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"

#include <math.h>

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
