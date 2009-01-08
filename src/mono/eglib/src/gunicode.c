/*
 * gunicode.c: Some Unicode routines 
 *
 * Author:
 *   Miguel de Icaza (miguel@novell.com)
 *
 * (C) 2006 Novell, Inc.
 *
 * utf8 validation code came from:
 * 	libxml2-2.6.26 licensed under the MIT X11 license
 *
 * Authors credit in libxml's string.c:
 *   William Brack <wbrack@mmm.com.hk>
 *   daniel@veillard.com
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *
 */
#include <config.h>
#include <stdio.h>
#include <glib.h>
#include <unicode-data.h>
#include <errno.h>
#ifdef _MSC_VER
/* FIXME */
#define CODESET 1
#include <Windows.h>

typedef int iconv_t;
#else
#ifdef HAVE_LANGINFO_H
#include <langinfo.h>
#endif
#ifdef HAVE_ICONV_H
#include <iconv.h>
#endif
#endif

static char *my_charset;
static gboolean is_utf8;

/*
 * Character set conversion
 */
/*
* Index into the table below with the first byte of a UTF-8 sequence to
* get the number of trailing bytes that are supposed to follow it.
* Note that *legal* UTF-8 values can't have 4 or 5-bytes. The table is
* left as-is for anyone who may want to do such conversion, which was
* allowed in earlier algorithms.
*/
const gchar g_trailingBytesForUTF8 [256] = {
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
static const gulong offsetsFromUTF8[6] = { 0x00000000UL, 0x00003080UL, 0x000E2080UL,
0x03C82080UL, 0xFA082080UL, 0x82082080UL };

GUnicodeType 
g_unichar_type (gunichar c)
{
int i;

	guint16 cp = (guint16) c;
	for (i = 0; i < unicode_category_ranges_count; i++) {
		if (cp < unicode_category_ranges [i].start)
			continue;
		if (unicode_category_ranges [i].end <= cp)
			continue;
		return unicode_category [i] [cp - unicode_category_ranges [i].start];
	}

	/*
	// 3400-4DB5: OtherLetter
	// 4E00-9FC3: OtherLetter
	// AC00-D7A3: OtherLetter
	// D800-DFFF: OtherSurrogate
	// E000-F8FF: OtherPrivateUse
	// 20000-2A6D6 OtherLetter
	// F0000-FFFFD OtherPrivateUse
	// 100000-10FFFD OtherPrivateUse
	*/
	if (0x3400 <= cp && cp < 0x4DB5)
		return G_UNICODE_OTHER_LETTER;
	if (0x4E00 <= cp && cp < 0x9FC3)
		return G_UNICODE_OTHER_LETTER;
	if (0xAC00<= cp && cp < 0xD7A3)
		return G_UNICODE_OTHER_LETTER;
	if (0xD800 <= cp && cp < 0xDFFF)
		return G_UNICODE_SURROGATE;
	if (0xE000 <= cp && cp < 0xF8FF)
		return G_UNICODE_PRIVATE_USE;
	/* since the argument is UTF-16, we cannot check beyond FFFF */

	/* It should match any of above */
	return 0;
}

gunichar
g_unichar_case (gunichar c, gboolean upper)
{
	gint8 i, i2;
	guint32 cp = (guint32) c, v;

	for (i = 0; i < simple_case_map_ranges_count; i++) {
		if (cp < simple_case_map_ranges [i].start)
			return c;
		if (simple_case_map_ranges [i].end <= cp)
			continue;
		if (c < 0x10000) {
			const guint16 *tab = upper ? simple_upper_case_mapping_lowarea [i] : simple_lower_case_mapping_lowarea [i];
			v = tab [cp - simple_case_map_ranges [i].start];
		} else {
			const guint32 *tab;
			i2 = (gint8)(i - (upper ? simple_upper_case_mapping_lowarea_table_count : simple_lower_case_mapping_lowarea_table_count));
			tab = upper ? simple_upper_case_mapping_higharea [i2] : simple_lower_case_mapping_higharea [i2];
			v = tab [cp - simple_case_map_ranges [i].start];
		}
		return v != 0 ? (gunichar) v : c;
	}
	return c;
}

gunichar
g_unichar_toupper (gunichar c)
{
	return g_unichar_case (c, TRUE);
}

gunichar
g_unichar_tolower (gunichar c)
{
	return g_unichar_case (c, FALSE);
}

gunichar
g_unichar_totitle (gunichar c)
{
	guint8 i;
	guint32 cp;

	cp = (guint32) c;
	for (i = 0; i < simple_titlecase_mapping_count; i++) {
		if (simple_titlecase_mapping [i].codepoint == cp)
			return simple_titlecase_mapping [i].title;
		if (simple_titlecase_mapping [i].codepoint > cp)
			/* it is ordered, hence no more match */
			break;
	}
	return g_unichar_toupper (c);
}

gboolean
g_unichar_isxdigit (gunichar c)
{
	return (g_unichar_xdigit_value (c) != -1);

}

gint
g_unichar_xdigit_value (gunichar c)
{
	if (c >= 0x30 && c <= 0x39) /*0-9*/
		return (c - 0x30);
	if (c >= 0x41 && c <= 0x46) /*A-F*/
		return (c - 0x37);
	if (c >= 0x61 && c <= 0x66) /*a-f*/
		return (c - 0x57);
	return -1;
}

gchar *
g_convert (const gchar *str, gssize len,
	   const gchar *to_codeset, const gchar *from_codeset,
	   gsize *bytes_read, gsize *bytes_written, GError **error)
{
	char *result = NULL;
#ifdef G_OS_WIN32
#elif HAVE_ICONV_H
	iconv_t convertor;
	char *buffer, *output;
	const char *strptr = (const char *) str;
	size_t str_len = len == -1 ? strlen (str) : len;
	size_t buffer_size;
	size_t left, out_left;
	
	convertor = iconv_open (to_codeset, from_codeset);
	if (convertor == (iconv_t) -1){
		*bytes_written = 0;
		*bytes_read = 0;
		return NULL;
	}

	buffer_size = str_len + 1 + 8;
	buffer = g_malloc (buffer_size);
	out_left = str_len;
	output = buffer;
	left = str_len;
	while (left > 0){
		int res = iconv (convertor, (char **) &strptr, &left, &output, &out_left);
		if (res == (size_t) -1){
			if (errno == E2BIG){
				char *n;
				size_t extra_space = 8 + left;
				size_t output_used = output - buffer;
				
				buffer_size += extra_space;
				
				n = g_realloc (buffer, buffer_size);
				
				if (n == NULL){
					if (error != NULL)
						*error = g_error_new (NULL, G_CONVERT_ERROR_FAILED, "No memory left");
					g_free (buffer);
					result = NULL;
					goto leave;
				}
				buffer = n;
				out_left += extra_space;
				output = buffer + output_used;
			} else if (errno == EILSEQ){
				if (error != NULL)
					*error = g_error_new (NULL, G_CONVERT_ERROR_ILLEGAL_SEQUENCE, "Invalid multi-byte sequence on input");
				result = NULL;
				g_free (buffer);
				goto leave;
			} else if (errno == EINVAL){
				if (error != NULL)
					*error = g_error_new (NULL, G_CONVERT_ERROR_PARTIAL_INPUT, "Partial character sequence");
				result = NULL;
				g_free (buffer);
				goto leave;
			}
		} 
	}
	if (bytes_read != NULL)
		*bytes_read = strptr - str;
	if (bytes_written != NULL)
		*bytes_written = output - buffer;
	*output = 0;
	result = buffer;
 leave:
	iconv_close (convertor);
#endif
	return result;
}

/*
 * This is broken, and assumes an UTF8 system, but will do for eglib's first user
 */
gchar *
g_filename_from_utf8 (const gchar *utf8string, gssize len, gsize *bytes_read, gsize *bytes_written, GError **error)
{
	char *res;
	
	if (len == -1)
		len = strlen (utf8string);

	res = g_malloc (len + 1);
	g_strlcpy (res, utf8string, len + 1);
	return res;
}

gboolean
g_get_charset (G_CONST_RETURN char **charset)
{
#ifdef G_OS_WIN32
	static char buf[14];
	sprintf (buf, "CP%u", GetACP ());
	*charset = buf;
	is_utf8 = FALSE;
#else
	if (my_charset == NULL){
#if HAVE_LANGINFO_H
		my_charset = g_strdup (nl_langinfo (CODESET));
#else
		my_charset = g_strdup ("UTF-8");
#endif
		is_utf8 = strcmp (my_charset, "UTF-8") == 0;
	}
	
	if (charset != NULL)
		*charset = my_charset;

#endif
	return is_utf8;
}

gchar *
g_locale_to_utf8 (const gchar *opsysstring, gssize len, gsize *bytes_read, gsize *bytes_written, GError **error)
{
	g_get_charset (NULL);

	return g_convert (opsysstring, len, "UTF-8", my_charset, bytes_read, bytes_written, error);
}

gchar *
g_locale_from_utf8 (const gchar *utf8string, gssize len, gsize *bytes_read, gsize *bytes_written, GError **error)
{
	g_get_charset (NULL);

	return g_convert (utf8string, len, my_charset, "UTF-8", bytes_read, bytes_written, error);
}
/**
 * g_utf8_validate
 * @utf: Pointer to putative UTF-8 encoded string.
 *
 * Checks @utf for being valid UTF-8. @utf is assumed to be
 * null-terminated. This function is not super-strict, as it will
 * allow longer UTF-8 sequences than necessary. Note that Java is
 * capable of producing these sequences if provoked. Also note, this
 * routine checks for the 4-byte maximum size, but does not check for
 * 0x10ffff maximum value.
 *
 * Return value: true if @utf is valid.
 **/
gboolean
g_utf8_validate (const gchar *str, gssize max_len, const gchar **end)
{
	gssize byteCount = 0;
	gboolean retVal = TRUE;
	gboolean lastRet = TRUE;
	guchar* ptr = (guchar*) str;
	guint length;
	guchar a;
	guchar* srcPtr;
	if (max_len == 0)
		return 0;
	else if (max_len < 0)
		byteCount = max_len;
	while (*ptr != 0 && byteCount <= max_len) {
		length = g_trailingBytesForUTF8 [*ptr] + 1;
		srcPtr = (guchar*) ptr + length;
		switch (length) {
		default: retVal = FALSE;
		/* Everything else falls through when "TRUE"... */
		case 4: if ((a = (*--srcPtr)) < (guchar) 0x80 || a > (guchar) 0xBF) retVal = FALSE;
				if ((a == (guchar) 0xBF || a == (guchar) 0xBE) && *(srcPtr-1) == (guchar) 0xBF) {
				if (*(srcPtr-2) == (guchar) 0x8F || *(srcPtr-2) == (guchar) 0x9F ||
					*(srcPtr-2) == (guchar) 0xAF || *(srcPtr-2) == (guchar) 0xBF)
					retVal = FALSE;
				}
		case 3: if ((a = (*--srcPtr)) < (guchar) 0x80 || a > (guchar) 0xBF) retVal = FALSE;
		case 2: if ((a = (*--srcPtr)) < (guchar) 0x80 || a > (guchar) 0xBF) retVal = FALSE;

		switch (*ptr) {
		/* no fall-through in this inner switch */
		case 0xE0: if (a < (guchar) 0xA0) retVal = FALSE; break;
		case 0xED: if (a > (guchar) 0x9F) retVal = FALSE; break;
		case 0xEF: if (a == (guchar)0xB7 && (*(srcPtr+1) > (guchar) 0x8F && *(srcPtr+1) < 0xB0)) retVal = FALSE;
				   if (a == (guchar)0xBF && (*(srcPtr+1) == (guchar) 0xBE || *(srcPtr+1) == 0xBF)) retVal = FALSE; break;
		case 0xF0: if (a < (guchar) 0x90) retVal = FALSE; break;
		case 0xF4: if (a > (guchar) 0x8F) retVal = FALSE; break;
		default:   if (a < (guchar) 0x80) retVal = FALSE;
		}

		case 1: if (*ptr >= (guchar ) 0x80 && *ptr < (guchar) 0xC2) retVal = FALSE;
		}
		if (*ptr > (guchar) 0xF4)
			retVal = FALSE;
		//If the string is invalid, set the end to the invalid byte.
		if (!retVal && lastRet) {
			if (end != NULL)
				*end = (gchar*) ptr;
			lastRet = FALSE;
		}
		ptr += length;
		if(max_len > 0)
			byteCount += length;
	}
	if (retVal && end != NULL)
		*end = (gchar*) ptr;
	return retVal;
}
/**
 * g_utf8_get_char
 * @src: Pointer to UTF-8 encoded character.
 *
 * Return value: UTF-16 value of @src
 **/
gunichar
g_utf8_get_char (const gchar *src)
{
	gunichar ch = 0;
	guchar* ptr = (guchar*) src;
	gushort extraBytesToRead = g_trailingBytesForUTF8 [*ptr];

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
		gssize cLen = g_trailingBytesForUTF8 [*ptr] + 1;
		if (max > 0 && (byteCount + cLen) > max)
			return length;
		ptr += cLen;
		length++;
		if (max > 0)
			byteCount += cLen;
	}
	return length;
}
