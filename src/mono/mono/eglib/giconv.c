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

#ifdef _MSC_VER
#define FORCE_INLINE(RET_TYPE) __forceinline RET_TYPE
#else
#define FORCE_INLINE(RET_TYPE) inline RET_TYPE __attribute__((always_inline))
#endif


#define UNROLL_DECODE_UTF8 0
#define UNROLL_ENCODE_UTF8 0

static int decode_utf32be (char *inbuf, size_t inleft, gunichar *outchar);
static int encode_utf32be (gunichar c, char *outbuf, size_t outleft);

static int decode_utf32le (char *inbuf, size_t inleft, gunichar *outchar);
static int encode_utf32le (gunichar c, char *outbuf, size_t outleft);

static int decode_utf16be (char *inbuf, size_t inleft, gunichar *outchar);
static int encode_utf16be (gunichar c, char *outbuf, size_t outleft);

static int decode_utf16le (char *inbuf, size_t inleft, gunichar *outchar);
static int encode_utf16le (gunichar c, char *outbuf, size_t outleft);

static FORCE_INLINE (int) decode_utf8 (char *inbuf, size_t inleft, gunichar *outchar);
static int encode_utf8 (gunichar c, char *outbuf, size_t outleft);

static int decode_latin1 (char *inbuf, size_t inleft, gunichar *outchar);
static int encode_latin1 (gunichar c, char *outbuf, size_t outleft);

#if G_BYTE_ORDER == G_LITTLE_ENDIAN
#define decode_utf32 decode_utf32le
#define encode_utf32 encode_utf32le
#define decode_utf16 decode_utf16le
#define encode_utf16 encode_utf16le
#else
#define decode_utf32 decode_utf32be
#define encode_utf32 encode_utf32be
#define decode_utf16 decode_utf16be
#define encode_utf16 encode_utf16be
#endif

/*
 * Unicode encoders and decoders
 */

static FORCE_INLINE (uint32_t)
read_uint32_endian (unsigned char *inptr, unsigned endian)
{
	if (endian == G_LITTLE_ENDIAN)
		return (inptr[3] << 24) | (inptr[2] << 16) | (inptr[1] << 8) | inptr[0];
	return (inptr[0] << 24) | (inptr[1] << 16) | (inptr[2] << 8) | inptr[3];
}

static int
decode_utf32_endian (char *inbuf, size_t inleft, gunichar *outchar, unsigned endian)
{
	unsigned char *inptr = (unsigned char *) inbuf;
	gunichar c;

	if (inleft < 4) {
		mono_set_errno (EINVAL);
		return -1;
	}

	c = read_uint32_endian (inptr, endian);

	if (c >= 0xd800 && c < 0xe000) {
		mono_set_errno (EILSEQ);
		return -1;
	} else if (c >= 0x110000) {
		mono_set_errno (EILSEQ);
		return -1;
	}

	*outchar = c;

	return 4;
}

static int
decode_utf32be (char *inbuf, size_t inleft, gunichar *outchar)
{
	return decode_utf32_endian (inbuf, inleft, outchar, G_BIG_ENDIAN);
}

static int
decode_utf32le (char *inbuf, size_t inleft, gunichar *outchar)
{
	return decode_utf32_endian (inbuf, inleft, outchar, G_LITTLE_ENDIAN);
}

static int
encode_utf32be (gunichar c, char *outbuf, size_t outleft)
{
	unsigned char *outptr = (unsigned char *) outbuf;

	if (outleft < 4) {
		mono_set_errno (E2BIG);
		return -1;
	}

	outptr[0] = (c >> 24) & 0xff;
	outptr[1] = (c >> 16) & 0xff;
	outptr[2] = (c >> 8) & 0xff;
	outptr[3] = c & 0xff;

	return 4;
}

static int
encode_utf32le (gunichar c, char *outbuf, size_t outleft)
{
	unsigned char *outptr = (unsigned char *) outbuf;

	if (outleft < 4) {
		mono_set_errno (E2BIG);
		return -1;
	}

	outptr[0] = c & 0xff;
	outptr[1] = (c >> 8) & 0xff;
	outptr[2] = (c >> 16) & 0xff;
	outptr[3] = (c >> 24) & 0xff;

	return 4;
}

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

