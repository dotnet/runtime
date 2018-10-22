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

#include "number-ms.h"
#include "utils/mono-compiler.h"
#include "icalls.h"
#include "icall-decl.h"

ICALL_EXPORT
gdouble
ves_icall_System_Math_Floor (gdouble x)
{
	return floor(x);
}

ICALL_EXPORT
gdouble
ves_icall_System_Math_Round (gdouble x)
{
	gdouble floor_tmp;

	/* If the number has no fractional part do nothing This shortcut is necessary
	 * to workaround precision loss in borderline cases on some platforms */
	if (x == (gdouble)(gint64) x)
		return x;

	floor_tmp = floor (x + 0.5);

	if ((x == (floor (x) + 0.5)) && (fmod (floor_tmp, 2.0) != 0)) {
		floor_tmp -= 1.0;
	}

	return copysign (floor_tmp, x);
}

ICALL_EXPORT
gdouble
ves_icall_System_Math_FMod (gdouble x, gdouble y)
{
	return fmod (x, y);
}

ICALL_EXPORT
gdouble
ves_icall_System_Math_ModF (gdouble x, gdouble *d)
{
	return modf (x, d);
}

ICALL_EXPORT
gdouble 
ves_icall_System_Math_Sin (gdouble x)
{
	return sin (x);
}

ICALL_EXPORT
gdouble 
ves_icall_System_Math_Cos (gdouble x)
{
	return cos (x);
}

ICALL_EXPORT
gdouble 
ves_icall_System_Math_Cbrt (gdouble x)
{
	return cbrt (x);
}

ICALL_EXPORT
gdouble 
ves_icall_System_Math_Tan (gdouble x)
{
	return tan (x);
}

ICALL_EXPORT
gdouble 
ves_icall_System_Math_Sinh (gdouble x)
{
	return sinh (x);
}

ICALL_EXPORT
gdouble 
ves_icall_System_Math_Cosh (gdouble x)
{
	return cosh (x);
}

ICALL_EXPORT
gdouble 
ves_icall_System_Math_Tanh (gdouble x)
{
	return tanh (x);
}

ICALL_EXPORT
gdouble 
ves_icall_System_Math_Acos (gdouble x)
{
	return acos (x);
}

ICALL_EXPORT
gdouble
ves_icall_System_Math_Acosh (gdouble x)
{
	return acosh (x);
}

ICALL_EXPORT
gdouble 
ves_icall_System_Math_Asin (gdouble x)
{
	return asin (x);
}

ICALL_EXPORT
gdouble 
ves_icall_System_Math_Asinh (gdouble x)
{
	return asinh (x);
}

ICALL_EXPORT
gdouble 
ves_icall_System_Math_Atan (gdouble x)
{
	return atan (x);
}

ICALL_EXPORT
gdouble 
ves_icall_System_Math_Atan2 (gdouble y, gdouble x)
{
	return atan2 (y, x);
}

ICALL_EXPORT
gdouble 
ves_icall_System_Math_Atanh (gdouble x)
{
	return atanh (x);
}

ICALL_EXPORT
gdouble 
ves_icall_System_Math_Exp (gdouble x)
{
	return exp (x);
}

ICALL_EXPORT
gdouble 
ves_icall_System_Math_Log (gdouble x)
{
	return log (x);
}

ICALL_EXPORT
gdouble 
ves_icall_System_Math_Log10 (gdouble x)
{
	return log10 (x);
}

ICALL_EXPORT
gdouble 
ves_icall_System_Math_Pow (gdouble x, gdouble y)
{
	return pow (x, y);
}

ICALL_EXPORT
gdouble 
ves_icall_System_Math_Sqrt (gdouble x)
{
	return sqrt (x);
}

ICALL_EXPORT
gdouble
ves_icall_System_Math_Abs_double (gdouble v)
{
	return fabs (v);
}

ICALL_EXPORT
float
ves_icall_System_Math_Abs_single (float v)
{
	return fabsf (v);
}

ICALL_EXPORT
gdouble
ves_icall_System_Math_Ceiling (gdouble v)
{
	return ceil (v);
}

ICALL_EXPORT
float
ves_icall_System_MathF_Acos (float x)
{
	return acosf (x);
}

ICALL_EXPORT
float
ves_icall_System_MathF_Acosh (float x)
{
	return acoshf (x);
}

ICALL_EXPORT
float
ves_icall_System_MathF_Asin (float x)
{
	return asinf (x);
}

ICALL_EXPORT
float
ves_icall_System_MathF_Asinh  (float x)
{
	return asinhf (x);
}

ICALL_EXPORT
float
ves_icall_System_MathF_Atan  (float x)
{
	return atan (x);
}

ICALL_EXPORT
float
ves_icall_System_MathF_Atan2 (float x, float y)
{
	return atan2f (x, y);
}

ICALL_EXPORT
float
ves_icall_System_MathF_Atanh (float x)
{
	return atanhf (x);
}

ICALL_EXPORT
float
ves_icall_System_MathF_Cbrt (float x)
{
	return cbrtf (x);
}

ICALL_EXPORT
float
ves_icall_System_MathF_Ceiling (float x)
{
	return ceilf(x);
}

ICALL_EXPORT
float
ves_icall_System_MathF_Cos (float x)
{
	return cosf (x);
}

ICALL_EXPORT
float
ves_icall_System_MathF_Cosh (float x)
{
	return coshf (x);
}

ICALL_EXPORT
float
ves_icall_System_MathF_Exp (float x)
{
	return expf (x);
}

ICALL_EXPORT
float
ves_icall_System_MathF_Floor (float x)
{
	return floorf (x);
}

ICALL_EXPORT
float
ves_icall_System_MathF_Log (float x)
{
	return logf (x);
}

ICALL_EXPORT
float
ves_icall_System_MathF_Log10 (float x)
{
	return log10f (x);
}

ICALL_EXPORT
float
ves_icall_System_MathF_Pow (float x, float y)
{
	return powf (x, y);
}

ICALL_EXPORT
float
ves_icall_System_MathF_Sin (float x)
{
	return sinf (x);
}

ICALL_EXPORT
float
ves_icall_System_MathF_Sinh (float x)
{
	return sinh (x);
}

ICALL_EXPORT
float
ves_icall_System_MathF_Sqrt (float x)
{
	return sqrtf (x);
}

ICALL_EXPORT
float
ves_icall_System_MathF_Tan (float x)
{
	return tanf (x);
}

ICALL_EXPORT
float
ves_icall_System_MathF_Tanh (float x)
{
	return tanh (x);
}

ICALL_EXPORT
float
ves_icall_System_MathF_FMod (float x, float y)
{
	return fmodf (x, y);
}

ICALL_EXPORT
float
ves_icall_System_MathF_ModF (float x, float *d)
{
	return modff (x, d);
}
