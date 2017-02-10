/**
 * \file
 * these are based on bob smith's csharp routines 
 *
 * Author:
 *	Mono Project (http://www.mono-project.com)
 *	Ludovic Henry (ludovic@xamarin.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2015 Xamarin, Inc (https://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Files:
//  - src/classlibnative/float/floatnative.cpp
//  - src/pal/src/cruntime/floatnative.cpp
//
// Ported from C++ to C and adjusted to Mono runtime

#define __USE_ISOC99

#include <math.h>
#include <mono/metadata/sysmath.h>

#include "number-ms.h"
#include "utils/mono-compiler.h"

static const MonoDouble_double NaN = { .s = { .sign = 0x0, .exp = 0x7FF, .mantHi = 0x80000, .mantLo = 0x0 } };

/* +Infinity */
static const MonoDouble_double PInfinity = { .s = { .sign = 0x0, .exp = 0x7FF, .mantHi = 0x0, .mantLo = 0x0 } };

/* -Infinity */
static const MonoDouble_double MInfinity = { .s = { .sign = 0x1, .exp = 0x7FF, .mantHi = 0x0, .mantLo = 0x0 } };

/* +1 */
static const MonoDouble_double POne = { .s = { .sign = 0x0, .exp = 0x3FF, .mantHi = 0x0, .mantLo = 0x0 } };

/* -1 */
static const MonoDouble_double MOne = { .s = { .sign = 0x1, .exp = 0x3FF, .mantHi = 0x0, .mantLo = 0x0 } };

static MONO_ALWAYS_INLINE gboolean
isplusinfinity (gdouble d)
{
	return d == PInfinity.d;
}

static MONO_ALWAYS_INLINE gboolean
isminusinfinity (gdouble d)
{
	return d == MInfinity.d;
}

static MONO_ALWAYS_INLINE gboolean
isinfinity (gdouble d)
{
	return isplusinfinity (d) || isminusinfinity (d);
}

static MONO_ALWAYS_INLINE gboolean
isplusone (gdouble d)
{
	return d == POne.d;
}

static MONO_ALWAYS_INLINE gboolean
isminusone (gdouble d)
{
	return d == MOne.d;
}

gdouble
ves_icall_System_Math_Floor (gdouble x)
{
	return floor(x);
}

gdouble
ves_icall_System_Math_Round (gdouble x)
{
	gdouble tmp, floor_tmp;

	/* If the number has no fractional part do nothing This shortcut is necessary
	 * to workaround precision loss in borderline cases on some platforms */
	if (x == (gdouble)(gint64) x)
		return x;

	tmp = x + 0.5;
	floor_tmp = floor (tmp);

	if (floor_tmp == tmp) {
		if (fmod (tmp, 2.0) != 0)
			floor_tmp -= 1.0;
	}

	return copysign (floor_tmp, x);
}

gdouble 
ves_icall_System_Math_Sin (gdouble x)
{
	return sin (x);
}

gdouble 
ves_icall_System_Math_Cos (gdouble x)
{
	return cos (x);
}

gdouble 
ves_icall_System_Math_Tan (gdouble x)
{
	return tan (x);
}

gdouble 
ves_icall_System_Math_Sinh (gdouble x)
{
	return sinh (x);
}

gdouble 
ves_icall_System_Math_Cosh (gdouble x)
{
	return cosh (x);
}

gdouble 
ves_icall_System_Math_Tanh (gdouble x)
{
	return tanh (x);
}

gdouble 
ves_icall_System_Math_Acos (gdouble x)
{
	if (x < -1 || x > 1)
		return NaN.d;

	return acos (x);
}

gdouble 
ves_icall_System_Math_Asin (gdouble x)
{
	if (x < -1 || x > 1)
		return NaN.d;

	return asin (x);
}

gdouble 
ves_icall_System_Math_Atan (gdouble x)
{
	return atan (x);
}

