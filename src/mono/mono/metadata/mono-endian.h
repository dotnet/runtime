#ifndef _MONO_METADATA_ENDIAN_H_
#define _MONO_METADATA_ENDIAN_H_ 1

#include <glib.h>

typedef union {
	guint32 ival;
	float fval;
} mono_rfloat;

typedef union {
	guint64 ival;
	double fval;
} mono_rdouble;

#if NO_UNALIGNED_ACCESS

#define read16(x) (mono_read16 ((x)))
#define read32(x) (mono_read32 ((x)))
#define read64(x) (mono_read64 ((x)))

#else

#define read16(x) GUINT16_FROM_LE (*((guint16 *) (x)))
#define read32(x) GUINT32_FROM_LE (*((guint32 *) (x)))
#define read64(x) GUINT64_FROM_LE (*((guint64 *) (x)))

#endif

#define readr4(x,dest)	\
	do {	\
		mono_rfloat mf;	\
		mf.ival = read32 ((x));	\
		*(dest) = mf.fval;	\
	} while (0)

#define readr8(x,dest)	\
	do {	\
		mono_rdouble mf;	\
		mf.ival = read64 ((x));	\
		*(dest) = mf.fval;	\
	} while (0)

#endif /* _MONO_METADATA_ENDIAN_H_ */
