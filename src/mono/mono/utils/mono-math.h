
#ifndef __MONO_SIGNBIT_H__
#define __MONO_SIGNBIT_H__

#include <math.h>

#ifdef HAVE_SIGNBIT
#define mono_signbit signbit
#else
#define mono_signbit(x) (sizeof (x) == sizeof (float) ? mono_signbit_float (x) : mono_signbit_double (x))

int
mono_signbit_double (double x);

int
mono_signbit_float (float x);

#endif

#endif
