// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <minipal/utf8converter.h>

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

/*
 * Index into the table below with the first byte of a UTF-8 sequence to get
 * the number of bytes that are supposed to follow it to complete the sequence.
 *
 * Note that *legal* UTF-8 values can't have 4 or 5-bytes. The table is left
 * as-is for anyone who may want to do such conversion, which was allowed in
 * earlier algorithms.
*/
const guchar g_utf8_jump_table[256] = {
	1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
	1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
	1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
	1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
	1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
	1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
	2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2, 2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,
	3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3, 4,4,4,4,4,4,4,4,5,5,5,5,6,6,1,1
};

static gboolean
utf8_validate (const unsigned char *inptr, size_t len)
{
	const unsigned char *ptr = inptr + len;
	unsigned char c;

	/* Everything falls through when TRUE... */
	switch (len) {
	default:
		return FALSE;
	case 4:
		if ((c = (*--ptr)) < 0x80 || c > 0xBF)
			return FALSE;

		if ((c == 0xBF || c == 0xBE) && ptr[-1] == 0xBF) {
			if (ptr[-2] == 0x8F || ptr[-2] == 0x9F ||
			    ptr[-2] == 0xAF || ptr[-2] == 0xBF)
				return FALSE;
		}
	case 3:
		if ((c = (*--ptr)) < 0x80 || c > 0xBF)
			return FALSE;
	case 2:
		if ((c = (*--ptr)) < 0x80 || c > 0xBF)
			return FALSE;

		/* no fall-through in this inner switch */
		switch (*inptr) {
		case 0xE0: if (c < 0xA0) return FALSE; break;
		case 0xED: if (c > 0x9F) return FALSE; break;
		case 0xEF: if (c == 0xB7 && (ptr[1] > 0x8F && ptr[1] < 0xB0)) return FALSE;
			if (c == 0xBF && (ptr[1] == 0xBE || ptr[1] == 0xBF)) return FALSE;
			break;
		case 0xF0: if (c < 0x90) return FALSE; break;
		case 0xF4: if (c > 0x8F) return FALSE; break;
		default:   if (c < 0x80) return FALSE; break;
		}
	case 1: if (*inptr >= 0x80 && *inptr < 0xC2) return FALSE;
	}

	if (*inptr > 0xF4)
		return FALSE;

	return TRUE;
}

/**
 * g_utf8_validate:
 * @str: a utf-8 encoded string
 * @max_len: max number of bytes to validate (or -1 to validate the entire null-terminated string)
 * @end: output parameter to mark the end of the valid input
 *
 * Checks @utf for being valid UTF-8. @str is assumed to be
 * null-terminated. This function is not super-strict, as it will
 * allow longer UTF-8 sequences than necessary. Note that Java is
 * capable of producing these sequences if provoked. Also note, this
 * routine checks for the 4-byte maximum size, but does not check for
 * 0x10ffff maximum value.
 *
 * Return value: %TRUE if @str is valid or %FALSE otherwise.
 **/
gboolean
g_utf8_validate (const gchar *str, gssize max_len, const gchar **end)
{
	guchar *inptr = (guchar *) str;
	gboolean valid = TRUE;
	guint length, min;
	gssize n = 0;

	if (max_len == 0)
		return FALSE;

	if (max_len < 0) {
		while (*inptr != 0) {
			length = g_utf8_jump_table[*inptr];
			if (!utf8_validate (inptr, length)) {
				valid = FALSE;
				break;
			}

			inptr += length;
		}
	} else {
		while (n < max_len) {
			if (*inptr == 0) {
				/* Note: return FALSE if we encounter nul-byte
				 * before max_len is reached. */
				valid = FALSE;
				break;
			}

			length = g_utf8_jump_table[*inptr];
			min = MIN (length, GSSIZE_TO_UINT (max_len - n));

			if (!utf8_validate (inptr, min)) {
				valid = FALSE;
				break;
			}

			if (min < length) {
				valid = FALSE;
				break;
			}

			inptr += length;
			n += length;
		}
	}

	if (end != NULL)
		*end = (gchar *) inptr;

	return valid;
}

