/**
 * \file
* C99 Complex math cross-platform support code
*
* Author:
*	Joao Matos (joao.matos@xamarin.com)
*
* Copyright 2015 Xamarin, Inc (http://www.xamarin.com)
* Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/

#include <config.h>
#include <glib.h>
#define _USE_MATH_DEFINES // needed by MSVC to define math constants
#include <math.h>

#undef cabs
#undef cimag
#undef creal
#define cabs mono_cabs
#define cimag mono_cimag
#define creal mono_creal

typedef struct double_complex {
	double real;
	double imag;
} double_complex;

static inline double
creal (double_complex c)
{
	return c.real;
}

static inline double
cimag (double_complex c)
{
	return c.imag;
}

static inline
double_complex mono_double_complex_make(gdouble re, gdouble im)
{
	double_complex const a = { re, im };
	return a;
}

static inline
double_complex mono_double_complex_scalar_div(double_complex c, gdouble s)
{
	return mono_double_complex_make(creal(c) / s, cimag(c) / s);
}

static inline
double_complex mono_double_complex_scalar_mul(double_complex c, gdouble s)
{
	return mono_double_complex_make(creal(c) * s, cimag(c) * s);
}

static inline
double_complex mono_double_complex_div(double_complex left, double_complex right)
{
	double denom = creal(right) * creal(right) + cimag(right) * cimag(right);

	return mono_double_complex_make(
		(creal(left) * creal(right) + cimag(left) * cimag(right)) / denom,
		(-creal(left) * cimag(right) + cimag(left) * creal(right)) / denom);
}

static inline
double_complex mono_double_complex_sub(double_complex left, double_complex right)
{
	return mono_double_complex_make(creal(left) - creal(right), cimag(left)
		- cimag(right));
}

#include "../support/libm/complex.c"
