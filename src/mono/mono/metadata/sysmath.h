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

extern gdouble ves_icall_System_Math_Floor (gdouble x);
extern gdouble ves_icall_System_Math_Round (gdouble x);
extern gdouble ves_icall_System_Math_Round2 (gdouble value, gint32 digits);

extern gdouble 
ves_icall_System_Math_Sin (gdouble x);

extern gdouble 
ves_icall_System_Math_Cos (gdouble x);

extern gdouble 
ves_icall_System_Math_Tan (gdouble x);

extern gdouble 
ves_icall_System_Math_Sinh (gdouble x);

extern gdouble 
ves_icall_System_Math_Cosh (gdouble x);

extern gdouble 
ves_icall_System_Math_Tanh (gdouble x);

extern gdouble 
ves_icall_System_Math_Acos (gdouble x);

extern gdouble 
ves_icall_System_Math_Asin (gdouble x);

extern gdouble 
ves_icall_System_Math_Atan (gdouble x);

extern gdouble 
ves_icall_System_Math_Atan2 (gdouble y, gdouble x);

extern gdouble 
ves_icall_System_Math_Exp (gdouble x);

extern gdouble 
ves_icall_System_Math_Log (gdouble x);

extern gdouble 
ves_icall_System_Math_Log10 (gdouble x);

extern gdouble 
ves_icall_System_Math_Pow (gdouble x, gdouble y);

extern gdouble 
ves_icall_System_Math_Sqrt (gdouble x);

#endif
