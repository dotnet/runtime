/**
 * \file
 */

#ifndef __MONO_NUMBER_MS_H__
#define __MONO_NUMBER_MS_H__

#include <glib.h>

// Double floating point Bias
#define MONO_DOUBLE_BIAS 1022

// Structure to access an encoded double floating point
typedef struct {
#if G_BYTE_ORDER == G_BIG_ENDIAN
	guint sign   : 1;
	guint exp    : 11;
	guint mantHi : 20;
	guint mantLo : 32;
#else // BIGENDIAN
	guint mantLo : 32;
	guint mantHi : 20;
	guint exp    : 11;
	guint sign   : 1;
#endif
} MonoDouble;

typedef union {
	MonoDouble s;
	gdouble d;
} MonoDouble_double;

#endif
