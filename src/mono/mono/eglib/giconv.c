/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */
/*
 *  Copyright (C) 2011 Jeffrey Stedfast
 *
 *  Permission is hereby granted, free of charge, to any person
 *  obtaining a copy of this software and associated documentation
 *  files (the "Software"), to deal in the Software without
 *  restriction, including without limitation the rights to use, copy,
 *  modify, merge, publish, distribute, sublicense, and/or sell copies
 *  of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be
 *  included in all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 *  EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 *  MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 *  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 *  HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 *  WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 *  DEALINGS IN THE SOFTWARE.
 */
#include <config.h>
#include <glib.h>
#include <string.h>
#include <errno.h>
#include "../utils/mono-errno.h"

#include <minipal/utf8.h>

#ifdef _MSC_VER
#define FORCE_INLINE(RET_TYPE) __forceinline RET_TYPE
#else
#define FORCE_INLINE(RET_TYPE) inline RET_TYPE __attribute__((always_inline))
#endif

#if G_BYTE_ORDER == G_LITTLE_ENDIAN
#define decode_utf16 decode_utf16le
#else
#define decode_utf16 decode_utf16be
#endif

static FORCE_INLINE (uint16_t)
read_uint16_endian (unsigned char *inptr, unsigned endian)
{
	if (endian == G_LITTLE_ENDIAN)
		return (uint16_t)((inptr[1] << 8) | inptr[0]);
	return (uint16_t)((inptr[0] << 8) | inptr[1]);
}

static FORCE_INLINE (int)
decode_utf16_endian (char *inbuf, size_t inleft, gunichar *outchar, unsigned endian)
{
	unsigned char *inptr = (unsigned char *) inbuf;
	gunichar2 c;
	gunichar u;

	if (inleft < 2) {
		mono_set_errno (E2BIG);
		return -1;
	}

	u = read_uint16_endian (inptr, endian);

	if (u < 0xd800) {
		/* 0x0000 -> 0xd7ff */
		*outchar = u;
		return 2;
	} else if (u < 0xdc00) {
		/* 0xd800 -> 0xdbff */
		if (inleft < 4) {
			mono_set_errno (EINVAL);
			return -2;
		}

		c = read_uint16_endian (inptr + 2, endian);

		if (c < 0xdc00 || c > 0xdfff) {
			mono_set_errno (EILSEQ);
			return -2;
		}

		u = ((u - 0xd800) << 10) + (c - 0xdc00) + 0x0010000UL;
		*outchar = u;

		return 4;
	} else if (u < 0xe000) {
		/* 0xdc00 -> 0xdfff */
		mono_set_errno (EILSEQ);
		return -1;
	} else {
		/* 0xe000 -> 0xffff */
		*outchar = u;
		return 2;
	}
}

static int
decode_utf16be (char *inbuf, size_t inleft, gunichar *outchar)
{
	return decode_utf16_endian (inbuf, inleft, outchar, G_BIG_ENDIAN);
}

static int
decode_utf16le (char *inbuf, size_t inleft, gunichar *outchar)
{
	return decode_utf16_endian (inbuf, inleft, outchar, G_LITTLE_ENDIAN);
}

static FORCE_INLINE (void)
write_uint16_endian (unsigned char *outptr, uint16_t c, unsigned endian)
{
	if (endian == G_LITTLE_ENDIAN) {
		outptr[0] = c & 0xff;
		outptr[1] = (c >> 8) & 0xff;
		return;
	}
	outptr[0] = (c >> 8) & 0xff;
	outptr[1] = c & 0xff;
}

static gpointer error_quark = (gpointer)"ConvertError";

gpointer
g_convert_error_quark (void)
{
	return error_quark;
}