static FORCE_INLINE (int)
encode_utf16_endian (gunichar c, char *outbuf, size_t outleft, unsigned endian)
{
	unsigned char *outptr = (unsigned char *) outbuf;
	gunichar2 ch;
	gunichar c2;

	if (c < 0x10000) {
		if (outleft < 2) {
			mono_set_errno (E2BIG);
			return -1;
		}

		write_uint16_endian (outptr, GUNICHAR_TO_UINT16 (c), endian);
		return 2;
	} else {
		if (outleft < 4) {
			mono_set_errno (E2BIG);
			return -1;
		}

		c2 = c - 0x10000;

		ch = (gunichar2) ((c2 >> 10) + 0xd800);
		write_uint16_endian (outptr, ch, endian);

		ch = (gunichar2) ((c2 & 0x3ff) + 0xdc00);
		write_uint16_endian (outptr + 2, ch, endian);
		return 4;
	}
}

static int
encode_utf16be (gunichar c, char *outbuf, size_t outleft)
{
	return encode_utf16_endian (c, outbuf, outleft, G_BIG_ENDIAN);
}

static int
encode_utf16le (gunichar c, char *outbuf, size_t outleft)
{
	return encode_utf16_endian (c, outbuf, outleft, G_LITTLE_ENDIAN);
}

static FORCE_INLINE (int)
decode_utf8 (char *inbuf, size_t inleft, gunichar *outchar)
{
	unsigned char *inptr = (unsigned char *) inbuf;
	gunichar u;
	int n, i;

	u = *inptr;

	if (u < 0x80) {
		/* simple ascii case */
		*outchar = u;
		return 1;
	} else if (u < 0xc2) {
		mono_set_errno (EILSEQ);
		return -1;
	} else if (u < 0xe0) {
		u &= 0x1f;
		n = 2;
	} else if (u < 0xf0) {
		u &= 0x0f;
		n = 3;
	} else if (u < 0xf8) {
		u &= 0x07;
		n = 4;
	} else if (u < 0xfc) {
		u &= 0x03;
		n = 5;
	} else if (u < 0xfe) {
		u &= 0x01;
		n = 6;
	} else {
		mono_set_errno (EILSEQ);
		return -1;
	}

	if (n > inleft) {
		mono_set_errno (EINVAL);
		return -1;
	}

#if UNROLL_DECODE_UTF8
	switch (n) {
	case 6: u = (u << 6) | (*++inptr ^ 0x80);
	case 5: u = (u << 6) | (*++inptr ^ 0x80);
	case 4: u = (u << 6) | (*++inptr ^ 0x80);
	case 3: u = (u << 6) | (*++inptr ^ 0x80);
	case 2: u = (u << 6) | (*++inptr ^ 0x80);
	}
#else
	for (i = 1; i < n; i++)
		u = (u << 6) | (*++inptr ^ 0x80);
#endif

	*outchar = u;

	return n;
}

static int
encode_utf8 (gunichar c, char *outbuf, size_t outleft)
{
	unsigned char *outptr = (unsigned char *) outbuf;
	int base, n, i;

	if (c < 0x80) {
		outptr[0] = GUNICHAR_TO_UINT8 (c);
		return 1;
	} else if (c < 0x800) {
		base = 192;
		n = 2;
	} else if (c < 0x10000) {
		base = 224;
		n = 3;
	} else if (c < 0x200000) {
		base = 240;
		n = 4;
	} else if (c < 0x4000000) {
		base = 248;
		n = 5;
	} else {
		base = 252;
		n = 6;
	}

	if (outleft < n) {
		mono_set_errno (E2BIG);
		return -1;
	}

#if UNROLL_ENCODE_UTF8
	switch (n) {
	case 6: outptr[5] = (c & 0x3f) | 0x80; c >>= 6;
	case 5: outptr[4] = (c & 0x3f) | 0x80; c >>= 6;
	case 4: outptr[3] = (c & 0x3f) | 0x80; c >>= 6;
	case 3: outptr[2] = (c & 0x3f) | 0x80; c >>= 6;
	case 2: outptr[1] = (c & 0x3f) | 0x80; c >>= 6;
	case 1: outptr[0] = c | base;
	}
#else
	for (i = n - 1; i > 0; i--) {
		outptr[i] = (c & 0x3f) | 0x80;
		c >>= 6;
	}

	outptr[0] = GUNICHAR_TO_UINT8 (c | base);
#endif

	return n;
}