gunichar
g_utf8_get_char_validated (const gchar *str, gssize max_len)
{
	unsigned char *inptr = (unsigned char *) str;
	gunichar u = *inptr;
	int n, i;

	if (max_len == 0)
		return -2;

	if (u < 0x80) {
		/* simple ascii case */
		return u;
	} else if (u < 0xc2) {
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
		return -1;
	}

	if (max_len > 0) {
		if (!utf8_validate (inptr, MIN (max_len, n)))
			return -1;

		if (max_len < n)
			return -2;
	} else {
		if (!utf8_validate (inptr, n))
			return -1;
	}

	for (i = 1; i < n; i++)
		u = (u << 6) | (*++inptr ^ 0x80);

	return u;
}

glong
g_utf8_strlen (const gchar *str, gssize max_len)
{
	const guchar *inptr = (const guchar *) str;
	glong clen = 0, len = 0, n;

	if (max_len == 0)
		return 0;

	if (max_len < 0) {
		while (*inptr) {
			inptr += g_utf8_jump_table[*inptr];
			len++;
		}
	} else {
		while (len < max_len && *inptr) {
			n = g_utf8_jump_table[*inptr];
			if ((clen + n) > max_len)
				break;

			inptr += n;
			clen += n;
			len++;
		}
	}

	return len;
}

gunichar
g_utf8_get_char (const gchar *src)
{
	unsigned char *inptr = (unsigned char *) src;
	gunichar u = *inptr;
	int n, i;

	if (u < 0x80) {
		/* simple ascii case */
		return u;
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
	} else {
		u &= 0x01;
		n = 6;
	}

	for (i = 1; i < n; i++)
		u = (u << 6) | (*++inptr ^ 0x80);

	return u;
}

gchar *
g_utf8_offset_to_pointer (const gchar *str, glong offset)
{
	const gchar *p = str;

	if (offset > 0) {
		do {
			p = g_utf8_next_char (p);
			offset --;
		} while (offset > 0);
	}
	else if (offset < 0) {
		const gchar *jump = str;
		do {
			// since the minimum size of a character is 1
			// we know we can step back at least offset bytes
			jump = jump + offset;

			// if we land in the middle of a character
			// walk to the beginning
			while ((*jump & 0xc0) == 0x80)
				jump --;

			// count how many characters we've actually walked
			// by going forward
			p = jump;
			do {
				p = g_utf8_next_char (p);
				offset ++;
			} while (p < jump);

		} while (offset < 0);
	}

	return (gchar *)p;
}

glong
g_utf8_pointer_to_offset (const gchar *str, const gchar *pos)
{
	const gchar *inptr, *inend;
	glong offset = 0;
	glong sign = 1;

	if (pos == str)
		return 0;

	if (str < pos) {
		inptr = str;
		inend = pos;
	} else {
		inptr = pos;
		inend = str;
		sign = -1;
	}

	do {
		inptr = g_utf8_next_char (inptr);
		offset++;
	} while (inptr < inend);

	return offset * sign;
}

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
	size_t n;

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
	for (size_t i = 1; i < n; i++)
		u = (u << 6) | (*++inptr ^ 0x80);
#endif

	*outchar = u;

	return GSIZE_TO_INT(n);
}

static int
encode_utf8 (gunichar c, char *outbuf, size_t outleft)
{
	unsigned char *outptr = (unsigned char *) outbuf;
	int base;
	size_t n;

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
	for (size_t i = n - 1; i > 0; i--) {
		outptr[i] = (c & 0x3f) | 0x80;
		c >>= 6;
	}

	outptr[0] = GUNICHAR_TO_UINT8 (c | base);
#endif

	return GSIZE_TO_INT(n);
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

static gpointer g_error_quark = (gpointer)"ConvertError";

gpointer
g_convert_error_quark (void)
{
	return g_error_quark;
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
g_unichar_to_utf16_endian (gunichar c, gunichar2 *outbuf, unsigned endian)
{
	gunichar c2;

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
			c2 = c - 0x10000;

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

	outptr = outbuf = (gunichar *)g_malloc ((n + 1) * sizeof (gunichar));
	inptr = (char *) str;

	for (i = 0; i < n; i++) {
		*outptr++ = g_utf8_get_char (inptr);
		inptr = g_utf8_next_char (inptr);
	}

	*outptr = 0;

	return outbuf;
}

static gunichar2 *
eg_utf8_to_utf16_general (const gchar *str, glong len, glong *items_read, glong *items_written, gboolean include_nuls, gboolean replace_invalid_codepoints, gboolean null_terminate, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err, unsigned endian)
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

		if ((u = g_unichar_to_utf16_endian (c, NULL, endian)) < 0) {
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
		outptr = outbuf = (gunichar2 *)g_malloc ((outlen + 1) * sizeof (gunichar2));
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

		u = g_unichar_to_utf16_endian (c, outptr, endian);
		if ((u < 0) && replace_invalid_codepoints) {
			outptr[0] = 0xFFFD;
			outptr[1] = 0xFFFD;
			u = 2;
		}

		outptr += u;
		inleft -= n;
		inptr += n;
	}

	if (null_terminate)
		*outptr = '\0';

	return outbuf;

error:
	if (errno == ENOMEM) {
		g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_NO_MEMORY,
			     "Allocation failed.");
	} else if (errno == EILSEQ) {
		g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
			     "Illegal byte sequence encountered in the input.");
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
	return eg_utf8_to_utf16_general (str, len, items_read, items_written, FALSE, FALSE, TRUE, NULL, NULL, err, G_BYTE_ORDER);
}

