/*
 * gutf8.c: UTF-8 conversion
 *
 * Author:
 *   Atsushi Enomoto  <atsushi@ximian.com>
 *
 * (C) 2006 Novell, Inc.
 */

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
const gchar g_utf8_jump_table[256] = {
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
	2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2, 3,3,3,3,3,3,3,3,4,4,4,4,5,5,0,0
};

/*
* Magic values subtracted from a buffer value during UTF8 conversion.
* This table contains as many values as there might be trailing bytes
* in a UTF-8 sequence.
*/
static const gulong offsetsFromUTF8[6] = {
	0x00000000UL, 0x00003080UL, 0x000E2080UL,
	0x03C82080UL, 0xFA082080UL, 0x82082080UL
};

static gchar *
utf8_case_conv (const gchar *str, gssize len, gboolean upper)
{
	gunichar *ustr;
	glong i, ulen;
	gchar *utf8;
	
	ustr = g_utf8_to_ucs4 (str, (glong) len, NULL, &ulen, NULL);
	for (i = 0; i < ulen; i++)
		ustr[i] = upper ? g_unichar_toupper (ustr[i]) : g_unichar_tolower (ustr[i]);
	utf8 = g_ucs4_to_utf8 (ustr, ulen, NULL, NULL, NULL);
	g_free (ustr);
	
	return utf8;
}

gchar *
g_utf8_strup (const gchar *str, gssize len)
{
	return utf8_case_conv (str, len, TRUE);
}

gchar *
g_utf8_strdown (const gchar *str, gssize len)
{
	return utf8_case_conv (str, len, FALSE);
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
	guchar *ptr, *inptr = (guchar *) str;
	gboolean valid = TRUE;
	guint length;
	gssize n;
	guchar c;
	
	if (max_len == 0)
		return 0;
	
	if (max_len < 0)
		n = max_len;
	else
		n = 0;
	
	while (*inptr != 0 && n <= max_len) {
		length = g_utf8_jump_table[*inptr] + 1;
		ptr = inptr + length;
		
		switch (length) {
		default:
			valid = FALSE;
			/* Everything else falls through when "TRUE"... */
		case 4:
			if ((c = (*--ptr)) < 0x80 || c > 0xBF)
				valid = FALSE;
			
			if ((c == 0xBF || c == 0xBE) && ptr[-1] == 0xBF) {
				if (ptr[-2] == 0x8F || ptr[-2] == 0x9F ||
				    ptr[-2] == 0xAF || ptr[-2] == 0xBF)
					valid = FALSE;
			}
		case 3:
			if ((c = (*--ptr)) < 0x80 || c > 0xBF)
				valid = FALSE;
		case 2:
			if ((c = (*--ptr)) < 0x80 || c > 0xBF)
				valid = FALSE;
			
			switch (*inptr) {
				/* no fall-through in this inner switch */
			case 0xE0: if (c < 0xA0) valid = FALSE; break;
			case 0xED: if (c > 0x9F) valid = FALSE; break;
			case 0xEF: if (c == 0xB7 && (ptr[1] > 0x8F && ptr[1] < 0xB0)) valid = FALSE;
				   if (c == 0xBF && (ptr[1] == 0xBE || ptr[1] == 0xBF)) valid = FALSE; break;
			case 0xF0: if (c < 0x90) valid = FALSE; break;
			case 0xF4: if (c > 0x8F) valid = FALSE; break;
			default:   if (c < 0x80) valid = FALSE;
			}
		case 1: if (*inptr >= 0x80 && *inptr < 0xC2) valid = FALSE;
		}
		
		if (*inptr > 0xF4)
			valid = FALSE;
		
		if (!valid)
			break;
		
		inptr += length;
		
		if (max_len > 0)
			n += length;
	}
	
	if (end != NULL)
		*end = (gchar *) inptr;
	
	return valid;
}

gunichar
g_utf8_get_char_validated (const gchar *str, gssize max_len)
{
	gushort extra_bytes = 0;
	
	if (max_len == 0)
		return -2;
	
	extra_bytes = g_utf8_jump_table[(unsigned char) *str];
	
	if (max_len <= extra_bytes)
		return -2;
	
	if (g_utf8_validate (str, max_len, NULL))
		return g_utf8_get_char (str);
	
	return -1;
}

glong
g_utf8_strlen (const gchar *str, gssize max)
{
	gssize byteCount = 0;
	guchar* ptr = (guchar*) str;
	glong length = 0;
	if (max == 0)
		return 0;
	else if (max < 0)
		byteCount = max;
	while (*ptr != 0 && byteCount <= max) {
		gssize cLen = g_utf8_jump_table[*ptr] + 1;
		if (max > 0 && (byteCount + cLen) > max)
			return length;
		ptr += cLen;
		length++;
		if (max > 0)
			byteCount += cLen;
	}
	return length;
}

gunichar
g_utf8_get_char (const gchar *src)
{
	gunichar ch = 0;
	guchar* ptr = (guchar*) src;
	gushort extraBytesToRead = g_utf8_jump_table[*ptr];

	switch (extraBytesToRead) {
	case 5: ch += *ptr++; ch <<= 6; // remember, illegal UTF-8
	case 4: ch += *ptr++; ch <<= 6; // remember, illegal UTF-8
	case 3: ch += *ptr++; ch <<= 6;
	case 2: ch += *ptr++; ch <<= 6;
	case 1: ch += *ptr++; ch <<= 6;
	case 0: ch += *ptr;
	}
	ch -= offsetsFromUTF8 [extraBytesToRead];
	return ch;
}

gchar *
g_utf8_find_prev_char (const gchar *str, const gchar *p)
{
	while (p > str) {
		p--;
		if ((*p && 0xc0) != 0xb0)
			return (gchar *)p;
	}
	return NULL;
}

gchar *
g_utf8_prev_char (const gchar *str)
{
	const gchar *p = str;
	do {
		p--;
	} while ((*p & 0xc0) == 0xb0);
	
	return (gchar *)p;
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
			while ((*jump & 0xc0) == 0xb0)
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