static int
decode_latin1 (char *inbuf, size_t inleft, gunichar *outchar)
{
	*outchar = (unsigned char) *inbuf;
	return 1;
}

static int
encode_latin1 (gunichar c, char *outbuf, size_t outleft)
{
	if (outleft < 1) {
		mono_set_errno (E2BIG);
		return -1;
	}

	if (c > 0xff) {
		mono_set_errno (EILSEQ);
		return -1;
	}

	*outbuf = (char) c;

	return 1;
}


/*
 * Simple conversion API
 */

static gpointer error_quark = (gpointer)"ConvertError";

gpointer
g_convert_error_quark (void)
{
	return error_quark;
}
/*
 * Unicode conversion
 */

/**
 * An explanation of the conversion can be found at:
 * http://home.tiscali.nl/t876506/utf8tbl.html
 *
 **/
gint
g_unichar_to_utf8 (gunichar c, gchar *outbuf)
{
	int base, n, i;

	if (c < 0x80) {
		base = 0;
		n = 1;
	} else if (c < 0x800) {
		base = 192;
		n = 2;
	} else if (c < 0x10000) {
		base = 224;
		n = 3;
	} else if (c < 0x200000) {
		base = 240;
		n = 4;
	} else if (c < 0x4000000) {
		base = 248;
		n = 5;
	} else if (c < 0x80000000) {
		base = 252;
		n = 6;
	} else {
		return -1;
	}

	if (outbuf != NULL) {
		for (i = n - 1; i > 0; i--) {
			/* mask off 6 bits worth and add 128 */
			outbuf[i] = (c & 0x3f) | 0x80;
			c >>= 6;
		}

		/* first character has a different base */
		outbuf[0] = GUNICHAR_TO_CHAR (c | base);
	}

	return n;
}

static FORCE_INLINE (int)
g_unichar_to_utf16 (gunichar c, gunichar2 *outbuf)
{
	gunichar c2;

	if (c < 0xd800) {
		if (outbuf)
			*outbuf = (gunichar2) c;

		return 1;
	} else if (c < 0xe000) {
		return -1;
	} else if (c < 0x10000) {
		if (outbuf)
			*outbuf = (gunichar2) c;

		return 1;
	} else if (c < 0x110000) {
		if (outbuf) {
			c2 = c - 0x10000;

			outbuf[0] = (gunichar2) ((c2 >> 10) + 0xd800);
			outbuf[1] = (gunichar2) ((c2 & 0x3ff) + 0xdc00);
		}

		return 2;
	} else {
		return -1;
	}
}

gunichar *
g_utf8_to_ucs4_fast (const gchar *str, glong len, glong *items_written)
{
	gunichar *outbuf, *outptr;
	char *inptr;
	glong n, i;

	g_return_val_if_fail (str != NULL, NULL);

	n = g_utf8_strlen (str, len);

	if (items_written)
		*items_written = n;

	outptr = outbuf = g_malloc ((n + 1) * sizeof (gunichar));
	inptr = (char *) str;

	for (i = 0; i < n; i++) {
		*outptr++ = g_utf8_get_char (inptr);
		inptr = g_utf8_next_char (inptr);
	}

	*outptr = 0;

	return outbuf;
}