gunichar2 *
g_utf8_to_utf16be (const gchar *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	return eg_utf8_to_utf16_general (str, len, items_read, items_written, FALSE, FALSE, TRUE, NULL, NULL, err, G_BIG_ENDIAN);
}

gunichar2 *
g_utf8_to_utf16le (const gchar *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	return eg_utf8_to_utf16_general (str, len, items_read, items_written, FALSE, FALSE, TRUE, NULL, NULL, err, G_LITTLE_ENDIAN);
}

gunichar2 *
g_utf8_to_utf16_custom_alloc (const gchar *str, glong len, glong *items_read, glong *items_written, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err)
{
	return eg_utf8_to_utf16_general (str, len, items_read, items_written, FALSE, FALSE, TRUE, custom_alloc_func, custom_alloc_data, err, G_BYTE_ORDER);
}

gunichar2 *
g_utf8_to_utf16_custom_alloc_optional (const gchar *str, glong len, glong *items_read, glong *items_written, gboolean include_nuls, gboolean replace_invalid_codepoints, gboolean null_terminate, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err)
{
	return eg_utf8_to_utf16_general (str, len, items_read, items_written, include_nuls, replace_invalid_codepoints, null_terminate, custom_alloc_func, custom_alloc_data, err, G_BYTE_ORDER);
}

gunichar2 *
g_utf8_to_utf16be_custom_alloc (const gchar *str, glong len, glong *items_read, glong *items_written, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err)
{
	return eg_utf8_to_utf16_general (str, len, items_read, items_written, FALSE, FALSE, TRUE, custom_alloc_func, custom_alloc_data, err, G_BIG_ENDIAN);
}

gunichar2 *
g_utf8_to_utf16le_custom_alloc (const gchar *str, glong len, glong *items_read, glong *items_written, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err)
{
	return eg_utf8_to_utf16_general (str, len, items_read, items_written, FALSE, FALSE, TRUE, custom_alloc_func, custom_alloc_data, err, G_LITTLE_ENDIAN);
}

gunichar2 *
eg_utf8_to_utf16_with_nuls (const gchar *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	return eg_utf8_to_utf16_general (str, len, items_read, items_written, TRUE, FALSE, TRUE, NULL, NULL, err, G_BYTE_ORDER);
}

