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
#include <stdio.h>
#include <glib.h>
#include <langinfo.h>
#include <iconv.h>
#include <errno.h>

static char *my_charset;
static gboolean is_utf8;

GUnicodeType 
g_unichar_type (gunichar c)
{
	g_error ("g_unichar_type is not implemented");
	return 0;
}

gunichar
g_unichar_tolower (gunichar c)
{
	g_error ("g_unichar_type is not implemented");
	return 0;
}

gchar *
g_convert (const gchar *str, gssize len,
	   const gchar *to_codeset, const gchar *from_codeset,
	   gsize *bytes_read, gsize *bytes_written, GError **error)
{
	iconv_t convertor;
	char *buffer, *result, *output;
	const char *strptr = (const char *) str;
	int str_len = len == -1 ? strlen (str) : len;
	int buffer_size;
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
				int extra_space = 8 + left;
				int output_used = output - buffer;
				
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
	g_strlcpy (res, utf8string, len);
	return res;
}

gboolean
g_get_charset (G_CONST_RETURN char **charset)
{
	if (my_charset == NULL){
		my_charset = g_strdup (nl_langinfo (CODESET));
		is_utf8 = strcmp (my_charset, "UTF-8") == 0;
	}
	
	if (charset != NULL)
		*charset = my_charset;

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
g_utf8_validate (const gchar *utf, gssize max_len, const gchar **end)
{
	int ix;
	
	g_return_val_if_fail (utf != NULL, FALSE);

	if (max_len == -1)
		max_len = strlen (utf);
	
	/*
	 * utf is a string of 1, 2, 3 or 4 bytes.  The valid strings
	 * are as follows (in "bit format"):
	 *    0xxxxxxx                                      valid 1-byte
	 *    110xxxxx 10xxxxxx                             valid 2-byte
	 *    1110xxxx 10xxxxxx 10xxxxxx                    valid 3-byte
	 *    11110xxx 10xxxxxx 10xxxxxx 10xxxxxx           valid 4-byte
	 */
	for (ix = 0; ix < max_len;) {      /* string is 0-terminated */
		unsigned char c;
		
		c = utf[ix];
		if ((c & 0x80) == 0x00) {	/* 1-byte code, starts with 10 */
			ix++;
		} else if ((c & 0xe0) == 0xc0) {/* 2-byte code, starts with 110 */
			if (((ix+1) >= max_len) || (utf[ix+1] & 0xc0 ) != 0x80){
				if (end != NULL)
					*end = &utf [ix];
				return FALSE;
			}
			ix += 2;
		} else if ((c & 0xf0) == 0xe0) {/* 3-byte code, starts with 1110 */
			if (((ix + 2) >= max_len) || 
			    ((utf[ix+1] & 0xc0) != 0x80) ||
			    ((utf[ix+2] & 0xc0) != 0x80)){
				if (end != NULL)
					*end = &utf [ix];
				return FALSE;
			}
			ix += 3;
		} else if ((c & 0xf8) == 0xf0) {/* 4-byte code, starts with 11110 */
			if (((ix + 3) >= max_len) ||
			    ((utf[ix+1] & 0xc0) != 0x80) ||
			    ((utf[ix+2] & 0xc0) != 0x80) ||
			    ((utf[ix+3] & 0xc0) != 0x80)){
				if (end != NULL)
					*end = &utf [ix];
				return FALSE;
			}
			ix += 4;
		} else {/* unknown encoding */
			if (end != NULL)
				*end = &utf [ix];
			return FALSE;
		}
	}
	
	return TRUE;
}
