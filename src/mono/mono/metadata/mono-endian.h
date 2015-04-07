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
	unsigned char cval [8];
} mono_rdouble;

#if defined(__s390x__)

#define read16(x)	s390x_read16(*(guint16 *)(x))
#define read32(x)	s390x_read32(*(guint32 *)(x))
#define read64(x)	s390x_read64(*(guint64 *)(x))

static __inline__ guint16
s390x_read16(guint16 x)
{
	guint16 ret;

	__asm__ ("	lrvr	%0,%1\n"
		 "	sra	%0,16\n"
		 : "=r" (ret) : "r" (x));

	return(ret);
}

static __inline__ guint32
s390x_read32(guint32 x)
{
	guint32 ret;

	__asm__ ("	lrvr	%0,%1\n"
		 : "=r" (ret) : "r" (x));

	return(ret);
}

static __inline__ guint64
s390x_read64(guint64 x)
{
	guint64 ret;

	__asm__ ("	lrvgr	%0,%1\n"
		 : "=r" (ret) : "r" (x));

	return(ret);
}

#else

# if NO_UNALIGNED_ACCESS

guint16 mono_read16 (const unsigned char *x);
guint32 mono_read32 (const unsigned char *x);
guint64 mono_read64 (const unsigned char *x);

#define read16(x) (mono_read16 ((const unsigned char *)(x)))
#define read32(x) (mono_read32 ((const unsigned char *)(x)))
#define read64(x) (mono_read64 ((const unsigned char *)(x)))

# else

#define read16(x) GUINT16_FROM_LE (*((const guint16 *) (x)))
#define read32(x) GUINT32_FROM_LE (*((const guint32 *) (x)))
#define read64(x) GUINT64_FROM_LE (*((const guint64 *) (x)))

# endif

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
