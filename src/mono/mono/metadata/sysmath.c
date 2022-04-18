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

#include "utils/mono-compiler.h"
#include "utils/mono-math.h"
#include "icalls.h"
#include "icall-decl.h"

gdouble
ves_icall_System_Math_Floor (gdouble x)
{
	return floor(x);
}

gdouble
ves_icall_System_Math_Round (gdouble x)
{
	return mono_round_to_even (x);
}

gdouble
ves_icall_System_Math_FMod (gdouble x, gdouble y)
{
	return fmod (x, y);
}

gdouble
ves_icall_System_Math_ModF (gdouble x, gdouble *d)
{
	return modf (x, d);
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
ves_icall_System_Math_Cbrt (gdouble x)
{
	return cbrt (x);
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
	return acos (x);
}

gdouble
ves_icall_System_Math_Acosh (gdouble x)
{
	return acosh (x);
}

gdouble
ves_icall_System_Math_Asin (gdouble x)
{
	return asin (x);
}

gdouble
ves_icall_System_Math_Asinh (gdouble x)
{
	return asinh (x);
}

gdouble
ves_icall_System_Math_Atan (gdouble x)
{
	return atan (x);
}

gdouble
ves_icall_System_Math_Atan2 (gdouble y, gdouble x)
{
	return atan2 (y, x);
}

gdouble
ves_icall_System_Math_Atanh (gdouble x)
{
	return atanh (x);
}

gdouble
ves_icall_System_Math_Exp (gdouble x)
{
	return exp (x);
}

gdouble
ves_icall_System_Math_Log (gdouble x)
{
	return log (x);
}

gdouble
ves_icall_System_Math_Log10 (gdouble x)
{
	return log10 (x);
}

gdouble
ves_icall_System_Math_Pow (gdouble x, gdouble y)
{
	return pow (x, y);
}

gdouble
ves_icall_System_Math_Sqrt (gdouble x)
{
	return sqrt (x);
}

gdouble
ves_icall_System_Math_Ceiling (gdouble v)
{
	return ceil (v);
}

gdouble
ves_icall_System_Math_Log2 (gdouble x)
{
	return log2 (x);
}

gdouble
ves_icall_System_Math_FusedMultiplyAdd (gdouble x, gdouble y, gdouble z)
{
	return fma (x, y, z);
}

float
ves_icall_System_MathF_Acos (float x)
{
	return acosf (x);
}

float
ves_icall_System_MathF_Acosh (float x)
{
	return acoshf (x);
}

float
ves_icall_System_MathF_Asin (float x)
{
	return asinf (x);
}

float
ves_icall_System_MathF_Asinh  (float x)
{
	return asinhf (x);
}

float
ves_icall_System_MathF_Atan  (float x)
{
	return atanf (x);
}

float
ves_icall_System_MathF_Atan2 (float x, float y)
{
	return atan2f (x, y);
}

float
ves_icall_System_MathF_Atanh (float x)
{
	return atanhf (x);
}

float
ves_icall_System_MathF_Cbrt (float x)
{
	return cbrtf (x);
}

float
ves_icall_System_MathF_Ceiling (float x)
{
	return ceilf(x);
}

float
ves_icall_System_MathF_Cos (float x)
{
	return cosf (x);
}

float
ves_icall_System_MathF_Cosh (float x)
{
	return coshf (x);
}

float
ves_icall_System_MathF_Exp (float x)
{
	return expf (x);
}

float
ves_icall_System_MathF_Floor (float x)
{
	return floorf (x);
}

float
ves_icall_System_MathF_Log (float x)
{
	return logf (x);
}

float
ves_icall_System_MathF_Log10 (float x)
{
	return log10f (x);
}

float
ves_icall_System_MathF_Pow (float x, float y)
{
	return powf (x, y);
}

float
ves_icall_System_MathF_Sin (float x)
{
	return sinf (x);
}

float
ves_icall_System_MathF_Sinh (float x)
{
	return sinhf (x);
}

float
ves_icall_System_MathF_Sqrt (float x)
{
	return sqrtf (x);
}

float
ves_icall_System_MathF_Tan (float x)
{
	return tanf (x);
}

float
ves_icall_System_MathF_Tanh (float x)
{
	return tanhf (x);
}

float
ves_icall_System_MathF_FMod (float x, float y)
{
	return fmodf (x, y);
}

float
ves_icall_System_MathF_ModF (float x, float *d)
{
	return modff (x, d);
}

float
ves_icall_System_MathF_Log2 (float x)
{
	return log2f (x);
}

float
ves_icall_System_MathF_FusedMultiplyAdd (float x, float y, float z)
{
	return fmaf (x, y, z);
}
