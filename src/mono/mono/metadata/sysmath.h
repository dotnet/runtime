/*
 * math.h
 *
 * Author:
 *   Dan Lewis (dihlewis@yahoo.co.uk)
 *
 * (C) Ximian, Inc. 2002
 */

#ifndef __METADATA_SYSMATH_H__
#define __METADATA_SYSMATH_H__

#include <config.h>
#include <glib.h>
#include "mono/utils/mono-compiler.h"

extern gdouble ves_icall_System_Math_Floor (gdouble x) MONO_INTERNAL;
extern gdouble ves_icall_System_Math_Round (gdouble x) MONO_INTERNAL;
extern gdouble ves_icall_System_Math_Round2 (gdouble value, gint32 digits, gboolean away_from_zero) MONO_INTERNAL;

extern gdouble 
ves_icall_System_Math_Sin (gdouble x) MONO_INTERNAL;

extern gdouble 
ves_icall_System_Math_Cos (gdouble x) MONO_INTERNAL;

extern gdouble 
ves_icall_System_Math_Tan (gdouble x) MONO_INTERNAL;

extern gdouble 
ves_icall_System_Math_Sinh (gdouble x) MONO_INTERNAL;

extern gdouble 
ves_icall_System_Math_Cosh (gdouble x) MONO_INTERNAL;

extern gdouble 
ves_icall_System_Math_Tanh (gdouble x) MONO_INTERNAL;

extern gdouble 
ves_icall_System_Math_Acos (gdouble x) MONO_INTERNAL;

extern gdouble 
ves_icall_System_Math_Asin (gdouble x) MONO_INTERNAL;

extern gdouble 
ves_icall_System_Math_Atan (gdouble x) MONO_INTERNAL;

extern gdouble 
ves_icall_System_Math_Atan2 (gdouble y, gdouble x) MONO_INTERNAL;

extern gdouble 
ves_icall_System_Math_Exp (gdouble x) MONO_INTERNAL;

extern gdouble 
ves_icall_System_Math_Log (gdouble x) MONO_INTERNAL;

extern gdouble 
ves_icall_System_Math_Log10 (gdouble x) MONO_INTERNAL;

extern gdouble 
ves_icall_System_Math_Pow (gdouble x, gdouble y) MONO_INTERNAL;

extern gdouble 
ves_icall_System_Math_Sqrt (gdouble x) MONO_INTERNAL;

#endif
