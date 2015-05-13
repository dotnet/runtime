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

// Single floating point Bias
#define MONO_SINGLE_BIAS 126

// Structure to access an encoded single floating point
typedef struct {
#if G_BYTE_ORDER == G_BIG_ENDIAN
	guint sign : 1;
	guint exp  : 8;
	guint mant : 23;
#else
	guint mant : 23;
	guint exp  : 8;
	guint sign : 1;
#endif
} MonoSingle;

typedef union {
	MonoSingle s;
	gfloat f;
} MonoSingle_float;

#define MONO_NUMBER_MAXDIGITS 50

typedef struct  {
	gint32 precision;
	gint32 scale;
	gint32 sign;
	guint16 digits [MONO_NUMBER_MAXDIGITS + 1];
	guint16 *allDigits;
} MonoNumber;

gint
mono_double_from_number (gpointer from, MonoDouble *target);

#endif
