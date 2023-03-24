#ifndef __MONO_MINI_INTERP_INTRINSICS_H__
#define __MONO_MINI_INTERP_INTRINSICS_H__

#include <glib.h>
#include <mono/metadata/object.h>

#include "interp-internals.h"

#ifdef __GNUC__
static inline gint32
interp_intrins_clz_i4 (guint32 val)
{
	if (val == 0)
		return 32;
	return __builtin_clz (val);
}

static inline gint32
interp_intrins_clz_i8 (guint64 val)
{
	if (val == 0)
		return 64;
	return __builtin_clzll (val);
}

static inline gint32
interp_intrins_ctz_i4 (guint32 val)
{
	if (val == 0)
		return 32;
	return __builtin_ctz (val);
}

static inline gint32
interp_intrins_ctz_i8 (guint64 val)
{
	if (val == 0)
		return 64;
	return __builtin_ctzll (val);
}

static inline gint32
interp_intrins_popcount_i4 (guint32 val)
{
	return __builtin_popcount (val);
}

static inline gint32
interp_intrins_popcount_i8 (guint64 val)
{
	return __builtin_popcountll (val);
}
#else
static inline gint32
interp_intrins_clz_i4 (guint32 val)
{
	gint32 count = 0;
	while (val) {
		val = val >> 1;
		count++;
	}
	return 32 - count;
}

static inline gint32
interp_intrins_clz_i8 (guint64 val)
{
	gint32 count = 0;
	while (val) {
		val = val >> 1;
		count++;
	}
	return 64 - count;
}

static inline gint32
interp_intrins_ctz_i4 (guint32 val)
{
	if (val == 0)
		return 32;
	gint32 count = 0;
	while ((val & 1) == 0) {
		val = val >> 1;
		count++;
	}
	return count;
}

static inline gint32
interp_intrins_ctz_i8 (guint64 val)
{
	if (val == 0)
		return 64;
	gint32 count = 0;
	while ((val & 1) == 0) {
		val = val >> 1;
		count++;
	}
	return count;
}

static inline gint32
interp_intrins_popcount_i4 (guint32 val)
{
	gint32 count = 0;
	while (val != 0) {
		count += val & 1;
		val = val >> 1;
	}
	return count;
}

static inline gint32
interp_intrins_popcount_i8 (guint64 val)
{
	gint32 count = 0;
	while (val != 0) {
		count += val & 1;
		val = val >> 1;
	}
	return count;
}

#endif

void
interp_intrins_marvin_block (guint32 *pp0, guint32 *pp1);

guint32
interp_intrins_ascii_chars_to_uppercase (guint32 val);

int
interp_intrins_ordinal_ignore_case_ascii (guint32 valueA, guint32 valueB);

int
interp_intrins_64ordinal_ignore_case_ascii (guint64 valueA, guint64 valueB);

mono_u
interp_intrins_widen_ascii_to_utf16 (guint8 *pAsciiBuffer, mono_unichar2 *pUtf16Buffer, mono_u elementCount);

#endif /* __MONO_MINI_INTERP_INTRINSICS_H__ */
