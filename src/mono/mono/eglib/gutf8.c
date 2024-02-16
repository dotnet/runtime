/*
 * gutf8.c: UTF-8 conversion
 *
 * Author:
 *   Atsushi Enomoto  <atsushi@ximian.com>
 *
 * (C) 2006 Novell, Inc.
 * Copyright 2012 Xamarin Inc
 */
#include "config.h"
#include <stdio.h>
#include <glib.h>

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
