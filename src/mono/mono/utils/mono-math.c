
#include "mono-math.h"

#ifndef HAVE_SIGNBIT

int
mono_signbit_float (float x)
{
	union { float f; int i; } u = { f: x };

	return u.i < 0;
}

int
mono_signbit_double (double x)
{
	union { double d; int i[2]; } u = { d: x };

	return u.i [1] < 0;
}

#endif
