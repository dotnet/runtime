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

guint16 mono_read16 (const unsigned char *x);
guint32 mono_read32 (const unsigned char *x);
guint64 mono_read64 (const unsigned char *x);
guint64 mono_read64_swap_words (const unsigned char *x);

#define read16(x) (mono_read16 ((const unsigned char *)(x)))
#define read32(x) (mono_read32 ((const unsigned char *)(x)))
#define read64(x) (mono_read64 ((const unsigned char *)(x)))

#else

#define read16(x) GUINT16_FROM_LE (*((const guint16 *) (x)))
#define read32(x) GUINT32_FROM_LE (*((const guint32 *) (x)))
#define read64(x) GUINT64_FROM_LE (*((const guint64 *) (x)))

#endif

#define readr4(x,dest)	\
	do {	\
		mono_rfloat mf;	\
		mf.ival = read32 ((x));	\
		*(dest) = mf.fval;	\
	} while (0)

#ifdef __arm__

#define readr8(x,dest)	\
	do {	\
		mono_rdouble mf;	\
		mf.ival = mono_read64_swap_words ((x));	\
		*(dest) = mf.fval;	\
	} while (0)
#else

#define readr8(x,dest)	\
	do {	\
		mono_rdouble mf;	\
		mf.ival = read64 ((x));	\
		*(dest) = mf.fval;	\
	} while (0)

#endif /* __arm__ */

#endif /* _MONO_METADATA_ENDIAN_H_ */