static FORCE_INLINE (int)
g_unichar_to_utf16_endian (gunichar c, gunichar2 *outbuf, unsigned endian)
{
	if (c < 0xd800) {
		if (outbuf)
			*outbuf = (gunichar2) (endian == G_BIG_ENDIAN ? GUINT16_TO_BE(c) : GUINT16_TO_LE(c));

		return 1;
	} else if (c < 0xe000) {
		return -1;
	} else if (c < 0x10000) {
		if (outbuf)
			*outbuf = (gunichar2) (endian == G_BIG_ENDIAN ? GUINT16_TO_BE(c) : GUINT16_TO_LE(c));

		return 1;
	} else if (c < 0x110000) {
		if (outbuf) {
			gunichar2 c2 = (gunichar2)(c - 0x10000);

			gunichar2 part1 = (c2 >> 10) + 0xd800;
			gunichar2 part2 = (c2 & 0x3ff) + 0xdc00;
			if (endian == G_BIG_ENDIAN) {
				outbuf[0] = (gunichar2) GUINT16_TO_BE(part1);
				outbuf[1] = (gunichar2) GUINT16_TO_BE(part2);
			} else {
				outbuf[0] = (gunichar2) GUINT16_TO_LE(part1);
				outbuf[1] = (gunichar2) GUINT16_TO_LE(part2);
			}
		}

		return 2;
	} else {
		return -1;
	}
}

static FORCE_INLINE (int)
g_unichar_to_utf16 (gunichar c, gunichar2 *outbuf)
{
	return g_unichar_to_utf16_endian (c, outbuf, G_BYTE_ORDER);
}

static FORCE_INLINE (int)
g_unichar_to_utf16be (gunichar c, gunichar2 *outbuf)
{
	return g_unichar_to_utf16_endian (c, outbuf, G_BIG_ENDIAN);
}

static FORCE_INLINE (int)
g_unichar_to_utf16le (gunichar c, gunichar2 *outbuf)
{
	return g_unichar_to_utf16_endian (c, outbuf, G_LITTLE_ENDIAN);
}

static FORCE_INLINE (void)
map_error(GError **err)
{
	if (errno == MINIPAL_ERROR_INSUFFICIENT_BUFFER) {
		g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_NO_MEMORY, "Allocation failed.");
	} else if (errno == MINIPAL_ERROR_NO_UNICODE_TRANSLATION) {
		g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE, "Illegal byte sequence encountered in the input.");
	}
}

static FORCE_INLINE (gunichar2 *)
g_utf8_to_utf16_impl (const gchar *str, glong len, glong *items_read, glong *items_written, GError **err, int flags, bool treatAsLE)
{
	errno = 0;
	gunichar2* lpDestStr = NULL;
#if G_BYTE_ORDER == G_BIG_ENDIAN
	if (treatAsLE)
		flags |= MINIPAL_TREAT_AS_LITTLE_ENDIAN;
#endif

	if (len < 0)
		len = (glong)strlen(str) + 1;

	glong ret = (glong)minipal_get_length_utf8_to_utf16 (str, len, flags);

	map_error(err);

	if (items_written)
		*items_written = errno == 0 ? ret : 0;

	if (ret <= 0)
		return NULL;

	lpDestStr = g_malloc ((ret + 1) * sizeof(gunichar2));
	ret = (glong)minipal_convert_utf8_to_utf16 (str, len, lpDestStr, ret, flags);
	lpDestStr[ret] = '\0';

	if (items_written)
		*items_written = errno == 0 ? ret : 0;

	map_error(err);
	return lpDestStr;
}

static FORCE_INLINE (gunichar2 *)
g_utf8_to_utf16le_custom_alloc_impl (const gchar *str, glong len, glong *items_read, glong *items_written, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err, bool treatAsLE)
{
	guint flags = 0;
	errno = 0;
#if G_BYTE_ORDER == G_BIG_ENDIAN
	if (treatAsLE)
		flags = MINIPAL_TREAT_AS_LITTLE_ENDIAN;
#endif
	if (len < 0)
		len = (glong)strlen(str) + 1;

	glong ret = (glong)minipal_get_length_utf8_to_utf16 (str, len, flags);

	map_error(err);

	if (items_written)
		*items_written = errno == 0 ? ret : 0;

	if (ret <= 0)
		return NULL;

	gunichar2 *lpDestStr = custom_alloc_func((ret + 1) * sizeof(gunichar2), custom_alloc_data);
	if (G_UNLIKELY (!lpDestStr)) {
		g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_NO_MEMORY, "Allocation failed.");
		return NULL;
	}

	flags |= MINIPAL_MB_NO_REPLACE_INVALID_CHARS;
	ret = (glong)minipal_convert_utf8_to_utf16 (str, len, lpDestStr, ret, flags);
	lpDestStr[ret] = '\0';

	map_error(err);
	return lpDestStr;
}

