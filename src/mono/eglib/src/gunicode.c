/*
 * gunicode.c: Some Unicode routines 
 *
 * Author:
 *   Miguel de Icaza (miguel@novell.com)
 *
 * (C) 2006 Novell, Inc.
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
	int str_len = len == -1 ? strlen (str) : len;
	int buffer_size;
	size_t left, out_left;
	
	convertor = iconv_open (to_codeset, from_codeset);
	if (convertor == (iconv_t) -1){
		*bytes_written = 0;
		*bytes_read = 0;
		return NULL;
	}

	buffer_size = out_left = str_len + 1;
	buffer = g_malloc (out_left);
	output = buffer;
	left = str_len;
	while (left > 0){
		int res = iconv (convertor, (char **) &str, &left, &output, &out_left);
		if (res == (size_t) -1){
			int out_size = buffer_size - out_left;
			
			if (errno == E2BIG){
				char *n;

				buffer_size += left + 2;
				n = g_realloc (buffer, buffer_size);
				
				if (n == NULL){
					if (error != NULL)
						*error = g_error_new (NULL, G_CONVERT_ERROR_FAILED, "No memory left");
					g_free (buffer);
					result = NULL;
					goto leave;
				}
				buffer = n;
				output = buffer + out_size;
				out_left = buffer_size - out_size;
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
	strcpy (res, utf8string);
	return res;
}

gboolean
g_get_charset (char **charset)
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

