// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_UTF8CONVERTER_H
#define HAVE_MINIPAL_UTF8CONVERTER_H

#include <config.h>
#include <stdarg.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <errno.h>
#include <stdint.h>
#include <stdbool.h>

#ifndef CORECLR
#include "glib.h"
#endif

#ifdef _MSC_VER
#define FORCE_INLINE(RET_TYPE) __forceinline RET_TYPE
#else
#define FORCE_INLINE(RET_TYPE) inline RET_TYPE __attribute__((always_inline))
#endif

#if G_BYTE_ORDER == G_LITTLE_ENDIAN
#define decode_utf32 decode_utf32le
#define encode_utf32 encode_utf32le
#define decode_utf16 decode_utf16le
#define encode_utf16 encode_utf16le
#define GUINT16_TO_LE(x) (x)
#define GUINT16_TO_BE(x) GUINT16_SWAP_LE_BE(x)
#else
#define decode_utf32 decode_utf32be
#define encode_utf32 encode_utf32be
#define decode_utf16 decode_utf16be
#define encode_utf16 encode_utf16be
#define GUINT16_TO_LE(x) GUINT16_SWAP_LE_BE(x)
#define GUINT16_TO_BE(x) (x)
#endif

#ifdef CORECLR

#ifdef TARGET_64BIT
#define ptrdiff_t int64_t
#else
#define ptrdiff_t int32_t
#endif

#define gunichar uint32_t
#define gunichar2 uint16_t
#define guint uint32_t
#define gchar char
#define guchar unsigned char
#define gboolean bool
#define gsize size_t
#define gssize ptrdiff_t
#define gint int32_t
#define glong long
#define gptrdiff ptrdiff_t
#define guint8 uint8_t
#define guint16 uint16_t
#define gpointer void*
#define g_malloc malloc
#define TRUE 1
#define FALSE 0
#ifndef MIN
#define MIN(a,b) ((a) < (b) ? (a) : (b))
#endif

typedef void* (*GCustomAllocator) (size_t req_size, void* custom_alloc_data);

typedef struct {
	/* In the real glib, this is a GQuark, but we dont use/need that */
	void* domain;
	int32_t code;
	char *message;
} GError;

typedef struct {
	void* buffer;
	size_t buffer_size;
	size_t req_buffer_size;
} GFixedBufferCustomAllocatorData;

typedef enum {
	G_CONVERT_ERROR_NO_CONVERSION,
	G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
	G_CONVERT_ERROR_FAILED,
	G_CONVERT_ERROR_PARTIAL_INPUT,
	G_CONVERT_ERROR_BAD_URI,
	G_CONVERT_ERROR_NOT_ABSOLUTE_PATH,
	G_CONVERT_ERROR_NO_MEMORY
} GConvertError;

#define UNROLL_DECODE_UTF8 0
#define UNROLL_ENCODE_UTF8 0

static int decode_utf32be (char *inbuf, size_t inleft, uint32_t *outchar);
static int encode_utf32be (uint32_t c, char *outbuf, size_t outleft);

static int decode_utf32le (char *inbuf, size_t inleft, uint32_t *outchar);
static int encode_utf32le (uint32_t c, char *outbuf, size_t outleft);

static int decode_utf16be (char *inbuf, size_t inleft, uint32_t *outchar);
static int encode_utf16be (uint32_t c, char *outbuf, size_t outleft);

static int decode_utf16le (char *inbuf, size_t inleft, uint32_t *outchar);
static int encode_utf16le (uint32_t c, char *outbuf, size_t outleft);

static FORCE_INLINE (int) decode_utf8 (char *inbuf, size_t inleft, uint32_t *outchar);
static int encode_utf8 (uint32_t c, char *outbuf, size_t outleft);

static int decode_latin1 (char *inbuf, size_t inleft, uint32_t *outchar);
static int encode_latin1 (uint32_t c, char *outbuf, size_t outleft);

#define G_LITTLE_ENDIAN 1234
#define G_BIG_ENDIAN 4321
#define GUINT16_SWAP_LE_BE(x) ((uint16_t) (((uint16_t) x) >> 8) | ((((uint16_t)(x)) & 0xff) << 8))

#ifdef BIGENDIAN
#define G_BYTE_ORDER G_BIG_ENDIAN
#else
#define G_BYTE_ORDER G_LITTLE_ENDIAN
#endif

#define G_CAST_TYPE_TO_TYPE(src,dest,v) ((dest)(v))
#define G_CAST_PTRTYPE_TO_STYPE(src,dest,v) ((dest)(gssize)(v))
#define GUINT32_TO_UINT16(v) G_CAST_TYPE_TO_TYPE(guint32, guint16, v)
#define GSIZE_TO_INT(v) G_CAST_TYPE_TO_TYPE(gsize, gint, v)
#define GSSIZE_TO_UINT(v) G_CAST_TYPE_TO_TYPE(gssize, guint, v)
#define GUNICHAR_TO_UINT8(v) G_CAST_TYPE_TO_TYPE(gunichar, guint8, v)
#define GUNICHAR_TO_UINT16(v) G_CAST_TYPE_TO_TYPE(gunichar, guint16, v)
#define GUNICHAR_TO_CHAR(v) G_CAST_TYPE_TO_TYPE(gunichar, gchar, v)
#define GPTRDIFF_TO_LONG(v) G_CAST_PTRTYPE_TO_STYPE(gptrdiff, glong, v)
#define g_return_val_if_fail(x,e)  do { if (!(x)) { printf ("%s:%d: assertion '%s' failed\n", __FILE__, __LINE__, #x); return (e); } } while(0)
#define g_utf8_next_char(p) ((p) + g_utf8_jump_table[(unsigned char)(*p)])

#if defined(__GNUC__) && (__GNUC__ > 2)
#define G_LIKELY(expr) (__builtin_expect ((expr) != 0, 1))
#define G_UNLIKELY(expr) (__builtin_expect ((expr) != 0, 0))
#else
#define G_LIKELY(x) (x)
#define G_UNLIKELY(x) (x)
#endif

void
g_set_error (GError **err, void* domain, int32_t code, const char *format, ...)
{
	va_list args;

	if (err) {
		*err = (GError *) malloc (sizeof (GError));
		(*err)->domain = domain;
		(*err)->code = code;

		va_start (args, format);
		int s = vsnprintf(NULL, 0, format, args);
		va_end(args);

		if (s > -1)
		{
			(*err)->message = (char*)malloc(s);

			va_start(args, format);
			vsnprintf((*err)->message, s, format, args);
			va_end (args);
		}
	}
}

#define G_CONVERT_ERROR g_convert_error_quark()

inline static void
mono_set_errno (int errno_val)
{
	errno = errno_val;
}

#endif // CORECLR

#ifdef __cplusplus
extern "C" {
#endif

/*
 * Unicode encoders and decoders
 */

gunichar2 *
g_utf8_to_utf16_custom_alloc_optional (const gchar *str, glong len, glong *items_read, glong *items_written, gboolean include_nuls, gboolean replace_invalid_codepoints, gboolean null_terminate, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err);

gchar *
g_utf16_to_utf8_custom_alloc_with_nulls (const gunichar2 *str, glong len, glong *items_read, glong *items_written, gboolean include_nuls, gboolean null_terminate, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err);

#ifdef __cplusplus
}
#endif // extern "C"

#endif //HAVE_MINIPAL_UTF8CONVERTER_H
