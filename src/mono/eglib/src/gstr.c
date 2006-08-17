/*
 * gstr.c: String Utility Functions.
 *
 * Author:
 *   Miguel de Icaza (miguel@novell.com)
 *   Aaron Bockover (abockover@novell.com)
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
#define _GNU_SOURCE
#include <stdio.h>
#include <string.h>
#include <glib.h>

/* This is not a macro, because I dont want to put _GNU_SOURCE in the glib.h header */
gchar *
g_strndup (const gchar *str, gsize n)
{
	return strndup (str, n);
}

void
g_strfreev (gchar **str_array)
{
	gchar **orig = str_array;
	if (str_array == NULL)
		return;
	while (*str_array != NULL){
		g_free (*str_array);
		str_array++;
	}
	g_free (orig);
}

gchar *
g_strdup_vprintf (const gchar *format, va_list args)
{
	int n;
	char *ret;
	
	n = vasprintf (&ret, format, args);
	if (n == -1)
		return NULL;

	return ret;
}

gchar *
g_strdup_printf (const gchar *format, ...)
{
	gchar *ret;
	va_list args;
	int n;

	va_start (args, format);
	n = vasprintf (&ret, format, args);
	va_end (args);
	if (n == -1)
		return NULL;

	return ret;
}

const gchar *
g_strerror (gint errnum)
{
	return strerror (errnum);
}

gchar *
g_strconcat (const gchar *first, ...)
{
	g_return_val_if_fail (first != NULL, NULL);
	va_list args;
	int total = 0;
	char *s, *ret;

	total += strlen (first);
	va_start (args, first);
	for (s = va_arg (args, char *); s != NULL; s = va_arg(args, char *)){
		total += strlen (s);
	}
	va_end (args);
	
	ret = g_malloc (total + 1);
	if (ret == NULL)
		return NULL;

	ret [total] = 0;
	strcpy (ret, first);
	va_start (args, first);
	for (s = va_arg (args, char *); s != NULL; s = va_arg(args, char *)){
		strcat (ret, s);
	}
	va_end (args);

	return ret;
}

gchar ** 
g_strsplit (const gchar *string, const gchar *delimiter, gint max_tokens)
{
	gchar *string_c;
	gchar *strtok_save, **vector;
	gchar *token, *token_c;
	gint size = 1;
	gint token_length;

	g_return_val_if_fail(string != NULL, NULL);
	g_return_val_if_fail(delimiter != NULL, NULL);
	g_return_val_if_fail(delimiter[0] != '\0', NULL);
	
	token_length = strlen(string);
	string_c = (gchar *)g_malloc(token_length + 1);
	strncpy(string_c, string, token_length);
	string_c[token_length] = '\0';
	
	vector = NULL;
	token = (gchar *)strtok_r(string_c, delimiter, &strtok_save);
	
	while(token != NULL) {
		token_length = strlen(token);
		token_c = (gchar *)g_malloc(token_length + 1);
		strncpy(token_c, token, token_length);
		token_c[token_length] = '\0';

		vector = vector == NULL ? 
			(gchar **)g_malloc(sizeof(vector)) :
			(gchar **)g_realloc(vector, (size + 1) * sizeof(vector));
	
		vector[size - 1] = token_c;	
		size++;

		if(max_tokens > 0 && size >= max_tokens) {
			if(size > max_tokens) {
				break;
			}

			token = strtok_save;
		} else {
			token = (gchar *)strtok_r(NULL, delimiter, &strtok_save);
		}
	}

	vector[size - 1] = NULL;
	g_free(string_c);
	string_c = NULL;

	return vector;
}

gchar *
g_strreverse (gchar *str)
{
	guint len, half;
	gint i;
	gchar c;

	if (str == NULL)
		return NULL;

	len = strlen (str);
	half = len / 2;
	len--;
	for (i = 0; i < half; i++, len--) {
		c = str [i];
		str [i] = str [len];
		str [len] = c;
	}
	return str;
}

