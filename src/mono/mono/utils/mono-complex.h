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

#if !defined (HAVE_COMPLEX_H) || (defined (ANDROID_UNIFIED_HEADERS) && __ANDROID_API__ < 23)
#include <../../support/libm/complex.h>
#else
#include <complex.h>
#endif

#define _USE_MATH_DEFINES // needed by MSVC to define math constants
#include <math.h>

#ifdef _MSC_VER

#define double_complex _C_double_complex

static inline
double_complex mono_double_complex_make(gdouble re, gdouble im)
{
	return _Cbuild (re, im);
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

#else

#define double_complex double complex

static inline
double_complex mono_double_complex_make(gdouble re, gdouble im)
{
	return re + im * I;
}

static inline
double_complex mono_double_complex_scalar_div(double_complex c, gdouble s)
{
	return c / s;
}

static inline
double_complex mono_double_complex_scalar_mul(double_complex c, gdouble s)
{
	return c * s;
}

static inline
double_complex mono_double_complex_div(double_complex left, double_complex right)
{
	return left / right;
}

static inline
double_complex mono_double_complex_sub(double_complex left, double_complex right)
{
	return left - right;
}

#endif
