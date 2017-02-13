/**
 * \file
 */

#ifndef __MONO_SIGNBIT_H__
#define __MONO_SIGNBIT_H__

#include <math.h>
#include <mono/utils/mono-publib.h>

#ifdef HAVE_SIGNBIT
#define mono_signbit signbit
#else
#define mono_signbit(x) (sizeof (x) == sizeof (float) ? mono_signbit_float (x) : mono_signbit_double (x))

MONO_API int
mono_signbit_double (double x);

MONO_API int
mono_signbit_float (float x);

#endif

#endif