gunichar2 *
eg_wtf8_to_utf16 (const gchar *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	return eg_utf8_to_utf16_general (str, len, items_read, items_written, TRUE, TRUE, TRUE, NULL, NULL, err, G_BYTE_ORDER);
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
					     "Illegal byte sequence encountered in the input.");
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

	outptr = outbuf = (gunichar *)g_malloc (outlen + 4);
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
eg_utf16_to_utf8_general (const gunichar2 *str, glong len, glong *items_read, glong *items_written, gboolean include_nuls, gboolean replace_invalid_codepoints, gboolean null_terminate, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err, unsigned endian)
{
	char *inptr, *outbuf, *outptr;
	size_t outlen = 0;
	size_t inleft;
	gunichar c;
	gboolean replaced = FALSE;
	int n;

	g_return_val_if_fail (str != NULL, NULL);

	if (len < 0) {
		if (include_nuls) {
			g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_FAILED, "Conversions with embedded nulls must pass the string length");
			return NULL;
		}

		len = 0;
		while (str[len])
			len++;
	}

	inptr = (char *) str;
	inleft = len * 2;

	while (inleft > 0) {
		if ((n = decode_utf16_endian (inptr, inleft, &c, endian)) < 0) {
			if (n == -2 && inleft > 2) {
				/* This means that the first UTF-16 char was read, but second failed */
				inleft -= 2;
				inptr += 2;
			}

			if (errno == EILSEQ && !replace_invalid_codepoints) {
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
					     "Illegal byte sequence encountered in the input.");
			} else if (items_read && !replace_invalid_codepoints) {
				/* partial input is ok if we can let our caller know... */
				break;
			} else if (!replace_invalid_codepoints) {
				g_set_error (err, G_CONVERT_ERROR, G_CONVERT_ERROR_PARTIAL_INPUT,
					     "Partial byte sequence encountered in the input.");
			}

			if (replace_invalid_codepoints) {
				n = sizeof(gunichar);
				c = '?';
				replaced = TRUE;
			} else {
				if (items_read)
					*items_read = GPTRDIFF_TO_LONG ((inptr - (char *) str) / 2);

				if (items_written)
					*items_written = 0;

				return NULL;
			}
		} else if (c == 0 && !include_nuls)
			break;

		outlen += (replaced && replace_invalid_codepoints) ? n - 1 : g_unichar_to_utf8 (c, NULL);
		inleft -= n;
		inptr += n;
		replaced = FALSE;
	}

	if (items_read)
		*items_read = GPTRDIFF_TO_LONG ((inptr - (char *) str) / 2);

	if (items_written)
		*items_written = (glong)outlen;

	if (G_LIKELY (!custom_alloc_func))
		outptr = outbuf = (char *)g_malloc (outlen + 1);
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
		if ((n = decode_utf16_endian (inptr, inleft, &c, endian)) < 0) {
			if (replace_invalid_codepoints) {
				outptr += '?';
				n = sizeof(gunichar);
			} else
				break;
		} else if (c == 0 && !include_nuls) {
			break;
		} else {
			outptr += g_unichar_to_utf8 (c, outptr);
		}

		inleft -= n;
		inptr += n;
	}

	if (null_terminate)
		*outptr = '\0';

	return outbuf;
}

gchar *
g_utf16_to_utf8 (const gunichar2 *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	return eg_utf16_to_utf8_general (str, len, items_read, items_written, FALSE, FALSE, TRUE, NULL, NULL, err, G_BYTE_ORDER);
}

gchar *
g_utf16le_to_utf8 (const gunichar2 *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	return eg_utf16_to_utf8_general (str, len, items_read, items_written, FALSE, FALSE, TRUE, NULL, NULL, err, G_LITTLE_ENDIAN);
}

gchar *
g_utf16be_to_utf8 (const gunichar2 *str, glong len, glong *items_read, glong *items_written, GError **err)
{
	return eg_utf16_to_utf8_general (str, len, items_read, items_written, FALSE, FALSE, TRUE, NULL, NULL, err, G_BIG_ENDIAN);
}

gchar *
g_utf16_to_utf8_custom_alloc (const gunichar2 *str, glong len, glong *items_read, glong *items_written, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err)
{
	return eg_utf16_to_utf8_general (str, len, items_read, items_written, FALSE, FALSE, TRUE, custom_alloc_func, custom_alloc_data, err, G_BYTE_ORDER);
}

gchar *
g_utf16_to_utf8_custom_alloc_with_nulls (const gunichar2 *str, glong len, glong *items_read, glong *items_written, gboolean include_nuls, gboolean null_terminate, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err)
{
	return eg_utf16_to_utf8_general (str, len, items_read, items_written, include_nuls, TRUE, null_terminate, custom_alloc_func, custom_alloc_data, err, G_BYTE_ORDER);
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

	outptr = outbuf = (gunichar *)g_malloc (outlen + 4);
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
			if ((n = g_unichar_to_utf8 (str[i], NULL)) < 0) {
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

	outptr = outbuf = (char *)g_malloc (outlen + 1);
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

	outptr = outbuf = (gunichar2 *)g_malloc ((outlen + 1) * sizeof (gunichar2));
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
