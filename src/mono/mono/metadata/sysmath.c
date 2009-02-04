/*
 * sysmath.c: these are based on bob smith's csharp routines 
 *
 * Author:
 *	Mono Project (http://www.mono-project.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 */
#define __USE_ISOC99
#include <math.h>
#include <mono/metadata/sysmath.h>
#include <mono/metadata/exception.h>

#ifndef NAN
# if G_BYTE_ORDER == G_BIG_ENDIAN
#  define __nan_bytes           { 0x7f, 0xc0, 0, 0 }
# endif
# if G_BYTE_ORDER == G_LITTLE_ENDIAN
#  define __nan_bytes           { 0, 0, 0xc0, 0x7f }
# endif

static union { unsigned char __c[4]; float __d; } __nan_union = { __nan_bytes };
# define NAN    (__nan_union.__d)
#endif

#ifndef HUGE_VAL
#define __huge_val_t   union { unsigned char __c[8]; double __d; }
# if G_BYTE_ORDER == G_BIG_ENDIAN
#  define __HUGE_VAL_bytes       { 0x7f, 0xf0, 0, 0, 0, 0, 0, 0 }
# endif
# if G_BYTE_ORDER == G_LITTLE_ENDIAN
#  define __HUGE_VAL_bytes       { 0, 0, 0, 0, 0, 0, 0xf0, 0x7f }
# endif
static __huge_val_t __huge_val = { __HUGE_VAL_bytes };
#  define HUGE_VAL      (__huge_val.__d)
#endif


gdouble ves_icall_System_Math_Floor (gdouble x) {
	MONO_ARCH_SAVE_REGS;
	return floor(x);
}

gdouble ves_icall_System_Math_Round (gdouble x) {
	double int_part, dec_part;
	MONO_ARCH_SAVE_REGS;
	int_part = floor(x);
	dec_part = x - int_part;
	if (((dec_part == 0.5) &&
		((2.0 * ((int_part / 2.0) - floor(int_part / 2.0))) != 0.0)) ||
		(dec_part > 0.5)) {
		int_part++;
	}
	return int_part;
}

gdouble ves_icall_System_Math_Round2 (gdouble value, gint32 digits, gboolean away_from_zero) {
#if !defined (HAVE_ROUND) || !defined (HAVE_RINT)
	double int_part, dec_part;
#endif
	double p;

	MONO_ARCH_SAVE_REGS;
	if (value == HUGE_VAL)
		return HUGE_VAL;
	if (value == -HUGE_VAL)
		return -HUGE_VAL;
	if (digits == 0)
		return ves_icall_System_Math_Round(value);
	p = pow(10, digits);
#if defined (HAVE_ROUND) && defined (HAVE_RINT)
	if (away_from_zero)
		return round (value * p) / p;
	else
		return rint (value * p) / p;
#else
	dec_part = modf (value, &int_part);
	dec_part *= 1000000000000000ULL;
	if (away_from_zero && dec_part > 0)
		dec_part = ceil (dec_part);
	else
		dec_part = floor (dec_part);
	dec_part /= (1000000000000000ULL / p);
	if (away_from_zero) {
		if (dec_part > 0)
			dec_part = floor (dec_part + 0.5);
		else
			dec_part = ceil (dec_part - 0.5);
	} else
		dec_part = ves_icall_System_Math_Round (dec_part);
	dec_part /= p;
	return ves_icall_System_Math_Round ((int_part + dec_part) * p) / p;
#endif
}

gdouble 
ves_icall_System_Math_Sin (gdouble x)
{
	MONO_ARCH_SAVE_REGS;

	return sin (x);
}

gdouble 
ves_icall_System_Math_Cos (gdouble x)
{
	MONO_ARCH_SAVE_REGS;

	return cos (x);
}

gdouble 
ves_icall_System_Math_Tan (gdouble x)
{
	MONO_ARCH_SAVE_REGS;

	return tan (x);
}

gdouble 
ves_icall_System_Math_Sinh (gdouble x)
{
	MONO_ARCH_SAVE_REGS;

	return sinh (x);
}

gdouble 
ves_icall_System_Math_Cosh (gdouble x)
{
	MONO_ARCH_SAVE_REGS;

	return cosh (x);
}

gdouble 
ves_icall_System_Math_Tanh (gdouble x)
{
	MONO_ARCH_SAVE_REGS;

	return tanh (x);
}

gdouble 
ves_icall_System_Math_Acos (gdouble x)
{
	MONO_ARCH_SAVE_REGS;

	if (x < -1 || x > 1)
		return NAN;

	return acos (x);
}

gdouble 
ves_icall_System_Math_Asin (gdouble x)
{
	MONO_ARCH_SAVE_REGS;

	if (x < -1 || x > 1)
		return NAN;

	return asin (x);
}

gdouble 
ves_icall_System_Math_Atan (gdouble x)
{
	MONO_ARCH_SAVE_REGS;

	return atan (x);
}

gdouble 
ves_icall_System_Math_Atan2 (gdouble y, gdouble x)
{
	double result;
	MONO_ARCH_SAVE_REGS;

	if ((y == HUGE_VAL && x == HUGE_VAL) ||
		(y == HUGE_VAL && x == -HUGE_VAL) ||
		(y == -HUGE_VAL && x == HUGE_VAL) ||
		(y == -HUGE_VAL && x == -HUGE_VAL)) {
		return NAN;
	}
	result = atan2 (y, x);
	return (result == -0)? 0: result;
}

gdouble 
ves_icall_System_Math_Exp (gdouble x)
{
	MONO_ARCH_SAVE_REGS;

	return exp (x);
}

gdouble 
ves_icall_System_Math_Log (gdouble x)
{
	MONO_ARCH_SAVE_REGS;

	if (x == 0)
		return -HUGE_VAL;
	else if (x < 0)
		return NAN;

	return log (x);
}

gdouble 
ves_icall_System_Math_Log10 (gdouble x)
{
	MONO_ARCH_SAVE_REGS;

	if (x == 0)
		return -HUGE_VAL;
	else if (x < 0)
		return NAN;

	return log10 (x);
}

gdouble 
ves_icall_System_Math_Pow (gdouble x, gdouble y)
{
	double result;
	MONO_ARCH_SAVE_REGS;

	if (isnan(x) || isnan(y)) {
		return NAN;
	}

	if ((x == 1 || x == -1) && (y == HUGE_VAL || y == -HUGE_VAL)) {
		return NAN;
	}

	/* This code is for return the same results as MS.NET for certain
	 * limit values */
	if (x < -9007199254740991.0) {
		if (y > 9007199254740991.0)
			return HUGE_VAL;
		if (y < -9007199254740991.0)
			return 0;
	}

	result = pow (x, y);

	/* This code is for return the same results as MS.NET for certain
	 * limit values */
	if (isnan(result) &&
		(x == -1.0) &&
		((y > 9007199254740991.0) || (y < -9007199254740991.0))) {
		return 1;
	}

	return (result == -0)? 0: result;
}

gdouble 
ves_icall_System_Math_Sqrt (gdouble x)
{
	MONO_ARCH_SAVE_REGS;

	if (x < 0)
		return NAN;

	return sqrt (x);
}