gunichar2 *
g_utf8_to_utf16 (const gchar *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	return g_utf8_to_utf16_impl (str, len, items_read, items_written, err, MINIPAL_MB_NO_REPLACE_INVALID_CHARS, false);
}

gunichar2 *
g_utf8_to_utf16le (const gchar *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	return g_utf8_to_utf16_impl (str, len, items_read, items_written, err, MINIPAL_MB_NO_REPLACE_INVALID_CHARS, true);
}

gunichar2 *
g_wtf8_to_utf16 (const gchar *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	return g_utf8_to_utf16_impl (str, len, items_read, items_written, err, 0, false);
}

gunichar2 *
g_utf8_to_utf16_custom_alloc (const gchar *str, glong len, glong *items_read, glong *items_written, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err)
{
	return g_utf8_to_utf16le_custom_alloc_impl (str, len, items_read, items_written, custom_alloc_func, custom_alloc_data, err, false);
}

gunichar2 *
g_utf8_to_utf16le_custom_alloc (const gchar *str, glong len, glong *items_read, glong *items_written, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err)
{
	return g_utf8_to_utf16le_custom_alloc_impl (str, len, items_read, items_written, custom_alloc_func, custom_alloc_data, err, true);
}

static FORCE_INLINE (gchar *)
g_utf16_to_utf8_impl (const gunichar2 *str, glong len, glong *items_read, glong *items_written, GError **err, bool treatAsLE)
{
	guint flags = 0;
	errno = 0;
	gchar* lpDestStr = NULL;
#if G_BYTE_ORDER == G_BIG_ENDIAN
	if (treatAsLE)
		flags |= MINIPAL_TREAT_AS_LITTLE_ENDIAN;
#endif
	if (len < 0) {
		len = 0;
		while (str[len])
			len++;

		len++;
	}

	glong ret = (glong)minipal_get_length_utf16_to_utf8 (str, len, flags);
	map_error(err);

	if (items_written)
		*items_written = errno == 0 ? ret : 0;

	if (ret <= 0)
		return NULL;

	lpDestStr = (gchar *)g_malloc((ret + 1) * sizeof(gchar));
	ret = (glong)minipal_convert_utf16_to_utf8 (str, len, lpDestStr, ret, flags);
	lpDestStr[ret] = '\0';

	if (items_written)
		*items_written = errno == 0 ? ret : 0;

	map_error(err);
	return lpDestStr;
}

gchar *
g_utf16_to_utf8 (const gunichar2 *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	return g_utf16_to_utf8_impl (str, len, items_read, items_written, err, /* treatAsLE */ false);
}

gchar *
g_utf16le_to_utf8 (const gunichar2 *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	return g_utf16_to_utf8_impl (str, len, items_read, items_written, err, /* treatAsLE */ true);
}

gchar *
g_utf16_to_utf8_custom_alloc (const gunichar2 *str, glong len, glong *items_read, glong *items_written, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err)
{
	errno = 0;

	if (len < 0) {
		len = 0;
		while (str[len])
			len++;

		len++;
	}

	glong ret = (glong)minipal_get_length_utf16_to_utf8 (str, len, 0);
	map_error(err);

	if (items_written)
		*items_written = errno == 0 ? ret : 0;

	if (ret <= 0)
		return NULL;

	gchar *lpDestStr = custom_alloc_func((ret + 1) * sizeof (gunichar2), custom_alloc_data);
	if (G_UNLIKELY (!lpDestStr)) {
		g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_NO_MEMORY, "Allocation failed.");
		return NULL;
	}

	ret = (glong)minipal_convert_utf16_to_utf8 (str, len, lpDestStr, ret, 0);
	lpDestStr[ret] = '\0';

	map_error(err);
	return lpDestStr;
}

