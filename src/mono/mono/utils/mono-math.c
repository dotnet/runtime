
#include "mono-math.h"

#ifndef HAVE_SIGNBIT

int
mono_signbit_float (float x)
{
	union { float f; int i; } u;

	u.f = x;

	return u.i < 0;
}

int
mono_signbit_double (double x)
{
	union { double d; int i[2]; } u;

	u.d = x;

#if G_BYTE_ORDER == G_LITTLE_ENDIAN
	return u.i [1] < 0;
#else
	return u.i [0] < 0;
#endif
}

#endif