gdouble 
ves_icall_System_Math_Atan2 (gdouble y, gdouble x)
{
	gdouble result;

	if (isinfinity (x) && isinfinity (y))
		return NaN.d;

	result = atan2 (y, x);
	return result == -0.0 ? 0.0: result;
}

gdouble 
ves_icall_System_Math_Exp (gdouble x)
{
	if (isinfinity (x))
		return x < 0 ? 0.0 : x;

	return exp (x);
}

gdouble 
ves_icall_System_Math_Log (gdouble x)
{
	if (x == 0)
		return MInfinity.d;
	else if (x < 0)
		return NaN.d;

	return log (x);
}

gdouble 
ves_icall_System_Math_Log10 (gdouble x)
{
	if (x == 0)
		return MInfinity.d;
	else if (x < 0)
		return NaN.d;

	return log10 (x);
}

gdouble 
ves_icall_System_Math_Pow (gdouble x, gdouble y)
{
	gdouble result;

	if (isnan (y))
		return y;
	if (isnan (x))
		return x;

	if (isinfinity (y)) {
		if (isplusone (x))
			return x;
		if (isminusone (x))
			return NaN.d;
	}

	/* following are cases from PAL_pow which abstract the implementation of pow for posix and win32 platforms
	 * (https://github.com/dotnet/coreclr/blob/master/src/pal/src/cruntime/finite.cpp#L331) */

	if (isplusinfinity (y) && !isnan (x)) {
		if (isplusone (x) || isminusone (x))
			result = NaN.d;
		else if (x > MOne.d && x < POne.d)
			result = 0.0;
		else
			result = PInfinity.d;
	} else if (isminusinfinity (y) && !isnan (x)) {
		if (isplusone (x) || isminusone (x))
			result = NaN.d;
		if (x > MOne.d && x < POne.d)
			result = PInfinity.d;
		else
			result = 0.0;
	} else if (x == 0.0 && y < 0.0) {
		result = PInfinity.d;
	} else if (y == 0.0 && isnan (x)) {
		/* Windows returns NaN for pow(NaN, 0), but POSIX specifies
		 * a return value of 1 for that case.  We need to return
		 * the same result as Windows. */
		result = NaN.d;
	} else {
		result = pow (x, y);
	}

	if (result == PInfinity.d && x < 0.0 && isfinite (x) && ceil (y / 2) != floor (y / 2))
		result = MInfinity.d;

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
	if (result == MInfinity.d && x < 0.0 && isfinite (x) && ceil (y / 2) == floor (y / 2))
		result = PInfinity.d;

#if defined (__linux__) && SIZEOF_VOID_P == 4
	/* On Linux 32bits, some tests erroneously return NaN */
	if (isnan (result)) {
		if (isminusone (x) && (y > 9007199254740991.0 || y < -9007199254740991.0)) {
			/* Math.Pow (-1, Double.MaxValue) and Math.Pow (-1, Double.MinValue) should return 1 */
			result = POne.d;
		} else if (x < -9007199254740991.0 && y < -9007199254740991.0) {
			/* Math.Pow (Double.MinValue, Double.MinValue) should return 0 */
			result = 0.0;
		} else if (x < -9007199254740991.0 && y > 9007199254740991.0) {
			/* Math.Pow (Double.MinValue, Double.MaxValue) should return Double.PositiveInfinity */
			result = PInfinity.d;
		}
	}
#endif

	return result == -0.0 ? 0 : result;
}

gdouble 
ves_icall_System_Math_Sqrt (gdouble x)
{
	if (x < 0)
		return NaN.d;

	return sqrt (x);
}

gdouble
ves_icall_System_Math_Abs_double (gdouble v)
{
	return fabs (v);
}

gfloat
ves_icall_System_Math_Abs_single (gfloat v)
{
	return fabsf (v);
}

gdouble
ves_icall_System_Math_Ceiling (gdouble v)
{
	return ceil (v);
}

gdouble
ves_icall_System_Math_SplitFractionDouble (gdouble *v)
{
	return modf (*v, v);
}