static gunichar2 *
eg_utf8_to_utf16_general (const gchar *str, glong len, glong *items_read, glong *items_written, gboolean include_nuls, gboolean replace_invalid_codepoints, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err)
{
	gunichar2 *outbuf, *outptr;
	size_t outlen = 0;
	size_t inleft;
	char *inptr;
	gunichar c;
	int u, n;

	g_return_val_if_fail (str != NULL, NULL);

	if (len < 0) {
		if (include_nuls) {
			g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_FAILED, "Conversions with embedded nulls must pass the string length");
			return NULL;
		}

		len = (glong)strlen (str);
	}

	inptr = (char *) str;
	inleft = len;

	while (inleft > 0) {
		if ((n = decode_utf8 (inptr, inleft, &c)) < 0)
			goto error;

		if (c == 0 && !include_nuls)
			break;

		if ((u = g_unichar_to_utf16 (c, NULL)) < 0) {
			if (replace_invalid_codepoints) {
				u = 2;
			} else {
				mono_set_errno (EILSEQ);
				goto error;
			}
		}

		outlen += u;
		inleft -= n;
		inptr += n;
	}

	if (items_read)
		*items_read = GPTRDIFF_TO_LONG (inptr - str);

	if (items_written)
		*items_written = (glong)outlen;

	if (G_LIKELY (!custom_alloc_func))
		outptr = outbuf = g_malloc ((outlen + 1) * sizeof (gunichar2));
	else
		outptr = outbuf = (gunichar2 *)custom_alloc_func ((outlen + 1) * sizeof (gunichar2), custom_alloc_data);

	if (G_UNLIKELY (custom_alloc_func && !outbuf)) {
		mono_set_errno (ENOMEM);
		goto error;
	}

	inptr = (char *) str;
	inleft = len;

	while (inleft > 0) {
		if ((n = decode_utf8 (inptr, inleft, &c)) < 0)
			break;

		if (c == 0 && !include_nuls)
			break;

		u = g_unichar_to_utf16 (c, outptr);
		if ((u < 0) && replace_invalid_codepoints) {
			outptr[0] = 0xFFFD;
			outptr[1] = 0xFFFD;
			u = 2;
		}

		outptr += u;
		inleft -= n;
		inptr += n;
	}

	*outptr = '\0';

	return outbuf;

error:
	if (errno == ENOMEM) {
		g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_NO_MEMORY,
			     "Allocation failed.");
	} else if (errno == EILSEQ) {
		g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
			     "Illegal byte sequence encounted in the input.");
	} else if (items_read) {
		/* partial input is ok if we can let our caller know... */
	} else {
		g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_PARTIAL_INPUT,
			     "Partial byte sequence encountered in the input.");
	}

	if (items_read)
		*items_read = GPTRDIFF_TO_LONG (inptr - str);

	if (items_written)
		*items_written = 0;

	return NULL;
}

gunichar2 *
g_utf8_to_utf16 (const gchar *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	return eg_utf8_to_utf16_general (str, len, items_read, items_written, FALSE, FALSE, NULL, NULL, err);
}

gunichar2 *
g_utf8_to_utf16_custom_alloc (const gchar *str, glong len, glong *items_read, glong *items_written, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err)
{
	return eg_utf8_to_utf16_general (str, len, items_read, items_written, FALSE, FALSE, custom_alloc_func, custom_alloc_data, err);
}

gunichar2 *
eg_utf8_to_utf16_with_nuls (const gchar *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	return eg_utf8_to_utf16_general (str, len, items_read, items_written, TRUE, FALSE, NULL, NULL, err);
}

gunichar2 *
eg_wtf8_to_utf16 (const gchar *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	return eg_utf8_to_utf16_general (str, len, items_read, items_written, TRUE, TRUE, NULL, NULL, err);
}

