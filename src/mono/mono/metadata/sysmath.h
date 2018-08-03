/**
 * \file
 *
 * Author:
 *   Dan Lewis (dihlewis@yahoo.co.uk)
 *   Ludovic Henry (ludovic@xamarin.com)
 *
 * (C) Ximian, Inc. 2002
 * Copyright 2015 Xamarin, Inc (https://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __METADATA_SYSMATH_H__
#define __METADATA_SYSMATH_H__

#include <config.h>
#include <glib.h>
#include <mono/metadata/icalls.h>

ICALL_EXPORT
gdouble
ves_icall_System_Math_Floor (gdouble x);

ICALL_EXPORT
gdouble
ves_icall_System_Math_Round (gdouble x);

ICALL_EXPORT
gdouble
ves_icall_System_Math_Sin (gdouble x);

ICALL_EXPORT
gdouble
ves_icall_System_Math_Cos (gdouble x);

ICALL_EXPORT
gdouble
ves_icall_System_Math_Tan (gdouble x);

ICALL_EXPORT
gdouble
ves_icall_System_Math_Sinh (gdouble x);

ICALL_EXPORT
gdouble
ves_icall_System_Math_Cosh (gdouble x);

ICALL_EXPORT
gdouble
ves_icall_System_Math_Tanh (gdouble x);

ICALL_EXPORT
gdouble
ves_icall_System_Math_Acos (gdouble x);

ICALL_EXPORT
gdouble
ves_icall_System_Math_Asin (gdouble x);

ICALL_EXPORT
gdouble
ves_icall_System_Math_Atan (gdouble x);

ICALL_EXPORT
gdouble
ves_icall_System_Math_Atan2 (gdouble y, gdouble x);

ICALL_EXPORT
gdouble
ves_icall_System_Math_Exp (gdouble x);

ICALL_EXPORT
gdouble
ves_icall_System_Math_Log (gdouble x);

ICALL_EXPORT
gdouble
ves_icall_System_Math_Log10 (gdouble x);

ICALL_EXPORT
gdouble
ves_icall_System_Math_Pow (gdouble x, gdouble y);

ICALL_EXPORT
gdouble
ves_icall_System_Math_Sqrt (gdouble x);

ICALL_EXPORT
gdouble
ves_icall_System_Math_Abs_double (gdouble v);

ICALL_EXPORT
gfloat
ves_icall_System_Math_Abs_single (gfloat v);

ICALL_EXPORT
gdouble
ves_icall_System_Math_SplitFractionDouble (gdouble *v);

ICALL_EXPORT
gdouble
ves_icall_System_Math_Ceiling (gdouble v);

ICALL_EXPORT
float
ves_icall_System_MathF_Acos (float x);

ICALL_EXPORT
float
ves_icall_System_MathF_Acosh (float x);

ICALL_EXPORT
float
ves_icall_System_MathF_Asin (float x);

ICALL_EXPORT
float
ves_icall_System_MathF_Asinh  (float x);

ICALL_EXPORT
float
ves_icall_System_MathF_Atan  (float x);

ICALL_EXPORT
float
ves_icall_System_MathF_Atan2 (float x, float y);

ICALL_EXPORT
float
ves_icall_System_MathF_Atanh (float x);

ICALL_EXPORT
float
ves_icall_System_MathF_Cbrt (float x);

ICALL_EXPORT
float
ves_icall_System_MathF_Ceiling (float x);

ICALL_EXPORT
float
ves_icall_System_MathF_Cos (float x);

ICALL_EXPORT
float
ves_icall_System_MathF_Cosh (float x);

ICALL_EXPORT
float
ves_icall_System_MathF_Exp (float x);

ICALL_EXPORT
float
ves_icall_System_MathF_Floor (float x);

ICALL_EXPORT
float
ves_icall_System_MathF_Log (float x);

ICALL_EXPORT
float
ves_icall_System_MathF_Log10 (float x);

ICALL_EXPORT
float
ves_icall_System_MathF_Pow (float x, float y);

ICALL_EXPORT
float
ves_icall_System_MathF_Sin (float x);

ICALL_EXPORT
float
ves_icall_System_MathF_Sinh (float x);

ICALL_EXPORT
float
ves_icall_System_MathF_Sqrt (float x);

ICALL_EXPORT
float
ves_icall_System_MathF_Tan (float x);

ICALL_EXPORT
float
ves_icall_System_MathF_Tanh (float x);

ICALL_EXPORT
float
ves_icall_System_MathF_FMod (float x, float y);

ICALL_EXPORT
float
ves_icall_System_MathF_ModF (float x, float *d);

#endif