gunichar *
g_utf16_to_ucs4 (const gunichar2 *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	gunichar *outbuf, *outptr;
	size_t outlen = 0;
	size_t inleft;
	char *inptr;
	gunichar c;
	int n;

	g_return_val_if_fail (str != NULL, NULL);

	if (len < 0) {
		len = 0;
		while (str[len])
			len++;
	}

	inptr = (char *) str;
	inleft = len * 2;

	while (inleft > 0) {
		if ((n = decode_utf16 (inptr, inleft, &c)) < 0) {
			if (n == -2 && inleft > 2) {
				/* This means that the first UTF-16 char was read, but second failed */
				inleft -= 2;
				inptr += 2;
			}

			if (errno == EILSEQ) {
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
					     "Illegal byte sequence encountered in the input.");
			} else if (items_read) {
				/* partial input is ok if we can let our caller know... */
				break;
			} else {
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_PARTIAL_INPUT,
					     "Partial byte sequence encountered in the input.");
			}

			if (items_read)
				*items_read = GPTRDIFF_TO_LONG ((inptr - (char *) str) / 2);

			if (items_written)
				*items_written = 0;

			return NULL;
		} else if (c == 0)
			break;

		outlen += 4;
		inleft -= n;
		inptr += n;
	}

	if (items_read)
		*items_read = GPTRDIFF_TO_LONG ((inptr - (char *) str) / 2);

	if (items_written)
		*items_written = (glong)(outlen / 4);

	outptr = outbuf = g_malloc (outlen + 4);
	inptr = (char *) str;
	inleft = len * 2;

	while (inleft > 0) {
		if ((n = decode_utf16 (inptr, inleft, &c)) < 0)
			break;
		else if (c == 0)
			break;

		*outptr++ = c;
		inleft -= n;
		inptr += n;
	}

	*outptr = 0;

	return outbuf;
}

gunichar2 *
g_ucs4_to_utf16 (const gunichar *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	gunichar2 *outbuf, *outptr;
	size_t outlen = 0;
	glong i;
	int n;

	g_return_val_if_fail (str != NULL, NULL);

	if (len < 0) {
		for (i = 0; str[i] != 0; i++) {
			if ((n = g_unichar_to_utf16 (str[i], NULL)) < 0) {
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
					     "Illegal byte sequence encountered in the input.");

				if (items_written)
					*items_written = 0;

				if (items_read)
					*items_read = i;

				return NULL;
			}

			outlen += n;
		}
	} else {
		for (i = 0; i < len && str[i] != 0; i++) {
			if ((n = g_unichar_to_utf16 (str[i], NULL)) < 0) {
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
					     "Illegal byte sequence encountered in the input.");

				if (items_written)
					*items_written = 0;

				if (items_read)
					*items_read = i;

				return NULL;
			}

			outlen += n;
		}
	}

	len = i;

	outptr = outbuf = g_malloc ((outlen + 1) * sizeof (gunichar2));
	for (i = 0; i < len; i++)
		outptr += g_unichar_to_utf16 (str[i], outptr);
	*outptr = 0;

	if (items_written)
		*items_written = (glong)outlen;

	if (items_read)
		*items_read = i;

	return outbuf;
}

gpointer
g_fixed_buffer_custom_allocator (gsize req_size, gpointer custom_alloc_data)
{
	GFixedBufferCustomAllocatorData *fixed_buffer_custom_alloc_data = (GFixedBufferCustomAllocatorData *)custom_alloc_data;
	if (!fixed_buffer_custom_alloc_data)
		return NULL;

	fixed_buffer_custom_alloc_data->req_buffer_size = req_size;
	if (req_size > fixed_buffer_custom_alloc_data->buffer_size)
		return NULL;

	return fixed_buffer_custom_alloc_data->buffer;
}
