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

gdouble
ves_icall_System_Math_Floor (gdouble x);

gdouble
ves_icall_System_Math_Round (gdouble x);

gdouble
ves_icall_System_Math_Sin (gdouble x);

gdouble
ves_icall_System_Math_Cos (gdouble x);

gdouble
ves_icall_System_Math_Tan (gdouble x);

gdouble
ves_icall_System_Math_Sinh (gdouble x);

gdouble
ves_icall_System_Math_Cosh (gdouble x);

gdouble
ves_icall_System_Math_Tanh (gdouble x);

gdouble
ves_icall_System_Math_Acos (gdouble x);

gdouble
ves_icall_System_Math_Asin (gdouble x);

gdouble
ves_icall_System_Math_Atan (gdouble x);

gdouble
ves_icall_System_Math_Atan2 (gdouble y, gdouble x);

gdouble
ves_icall_System_Math_Exp (gdouble x);

gdouble
ves_icall_System_Math_Log (gdouble x);

gdouble
ves_icall_System_Math_Log10 (gdouble x);

gdouble
ves_icall_System_Math_Pow (gdouble x, gdouble y);

gdouble
ves_icall_System_Math_Sqrt (gdouble x);

gdouble
ves_icall_System_Math_Abs_double (gdouble v);

gfloat
ves_icall_System_Math_Abs_single (gfloat v);

gdouble
ves_icall_System_Math_SplitFractionDouble (gdouble *v);

gdouble
ves_icall_System_Math_Ceiling (gdouble v);

#endif