gunichar *
g_utf8_to_ucs4 (const gchar *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	gunichar *outbuf, *outptr;
	size_t outlen = 0;
	size_t inleft;
	char *inptr;
	gunichar c;
	int n;

	g_return_val_if_fail (str != NULL, NULL);

	if (len < 0)
		len = (glong)strlen (str);

	inptr = (char *) str;
	inleft = len;

	while (inleft > 0) {
		if ((n = decode_utf8 (inptr, inleft, &c)) < 0) {
			if (errno == EILSEQ) {
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
					     "Illegal byte sequence encounted in the input.");
			} else if (items_read) {
				/* partial input is ok if we can let our caller know... */
				break;
			} else {
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_PARTIAL_INPUT,
					     "Partial byte sequence encountered in the input.");
			}

			if (items_read)
				*items_read = GPTRDIFF_TO_LONG (inptr - str);

			if (items_written)
				*items_written = 0;

			return NULL;
		} else if (c == 0)
			break;

		outlen += 4;
		inleft -= n;
		inptr += n;
	}

	if (items_written)
		*items_written = (glong)(outlen / 4);

	if (items_read)
		*items_read = GPTRDIFF_TO_LONG (inptr - str);

	outptr = outbuf = g_malloc (outlen + 4);
	inptr = (char *) str;
	inleft = len;

	while (inleft > 0) {
		if ((n = decode_utf8 (inptr, inleft, &c)) < 0)
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

static
gchar *
eg_utf16_to_utf8_general (const gunichar2 *str, glong len, glong *items_read, glong *items_written, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err)
{
	char *inptr, *outbuf, *outptr;
	size_t outlen = 0;
	size_t inleft;
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
					     "Illegal byte sequence encounted in the input.");
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

		outlen += g_unichar_to_utf8 (c, NULL);
		inleft -= n;
		inptr += n;
	}

	if (items_read)
		*items_read = GPTRDIFF_TO_LONG ((inptr - (char *) str) / 2);

	if (items_written)
		*items_written = (glong)outlen;

	if (G_LIKELY (!custom_alloc_func))
		outptr = outbuf = g_malloc (outlen + 1);
	else
		outptr = outbuf = (char *)custom_alloc_func (outlen + 1, custom_alloc_data);

	if (G_UNLIKELY (custom_alloc_func && !outbuf)) {
		g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_NO_MEMORY, "Allocation failed.");
		if (items_written)
			*items_written = 0;
		return NULL;
	}

	inptr = (char *) str;
	inleft = len * 2;

	while (inleft > 0) {
		if ((n = decode_utf16 (inptr, inleft, &c)) < 0)
			break;
		else if (c == 0)
			break;

		outptr += g_unichar_to_utf8 (c, outptr);
		inleft -= n;
		inptr += n;
	}

	*outptr = '\0';

	return outbuf;
}

gchar *
g_utf16_to_utf8 (const gunichar2 *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	return eg_utf16_to_utf8_general (str, len, items_read, items_written, NULL, NULL, err);
}

gchar *
g_utf16_to_utf8_custom_alloc (const gunichar2 *str, glong len, glong *items_read, glong *items_written, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err)
{
	return eg_utf16_to_utf8_general (str, len, items_read, items_written, custom_alloc_func, custom_alloc_data, err);
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
					     "Illegal byte sequence encounted in the input.");
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

gchar *
g_ucs4_to_utf8 (const gunichar *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	char *outbuf, *outptr;
	size_t outlen = 0;
	glong i;
	int n;

	g_return_val_if_fail (str != NULL, NULL);

	if (len < 0) {
		for (i = 0; str[i] != 0; i++) {
			if ((n = g_unichar_to_utf8 (str[i], NULL)) < 0) {
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
					     "Illegal byte sequence encounted in the input.");

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
			if ((n = g_unichar_to_utf8 (str[i], NULL)) < 0) {
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
					     "Illegal byte sequence encounted in the input.");

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

	outptr = outbuf = g_malloc (outlen + 1);
	for (i = 0; i < len; i++)
		outptr += g_unichar_to_utf8 (str[i], outptr);
	*outptr = 0;

	if (items_written)
		*items_written = (glong)outlen;

	if (items_read)
		*items_read = i;

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
					     "Illegal byte sequence encounted in the input.");

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
					     "Illegal byte sequence encounted in the input.");

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
